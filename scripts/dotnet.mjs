import { spawnSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { dirname } from 'node:path'
import { fileURLToPath } from 'node:url'
import { readDevelopmentEnv } from './development-env.mjs'

const executable = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet'
const localSdk = fileURLToPath(new URL(`../.taskdev-tools/dotnet10/${executable}`, import.meta.url))
const hasLocalSdk = existsSync(localSdk)
// Only the development run command reads the private root .env.
// Builds and tests continue using their explicit environment.
const environment = process.argv[2] === 'run'
  ? readDevelopmentEnv(fileURLToPath(new URL('../.env', import.meta.url)))
  : process.env
const result = spawnSync(hasLocalSdk ? localSdk : 'dotnet', process.argv.slice(2), {
  env: hasLocalSdk ? { ...environment, DOTNET_ROOT: dirname(localSdk) } : environment,
  stdio: 'inherit',
})

if (result.error) {
  console.error(result.error.code === 'ENOENT'
    ? 'Instale o SDK .NET 10 e deixe o comando dotnet disponível no PATH.'
    : result.error.message)
}

process.exitCode = result.status ?? 1
