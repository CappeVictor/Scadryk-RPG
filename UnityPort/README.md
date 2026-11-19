# Scadryk para Unity

> Direção criativa, diagnóstico e etapas de desenvolvimento: [DESIGN.md](DESIGN.md). A etapa de fundação está em andamento; esta versão ainda não atende a todos os critérios de produto completo.

Critérios de comparação com jogos de referência e lacunas verificáveis: [QUALITY_BENCHMARK.md](QUALITY_BENCHMARK.md).

Registro de builds, teste de progressão e roteiro de avaliação com jogadores: [QA.md](QA.md).

`UnityPort` contém uma aventura top-down em desenvolvimento com três biomas e nove salas conectadas, guardiões, três poderes, Arauto do Vazio, objetivos de exploração e narrativa ambiental. Cada bioma possui cenário e inimigos próprios. A cena inicial está em `Assets/Scenes/ScadrykDemo.unity`. Há menu, pausa, opções, áudio, remapeamento, suporte a controle e salvamento local. O progresso e as limitações de validação estão em `QA.md`; a build normal mais recente validada é Stage9. A alteração de cobertura de cenário ainda aguarda build normal devido a bloqueio do Unity pelo App Control do Windows.

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

O avanço é salvo automaticamente ao atravessar cada portal. Ao reabrir o jogo, a última sala alcançada é carregada com os poderes das fases já vencidas. A conclusão da campanha apaga o marcador de progresso para permitir uma nova jornada. As métricas ficam na pasta retornada por `Application.persistentDataPath` (no Windows, normalmente `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>`). Nenhum dado é enviado pela rede.

O registro atual usa `scadryk-sessions-v2.csv` e acrescenta `unclear_attacks`. Durante um teste, pressione **F8** quando sofrer ou observar um ataque difícil de perceber; a marca é contada na sala atual. O arquivo anterior `scadryk-sessions.csv` permanece apenas como histórico. Para comparar duas builds, jogue cada sala até o portal e examine tempo, mortes, marcas e uso de poderes antes de alterar o balanceamento.

Selos circulares ocultos respondem a um poder específico: Crescente na entrada da bacia, Estouro na entrada da costa e Rompante na sala do Arauto. Use o poder perto do selo para libertar um Eco e recuperar vida. Estes encontros são opcionais.

As salas têm ritmos diferentes: entradas do bosque e as travessias trazem combate; a comporta do bosque abre com **E** depois da luta; as entradas da bacia e da costa são momentos calmos com vestígios para escutar; as travessias de sal e de sinos exigem fazer dois objetos ressoarem com **Q**; os guardiões encerram cada bioma. O HUD mostra o objetivo e a contagem dos objetos ativados. Após um período sem progresso, uma pista aparece na parte inferior da tela.

## Menu, opções e áudio

A tela inicial oferece **Continuar jornada** (último portal salvo) e **Nova jornada** (reinicia sala e poderes). **Esc** ou **Start** pausam; **O** abre opções na tela inicial ou na pausa. O menu de opções ajusta música, efeitos, escala de texto (100–150%), redução de flashes e desligamento do tremor de câmera. Na aba **Teclas**, clique na ação e pressione a substituta para remapear ataque, esquiva, pulso, três poderes, interação ou as quatro direções de movimento. Se a tecla já estiver em uso, as duas ações trocam de tecla. As preferências são salvas localmente em `PlayerPrefs`.

A abertura de uma nova jornada apresenta o chamado de Nara. Vestígios do sal e da costa revelam a história da maré; cada guardião libertado acrescenta uma lembrança. Ao derrotar o Arauto, uma última memória explica o silêncio e o encerramento resolve a escolha de Nara. As legendas narrativas aparecem num painel durante o jogo.

Há trilhas ambientes sintetizadas para os três biomas e para o confronto final, além de sinais sonoros originais para ações de Nara, avisos, impactos, dano e portais. Os avisos visuais continuam presentes quando o volume está em zero. O mixer aplica os ajustes de volume imediatamente.

O controle usa analógico esquerdo para movimento e direito para mira; A ataca, B esquiva, X usa o pulso, Y interage, LB/RB/Back ativam os três poderes e Start pausa. A correspondência dos botões e eixos pode variar conforme o controle e o driver do Windows; essa etapa ainda precisa de teste com dispositivos físicos variados.

## Controles

| Tecla ou ação | Função |
| --- | --- |
| WASD (configurável para setas ou outras teclas) | Mover em qualquer direção, com diagonal normalizada |
| Mouse | Mirar; a direção de ataque independe da direção do movimento |
| Clique esquerdo ou Espaço | Ataque corpo a corpo na direção da mira |
| Shift esquerdo | Esquiva rápida com breve invulnerabilidade e recarga |
| Q ou clique direito | Pulso que atinge ecos próximos; tem recarga |
| 1 | Crescente Verdejante — golpe frontal amplo, aprendido após o guardião da primeira fase |
| 2 | Estouro de Sal — explosão circular de cristais, aprendido após o guardião da segunda fase |
| 3 | Rompante da Maré — onda frontal de longo alcance, aprendido após o guardião da terceira fase |
| E perto da passagem | Seguir para a sala/fase seguinte após limpar a sala |
| Esc | Pausar/retomar |

## Progressão e mapa

As fases jogáveis incluídas são **Margens do Primeiro Eco**, **Bacia do Sal Imóvel** e **Costa das Badaladas**. Cada uma tem três salas, caminhos laterais, coberturas e um guardião na última sala. O mapa no canto superior direito mostra a posição atual e o estado das salas; o painel superior esquerdo mostra fase, sala, vida, inimigos restantes, recargas e os poderes aprendidos. A cada sala limpa, Nara recupera um pouco de vida. Ao zerar a vida, ela retorna ao início da sala atual.

Ao limpar qualquer sala, um portal ilustrado se abre para levar Nara ao próximo nível. Cada guardião concede um poder com sprite próprio: **Crescente Verdejante**, **Estouro de Sal** e **Rompante da Maré**. Após o terceiro guardião, o **Arauto do Vazio** surge como superchefe, com 1.250 pontos de vida, barra de chefe própria e três padrões de ataque: salva de estrelas, chuva de impacto marcada no chão e uma onda de vazio que pode ser evitada com esquiva. Ao derrotá-lo, um portal colossal violeta aparece. Aproxime-se e pressione **E** para ver Nara entrar, atravessar a tela escurecida e emergir pelo portal de chegada; usar o portal final conclui a jornada.

Os três poderes têm funções complementares: o **Crescente** interrompe e retarda um Eco (chefes resistem por menos tempo); o **Estouro** deixa os alvos expostos por alguns segundos, aumentando em 30% o dano dos golpes seguintes; o **Rompante** empurra Ecos comuns. Uma sequência útil é Estouro para expor, Crescente para interromper e Rompante para afastar o grupo.

O Arauto agora muda de fase em 70% e 35% da vida: primeiro retoma as raízes, depois os estilhaços do sal e por fim reúne esses ataques à onda da costa. A barra marca os dois limiares. A linha ou círculo no chão precede o dano; a onda final mostra também um anel interno seguro junto ao chefe.

Os inimigos usam ataques próprios do bioma: raízes que avançam pelo chão e podem prender Nara por um instante, estilhaços de sal que são lançados à distância e ondas circulares de ressonância na costa. Esses ataques usam sprites ilustrados próprios, além do aviso visual antes de causar dano; guardiões ampliam o alcance ou disparam salvas múltiplas. Inimigos comuns ficam próximos do tamanho de Nara, e guardiões são maiores.

Os Ecos comuns agora têm quatro papéis distinguíveis pela escala e pelo tom: **Caçador** aproxima-se e ataca em linha; **Atirador** mantém distância e lança um projétil do bioma; **Ressonador** anuncia uma área circular; **Tecelão** anuncia um pulso verde e cura aliados próximos. O círculo de aviso do Ressonador e do guardião da costa corresponde ao raio efetivo de dano.

Nara também tem sprites para o golpe básico de espada, o pulso especial expansivo e os três poderes conquistados. Os projéteis dos inimigos de sal e do superchefe usam sprites próprios.

As coberturas quadradas foram substituídas por troncos caídos, tocos, cristais e rochas, distribuídos de forma diferente pelas salas e com colisores ajustados ao formato. A caminhada agora acompanha a velocidade, a esquiva deixa rastros, os ataques têm deslocamento e expansão, e os personagens reagem ao dano e caem antes de desaparecer ou retornar à sala. O jogo dá um breve período de proteção após Nara receber dano para evitar que salvas de projéteis a eliminem de uma vez. Barras de vida flutuam acima de Nara, dos ecos e dos guardiões.

Os obstáculos sólidos também funcionam como **cobertura**: estilhaços inimigos param ao atingir troncos, rochas ou ruínas; o golpe básico de Nara e a investida de raízes não causam dano através deles. Ataques circulares mágicos continuam a atingir através da cobertura, criando uma decisão entre proteger-se de linhas e sair de áreas anunciadas.

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

