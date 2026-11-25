import { spawnSync } from 'node:child_process'
import { existsSync } from 'node:fs'
import { dirname } from 'node:path'
import { fileURLToPath } from 'node:url'

const executable = process.platform === 'win32' ? 'dotnet.exe' : 'dotnet'
const localSdk = fileURLToPath(new URL(`../.taskdev-tools/dotnet10/${executable}`, import.meta.url))
const hasLocalSdk = existsSync(localSdk)
const result = spawnSync(hasLocalSdk ? localSdk : 'dotnet', process.argv.slice(2), {
  env: hasLocalSdk ? { ...process.env, DOTNET_ROOT: dirname(localSdk) } : process.env,
  stdio: 'inherit',
})

if (result.error) {
  console.error(result.error.code === 'ENOENT'
    ? 'Instale o SDK .NET 10 e deixe o comando dotnet disponível no PATH.'
    : result.error.message)
}

process.exitCode = result.status ?? 1
