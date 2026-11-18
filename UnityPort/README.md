# Scadryk para Unity

`UnityPort` agora é um projeto Unity completo com demo top-down de três fases e nove salas conectadas. Cada fase tem cenário ilustrado próprio: bosque encantado, bacia de sal com poças e cristais, e costa rochosa com marés e ruínas de sinos. Cada ambiente também tem inimigos próprios, como o guardião de raízes, a criatura de sal e o espectro das badaladas. Árvores, juncos, cristais, água e lanternas têm movimento sutil. Os mapas mantêm obstáculos e rotas diferentes; o jogo inclui guardiões, HUD, minimapa, combate, esquiva, pulso e pausa. A câmera permanece top-down e acompanha Nara. A cena inicial está em `Assets/Scenes/ScadrykDemo.unity`. Campanha completa, diálogos, missões, equipamentos, animações finais e saves da versão Windows Forms ainda não foram portados.

## Requisitos

- Unity 6.6 ou versão Unity 6 compatível.
- O projeto usa Input Manager (Old), já configurado em Project Settings. Artes de cenário e inimigos estão em `Assets/Resources/ScadrykArt/`.

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

Os inimigos usam ataques próprios do bioma: raízes que avançam pelo chão e podem prender Nara por um instante, estilhaços de sal que são lançados à distância e ondas circulares de ressonância na costa. Cada ataque tem um aviso visual antes de causar dano; guardiões ampliam o alcance ou disparam salvas múltiplas. Inimigos comuns ficam próximos do tamanho de Nara, e guardiões são maiores.

## Como ampliar

- `ScadrykGameDirector.cs`: conteúdo das fases, desenho procedural dos mapas, nomes das salas e progressão.
- `ScadrykPlayerController.cs`: velocidade, resposta dos controles, ataque, esquiva e pulso.
- `ScadrykEnemyHealth.cs`: vida, perseguição e janela de aviso do ataque inimigo.
- `ScadrykCameraFollow.cs`: acompanhamento suave do jogador.
- `ScadrykDepthSort.cs`: ordenação de desenho por profundidade, para Nara passar à frente ou atrás das árvores e pedras.
- `ScadrykAmbientProp.cs`: movimento sutil para vegetação, cristais, água e lanternas.
- `Editor/ScadrykSceneBuilder.cs`: cria e registra a cena jogável pelo menu da Unity.
- `Editor/ScadrykArtImporter.cs`: prepara as texturas para corte dos quadros e dos sprites ambientais em tempo de execução.

O jogo original permanece no repositório como versão Windows Forms/GDI+. Os scripts dentro de `UnityPort/Assets` são a nova implementação Unity e foram excluídos da compilação Windows pelo `.csproj`.
