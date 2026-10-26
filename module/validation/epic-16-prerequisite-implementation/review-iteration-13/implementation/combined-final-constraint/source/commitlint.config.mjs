export default {
  extends: ['@commitlint/config-conventional'],
  defaultIgnores: false,
  plugins: [
    {
      rules: {
        'header-format': ({ header }) => [
          /^[a-z]+(?:\([^\s()]+\))?!?: \S.*$/u.test(header ?? ''),
          'header must use an optional non-empty whitespace-free scope and exactly one space after the colon',
        ],
      },
    },
  ],
  rules: {
    'body-max-line-length': [2, 'always', 200],
    'header-format': [2, 'always'],
    'header-max-length': [2, 'always', 200],
    'type-enum': [
      2,
      'always',
      ['build', 'ci', 'docs', 'feat', 'fix', 'perf', 'refactor', 'revert', 'style', 'test'],
    ],
  },
};
