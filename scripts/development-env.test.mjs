import assert from 'node:assert/strict'
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import test from 'node:test'
import { readDevelopmentEnv } from './development-env.mjs'

function fixture(t, contents) {
  const directory = mkdtempSync(join(tmpdir(), 'taskdev-env-'))
  t.after(() => rmSync(directory, { recursive: true, force: true }))
  const file = join(directory, '.env')
  if (contents !== undefined) writeFileSync(file, contents)
  return file
}

test('loads quoted connection values and preserves equals signs and fragments', t => {
  const file = fixture(t, 'DATABASE_URL="Host=localhost;Password=example#value="\nSecurity__SecureCookies=false\n')
  assert.deepEqual(readDevelopmentEnv(file, {}), {
    DATABASE_URL: 'Host=localhost;Password=example#value=',
    Security__SecureCookies: 'false',
  })
})

test('shell values take precedence without mutating the inherited environment', t => {
  const inherited = { DATABASE_URL: 'shell-connection' }
  const file = fixture(t, 'DATABASE_URL=file-connection\nAGENT_ENCRYPTION_KEY=example-key\n')
  assert.deepEqual(readDevelopmentEnv(file, inherited), {
    DATABASE_URL: 'shell-connection', AGENT_ENCRYPTION_KEY: 'example-key',
  })
  assert.deepEqual(inherited, { DATABASE_URL: 'shell-connection' })
})

test('missing files and blank template values do not mask .NET configuration', t => {
  assert.deepEqual(readDevelopmentEnv(fixture(t), { EXAMPLE: 'kept' }), { EXAMPLE: 'kept' })
  const file = fixture(t, 'DATABASE_URL=\nDATABASE_URL_UNPOOLED=\nAGENT_ENCRYPTION_KEY=" "\n')
  assert.deepEqual(readDevelopmentEnv(file, {}), {})
})
