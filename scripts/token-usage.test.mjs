import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, writeFileSync, utimesSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';

import {
  collectUsage,
  costOf,
  findCurrentSession,
  listTranscripts,
  offRequirement,
  sanitizeProjectDirName,
  transcriptsRoot,
} from './token-usage.mjs';

function tempRoot() {
  return mkdtempSync(join(tmpdir(), 'token-usage-'));
}

function assistantLine({ id, model = 'claude-sonnet-5', timestamp = '2026-01-01T00:00:00Z', usage, effort }) {
  return JSON.stringify({ type: 'assistant', timestamp, effort, message: { id, model, usage } });
}

function transcriptOf(lines) {
  const projectDir = join(tempRoot(), 'my-project');
  mkdirSync(projectDir, { recursive: true });
  const transcript = join(projectDir, 'session-1.jsonl');
  writeFileSync(transcript, lines.join('\n'));
  return [{ transcript, session: 'session-1' }];
}

const usage = { input_tokens: 1, output_tokens: 1 };

test('sanitizeProjectDirName replaces every non-alphanumeric character with a dash', () => {
  const cwd = '/home/x/repos/sample-app/.claude/worktrees/a_b';
  assert.equal(sanitizeProjectDirName(cwd), '-home-x-repos-sample-app--claude-worktrees-a-b');
});

test('transcriptsRoot reads projects under CLAUDE_CONFIG_DIR when it is set', () => {
  assert.equal(transcriptsRoot({ CLAUDE_CONFIG_DIR: '/home/x/.claude-personal' }, '/home/x'), join('/home/x/.claude-personal', 'projects'));
});

test('transcriptsRoot falls back to ~/.claude/projects when CLAUDE_CONFIG_DIR is unset or empty', () => {
  assert.equal(transcriptsRoot({}, '/home/x'), join('/home/x', '.claude', 'projects'));
  assert.equal(transcriptsRoot({ CLAUDE_CONFIG_DIR: '' }, '/home/x'), join('/home/x', '.claude', 'projects'));
});

test('collectUsage counts a message once using the usage from its last line, not its first', () => {
  const root = tempRoot();
  const projectDir = join(root, 'my-project');
  mkdirSync(projectDir, { recursive: true });
  const transcript = join(projectDir, 'session-1.jsonl');
  writeFileSync(
    transcript,
    [
      assistantLine({ id: 'msg_1', usage: { input_tokens: 10, output_tokens: 5 } }),
      assistantLine({ id: 'msg_1', usage: { input_tokens: 10, output_tokens: 120 } }),
    ].join('\n'),
  );

  const { byAgent } = collectUsage([{ transcript, session: 'session-1' }]);
  const c = byAgent.get('main\tclaude-sonnet-5');

  assert.equal(c.output, 120);
  assert.equal(c.calls, 1);
});

test('collectUsage excludes messages timestamped before --since', () => {
  const root = tempRoot();
  const projectDir = join(root, 'my-project');
  mkdirSync(projectDir, { recursive: true });
  const transcript = join(projectDir, 'session-1.jsonl');
  writeFileSync(
    transcript,
    [
      assistantLine({ id: 'msg_old', timestamp: '2025-01-01T00:00:00Z', usage: { input_tokens: 1, output_tokens: 1 } }),
      assistantLine({ id: 'msg_new', timestamp: '2026-06-01T00:00:00Z', usage: { input_tokens: 2, output_tokens: 2 } }),
    ].join('\n'),
  );

  const { byAgent } = collectUsage([{ transcript, session: 'session-1' }], '2026-01-01');
  const c = byAgent.get('main\tclaude-sonnet-5');

  assert.equal(c.calls, 1);
  assert.equal(c.input, 2);
});

test('costOf prices claude-opus-5 usage at its per-million rates', () => {
  const cost = costOf('claude-opus-5', {
    input_tokens: 1_000_000,
    cache_creation_input_tokens: 1_000_000,
    cache_read_input_tokens: 1_000_000,
    output_tokens: 1_000_000,
  });
  assert.equal(cost, 5 + 10 + 0.5 + 25);
});

test('costOf prices claude-opus-5-5 usage at its per-million rates, with its 0.05x cache read', () => {
  const cost = costOf('claude-opus-5-5', {
    input_tokens: 1_000_000,
    cache_creation_input_tokens: 1_000_000,
    cache_read_input_tokens: 1_000_000,
    output_tokens: 1_000_000,
  });
  assert.equal(cost, 4 + 8 + 0.2 + 20);
});

test('costOf prices claude-sonnet-5 usage at its per-million rates', () => {
  const cost = costOf('claude-sonnet-5', {
    input_tokens: 1_000_000,
    cache_creation_input_tokens: 1_000_000,
    cache_read_input_tokens: 1_000_000,
    output_tokens: 1_000_000,
  });
  assert.equal(cost, 2 + 4 + 0.2 + 10);
});

test('costOf returns null for a model with no listed price', () => {
  assert.equal(costOf('claude-haiku-9000', { input_tokens: 1_000_000, output_tokens: 1_000_000 }), null);
});

test('collectUsage marks a row unpriced when its model has no listed price', () => {
  const root = tempRoot();
  const projectDir = join(root, 'my-project');
  mkdirSync(projectDir, { recursive: true });
  const transcript = join(projectDir, 'session-1.jsonl');
  writeFileSync(
    transcript,
    assistantLine({ id: 'msg_1', model: 'claude-haiku-9000', usage: { input_tokens: 10, output_tokens: 5 } }),
  );

  const { byAgent, unpricedModels } = collectUsage([{ transcript, session: 'session-1' }]);
  const c = byAgent.get('main\tclaude-haiku-9000');

  assert.equal(c.unpriced, true);
  assert.equal(c.cost, 0);
  assert.deepEqual([...unpricedModels], ['claude-haiku-9000']);
});

test('findCurrentSession maps cwd to its project directory and picks the newest top-level session', () => {
  const root = tempRoot();
  const cwd = '/some/project';
  const projectDir = join(root, sanitizeProjectDirName(cwd));
  mkdirSync(projectDir, { recursive: true });

  const older = join(projectDir, 'session-old.jsonl');
  const newer = join(projectDir, 'session-new.jsonl');
  writeFileSync(older, '');
  writeFileSync(newer, '');
  const past = new Date('2026-01-01T00:00:00Z');
  const recent = new Date('2026-06-01T00:00:00Z');
  utimesSync(older, past, past);
  utimesSync(newer, recent, recent);

  const found = findCurrentSession(root, cwd);

  assert.equal(found.sessionId, 'session-new');
});

test('findCurrentSession returns null when the cwd has no project directory', () => {
  const root = tempRoot();
  assert.equal(findCurrentSession(root, '/nothing/here'), null);
});

test('listTranscripts with --current includes the session transcript and its subagent transcripts', () => {
  const root = tempRoot();
  const cwd = '/some/project';
  const projectDir = join(root, sanitizeProjectDirName(cwd));
  mkdirSync(projectDir, { recursive: true });
  writeFileSync(join(projectDir, 'session-1.jsonl'), '');

  const subagentsDir = join(projectDir, 'session-1', 'subagents');
  mkdirSync(subagentsDir, { recursive: true });
  writeFileSync(join(subagentsDir, 'run-1.jsonl'), '');

  const transcripts = listTranscripts({ root, current: true, cwd });

  assert.deepEqual(
    transcripts.map((t) => t.transcript).sort(),
    [join(projectDir, 'session-1.jsonl'), join(subagentsDir, 'run-1.jsonl')].sort(),
  );
  assert.ok(transcripts.every((t) => t.session === 'session-1'));
});

test('listTranscripts with --current ignores other sessions in the same project directory', () => {
  const root = tempRoot();
  const cwd = '/some/project';
  const projectDir = join(root, sanitizeProjectDirName(cwd));
  mkdirSync(projectDir, { recursive: true });
  writeFileSync(join(projectDir, 'session-old.jsonl'), '');
  writeFileSync(join(projectDir, 'session-new.jsonl'), '');
  const past = new Date('2026-01-01T00:00:00Z');
  const recent = new Date('2026-06-01T00:00:00Z');
  utimesSync(join(projectDir, 'session-old.jsonl'), past, past);
  utimesSync(join(projectDir, 'session-new.jsonl'), recent, recent);

  const transcripts = listTranscripts({ root, current: true, cwd });

  assert.deepEqual(transcripts.map((t) => t.session), ['session-new']);
});

test('listTranscripts with --current returns nothing when no transcript is found', () => {
  const root = tempRoot();
  assert.deepEqual(listTranscripts({ root, current: true, cwd: '/nothing/here' }), []);
});

test('listTranscripts without --current still filters projects by name', () => {
  const root = tempRoot();
  const matching = join(root, 'sample-app-project');
  const other = join(root, 'other-project');
  mkdirSync(matching, { recursive: true });
  mkdirSync(other, { recursive: true });
  writeFileSync(join(matching, 'session-a.jsonl'), '');
  writeFileSync(join(other, 'session-b.jsonl'), '');

  const transcripts = listTranscripts({ root, project: 'sample-app', current: false });

  assert.deepEqual(transcripts.map((t) => t.session), ['session-a']);
});

test('collectUsage records the effort each agent and model ran at', () => {
  const { efforts } = collectUsage(
    transcriptOf([
      assistantLine({ id: 'msg_1', model: 'claude-opus-5-5', effort: 'high', usage }),
      assistantLine({ id: 'msg_2', model: 'claude-opus-5-5', effort: 'medium', usage }),
    ]),
  );

  assert.deepEqual([...efforts.get('main\tclaude-opus-5-5')].sort(), ['high', 'medium']);
});

test('offRequirement finds nothing when every call ran on the required model and effort', () => {
  const usageData = collectUsage(
    transcriptOf([assistantLine({ id: 'msg_1', model: 'claude-opus-5-5', effort: 'high', usage })]),
  );

  assert.deepEqual(offRequirement(usageData, { model: 'claude-opus-5-5', effort: 'high' }), []);
});

test('offRequirement names an agent that ran on another model', () => {
  const usageData = collectUsage(
    transcriptOf([assistantLine({ id: 'msg_1', model: 'claude-sonnet-5', effort: 'high', usage })]),
  );

  assert.deepEqual(offRequirement(usageData, { model: 'claude-opus-5-5', effort: 'high' }), [
    'main claude-sonnet-5/high',
  ]);
});

test('offRequirement names an agent with any call at another effort, or none recorded', () => {
  const usageData = collectUsage(
    transcriptOf([
      assistantLine({ id: 'msg_1', model: 'claude-opus-5-5', effort: 'high', usage }),
      assistantLine({ id: 'msg_2', model: 'claude-opus-5-5', effort: 'medium', usage }),
      assistantLine({ id: 'msg_3', model: 'claude-opus-5-5', usage }),
    ]),
  );

  assert.deepEqual(offRequirement(usageData, { model: 'claude-opus-5-5', effort: 'high' }), [
    'main claude-opus-5-5/high,medium,none',
  ]);
});

test('listTranscripts with --worktree includes every session in the cwd, with their subagents', () => {
  const root = tempRoot();
  const cwd = '/repo/.worktrees/task-520';
  const projectDir = join(root, sanitizeProjectDirName(cwd));
  mkdirSync(join(projectDir, 'attempt-2', 'subagents'), { recursive: true });
  writeFileSync(join(projectDir, 'attempt-1.jsonl'), '');
  writeFileSync(join(projectDir, 'attempt-2.jsonl'), '');
  writeFileSync(join(projectDir, 'attempt-2', 'subagents', 'qa.jsonl'), '');
  mkdirSync(join(root, sanitizeProjectDirName('/repo/.worktrees/task-521')), { recursive: true });
  writeFileSync(join(root, sanitizeProjectDirName('/repo/.worktrees/task-521'), 'other.jsonl'), '');

  const transcripts = listTranscripts({ root, worktree: true, cwd });

  assert.deepEqual(transcripts.map((t) => t.session).sort(), ['attempt-1', 'attempt-2', 'attempt-2']);
});

test('a repository folder with punctuation matches its sanitised transcript directory', () => {
  const root = tempRoot();
  mkdirSync(join(root, '-home-x-Sample-Template'), { recursive: true });
  writeFileSync(join(root, '-home-x-Sample-Template', 's.jsonl'), '');
  const transcripts = listTranscripts({ root, project: sanitizeProjectDirName('Sample.Template'), current: false });
  assert.deepEqual(transcripts.map((t) => t.session), ['s']);
});
