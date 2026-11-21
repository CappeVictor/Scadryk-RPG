# Scadryk para Unity

> Direção criativa, diagnóstico e etapas de desenvolvimento: [DESIGN.md](DESIGN.md). A etapa de fundação está em andamento; esta versão ainda não atende a todos os critérios de produto completo.

Critérios de comparação com jogos de referência e lacunas verificáveis: [QUALITY_BENCHMARK.md](QUALITY_BENCHMARK.md).

Registro de builds, teste de progressão e roteiro de avaliação com jogadores: [QA.md](QA.md).

`UnityPort` contém uma aventura top-down em desenvolvimento com três biomas e nove salas conectadas, guardiões, três poderes, Arauto do Vazio, objetivos de exploração e narrativa ambiental. Cada bioma possui cenário e inimigos próprios. A tela inicial usa arte original que conecta o bosque alagado, a bacia de sal e a costa das badaladas. A cena inicial está em `Assets/Scenes/ScadrykDemo.unity`. Há menu, pausa, opções, áudio, remapeamento, suporte a controle e salvamento local. A pausa congela o combate e os avisos temporizados. Os avisos de projéteis mostram cada trajetória do Arauto e limitam o alcance do disparo à faixa anunciada, incluindo o raio da hitbox. O Editor Unity 6000.6.9f1 está instalado; os 17 scripts também passaram em compilação C# isolada contra as DLLs do Editor, com 0 erros e 0 avisos. A validação pelo próprio Editor e uma build normal atual ainda aguardam ativação da licença Unity no Hub; o módulo Windows Build Support também precisa concluir sua instalação. O jogo não foi aberto nesta máquina. O progresso e as limitações de validação estão em `QA.md`. O CI da raiz compila o projeto .NET original, não valida este port Unity.

## Requisitos

- Unity Editor `6000.6.9f1` (ver `ProjectSettings/ProjectVersion.txt`) e o módulo Windows Build Support para gerar o executável Windows.
- Para builds Windows com backend IL2CPP, o Unity também exige Visual Studio C++ Build Tools e Windows SDK 10.0.19041 ou superior. São ferramentas de compilação; o código continua sendo editado no VS Code, sem precisar abrir o IDE Visual Studio.
- O projeto usa Input Manager (Old), já configurado em Project Settings. Artes de cenário e inimigos estão em `Assets/Resources/ScadrykArt/`.

## Programar em C# com VS Code

O VS Code é o editor de código do projeto; não é necessário instalar o Visual Studio. O Unity Editor continua necessário para importar scripts, compilar a cena, jogar e gerar a build.

1. Instale a extensão [Unity for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=visualstudiotoolsforunity.vstuc), publicada pela Microsoft. Ela instala as dependências de C# e o C# Dev Kit; siga o aviso da extensão para instalar o runtime .NET exigido, se necessário.
2. No Unity Hub, entre na sua conta Unity e ative uma licença Personal gratuita em **Licenses > Add license > Get a free personal license** (ou ative a licença válida da sua organização). Sem uma licença ativa, o Editor encerra em batch mode e não compila o projeto.
3. Abra o projeto `UnityPort` no Unity Editor e confirme no Package Manager que **Visual Studio Editor** está na versão 2.0.20 ou superior. O projeto usa a versão 2.0.26 em `Packages/manifest.json`.
4. No Unity, abra **Edit > Preferences > External Tools**, selecione **Visual Studio Code** em **External Script Editor**, ative **Generate all .csproj files** e use **Regenerate project files**.
5. No VS Code, abra a pasta `UnityPort`. O workspace recomenda a extensão Unity e C# Dev Kit; aceite a instalação sugerida se ainda não estiverem presentes. Edite os scripts em `Assets/Scripts`; deixe o Unity Editor aberto para recompilar e conferir alterações.

Não instale o pacote Unity antigo `com.unity.ide.vscode`: ele foi descontinuado. O fluxo atual usa a extensão Unity do VS Code e o pacote **Visual Studio Editor** já declarado no projeto.

## Abrir e jogar

1. Instale/abra o Unity Hub e instale o Editor `6000.6.9f1` com o módulo Windows Build Support.
2. Em **Projects**, escolha **Add > Add project from disk** e selecione a pasta `UnityPort` deste repositório (a pasta que contém `Assets`, `Packages` e `ProjectSettings`).
3. Abra `Assets/Scenes/ScadrykDemo.unity` e pressione **Play** no Editor.
4. Comece pelo menu. Limpe os Ecos e conclua o objetivo indicado em cada sala; quando o portal surgir, aproxime-se e use o comando de interação mostrado na tela (padrão **E** no teclado ou **Y** no controle). Complete as nove salas para concluir os três biomas e enfrentar o Arauto.

Para criar um executável Windows, escolha **Scadryk > Build Windows demo**. O build aparece em `Build/ScadrykDemo.exe` dentro do projeto Unity. Essa etapa ainda aguarda nova execução no Editor para validar os commits atuais.

Para importar e compilar os scripts sem gerar ou abrir o jogo, execute no PowerShell `.\Tools\compile-unity.ps1`. Esse passo não requer Windows Build Support. O script procura o Editor `6000.6.9f1` no destino configurado `C:\Outros Arquivos\Unity\Editors` e depois no caminho padrão do Unity Hub; para outro local, informe `-UnityEditorPath 'C:\caminho\para\Unity.exe'`.

Para gerar o executável Windows sem abrir a cena em Play Mode, execute `.\Tools\build-windows.ps1` na pasta `UnityPort`. Esse segundo passo requer o módulo Windows Build Support; procura o Editor nos mesmos caminhos e confirma se `Build/ScadrykDemo.exe` foi gerado. Ambos gravam logs em `%TEMP%`; nenhum inicia o executável. A compilação não substitui a avaliação manual da campanha.

O avanço é salvo automaticamente ao atravessar cada portal. Ao reabrir o jogo, a última sala alcançada é carregada com os poderes das fases já vencidas. A conclusão da campanha apaga o marcador de progresso para permitir uma nova jornada. As métricas ficam na pasta retornada por `Application.persistentDataPath` (no Windows, normalmente `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>`). Nenhum dado é enviado pela rede.

O registro atual usa `scadryk-sessions-v5.csv`. Cada linha inclui um `session_id` aleatório compartilhado pelas salas gravadas na mesma execução do jogo, além de `unclear_attacks`, `completed` e `input_mode` (último dispositivo usado na sala: `keyboard_mouse`, `gamepad` ou `unknown`). O ID também aparece como `SCADRYK_SESSION` no log para associar a ficha de avaliação à campanha sem registrar nome ou conta. Se a pessoa alternar dispositivos, `input_mode` mostra somente o último detectado. Durante um teste, pressione **F8** quando sofrer ou observar um ataque difícil de perceber; a marca é contada na sala atual. F8 fica reservado para esse registro e não pode ser remapeado. Os arquivos `scadryk-sessions.csv` e versões `v2`, `v3` e `v4` permanecem como histórico. A sessão é gravada ao trocar de sala, concluir a campanha ou sair do jogo; o cronômetro começa ao sair do menu e conta apenas jogo ativo, sem pausas nem travessias. Para comparar duas builds, jogue cada sala até o portal e examine tempo, mortes, marcas e uso de poderes antes de alterar o balanceamento.

Para registrar avaliações de pessoas iniciantes, intermediárias e experientes, use a [ficha de teste de campanha](PLAYTEST.md). Ela combina os dados automáticos do CSV com clareza de objetivos, justiça percebida, compreensão da história e comentários por sala; não substitua observações reais por resultados estimados.

Selos de Eco ocupam desvios distintos na entrada da bacia, na chegada à costa e na arena final. O brilho pulsante denuncia cada um; ao se aproximar, Nara recebe uma pista contextual. Se ainda precisar vencer o guardião local, o jogo explica isso antes de indicar o poder e o comando disponíveis. Use Crescente, Estouro ou Rompante junto ao selo correspondente para libertar uma memória: a silhueta de um Eco sobe do selo, toca um som próprio e revela um fragmento de história enquanto Nara recupera vida. Os três encontros são opcionais.

As salas têm ritmos diferentes: entradas do bosque e as travessias trazem combate; a comporta do bosque abre com o comando de interação configurado depois da luta (padrão **E** no teclado ou **Y** no controle); as entradas da bacia e da costa são momentos calmos com vestígios para escutar. Na travessia de sal, ressoe dois cristais com o Pulso (padrão **Q** ou clique direito; no controle, **X**). Na costa, faça o farol responder antes do sino do recife; os guardiões encerram cada bioma. O HUD mostra o objetivo e a contagem dos objetos ativados. O minimapa marca Nara com um quadrado dourado e usa um losango ciano contornado para o Eco vivo mais próximo, o próximo sinal ou o portal aberto. Quando esse alvo está fora da visão da câmera, um aviso ciano aponta a direção; ele cede espaço às falas e avisos temporários. Após um período sem progresso ativo, surgem pistas graduais específicas para a sala; na costa, elas explicam a ordem e a posição do farol e do sino do recife. Essa navegação ainda aguarda inspeção visual no Unity Editor.

## Menu, opções e áudio

A tela inicial oferece **Continuar jornada** (último portal salvo) e **Nova jornada**. Se já houver checkpoint, uma confirmação protege o progresso; cancelar mantém o save. **Esc** ou **Start** pausam; perder o foco da janela também pausa o jogo ativo, e ele permanece pausado ao voltar. **M** abre o mapa completo dos três biomas durante a exploração; no controle, use **L3** ou escolha **Mapa da jornada** no menu de pausa. O mapa pausa o combate e marca salas concluídas, sala atual, percurso adiante e objetivo ativo. HUD e mapa atualizam o próximo passo quando você derrota Ecos, completa sinais e abre a saída. **O** abre opções na tela inicial ou na pausa. O menu ajusta música, efeitos, escala do texto (100–150%), redução de flashes, tremor de câmera (corta o tremor ativo imediatamente) e alto contraste para avisos de perigo. Na aba **Teclas**, remapeie ataque, esquiva, pulso, poderes, interação e direções; teclas repetidas são trocadas e **Padrões** restaura o teclado. Os atalhos **Esc**, **F8** e **M** ficam reservados para navegação, QA e mapa. Na aba **Controle**, associe botões aos comandos de combate; botões repetidos também são trocados e **Padrões** restaura o controle. **Start** pausa e **L3** abre o mapa, então não podem ser remapeados. Enquanto escolhe um vínculo, **Esc**/**Start** cancela só a captura; pressione de novo para sair das opções. O analógico navega, A confirma e B retorna. HUD, pistas e mensagens de interação mostram o vínculo do dispositivo usado por último; teclas numéricas, teclado numérico, espaço, modificadoras e setas aparecem com rótulos legíveis. Preferências ficam em `PlayerPrefs`. Esta navegação/remapeação foi implementada na fonte, mas ainda aguarda compilação no Unity e teste com controle físico.

A primeira jornada e cada nova jornada começam com uma abertura narrativa própria. O combate e o cronômetro ficam pausados até você escolher **Começar a jornada** ou confirmar com **Enter**, **Espaço**, **A** ou o comando de interação configurado. Título e parágrafo se ajustam ao espaço disponível em telas estreitas; assim, os avisos de perigo não competem com a leitura da história. Nas chegadas tranquilas à bacia e à costa, Nima — uma voz devolvida pelas raízes — reaparece depois dos primeiros guardiões e conta como suas lembranças mudam com o sal e os sinos. Aproxime-se e use o comando de interação configurado nas opções (padrão **E** no teclado ou **Y** no controle); a indicação junto dela acompanha o dispositivo usado. Vestígios do sal e da costa ampliam a história da maré. Ao derrotar o Arauto, uma última memória explica o silêncio e o encerramento resolve a escolha de Nara. As legendas narrativas aparecem num painel durante o jogo.

Há trilhas ambientes sintetizadas para os três biomas e para o confronto final; as mudanças entre elas fazem uma transição cruzada de 1,25 s. Também há sinais sonoros originais para ações de Nara, avisos, impactos, dano, portais e para as memórias que Nima compartilha. Os avisos visuais continuam presentes quando o volume está em zero. O mixer aplica os ajustes de volume imediatamente.

O controle usa analógico esquerdo para movimento e direito para mira; A ataca, B esquiva, X usa o pulso, Y interage, LB/RB/Back ativam os três poderes, Start pausa e L3 abre o mapa. Antes de usar o analógico direito, a mira acompanha o movimento; depois que o jogador mira com ele, a direção fica independente e é conservada ao soltá-lo. Assim, atacar pelo controle não depende do cursor do mouse e mantém a mira twin-stick. A correspondência dos botões e eixos pode variar conforme o controle e o driver do Windows; essa etapa ainda precisa de teste com dispositivos físicos variados.

## Controles

A tabela mostra os comandos padrão; ataque, esquiva, Pulso, poderes, interação e direções podem ser remapeados nas opções.

| Tecla ou ação | Função |
| --- | --- |
| WASD (configurável para setas ou outras teclas) | Mover em qualquer direção, com diagonal normalizada |
| Mouse | Mirar; a direção de ataque independe da direção do movimento |
| Clique esquerdo ou Espaço | Ataque corpo a corpo na direção da mira |
| Shift esquerdo | Esquiva rápida com breve invulnerabilidade e recarga |
| Q ou clique direito (padrão; X no controle) | Pulso que atinge Ecos próximos; tem recarga |
| 1 | Crescente Verdejante — golpe frontal amplo, aprendido após o guardião da primeira fase |
| 2 | Estouro de Sal — explosão circular de cristais, aprendido após o guardião da segunda fase |
| 3 | Rompante da Maré — onda frontal de longo alcance, aprendido após o guardião da terceira fase |
| Interagir (padrão E / Y) perto da passagem | Seguir para a sala/fase seguinte após limpar a sala |
| Esc | Pausar/retomar |

## Progressão e mapa

As fases jogáveis incluídas são **Margens do Primeiro Eco**, **Bacia do Sal Imóvel** e **Costa das Badaladas**. Cada uma tem três salas, caminhos laterais, coberturas e um guardião na última sala. O mapa mostra a posição de Nara e identifica as salas atuais, concluídas e adiante por cores com legenda; quando a saída abre, o cartão da sala atual também exibe **PORTAL**. O progresso das salas anteriores é reconstruído ao continuar de um portal salvo. Em janelas estreitas, o mapa fica abaixo do painel de fase e vida, com a legenda incluída na área reservada. A cada sala limpa, Nara recupera um pouco de vida. Ao zerar a vida, ela retorna ao início da sala atual com vida cheia, poderes aprendidos preservados e todas as recargas de combate prontas. O ataque básico combina sinal de preparação, golpe visível e uma confirmação sonora aguda quando acerta; ataques dos Ecos sustentam uma pose de preparação durante o aviso, seguida de impacto e recuperação.

Ao limpar qualquer sala, um portal ilustrado se abre para levar Nara ao próximo nível; aproxime-se e use o comando de interação configurado (padrão **E** no teclado ou **Y** no controle). Cada guardião concede um poder com sprite próprio: **Crescente Verdejante**, **Estouro de Sal** e **Rompante da Maré**. Após o terceiro guardião, o **Arauto do Vazio** surge como superchefe, com 1.250 pontos de vida, barra de chefe própria e três padrões de ataque: salva de estrelas, chuva de impacto marcada no chão e uma onda de vazio que pode ser evitada com esquiva. Ao derrotá-lo, um portal colossal violeta aparece. Aproxime-se e use o comando de interação mostrado na tela para ver Nara entrar, atravessar a tela escurecida e emergir pelo portal de chegada; usar o portal final conclui a jornada.

Os guardiões se apresentam como **Raiz-Mor**, **Vigia do Sal** e **Vigia do Coro**; o nome permanece junto à barra de vida enquanto o guardião está visível. As memórias ligam Nima ao primeiro sino, a Vigia do Sal à Grande Onda suspensa e o Vigia do Coro à decisão do Arauto de calar os sinos. Depois da lembrança do terceiro guardião, o Arauto aparece, dando a Nara tempo para escutar antes de a luta começar.

Os três poderes têm funções complementares: o **Crescente** interrompe e retarda um Eco (chefes resistem por menos tempo); o **Estouro** deixa os alvos expostos por alguns segundos, aumentando em 30% o dano dos golpes seguintes; o **Rompante** empurra Ecos comuns. Uma sequência útil é Estouro para expor, Crescente para interromper e Rompante para afastar o grupo.

O ataque básico aceita um novo toque nos últimos 0,2 s antes da recarga terminar e o executa assim que fica disponível. O comando é consumido uma vez e expira se a janela passar, evitando que o golpe pareça ignorar um toque próximo do fim da animação.

A esquiva usa uma janela defensiva menor, de 0,12 s antes do fim da recarga. Pausar, morrer ou reiniciar uma sala limpa comandos pendentes; assim, nenhuma esquiva antiga dispara inesperadamente ao voltar ao jogo.

O Arauto agora muda de fase em 70% e 35% da vida: primeiro retoma as raízes, depois os estilhaços do sal e por fim reúne esses ataques à onda da costa. A barra marca os dois limiares. A linha ou círculo no chão precede o dano; a onda final mostra também um anel interno seguro junto ao chefe.

Os inimigos usam ataques próprios do bioma: raízes que avançam pelo chão e podem prender Nara por um instante, estilhaços de sal que são lançados à distância e ondas circulares de ressonância na costa. Esses ataques usam sprites ilustrados próprios, além do aviso visual antes de causar dano; guardiões ampliam o alcance ou disparam salvas múltiplas. Depois do impacto, o atacante faz uma breve pausa, maior nos chefes, para deixar clara a recuperação e abrir espaço para Nara reagir. Inimigos comuns ficam próximos do tamanho de Nara, e guardiões são maiores.

Os Ecos comuns agora têm quatro papéis distinguíveis pela escala e pelo tom: **Caçador** aproxima-se e ataca em linha; **Atirador** mantém distância e lança um projétil do bioma; **Ressonador** anuncia uma área circular; **Tecelão** anuncia um pulso verde e cura aliados próximos. Os quatro papéis aparecem juntos no Vau de Cinza; a Bacia concentra dois Caçadores com Ressonador e Tecelão, enquanto a Costa usa dois Atiradores com Ressonador e Tecelão. Ecos em perseguição tentam contornar obstáculos; atiradores reposicionam quando uma rocha bloqueia o disparo. Encostar num Eco não causa dano: ele vem de ataques anunciados ou projéteis. O círculo de aviso do Ressonador e do guardião da costa corresponde ao raio efetivo de dano.

Nara também tem sprites para o golpe básico de espada, o pulso especial expansivo e os três poderes conquistados. Os projéteis dos inimigos de sal e do superchefe usam sprites próprios.

As coberturas quadradas foram substituídas por troncos caídos, tocos, cristais e rochas, distribuídos de forma diferente pelas salas e com colisores ajustados ao formato. A caminhada agora acompanha a velocidade, a esquiva deixa rastros, os ataques têm deslocamento e expansão, e os personagens reagem ao dano e caem antes de desaparecer ou retornar à sala. O jogo dá um breve período de proteção após Nara receber dano para evitar que salvas de projéteis a eliminem de uma vez. Barras de vida flutuam acima de Nara, dos ecos e dos guardiões.

Os obstáculos sólidos também funcionam como **cobertura**: estilhaços inimigos param ao atingir troncos, rochas ou ruínas; o golpe básico, a Crescente Verdejante e o Rompante da Maré não causam dano através deles. O Estouro de Sal e o Pulso circular continuam a atingir através da cobertura, criando uma decisão entre proteger-se de ataques direcionais e sair de áreas anunciadas. Selos de Eco também exigem linha livre para poderes direcionais.

No bosque alagado, as poças rasas reduzem a velocidade de caminhada para 78%, mas não causam dano. A esquiva mantém o impulso normal, permitindo atravessá-las rapidamente quando houver perigo. Na primeira entrada na água, uma mensagem explica esse efeito.

Para manter os sinais de combate legíveis, a tela limita efeitos visuais secundários a 24 de cada vez. Avisos de ataques e áreas perigosas têm prioridade e não são descartados por esse limite.

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

