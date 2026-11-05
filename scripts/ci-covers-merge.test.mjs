import assert from 'node:assert/strict';
import { execFileSync, spawnSync } from 'node:child_process';
import { mkdtempSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import test from 'node:test';
import { fileURLToPath } from 'node:url';

const SCRIPT = join(dirname(fileURLToPath(import.meta.url)), 'ci-covers-merge.sh');

// A throwaway repo with a feature branch (named epic) and a task branch (named story) cut from it.
function repo() {
  const dir = mkdtempSync(join(tmpdir(), 'ci-covers-merge-'));
  const git = (...args) => execFileSync('git', ['-C', dir, ...args], { encoding: 'utf8' }).trim();
  git('init', '-q', '-b', 'epic');
  git('config', 'user.email', 'test@example.com');
  git('config', 'user.name', 'test');
  const commit = (file) => {
    writeFileSync(join(dir, file), `${file}\n`);
    git('add', file);
    git('commit', '-q', '-m', file);
    return git('rev-parse', 'HEAD');
  };
  // What a rebase merge leaves on the epic branch: the story's tree on a new commit.
  const rebaseMerge = (head, onto) => git('commit-tree', `${head}^{tree}`, '-p', onto, '-m', 'rebased');
  return { dir, git, commit, rebaseMerge };
}

const covers = (dir, tested, before, after) =>
  spawnSync('bash', [SCRIPT, dir, tested, before, after]).status;

test('covered when the story was cut from the epic tip and nothing moved it', () => {
  const { dir, git, commit, rebaseMerge } = repo();
  const before = commit('base');
  git('checkout', '-q', '-b', 'story');
  const head = commit('story');
  assert.equal(covers(dir, head, before, rebaseMerge(head, before)), 0);
});

test('not covered when the epic moved after the story was cut', () => {
  const { dir, git, commit } = repo();
  commit('base');
  git('checkout', '-q', '-b', 'story');
  const head = commit('story');
  git('checkout', '-q', 'epic');
  const before = commit('main-merge');
  git('merge', '-q', '--no-edit', 'story');
  assert.equal(covers(dir, head, before, git('rev-parse', 'HEAD')), 1);
});

test('covered when the story was rebased onto the moved epic before its CI ran', () => {
  const { dir, git, commit, rebaseMerge } = repo();
  commit('base');
  git('checkout', '-q', '-b', 'story');
  git('checkout', '-q', 'epic');
  const before = commit('main-merge');
  git('checkout', '-q', 'story');
  git('rebase', '-q', 'epic');
  const head = commit('story');
  assert.equal(covers(dir, head, before, rebaseMerge(head, before)), 0);
});

test('not covered when the epic tip after the merge holds more than the story', () => {
  const { dir, git, commit } = repo();
  const before = commit('base');
  git('checkout', '-q', '-b', 'story');
  const head = commit('story');
  const after = commit('pushed-during-merge');
  assert.equal(covers(dir, head, before, after), 1);
});
