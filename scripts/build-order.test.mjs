import assert from 'node:assert/strict';
import test from 'node:test';

import { layerOf, order, parse } from './build-order.mjs';

const tasks = (lines) => parse(lines.join('\n'));

test('layerOf ranks categories bottom-up and ignores the size tier', () => {
  assert.ok(layerOf('DATA: Add Model') < layerOf('BROKERS: Insert'));
  assert.ok(layerOf('BROKERS: Insert') < layerOf('MEDIUM FOUNDATIONS: Add'));
  assert.ok(layerOf('FOUNDATIONS: Add') < layerOf('ORCHESTRATIONS: Add'));
  assert.ok(layerOf('CONTROLLERS: Post') < layerOf('PAGES: Saved Searches'));
});

test('order builds by layer when nothing depends on anything', () => {
  const result = order(tasks(['520\tFOUNDATIONS: Add', '530\tCONTROLLERS: Post', '512\tBROKERS: Insert', '510\tDATA: Model']));
  assert.deepEqual(result, [510, 512, 520, 530]);
});

test('order keeps the planner order within a layer', () => {
  assert.deepEqual(order(tasks(['513\tBROKERS: Select', '512\tBROKERS: Insert'])), [513, 512]);
});

test('Depends on wins over the layer order', () => {
  // A foundation task that, unusually, depends on a later-layer task.
  const result = order(tasks(['520\tFOUNDATIONS: Add\t530', '530\tCONTROLLERS: Post']));
  assert.deepEqual(result, [530, 520]);
});

test('a dependency outside the feature is ignored', () => {
  assert.deepEqual(order(tasks(['520\tFOUNDATIONS: Add\t999'])), [520]);
});

test('a cycle is an error naming the tasks', () => {
  assert.throws(() => order(tasks(['1\tBROKERS: A\t2', '2\tBROKERS: B\t1'])), /cycle among #1, #2/);
});

test('parse reads number, title and dependencies, skipping blank lines', () => {
  assert.deepEqual(parse('5\tBROKERS: X\t3 4\n\n6\tT\t\n'), [
    { number: 5, title: 'BROKERS: X', deps: [3, 4], position: 0 },
    { number: 6, title: 'T', deps: [], position: 1 },
  ]);
});
