import assert from 'node:assert/strict';
import test from 'node:test';

import { decide, mergedBranches, parseWorktreeList } from './prune-merged-worktrees.mjs';

const PORCELAIN = [
  'worktree /repo',
  'HEAD aaa',
  'branch refs/heads/main',
  '',
  'worktree /repo/.claude/worktrees/one',
  'HEAD bbb',
  'branch refs/heads/feature/1-one',
  '',
  'worktree /repo/.claude/worktrees/detached',
  'HEAD ccc',
  'detached',
  '',
  'worktree /repo/.claude/worktrees/held',
  'HEAD ddd',
  'branch refs/heads/feature/2-held',
  'locked',
  '',
].join('\n');

test('parseWorktreeList reads path, branch and lock state, and marks the first entry as main', () => {
  assert.deepEqual(parseWorktreeList(PORCELAIN), [
    { path: '/repo', branch: 'main', locked: false, main: true },
    { path: '/repo/.claude/worktrees/one', branch: 'feature/1-one', locked: false, main: false },
    { path: '/repo/.claude/worktrees/detached', branch: null, locked: false, main: false },
    { path: '/repo/.claude/worktrees/held', branch: 'feature/2-held', locked: true, main: false },
  ]);
});

const merged = new Set(['feature/1-one', 'feature/2-held']);
const clean = { dirty: false, unpushed: 0, busy: false };
const wt = (over = {}) => ({ path: '/repo/.claude/worktrees/one', branch: 'feature/1-one', locked: false, main: false, ...over });

test('decide removes a clean, idle worktree whose branch has a merged PR', () => {
  assert.deepEqual(decide(wt(), clean, merged), { remove: true, reason: 'PR merged' });
});

test('decide keeps the main worktree, a detached one, a locked one and one with no merged PR', () => {
  assert.equal(decide(wt({ main: true }), clean, merged).remove, false);
  assert.equal(decide(wt({ branch: null }), clean, merged).remove, false);
  assert.equal(decide(wt({ locked: true }), clean, merged).remove, false);
  assert.equal(decide(wt({ branch: 'feature/9-open' }), clean, merged).remove, false);
});

test('decide keeps a worktree with local changes, unpushed commits or a running process', () => {
  assert.deepEqual(decide(wt(), { ...clean, dirty: true }, merged), { remove: false, reason: 'uncommitted or untracked files' });
  assert.deepEqual(decide(wt(), { ...clean, unpushed: 2 }, merged), { remove: false, reason: '2 commits on no remote' });
  assert.deepEqual(decide(wt(), { ...clean, busy: true }, merged), { remove: false, reason: 'a process is running in it' });
  assert.deepEqual(decide(wt(), { ...clean, busy: null }, merged), { remove: false, reason: "can't check for running processes" });
});

test('mergedBranches reads one branch per line from paginated gh output', () => {
  const set = mergedBranches('users/a/feature-1-x\nusers/a/brokers-x-insert\n\nusers/b/fix-y\n');
  assert.deepEqual([...set].sort(), ['users/a/brokers-x-insert', 'users/a/feature-1-x', 'users/b/fix-y']);
  assert.equal(mergedBranches('').size, 0);
});
