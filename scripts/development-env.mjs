import { existsSync, readFileSync } from 'node:fs'
import { parseEnv } from 'node:util'

export function readDevelopmentEnv(path, inherited = process.env) {
  if (!existsSync(path)) return { ...inherited }

  // Empty template values must not mask .NET User Secrets or appsettings.
  const values = Object.fromEntries(
    Object.entries(parseEnv(readFileSync(path, 'utf8')))
      .filter(([, value]) => value.trim() !== ''),
  )
  // Explicit shell configuration takes priority over the local file.
  return { ...values, ...inherited }
}
