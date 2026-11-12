# Scadryk RPG

Protótipo jogável de RPG de ação com visão superior e fantasia sombria, feito em **Godot 4 .NET e C#**. O projeto está em evolução; arte e efeitos ainda são provisórios e desenhados pelo código.

## Conteúdo atual

- Três salas de combate com inimigos comuns, arqueiros, elite, conjurador e Guardião da Cripta.
- O elite usa um golpe de área anunciado por um marcador que encolhe durante a preparação, dando tempo para esquivar.
- Combate corpo a corpo, acertos críticos, esquiva e habilidade Redemoinho.
- Segunda fase do guardião abaixo de metade da vida, com ataques mais rápidos.
- Barra de vida com alerta visual em vida baixa e indicador explícito da fase enfurecida do guardião.
- Ouro, poções, experiência, níveis e escolhas de talentos.
- Armas com atributos e efeitos próprios, armadura e relíquia equipáveis.
- Itens deixados no chão, baú final com escolha de recompensa e salvamento local.

## Requisitos e execução

1. Instale a **Godot 4.3 .NET** (a edição com suporte a C#) e o **SDK .NET 8**.
2. Clone o repositório ou baixe e extraia o ZIP.
3. Na Godot .NET, importe `project.godot` e abra o projeto.
4. Compile pelo botão **Build/Compilar** e pressione **F6** ou **F5** para executar a cena/projeto.

## Controles

| Tecla | Ação |
| --- | --- |
| WASD ou setas | Mover |
| Espaço ou J | Atacar |
| Shift ou K | Esquivar |
| R | Redemoinho |
| Q | Usar poção |
| E | Abrir passagem ou interagir com o baú |
| I | Abrir/fechar inventário |
| 1–5 no inventário | Equipar arma, armadura ou relíquia |
| F5 | Salvar progresso |
| C na tela inicial | Carregar progresso salvo |
| 1, 2 ou 3 no baú final | Escolher ouro, poções ou recuperar a vida |

Recolha os itens que caem antes de sair da sala. Na última câmara, derrote os inimigos, aproxime-se do baú e pressione **E**. O progresso é salvo em `user://scadryk-save.cfg`.

## Validação

O projeto compila com Godot .NET 4.3 e SDK .NET 8. A compilação confirma o código C#, mas não substitui uma partida manual. Se quiser executar os testes automatizados incluídos no repositório, abra `Tests/SmokeTest.tscn` na Godot .NET; eles cobrem verificações básicas de combate, itens, limites da sala e salvamento.

## Arquivos principais

- `project.godot`: configurações do projeto.
- `Scadryk.csproj`: projeto C# e versão do SDK.
- `Main.tscn`: cena principal.
- `Game.cs`: lógica, interface e desenho do protótipo.

## Próximas etapas

Separar os sistemas em cenas e classes, substituir os gráficos provisórios por arte original, adicionar áudio e animações, ampliar salas e inimigos, e validar o ciclo completo de jogo na Godot. O desempenho deve ser otimizado a partir de medições reais.
