# Scadryk RPG

Protótipo de RPG de ação com visão de cima, fantasia sombria e exploração de masmorras. A base usa **Godot 4 e C#** e foi pensada para crescer aos poucos.

> **Estado atual:** protótipo inicial. A versão em C# compila, mas ainda precisa ser testada jogando na Godot .NET. A arte é provisória e desenhada por código.

## O que existe na base

- Três salas em sequência, com porta liberada após derrotar os inimigos.
- Movimento em oito direções, ataque corpo a corpo, esquiva e poções.
- Inimigos que perseguem o jogador e um guardião na última sala.
- Vida, experiência, níveis, ouro, uma arma rara e um baú de recompensa.\n- Salvamento manual de atributos, ouro, poções, arma e sala atual. Ao carregar, a sala é reiniciada.
- Tela inicial, derrota e vitória.

## Como executar

1. Baixe a **Godot 4 com suporte a .NET** em [godotengine.org/download](https://godotengine.org/download/). A edição padrão não executa scripts C#.
2. Instale o [SDK .NET 8](https://dotnet.microsoft.com/download/dotnet/8.0). O projeto usa `net8.0` e `Godot.NET.Sdk/4.3.0`.
3. Baixe este repositório pelo botão **Code → Download ZIP** e extraia o arquivo, ou clone o repositório.
4. Abra a Godot .NET, clique em **Importar** e selecione o arquivo `project.godot`.
5. Abra o projeto, clique em **Build/Compilar** e pressione **F5** para executar.

Se ocorrer um erro de compilação, confira se instalou o **SDK** .NET, reinicie a Godot e verifique a mensagem na aba de erros. A compilação pode precisar de acesso ao NuGet para restaurar as dependências.

## Controles

| Tecla | Ação |
| --- | --- |
| WASD ou setas | Mover |
| Espaço ou J | Atacar |
| Shift ou K | Esquivar |
| Q | Usar poção |
| E | Interagir com a porta ou o baú |
| Enter ou Espaço | Começar ou reiniciar |\n| I | Ver arma e dano |\n| F5 durante o jogo | Salvar progresso |\n| C na tela inicial | Carregar progresso |

Derrote todos os inimigos, aproxime-se da porta à direita e pressione **E**. Na última sala, vença o guardião, aproxime-se do baú e pressione **E** para concluir.

## Arquivos principais

- `project.godot`: configurações do projeto.
- `Scadryk.csproj`: projeto C# e versão do SDK.
- `Main.tscn`: cena principal.
- `Game.cs`: lógica, interface e desenhos do protótipo.

## Próximas melhorias sugeridas

1. **Validar a base:** compilar na Godot .NET, testar o ciclo completo e corrigir problemas encontrados.
2. **Organizar o projeto:** separar jogador, inimigos, combate, salas e interface em cenas e classes reutilizáveis.
3. **Melhorar o combate:** criar animações, efeitos de impacto, padrões próprios para o guardião e ajustar dificuldade.
4. **Adicionar equipamentos e inventário:** começar com poucos itens, raridades e comparação de atributos.
5. **Persistir o progresso:** salvar personagem, equipamentos e configurações.
6. **Expandir as masmorras:** acrescentar salas, armadilhas e, após validar o conteúdo manual, geração procedural.

Otimizações devem ser guiadas por medições de desempenho, começando pelos pontos que realmente afetarem a experiência.
\n## Direção do projeto\n\nScadryk busca a atmosfera sombria, o combate de ação e a busca por equipamentos presentes nos RPGs de ação modernos, incluindo Diablo como referência. A identidade, os personagens, os cenários e os itens serão próprios. A meta é evoluir por etapas: primeiro validar combate e progressão, depois criar classes, loot variado, habilidades e masmorras mais ricas.\n\nA arma rara é equipada automaticamente ao sair da segunda sala; I mostra a arma atual. O salvamento é manual e reinicia os inimigos da sala ao carregar. Não há inventário completo ainda.\n