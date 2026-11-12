# Scadryk RPG

Protótipo de RPG de ação com visão de cima, fantasia sombria e exploração de masmorras. A base usa **Godot 4 e C#** e foi pensada para crescer aos poucos.

> **Estado atual:** protótipo inicial. A versão em C# compila e possui verificações automatizadas básicas na Godot .NET; ainda precisa de testes completos de jogabilidade. A arte é provisória e desenhada por código.

## O que existe na base

- Três salas em sequência, com porta liberada após derrotar os inimigos.
- Movimento em oito direções, ataque corpo a corpo, esquiva e poções.
- Inimigos corpo a corpo, arqueiros, um conjurador, um elite e um guardião com ataques anunciados.
- Vida, experiência, níveis, escolhas de talento, ouro, armas, armadura e recompensa final.
- Ouro e poções coletáveis ao se aproximar dos itens deixados por inimigos.
- Salvamento manual de atributos, ouro, poções, arma e sala atual. Salve após eliminar os inimigos; ao carregar um salvamento novo, a sala permanece limpa.
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
| Enter ou Espaço | Começar ou reiniciar |
| R | Redemoinho (dano em área) |
| I | Abrir ou fechar inventário |
| 1, 2 ou 3 no inventário | Equipar uma arma encontrada |
| 4 no inventário | Equipar ou desequipar o peitoral |
| 5 no inventário | Equipar ou desequipar a relíquia |
| 1, 2 ou 3 ao subir de nível | Escolher um talento |
| F5 após limpar uma sala ou vencer | Salvar progresso |
| C na tela inicial | Carregar progresso |

Derrote todos os inimigos, recolha os itens no chão aproximando-se deles, aproxime-se da porta à direita e pressione **E**. Na última sala, vença o guardião, aproxime-se do baú e pressione **E** para concluir.

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

## Direção do projeto

Scadryk busca a atmosfera sombria, o combate de ação e a busca por equipamentos presentes nos RPGs de ação modernos, incluindo Diablo como referência. A identidade, os personagens, os cenários e os itens serão próprios. A meta é evoluir por etapas: primeiro validar combate e progressão, depois criar classes, loot variado, habilidades e masmorras mais ricas.

Ao limpar as duas primeiras salas, novas armas aparecem no chão e são recolhidas ao se aproximar. Pressione I para abrir o inventário e 1, 2 ou 3 para equipar uma arma encontrada. A lâmina do crepúsculo ataca mais rápido; a lâmina rara oferece mais dano. O jogo pausa enquanto o inventário está aberto. O salvamento é manual e fica disponível após limpar uma sala ou concluir a masmorra. Salvamentos antigos, criados antes dessa regra, ainda reiniciam os inimigos ao carregar. O inventário inicial contém três armas e um peitoral de pedra e uma relíquia que cura 5 pontos de vida por abate. Ataques básicos têm 12% de chance de crítico. O conjurador anuncia uma área antes de causar dano. O jogador pode escolher Vigor (+20 vida máxima), Domínio (Redemoinho recarrega mais rápido) ou Agilidade (esquiva recarrega mais rápido) ao subir de nível. Pilares bloqueiam o movimento do jogador e dos inimigos e interrompem projéteis. O elite da segunda sala pode deixar o peitoral; equipado, reduz em 30% o dano recebido.

Recolha todos os itens da sala antes de salvar ou passar pela porta, para que nenhuma recompensa seja perdida.

## Testes automatizados básicos

Compile o projeto e execute a cena `Tests/SmokeTest.tscn` na Godot .NET. Ela verifica ataque, recarga da habilidade, coleta de armas e ouro, limites da sala e salvamento. Os testes usam um arquivo de salvamento separado do jogo. Esses testes não substituem uma partida completa.
