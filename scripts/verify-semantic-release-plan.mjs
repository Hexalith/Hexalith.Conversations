#!/usr/bin/env node

import { execFileSync } from 'node:child_process';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

import semanticRelease from 'semantic-release';

const expectedVersion = process.argv[2];
if (!/^\d+\.\d+\.\d+$/.test(expectedVersion ?? '')) {
  throw new Error('Expected version must be supplied as a stable semantic version.');
}

const releaseConfiguration = JSON.parse(await readFile('.releaserc.json', 'utf8'));
const analyzer = releaseConfiguration.plugins?.find((plugin) => {
  const name = Array.isArray(plugin) ? plugin[0] : plugin;
  return name === '@semantic-release/commit-analyzer';
});
if (
  !analyzer
  || !Array.isArray(releaseConfiguration.branches)
  || releaseConfiguration.branches.length !== 1
  || typeof releaseConfiguration.branches[0] !== 'string'
  || !releaseConfiguration.tagFormat
) {
  throw new Error('Release configuration is missing its planning contract.');
}
const planningBranch = releaseConfiguration.branches[0];

const temporary = await mkdtemp(path.join(tmpdir(), 'hexalith-conversations-release-plan-'));
const localRemote = path.join(temporary, 'planning.git');
let result;
try {
  execFileSync('git', ['init', '--bare', localRemote], { stdio: 'ignore' });
  const localBranch = execFileSync('git', ['branch', '--show-current'], { encoding: 'utf8' }).trim();
  const sourceBranch = process.env.GITHUB_REF_NAME || localBranch;
  if (sourceBranch && sourceBranch !== planningBranch) {
    throw new Error(`Semantic Release planning requires branch ${planningBranch}, found ${sourceBranch}.`);
  }
  execFileSync('git', ['push', '--force', localRemote, `HEAD:refs/heads/${planningBranch}`], {
    stdio: 'ignore',
  });
  execFileSync('git', ['--git-dir', localRemote, 'symbolic-ref', 'HEAD', `refs/heads/${planningBranch}`], {
    stdio: 'ignore',
  });
  const tags = execFileSync('git', ['tag', '--list'], { encoding: 'utf8' }).trim();
  if (tags) {
    execFileSync('git', ['push', '--force', localRemote, '--tags'], { stdio: 'ignore' });
  }

  result = await semanticRelease(
    {
      branches: releaseConfiguration.branches,
      tagFormat: releaseConfiguration.tagFormat,
      repositoryUrl: pathToFileURL(localRemote).href,
      plugins: [analyzer],
    },
    {
      ci: false,
      cwd: process.cwd(),
      dryRun: true,
      env: process.env,
      stderr: process.stderr,
      stdout: process.stdout,
    },
  );
} finally {
  await rm(temporary, { recursive: true, force: true });
}

if (result === false || result.nextRelease?.version !== expectedVersion) {
  const actual = result === false ? 'no release' : (result.nextRelease?.version ?? 'unknown');
  throw new Error(`Semantic Release planned ${actual}; expected exactly ${expectedVersion}.`);
}

process.stdout.write(`Semantic Release plan is exactly ${expectedVersion}.\n`);
