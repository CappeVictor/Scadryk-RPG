# Scadryk para Unity

`UnityPort` agora é um projeto Unity completo com demo top-down de três fases e nove salas conectadas, mapas procedurais com árvores, pedras, cristais, ruínas, água e objetos costeiros em sprites ilustrados, além de variações de obstáculos nas nove salas; inclui inimigos, guardiões, HUD, minimapa, combate, esquiva, pulso e pausa. A cena inicial já está salva em `Assets/Scenes/ScadrykDemo.unity`. A compilação de Windows x64 foi gerada no Unity Editor 6.6. Campanha completa, diálogos, missões, equipamentos, animações finais e saves da versão Windows Forms ainda não foram portados.

## Requisitos

- Unity 6.6 ou versão Unity 6 compatível.
- O projeto usa Input Manager (Old), já configurado em Project Settings. A folha de sprites ambientais está em `Assets/Resources/ScadrykArt/ScadrykEnvironment.png`.

## Abrir e jogar

1. Abra o Unity Hub e vá em **Projects**.
2. Escolha **Add > Add project from disk**.
3. Selecione a pasta `UnityPort` deste repositório, ou a pasta `ScadrykUnity` disponibilizada junto do projeto local.
4. Abra a cena `Assets/Scenes/ScadrykDemo.unity` e pressione **Play**.
5. Limpe os ecos de cada sala. Quando a passagem abrir, vá até o brilho verde à direita e pressione **E**. Complete as nove salas para concluir as três fases.

Para criar um executável Windows, escolha **Scadryk > Build Windows demo**. O build aparece em `Build/ScadrykDemo.exe` dentro do projeto Unity.

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
- `ScadrykDepthSort.cs`: ordenação de desenho por profundidade, para Nara passar à frente ou atrás das árvores e pedras.
- `Editor/ScadrykSceneBuilder.cs`: cria e registra a cena jogável pelo menu da Unity.
- `Editor/ScadrykArtImporter.cs`: prepara as texturas para corte dos quadros e dos sprites ambientais em tempo de execução.

O jogo original permanece no repositório como versão Windows Forms/GDI+. Os scripts dentro de `UnityPort/Assets` são a nova implementação Unity e foram excluídos da compilação Windows pelo `.csproj`.
