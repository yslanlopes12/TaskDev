# Segurança do Task.dev

## Relatar um problema

Não publique credenciais nem instruções de exploração de uma instância real em issues abertas. Use o relato privado da aba **Security** do repositório, quando habilitado, ou contate o mantenedor por um canal privado existente. Inclua o commit afetado, o impacto e uma reprodução mínima com dados fictícios.

## Verificações automáticas

O workflow [Security](.github/workflows/security.yml) executa em pushes, pull requests, manualmente e semanalmente:

| Verificação | Comportamento |
| --- | --- |
| npm audit | Audita os dois lockfiles, incluindo dependências de desenvolvimento; bloqueia alertas moderados, altos e críticos |
| NuGet Audit | Audita dependências diretas e transitivas da API e dos testes; bloqueia avisos de vulnerabilidade e falhas na consulta da auditoria |
| Gitleaks | Verifica o histórico Git disponível com valores de segredos redigidos; bloqueia detecções |
| CodeQL | Analisa C# e JavaScript/TypeScript com `security-extended` e publica alertas no GitHub |
| Dependabot | Propõe atualizações semanais de npm, NuGet e GitHub Actions |

As ações estão fixadas por SHA. O Gitleaks usa uma versão fixa do executável oficial com verificação de SHA-256. Nenhum segredo de produção é necessário para as auditorias.

CodeQL exige disponibilidade do GitHub Code Security para o repositório: é disponibilizado para repositórios públicos e requer a configuração/plano correspondente nos privados. Se houver **default setup** ativo, desative-o antes de usar este workflow de **advanced setup**. Para bloquear merges por alertas CodeQL, configure regras de code scanning e checks obrigatórios nas regras da branch. Adicionar o YAML não altera automaticamente essas regras no GitHub.

## Executar localmente

```sh
npm audit --audit-level=moderate
npm audit --prefix TaskDev-Front --audit-level=moderate
node scripts/dotnet.mjs restore TaskDev-Back/TaskDev.Tests/TaskDev.Tests.csproj --force-evaluate -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low -warnaserror:NU1900,NU1901,NU1902,NU1903,NU1904,NU1905
npm run test:back -- -c Release -p:UseAppHost=false
```

Com o Gitleaks oficial instalado, use `gitleaks git --redact=100 --log-opts="--all" .`. Esse modo analisa commits, não alterações locais ainda sem commit. Não use o scanner de diretórios indiscriminadamente sobre `.env` locais: eles contêm intencionalmente credenciais privadas.

## Configuração de produção

- Mantenha `.env`, chaves e conexão do banco fora do Git e do build frontend.
- Use HTTPS e `Security__SecureCookies=true`.
- Mantenha `Admin__BootstrapEnabled=false` publicamente. Quando necessário, habilite apenas em uma instância privada, com acesso administrativo controlado, e desabilite após criar o primeiro administrador.
- Configure somente proxies conhecidos para cabeçalhos encaminhados.
- Preserve a chave de criptografia e o armazenamento de Data Protection entre deploys.
- Endpoints externos de IA devem usar HTTPS. Redirecionamentos automáticos e proxies HTTP do ambiente estão desabilitados no cliente de IA; a conexão é feita ao IP público validado.
- `Agent__AllowLocalOllama=true` abre uma exceção explícita apenas para `localhost:11434`. Habilite somente em instalações confiáveis. A opção fica desabilitada por padrão e não abre acesso arbitrário à rede privada.

Após a atualização de segurança que adicionou expiração validada no servidor, sessões antigas exigem um novo login. A validade é de sete dias e o papel de acesso é atualizado a partir do banco em cada validação.

O [relatório de revisão](docs/security-review.md) descreve o escopo, as correções e os limites da verificação. Auditorias de pacotes e testes não garantem ausência de vulnerabilidades nem substituem avaliação do ambiente publicado.
