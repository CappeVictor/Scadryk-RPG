# Scadryk para Unity

Esta pasta contém uma demonstração jogável em Unity com três fases, três salas conectadas por fase, inimigos que perseguem e anunciam ataques, guardiões de fase, passagens entre salas, pausa, HUD e minimapa. O jogador e os inimigos usam spritesheets do projeto, importadas e cortadas automaticamente em quadros 4×4. Os controles usam aceleração e desaceleração curtas, direção de ataque independente do movimento, esquiva com recarga e um pulso de área. Os cenários são gerados por código para que a demonstração funcione sem configurar Tilemaps manualmente.

> A Unity ainda não foi usada para validar esta cena neste computador. Este é um recorte jogável para iniciar a migração. Campanha completa, diálogos, missões, equipamentos, animações e salvamento da versão Windows Forms não foram portados.

## Requisitos

- Unity 2022.3 LTS ou Unity 6 com template **2D Core**.
- Ative **Edit > Project Settings > Player > Active Input Handling > Input Manager (Old)** ou **Both**. Reinicie o Editor se ele pedir.

## Abrir e jogar

1. No Unity Hub, crie um projeto novo **2D Core** chamado `ScadrykUnity`.
2. Copie a pasta `UnityPort/Assets` deste repositório para dentro da pasta `Assets` do novo projeto, mesclando os conteúdos. Preserve `Assets/Editor`, `Assets/Scripts` e `Assets/Resources/ScadrykArt`.
3. Volte à Unity e aguarde a compilação dos scripts. No menu superior, escolha **Scadryk > Criar cena jogável**.
4. Abra `Assets/Scenes/ScadrykDemo.unity` e pressione **Play**.
5. Limpe os ecos de cada sala. Quando aparecer a mensagem de passagem aberta, vá até o brilho verde no lado direito e pressione **E**. Complete as nove salas para concluir as três fases da demonstração.

## Controles

| Tecla ou ação | Função |
| --- | --- |
| WASD ou setas | Mover em qualquer direção, com diagonal normalizada |
| Mouse | Mirar; a direção de ataque independe da direção do movimento |
| Clique esquerdo, Espaço ou J | Ataque corpo a corpo na direção da mira |
| Shift ou K | Esquiva rápida com breve invulnerabilidade e recarga |
| Q ou clique direito | Pulso que atinge ecos próximos; tem recarga |
| E perto da passagem | Seguir para a sala/fase seguinte após limpar a sala |
| Esc | Pausar/retomar |

## Progressão e mapa

As fases jogáveis incluídas são **Margens do Primeiro Eco**, **Bacia do Sal Imóvel** e **Costa das Badaladas**. Cada uma tem três salas, caminhos laterais, coberturas e um guardião na última sala. O mapa no canto superior direito mostra a posição atual e o estado das salas; o painel superior esquerdo mostra fase, sala, vida, inimigos restantes e recargas. A cada sala limpa, Nara recupera um pouco de vida. Ao zerar a vida, ela retorna ao início da sala atual.

## Como ampliar

- `ScadrykGameDirector.cs`: conteúdo das fases, desenho procedural dos mapas, nomes das salas e progressão.
- `ScadrykPlayerController.cs`: velocidade, resposta dos controles, ataque, esquiva e pulso.
- `ScadrykEnemyHealth.cs`: vida, perseguição e janela de aviso do ataque inimigo.
- `ScadrykCameraFollow.cs`: acompanhamento suave do jogador.
- `Editor/ScadrykSceneBuilder.cs`: cria e registra a cena jogável pelo menu da Unity.
- `Editor/ScadrykArtImporter.cs`: separa automaticamente as folhas 4×4 em sprites individuais.

O jogo original permanece no repositório como versão Windows Forms/GDI+. Os scripts dentro de `UnityPort/Assets` são a nova implementação Unity e foram excluídos da compilação Windows pelo `.csproj`.
