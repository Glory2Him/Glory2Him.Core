#!/usr/bin/env node
// Sums Claude Code token usage from the local session transcripts in
// $CLAUDE_CONFIG_DIR/projects (default ~/.claude/projects), grouped by agent type and model, with cost at API list
// prices.
//
//   node scripts/token-usage.mjs [--since YYYY-MM-DD] [--project <name>] [--by-session]
//   node scripts/token-usage.mjs --current [--since YYYY-MM-DD] [--by-session]
//   node scripts/token-usage.mjs --current --require claude-opus-5-5:high
//   node scripts/token-usage.mjs --worktree [--by-session]
//
// The main conversation counts as "main"; each subagent run is attributed to
// the agentType in its .meta.json. Cache reads are billed far below fresh
// input, so compare like columns rather than the total alone.
//
// --current restricts the report to the Claude Code session running in this
// working directory (its main transcript plus everything under its
// subagents/ folder) and ignores --project. --since still applies.
//
// --worktree reports every session ever run in this working directory, not only the
// newest. The build driver gives each task its own worktree and reuses it when a task is
// resumed after a usage limit or a halt, so this is the task's whole cost across attempts.
//
// --require <model>:<effort> exits 1, naming each agent, if any call in the
// report ran on another model or at another effort. Every transcript line
// records its effort, so this checks what ran, not what was asked for.

import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { homedir } from 'node:os';
import { basename, join } from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

// Price per million tokens: input / cache write (1-hour) / cache read / output.
// A 1-hour cache write is 2x input everywhere. A cache read is 0.1x input on
// every model except Claude Opus 5.5, where it is 0.05x — which is why a
// cache-read-heavy run costs the same on Opus 5.5 as on Sonnet 5.
// https://platform.claude.com/docs/en/about-claude/pricing
export const PRICES = {
  'claude-opus-5-5': { input: 4, cacheWrite: 8, cacheRead: 0.2, output: 20 },
  'claude-opus-5': { input: 5, cacheWrite: 10, cacheRead: 0.5, output: 25 },
  'claude-sonnet-5': { input: 2, cacheWrite: 4, cacheRead: 0.2, output: 10 },
  'claude-haiku-4-5': { input: 1, cacheWrite: 2, cacheRead: 0.1, output: 5 },
};

// Claude Code keeps transcripts under $CLAUDE_CONFIG_DIR when it is set,
// and under ~/.claude otherwise.
export function transcriptsRoot(env, home) {
  return join(env.CLAUDE_CONFIG_DIR || join(home, '.claude'), 'projects');
}

// Replaces every character that isn't [A-Za-z0-9] with '-', matching how
// Claude Code names a project's directory under ~/.claude/projects.
export function sanitizeProjectDirName(cwd) {
  return cwd.replace(/[^A-Za-z0-9]/g, '-');
}

// The current session is the top-level <session-id>.jsonl in this cwd's
// project directory with the newest mtime.
export function findCurrentSession(root, cwd) {
  const projectDir = join(root, sanitizeProjectDirName(cwd));
  if (!existsSync(projectDir)) return null;
  const topLevel = readdirSync(projectDir).filter((f) => f.endsWith('.jsonl'));
  if (topLevel.length === 0) return null;
  const newest = topLevel
    .map((file) => ({ file, mtime: statSync(join(projectDir, file)).mtimeMs }))
    .sort((a, b) => b.mtime - a.mtime)[0];
  return { projectDir, sessionId: newest.file.replace(/\.jsonl$/, '') };
}

// Returns [{ transcript, session }] for every transcript to scan.
export function listTranscripts({ root, project, current, worktree, cwd }) {
  if (worktree) {
    const projectDir = join(root, sanitizeProjectDirName(cwd));
    if (!existsSync(projectDir)) return [];
    return readdirSync(projectDir, { recursive: true })
      .filter((entry) => entry.endsWith('.jsonl'))
      .map((entry) => ({ transcript: join(projectDir, entry), session: entry.split(/[\\/]/)[0].replace(/\.jsonl$/, '') }));
  }
  if (current) {
    const found = findCurrentSession(root, cwd);
    if (!found) return [];
    const { projectDir, sessionId } = found;
    const transcripts = [join(projectDir, `${sessionId}.jsonl`)];
    const subagentsDir = join(projectDir, sessionId, 'subagents');
    if (existsSync(subagentsDir)) {
      for (const entry of readdirSync(subagentsDir, { recursive: true })) {
        if (entry.endsWith('.jsonl')) transcripts.push(join(subagentsDir, entry));
      }
    }
    return transcripts.map((transcript) => ({ transcript, session: sessionId }));
  }

  const result = [];
  for (const dir of readdirSync(root).filter((d) => d.includes(project))) {
    const projectDir = join(root, dir);
    for (const entry of readdirSync(projectDir, { recursive: true })) {
      if (!entry.endsWith('.jsonl')) continue;
      const transcript = join(projectDir, entry);
      const session = entry.split(/[\\/]/)[0].replace(/\.jsonl$/, '');
      result.push({ transcript, session });
    }
  }
  return result;
}

export function agentTypeOf(transcript) {
  if (!/[\\/]subagents[\\/]/.test(transcript)) return 'main';
  const meta = transcript.replace(/\.jsonl$/, '.meta.json');
  if (!existsSync(meta)) return 'unknown';
  return JSON.parse(readFileSync(meta, 'utf8')).agentType ?? 'unknown';
}

// Cost in USD for one usage object under the given model, or null if the
// model isn't priced.
export function costOf(model, usage) {
  const price = PRICES[model];
  if (!price) return null;
  return (
    ((usage.input_tokens ?? 0) * price.input +
      (usage.cache_creation_input_tokens ?? 0) * price.cacheWrite +
      (usage.cache_read_input_tokens ?? 0) * price.cacheRead +
      (usage.output_tokens ?? 0) * price.output) /
    1_000_000
  );
}

const empty = () => ({
  runs: new Set(),
  calls: 0,
  input: 0,
  cacheWrite: 0,
  cacheRead: 0,
  output: 0,
  first: undefined,
  cost: 0,
  unpriced: false,
});
const total = (c) => c.input + c.cacheWrite + c.cacheRead + c.output;

function record(map, key, run, timestamp, usage, model) {
  const c = map.get(key) ?? empty();
  c.runs.add(run);
  c.calls += 1;
  c.input += usage.input_tokens ?? 0;
  c.cacheWrite += usage.cache_creation_input_tokens ?? 0;
  c.cacheRead += usage.cache_read_input_tokens ?? 0;
  c.output += usage.output_tokens ?? 0;
  if (!c.first || timestamp < c.first) c.first = timestamp;
  const cost = costOf(model, usage);
  if (cost === null) c.unpriced = true;
  else c.cost += cost;
  map.set(key, c);
}

// A message's usage is repeated on every line of its content blocks while
// streaming, and only the last line carries the final output_tokens. Keep
// the latest line seen per message id, then record each id once.
export function collectUsage(transcripts, since) {
  const latest = new Map();

  for (const { transcript, session } of transcripts) {
    const agent = agentTypeOf(transcript);
    for (const line of readFileSync(transcript, 'utf8').split('\n')) {
      if (!line) continue;
      let event;
      try {
        event = JSON.parse(line);
      } catch {
        continue;
      }
      const message = event.message;
      if (event.type !== 'assistant' || !message?.usage || !message.id) continue;
      if (message.model === '<synthetic>') continue;
      latest.set(message.id, {
        transcript,
        session,
        agent,
        effort: event.effort ?? 'none',
        model: message.model,
        timestamp: event.timestamp,
        usage: message.usage,
      });
    }
  }

  const byAgent = new Map();
  const sessions = new Map();
  const unpricedModels = new Set();
  const efforts = new Map();

  for (const { transcript, session, agent, effort, model, timestamp, usage } of latest.values()) {
    if (since && timestamp < since) continue;
    record(byAgent, `${agent}\t${model}`, transcript, timestamp, usage, model);
    efforts.set(`${agent}\t${model}`, (efforts.get(`${agent}\t${model}`) ?? new Set()).add(effort));
    record(sessions, `${session}\t${agent}`, transcript, timestamp, usage, model);
    if (!PRICES[model]) unpricedModels.add(model);
  }

  return { byAgent, sessions, unpricedModels, efforts };
}

// Every agent that made a call on another model, or at another effort, as
// '<agent> <model>/<efforts>'.
export function offRequirement({ byAgent, efforts }, { model, effort }) {
  return [...byAgent.keys()]
    .filter((key) => {
      const seen = efforts.get(key);
      return key.split('\t')[1] !== model || seen.size !== 1 || !seen.has(effort);
    })
    .map((key) => `${key.replace('\t', ' ')}/${[...efforts.get(key)].sort().join(',')}`);
}

const n = (x) => x.toLocaleString('en-GB');
const costCell = (c) => (c.unpriced ? '?' : `$${c.cost.toFixed(2)}`);

function table(headers, rows) {
  const widths = headers.map((h, i) => Math.max(h.length, ...rows.map((r) => r[i].length)));
  const format = (r) => r.map((cell, i) => (i < 2 ? cell.padEnd(widths[i]) : cell.padStart(widths[i]))).join('  ');
  console.log(format(headers));
  console.log(widths.map((w) => '-'.repeat(w)).join('  '));
  rows.forEach((r) => console.log(format(r)));
}

const sorted = (map) => [...map.entries()].sort(([, a], [, b]) => total(b) - total(a));
const counters = (c) => [
  n(c.runs.size),
  n(c.calls),
  n(c.input),
  n(c.cacheWrite),
  n(c.cacheRead),
  n(c.output),
  n(total(c)),
  costCell(c),
];

function repoName() {
  try {
    return basename(execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'ignore'] }).trim());
  } catch {
    return basename(process.cwd());
  }
}

function main() {
  const args = process.argv.slice(2);
  const option = (name) => {
    const i = args.indexOf(name);
    return i === -1 ? undefined : args[i + 1];
  };
  const since = option('--since');
  const current = args.includes('--current');
  const worktree = args.includes('--worktree');
  // Default: transcripts of projects whose directory name contains this repository's folder
  // name, sanitised the way Claude Code names those directories (Glory2Him.Template -> Glory2Him-Template).
  const project = option('--project') ?? sanitizeProjectDirName(repoName());
  const bySession = args.includes('--by-session');
  const required = option('--require');

  const root = transcriptsRoot(process.env, homedir());
  if (!existsSync(root)) {
    console.error(`No transcripts found at ${root}`);
    process.exit(1);
  }

  const cwd = process.cwd();
  const transcripts = listTranscripts({ root, project, current, worktree, cwd });
  if ((current || worktree) && transcripts.length === 0) {
    console.error(`No current session transcript found for ${cwd} under ${root}`);
    process.exit(1);
  }

  const usage = collectUsage(transcripts, since);
  const { byAgent, sessions, unpricedModels, efforts } = usage;
  const scope = worktree
    ? `every session in ${cwd}`
    : current ? `the current session (${transcripts[0].session.slice(0, 8)})` : `projects matching "${project}"`;

  if (byAgent.size === 0) {
    console.log(`No usage found for ${scope}${since ? ` since ${since}` : ''}.`);
    process.exit(0);
  }

  console.log(`Token usage for ${scope}${since ? ` since ${since}` : ''}\n`);
  table(
    ['agent', 'model', 'effort', 'runs', 'calls', 'input', 'cache write', 'cache read', 'output', 'total', 'cost'],
    sorted(byAgent).map(([key, c]) => [...key.split('\t'), [...efforts.get(key)].sort().join(','), ...counters(c)]),
  );

  if (bySession) {
    console.log('');
    table(
      ['session', 'agent', 'started', 'runs', 'calls', 'input', 'cache write', 'cache read', 'output', 'total', 'cost'],
      [...sessions.entries()]
        .sort(([, a], [, b]) => a.first.localeCompare(b.first))
        .map(([key, c]) => {
          const [session, agent] = key.split('\t');
          return [session.slice(0, 8), agent, c.first.slice(0, 16).replace('T', ' '), ...counters(c)];
        }),
    );
  }

  const grandTotal = [...byAgent.values()].reduce((s, c) => s + c.cost, 0);
  const excludes = unpricedModels.size > 0 ? ` (excludes unpriced models: ${[...unpricedModels].sort().join(', ')})` : '';
  console.log(`\nCost: $${grandTotal.toFixed(2)} at API list prices, 1-hour cache writes${excludes}`);

  if (required) {
    const [model, effort] = required.split(':');
    const off = offRequirement(usage, { model, effort });
    if (off.length > 0) {
      console.error(`\nOff requirement ${model}/${effort}: ${off.join('; ')}`);
      process.exit(1);
    }
    console.log(`\nEvery agent ran on ${model}/${effort}.`);
  }
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  main();
}
