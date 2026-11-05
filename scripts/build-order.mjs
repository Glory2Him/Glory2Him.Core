#!/usr/bin/env node
// Orders a feature's tasks for the build driver. Reads one task per line on stdin,
//
//   <number>\t<title>\t<space-separated numbers from its **Depends on:** line>
//
// in the planner's sub-issue order, and prints the task numbers, space-separated, in build
// order: by layer first, from the category in the title — groundwork, broker, foundation,
// processing, orchestration, exposer, UI — and then by Depends on, which wins where the
// two disagree. Ties keep the input order. A dependency outside the feature is ignored; a
// cycle is an error, named on stderr with exit 1.
//
//   printf '520\tFOUNDATIONS: Add Saved Search\t512\n' | node scripts/build-order.mjs

import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const LAYERS = [
  ['DATA', 'MIGRATIONS', 'CONFIG', 'PROVISIONS', 'INFRA'],
  ['BROKERS'],
  ['FOUNDATIONS'],
  ['PROCESSINGS'],
  ['ORCHESTRATIONS', 'COORDINATIONS', 'MANAGEMENTS', 'AGGREGATIONS'],
  ['CONTROLLERS', 'EXPOSERS', 'CLIENTS', 'PROVIDERS'],
  ['BASE', 'COMPONENTS', 'VIEWS'],
  ['PAGES'],
  ['ACCEPTANCE', 'INTEGRATIONS'],
];

// The layer of a title such as "MEDIUM FOUNDATIONS: Add X". Unknown categories go last.
export function layerOf(title) {
  const category = (title.split(':')[0] || '').trim().toUpperCase().replace(/^(MINOR|MEDIUM|MAJOR) /, '');
  const i = LAYERS.findIndex((names) => names.includes(category));
  return i === -1 ? LAYERS.length : i;
}

export function parse(text) {
  return text
    .split('\n')
    .filter((line) => line.trim() !== '')
    .map((line, position) => {
      const [number, title = '', deps = ''] = line.split('\t');
      return { number: Number(number), title, deps: deps.split(/\s+/).filter(Boolean).map(Number), position };
    });
}

// Repeatedly takes the earliest-layer, earliest-listed task whose dependencies are all built.
export function order(tasks) {
  const inFeature = new Set(tasks.map((t) => t.number));
  const built = new Set();
  const result = [];
  const remaining = [...tasks].sort((a, b) => layerOf(a.title) - layerOf(b.title) || a.position - b.position);
  while (remaining.length) {
    const i = remaining.findIndex((t) => t.deps.every((d) => !inFeature.has(d) || d === t.number || built.has(d)));
    if (i === -1) {
      throw new Error(`Depends on lines form a cycle among #${remaining.map((t) => t.number).join(', #')}`);
    }
    const [next] = remaining.splice(i, 1);
    built.add(next.number);
    result.push(next.number);
  }
  return result;
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  try {
    console.log(order(parse(readFileSync(0, 'utf8'))).join(' '));
  } catch (error) {
    console.error(error.message);
    process.exit(1);
  }
}
