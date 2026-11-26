## Perfil e patente

Em **Configurações → Perfil & Patente**, consulte nome, sobrenome, usuário, papel de acesso, nível e XP acumulado. Essa área apresenta os dados da conta; não há formulário de salvamento de perfil na versão atual.

## Aparência e visualização

A aba **Aparência** apresenta as opções Escuro, Claro e Sistema, além da visualização padrão em lista ou quadro. As preferências são armazenadas no navegador, não sincronizadas entre dispositivos.

> O design atual usa uma paleta escura fixa em vários componentes. A seleção de Claro/Sistema é persistida, mas não representa ainda um tema claro completo para todas as telas.

![Preferências de aparência e visualização padrão](/docs/screens/aparencia.png)

## Integração de IA

Cadastre e teste a conexão em **Integração de IA**. Uma chave mascarada indica que uma credencial já foi salva; não é necessário publicar ou copiar essa chave em capturas. Confira o [guia do assistente](/docs/assistente-ia).

## Senha e recuperação de acesso

Se perder a senha, solicite a redefinição a um administrador ou operador da instância. O painel administrativo gera uma nova senha e retorna o valor para quem executou a ação. Guarde a credencial fornecida e use-a para entrar novamente. Não há fluxo de recuperação por e-mail ou formulário de troca de senha pelo próprio usuário na interface atual.

![Orientações da tela de segurança e recuperação de senha](/docs/screens/seguranca.png)

## Papéis de acesso

| Papel | Acesso administrativo |
| --- | --- |
| MEMBER | Usa os espaços aos quais tem acesso |
| OPERATOR | Pode listar usuários e redefinir senhas, sujeito às regras do domínio |
| ADMIN | Também pode alterar papéis e ativar ou desativar usuários |

O menu **Administração** aparece para administradores e operadores. O backend continua validando cada operação: visualizar um botão não substitui a autorização da API. A propriedade de um espaço tem suas próprias regras.

## Sair da conta

Use **Sair da conta** no menu para encerrar a sessão. A documentação permanece pública mesmo após sair. Em um computador compartilhado, encerre sua sessão quando terminar.
