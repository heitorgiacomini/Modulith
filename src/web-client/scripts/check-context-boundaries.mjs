import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join, normalize, relative, resolve } from 'node:path';

const appDirectory = resolve('src/app');
const contextsDirectory = join(appDirectory, 'contexts');
const importPattern = /from\s+['"]([^'"]+)['"]/g;
const errors = [];

function files(directory) {
  return readdirSync(directory).flatMap(entry => {
    const path = join(directory, entry);
    return statSync(path).isDirectory() ? files(path) : [path];
  });
}

function contextOf(path) {
  const parts = relative(contextsDirectory, path).split(/[\\/]/);
  return parts.length > 1 ? parts[0] : undefined;
}

function resolveImport(sourceFile, specifier) {
  const candidate = resolve(join(sourceFile, '..'), specifier);
  const alternatives = [candidate, `${candidate}.ts`, join(candidate, 'index.ts')];
  return alternatives.find(existsSync);
}

for (const sourceFile of files(contextsDirectory).filter(path => path.endsWith('.ts'))) {
  const sourceContext = contextOf(sourceFile);
  const source = readFileSync(sourceFile, 'utf8');

  for (const match of source.matchAll(importPattern)) {
    const specifier = match[1];
    if (!specifier.startsWith('.')) continue;

    const targetFile = resolveImport(sourceFile, specifier);
    if (!targetFile) continue;

    const targetContext = contextOf(targetFile);
    if (targetContext && targetContext !== sourceContext && normalize(targetFile) !== normalize(join(contextsDirectory, targetContext, 'index.ts'))) {
      errors.push(`${relative(appDirectory, sourceFile)} imports internal code from ${targetContext}: ${specifier}`);
    }
  }
}

if (errors.length) {
  console.error('Context boundary violations:\n' + errors.join('\n'));
  process.exitCode = 1;
} else {
  console.log('Context boundaries are valid.');
}
