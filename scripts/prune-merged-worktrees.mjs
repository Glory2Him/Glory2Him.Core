#!/usr/bin/env node
// Lists the git worktrees whose branch has a merged pull request and that hold
// nothing worth keeping, and with --apply removes them. Each build worktree holds
// its own bin/, obj/ and node_modules, so finished ones fill the disk.
//
//   node scripts/prune-merged-worktrees.mjs            # report only
//   node scripts/prune-merged-worktrees.mjs --apply    # remove the ones marked "remove"
//
// A worktree is kept if it is the main checkout, detached, locked, has
// uncommitted or untracked files, has commits on no remote, has a process
// running inside it, or its branch has no merged PR. Removal uses
// `git worktree remove` without --force, so git refuses anything dirty.
// Branches are left alone.
//
// A running process is found through /proc on Linux and lsof on macOS. Windows has
// neither, so there the worktree is renamed and renamed straight back: Windows refuses
// to rename a folder that a process is working in or holds a file open in. Only a
// worktree that would otherwise be removed is checked.

import { execFileSync } from 'node:child_process';
import { existsSync, readdirSync, readlinkSync, renameSync } from 'node:fs';
import { sep } from 'node:path';
import { fileURLToPath } from 'node:url';

// Parses `git worktree list --porcelain`. The first entry is the main checkout.
export function parseWorktreeList(porcelain) {
  return porcelain
    .split(/\n\s*\n/)
    .map((block) => block.split('\n').filter(Boolean))
    .filter((lines) => lines.length > 0 && lines[0].startsWith('worktree '))
    .map((lines, i) => {
      const branchLine = lines.find((l) => l.startsWith('branch '));
      return {
        path: lines[0].slice('worktree '.length),
        branch: branchLine ? branchLine.slice('branch refs/heads/'.length) : null,
        locked: lines.some((l) => l === 'locked' || l.startsWith('locked ')),
        main: i === 0,
      };
    });
}

// status: { dirty, unpushed, busy }, where busy is null when it can't be checked.
export function decide(worktree, status, mergedBranches) {
  if (worktree.main) return { remove: false, reason: 'main checkout' };
  if (!worktree.branch) return { remove: false, reason: 'detached HEAD' };
  if (worktree.locked) return { remove: false, reason: 'locked' };
  if (!mergedBranches.has(worktree.branch)) return { remove: false, reason: 'no merged PR' };
  if (status.dirty) return { remove: false, reason: 'uncommitted or untracked files' };
  if (status.unpushed > 0) return { remove: false, reason: `${status.unpushed} commits on no remote` };
  if (status.busy === null) return { remove: false, reason: "can't check for running processes" };
  if (status.busy) return { remove: false, reason: 'a process is running in it' };
  return { remove: true, reason: 'PR merged' };
}

// The head branches of merged PRs, from `gh api … --jq '.[] | … | .head.ref'`: one per line.
export function mergedBranches(output) {
  return new Set(output.split('\n').map((line) => line.trim()).filter(Boolean));
}

const git = (cwd, ...args) => execFileSync('git', ['-C', cwd, ...args], { encoding: 'utf8' });

// Working directories of every process we can see: through /proc on Linux, lsof on
// macOS and other Unix systems, or null where neither works (Windows).
function processCwds() {
  if (existsSync('/proc')) {
    const cwds = [];
    for (const pid of readdirSync('/proc').filter((d) => /^\d+$/.test(d))) {
      try {
        cwds.push(readlinkSync(`/proc/${pid}/cwd`));
      } catch {
        // The process exited, or belongs to another user.
      }
    }
    return cwds;
  }
  if (process.platform === 'win32') return null;
  try {
    const out = execFileSync('lsof', ['-d', 'cwd', '-Fn'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] });
    return out.split('\n').filter((line) => line.startsWith('n')).map((line) => line.slice(1));
  } catch {
    return null;
  }
}

// Windows only: true when a process is working in the folder or holds a file open in it,
// false when it is free, null when it cannot tell. A rename that succeeds is undone at once.
function busyOnWindows(path) {
  const probe = `${path}.prune-check`;
  try {
    renameSync(path, probe);
  } catch (error) {
    return error.code === 'EBUSY' || error.code === 'EPERM' || error.code === 'EACCES' ? true : null;
  }
  renameSync(probe, path);
  return false;
}

function busyOf(path, cwds) {
  if (cwds !== null) return cwds.some((c) => c === path || c.startsWith(path + sep) || c.startsWith(path + '/'));
  return process.platform === 'win32' ? busyOnWindows(path) : null;
}

function statusOf(path, cwds, merged, branch) {
  const status = {
    dirty: git(path, 'status', '--porcelain').trim() !== '',
    unpushed: Number(git(path, 'rev-list', '--count', 'HEAD', '--not', '--remotes').trim()),
    busy: false,
  };
  // The busy check is the costly one, and on Windows it renames the folder, so it runs
  // only when nothing else keeps the worktree.
  if (merged.has(branch) && !status.dirty && status.unpushed === 0) status.busy = busyOf(path, cwds);
  return status;
}

function main() {
  const apply = process.argv.includes('--apply');
  const repo = git(process.cwd(), 'rev-parse', '--show-toplevel').trim();
  const merged = mergedBranches(
    // REST, not `gh pr list`: the same call works from any shell or session.
    execFileSync('gh', ['api', 'repos/{owner}/{repo}/pulls?state=closed&per_page=100', '--paginate',
      '--jq', '.[] | select(.merged_at != null) | .head.ref'], { encoding: 'utf8' }),
  );
  const cwds = processCwds();

  let removed = 0;
  for (const worktree of parseWorktreeList(git(repo, 'worktree', 'list', '--porcelain'))) {
    const { remove, reason } = worktree.main || !worktree.branch
      ? decide(worktree, {}, merged)
      : decide(worktree, statusOf(worktree.path, cwds, merged, worktree.branch), merged);
    const action = remove ? (apply ? 'removed' : 'remove') : 'keep';
    if (remove && apply) {
      git(repo, 'worktree', 'remove', worktree.path);
      removed += 1;
    }
    console.log(`${action.padEnd(8)} ${worktree.path}  (${worktree.branch ?? 'detached'}: ${reason})`);
  }
  if (!apply) console.log('\nReport only. Run with --apply to remove the worktrees marked "remove".');
  else console.log(`\nRemoved ${removed} worktree(s).`);
}

if (process.argv[1] === fileURLToPath(import.meta.url)) main();
