# Scadryk RPG

Protótipo jogável de RPG de ação com visão superior e fantasia sombria, feito em **Godot 4 .NET e C#**. O projeto está em evolução; arte e efeitos ainda são provisórios e desenhados pelo código.

## Conteúdo atual

- Três salas de combate com inimigos comuns, arqueiros, elite, conjurador e Guardião da Cripta.
- O elite usa um golpe de área anunciado por um marcador que encolhe durante a preparação, dando tempo para esquivar.
- O elite recebe uma afixação aleatória por partida: Frenético ataca mais rápido; Blindado suporta mais dano e aparece com um aro de proteção.
- Combate corpo a corpo, acertos críticos, esquiva e habilidade Redemoinho.
- Segunda fase do guardião abaixo de metade da vida, com ataques mais rápidos.
- Barra de vida com alerta visual em vida baixa e indicador explícito da fase enfurecida do guardião.
- Ouro, poções, experiência, níveis e escolhas de talentos.
- Armas com atributos e efeitos próprios, armadura e relíquia equipáveis.
- Ataques corpo a corpo são bloqueados por pilares; inimigos comuns se espalham para evitar que todos ocupem o mesmo ponto.
- Nomes de função acima de arqueiros, conjuradores e elites deixam os papéis do grupo mais fáceis de reconhecer.
- Cada nível ganho gera sua própria escolha de talento, mesmo quando vários níveis são alcançados na mesma luta.
- A conclusão ou derrota mostra um resumo da tentativa: tempo, inimigos derrotados, críticos, poções usadas e dano recebido.
- Poções mostram o efeito imediato; tentar usar sem estoque também apresenta uma mensagem.
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
| Clique esquerdo | Atacar mirando na posição do cursor |
| Shift ou K | Esquivar |
| R ou clique direito | Redemoinho |
| Q | Usar poção |
| E | Abrir passagem ou interagir com o baú |
| I | Abrir/fechar inventário |
| 1–5 no inventário | Equipar arma, armadura ou relíquia |
| F5 | Salvar progresso |
| C na tela inicial | Carregar progresso salvo |
| Tab | Selecionar/ciclar alvo; ataques se voltam ao alvo marcado |
| 1, 2 ou 3 no baú final | Escolher ouro, poções ou recuperar a vida |

Recolha os itens que caem antes de sair da sala. Na última câmara, derrote os inimigos, aproxime-se do baú e pressione **E**. O progresso e as estatísticas da tentativa são salvos em `user://scadryk-save.cfg`.

## Validação

O projeto compila com Godot .NET 4.3 e SDK .NET 8. A compilação confirma o código C#, mas não substitui uma partida manual. Se quiser executar os testes automatizados incluídos no repositório, abra `Tests/SmokeTest.tscn` na Godot .NET; eles cobrem verificações básicas de combate, itens, limites da sala e salvamento.

## Arquivos principais

- `project.godot`: configurações do projeto.
- `Scadryk.csproj`: projeto C# e versão do SDK.
- `Main.tscn`: cena principal.
- `Game.cs`: lógica, interface e desenho do protótipo.

## Próximas etapas

Separar os sistemas em cenas e classes, substituir os gráficos provisórios por arte original, adicionar áudio e animações, ampliar salas e inimigos, e validar o ciclo completo de jogo na Godot. Antes de aumentar muito o escopo, preparar uma demonstração curta e colher feedback de jogadores para medir clareza, diversão e interesse. O desempenho deve ser otimizado a partir de medições reais.

## Referências de design consultadas

- [Diablo IV: leitura visual dos efeitos e ataques](https://news.blizzard.com/en-gb/article/23746639/diablo-iv-quarterly-updatedecember-2021): os sinais de combate devem continuar claros em meio aos efeitos.
- [Diablo IV: ajustes de masmorras e redução de retrocesso](https://news.blizzard.com/en-us/article/24009151/descend-into-the-diablo-iv-developer-update-livestream): objetivos e rotas mais diretos ajudam a manter o ritmo da exploração.
- [Diablo IV: identidade e comportamento dos inimigos](https://news.blizzard.com/en-us/article/24243142/sanctuary-ignites-with-itemization-systems-changes): cada tipo de inimigo ganha um papel reconhecível.
- [Diablo IV: afixações de monstros](https://news.blizzard.com/en-us/article/23189677/diablo-iv-feature-overview): modificadores devem alterar a estratégia contra o inimigo, não apenas seus números; isso inspirou as variantes do elite.
- [Last Epoch: melhorias de qualidade de vida e filtros de loot](https://forum.lastepoch.com/t/harbingers-of-ruin-quality-of-life-more/71756): informação de equipamento e filtros claros são boas referências para uma futura expansão do inventário.

Essas referências orientam princípios gerais; Scadryk mantém personagens, mundo, itens e apresentação próprios.
