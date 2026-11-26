# Revisão de segurança · 26 de novembro de 2026

## Escopo

Revisão manual do código local de autenticação, sessão, CSRF, autorização administrativa e por espaço, acesso ao PostgreSQL, cliente HTTP de IA, renderização de Markdown, Dockerfiles e GitHub Actions. Auditoria dos dois lockfiles npm, dos projetos NuGet e do histórico Git disponível com Gitleaks. As alterações locais de documentação e configuração foram preservadas.

Não foram feitos testes de intrusão contra a instância pública, acesso a dados reais do PostgreSQL ou verificação das permissões/configurações remotas do GitHub. CodeQL está configurado para execução no GitHub; não foi executado localmente. Imagens de contêiner, sistema operacional, TLS e infraestrutura de produção não foram auditados por um scanner de imagens nesta revisão.

## Achados corrigidos

| Severidade estimada | Achado confirmado | Correção |
| --- | --- | --- |
| Alta | O middleware CSRF aceitava escritas autenticadas quando faltavam os dois tokens ou havia somente um cabeçalho arbitrário | Exige cookie e cabeçalho presentes e iguais; caso contrário, retorna 403 |
| Alta | A política de rate limiting existia, mas login e cadastro não a aplicavam | Política aplicada às duas rotas e ao bootstrap administrativo |
| Alta | A validação de URL da IA ocorria antes da conexão, permitindo divergência de resolução DNS e redirecionamentos não validados | Validação no `ConnectCallback`, conexão direta ao IP aprovado, bloqueio de redirecionamentos e proxies de ambiente |
| Média | Intervalos reservados de IP não eram totalmente contemplados pela proteção de IA | Bloqueio adicional de CGNAT, multicast, endereços não especificados e IPv4 mapeado em IPv6; política restrita de IPv6 global |
| Média | Expiração da sessão era apenas a expiração do cookie no navegador | Expiração protegida no payload e validada no servidor; sessões legadas sem expiração são rejeitadas |
| Média | O papel contido na sessão podia ficar desatualizado após uma alteração administrativa | Claims usam o papel atual consultado no banco |
| Média | Corpo de erro do provedor de IA era devolvido e registrado parcialmente nos logs | Mensagens de erro passam a expor somente o código HTTP e orientação genérica, sem o corpo do provedor |
| Média | Quatro alertas npm na cadeia de build `vite-plugin-pwa → workbox-build → glob → minimatch` | Override específico de `glob` para 14.0.2 e novo lockfile, mantendo a PWA; o build foi validado |

As severidades das falhas do código são estimativas desta revisão, não pontuações CVSS formais. A exploração depende da configuração, da sessão e do acesso aos endpoints.

Os avisos de dependência eram [GHSA-3g3m-2fjr-6pw4](https://github.com/advisories/GHSA-3g3m-2fjr-6pw4) e [GHSA-g3hv-75wg-m249](https://github.com/advisories/GHSA-g3hv-75wg-m249), ambos moderados. Os quatro resultados npm incluem a propagação pela cadeia de dependências; não representam quatro falhas independentes no código da aplicação. O override deve ser revisto quando o Workbox adotar uma versão corrigida diretamente.

## Comportamentos afetados

- Sessões criadas antes da atualização exigem novo login.
- Integrações de escrita precisam obter `/api/auth/csrf` e enviar o token junto ao cookie da mesma sessão.
- Endpoints externos de IA precisam de HTTPS e de IPs públicos aprovados na conexão. Redirecionamentos deixam de ser seguidos.
- Ollama local exige `Agent__AllowLocalOllama=true` explicitamente, apenas para `localhost:11434`.
- Respostas da IA ficam limitadas a 1 MiB no buffer do cliente HTTP.

## Observações e riscos restantes

1. **Bootstrap administrativo:** a sequência de verificar a existência de administrador, cadastrar e promover não é uma transação atômica entre instâncias. Não exponha o bootstrap enquanto estiver habilitado. O fechamento padrão em produção e o uso privado são mitigadores de implantação, não uma correção de concorrência distribuída.
2. **Credenciais de usuário:** a política atual aceita senha escolhida com seis caracteres. Convém evoluir a política de senha e o fluxo de recuperação em uma mudança de produto dedicada; Argon2id protege o armazenamento, mas não elimina senhas fracas.
3. **Saída de rede:** bloqueios de aplicação devem ser complementados por políticas de egress no ambiente publicado. Não habilite a exceção de Ollama em uma instância pública compartilhada sem avaliar a confiança nos usuários.
4. **Cadastro e IA:** rate limiting local ao processo não é um controle distribuído entre réplicas. Quotas por usuário e limites no proxy podem ser necessários conforme o volume de publicação.
5. **Frontend:** os dois pontos de `v-html` encontrados, chat e documentação, usam DOMPurify. Não foi identificada interpolação SQL direta de entradas nos repositórios revisados; isso não representa uma prova de ausência de XSS ou SQL injection em todos os fluxos possíveis.
6. **Pipeline:** os arquivos foram preparados e validados localmente. A execução remota e a configuração de checks obrigatórios dependem de publicar as alterações e das opções do repositório. Alertas CodeQL precisam de regras de code scanning para bloquear merges por severidade.

## Como reproduzir as verificações

Resultados locais ao concluir a revisão:

| Verificação | Resultado |
| --- | --- |
| npm audit da raiz | Zero vulnerabilidades conhecidas reportadas |
| npm audit do frontend | Zero vulnerabilidades conhecidas após a atualização, contra quatro alertas moderados antes |
| NuGet Audit transitivo da API e testes | Restauração com os avisos NU1900–NU1905 tratados como erro passou |
| Testes .NET em Release | 74 aprovados, zero falhas |
| Testes do carregamento de ambiente | 3 aprovados |
| Build Vue / TypeScript / PWA | Aprovado |
| Gitleaks no histórico | Cinco commits verificados, nenhuma detecção |
| Gitleaks nas alterações locais | Snapshot de 198 arquivos públicos/versionáveis, nenhuma detecção; arquivos privados ignorados não foram copiados |
| actionlint | Ambos os workflows aprovados |
| git diff --check | Sem erros de whitespace |

Os resultados são pontuais, dependem das bases de avisos disponíveis e não cobrem a execução remota do CodeQL ou a infraestrutura publicada.

Consulte [SECURITY.md](../SECURITY.md) para os comandos, o workflow e as exigências de configuração. Os testes de regressão de segurança ficam em `TaskDev-Back/TaskDev.Tests/SecurityTests.cs` e verificam CSRF, validade e papel da sessão, rate limiting nas rotas, tipos de endereço e configuração do transporte de IA.

Fontes oficiais para a pipeline: [auditoria NuGet](https://learn.microsoft.com/en-us/nuget/concepts/auditing-packages), [CodeQL advanced setup](https://docs.github.com/en/code-security/how-tos/find-and-fix-code-vulnerabilities/configure-code-scanning/configuring-advanced-setup-for-code-scanning) e [Gitleaks](https://github.com/gitleaks/gitleaks).
