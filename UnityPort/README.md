# Scadryk para Unity

> Direção criativa, diagnóstico e etapas de desenvolvimento: [DESIGN.md](DESIGN.md). A etapa de fundação está em andamento; esta versão ainda não atende a todos os critérios de produto completo.

Critérios de comparação com jogos de referência e lacunas verificáveis: [QUALITY_BENCHMARK.md](QUALITY_BENCHMARK.md).

Registro de builds, teste de progressão e roteiro de avaliação com jogadores: [QA.md](QA.md).

**Tecnologia:** a campanha é um jogo Unity feito em C#. O código, cenas e recursos da aventura ficam em `UnityPort`; use o VS Code para editar C# e o Unity Editor para importar, executar e compilar. O protótipo `.NET` da raiz é legado e não faz parte do jogo Unity.

**Visão geral:** aventura top-down original com três biomas, nove salas conectadas, guardiões, três poderes de combate e exploração, o superchefe Arauto do Vazio e uma história sobre Nara e os Ecos. Consulte [Progressão e mapa](#progressão-e-mapa) para entender a campanha e [Menu, opções e áudio](#menu-opções-e-áudio) para os recursos do jogo.

**Validação atual (24/11/2026):** Unity `6000.6.9f1` importou/compilou os scripts em batch; o validador confirmou 36 referências e 51 texturas de arte GPU-only, incluindo o retrato de Nima. A build Windows foi gerada com sucesso para o commit de código `2a52f9277572`; o manifesto registra árvore limpa, Unity `6000.6.9f1` e horário UTC `2026-11-24T22:17:10Z`. O jogo não foi iniciado e a cena não entrou em Play Mode. No CI mais recente consultado (`f03d28c`), a validação de recursos Unity passou, mas build e smoke foram ignorados no gate por falta de `UNITY_LICENSE`, `UNITY_EMAIL` e `UNITY_PASSWORD`. O workflow C# passou build, smoke e publicação, mas verifica apenas o protótipo legado da raiz. O smoke da campanha Unity, a avaliação visual/sonora e a conclusão do jogo continuam pendentes. As instruções para abrir pelo Unity Hub/Editor ou executar a build, bem como estimativas gráficas mínimas e recomendadas, estão nas seções abaixo. Resultados e limitações estão em [QA.md](QA.md).

## Comece aqui

- [Requisitos do Editor e do jogo](#requisitos)
- [Instalar, abrir e executar pelo Unity Editor ou pela build Windows](#abrir-e-jogar)
- [Controles e remapeamento](#controles)
- [Progressão, biomas e mapa](#progressão-e-mapa)

## Destaques implementados

Quando cues de perigo se sobrepõem, cada novo aviso reduz o volume relativo conforme a quantidade de sinais ativos; nenhum cue é descartado. Os telegráficos visuais permanecem individualizados, com alto contraste opcional. A clareza da mixagem ainda precisa de avaliação em jogo.

Todo ataque inimigo que causa dano tem uma preparação visual e sonora; a duração mínima configurada é 0,7 s para Ecos comuns, guardiões e fases do Arauto. O smoke verifica esse limite no código. Ele é uma proteção contra avisos curtos acidentais, não comprova que o tempo seja suficiente para todos os jogadores; isso ainda precisa de sessões de avaliação.

Durante a preparação de um golpe, uma legenda central nomeia o ataque e mostra o tempo restante até o impacto, arredondado para cima em décimos de segundo. É uma alternativa textual aos sinais visuais e sonoros; o valor só é atualizado quando muda o décimo, e não pisca. A fonte diminui para caber em legendas longas e janelas menores, respeitando o limite mínimo de 9 pontos. Para preservar a leitura em combates cheios, ela reúne no máximo dois tipos de ameaça ao mesmo tempo e prioriza os impactos mais próximos; a legenda desaparece assim que a preparação termina. Em telas largas, ocupa a faixa entre HUD e minimapa; em telas baixas, substitui o objetivo apenas durante o aviso.

O Caderno de Memórias mantém em cache as alturas e conteúdos formatados dos cartões enquanto a lista, largura e escala de texto não mudam; isso evita recriar arrays e objetos de texto em cada redesenho do painel. Esse ajuste reduz alocações previstas no código, mas o efeito em GC e desempenho ainda precisa ser medido com o Profiler.

O HUD também reutiliza seus conteúdos de texto para fase, sala, vida, recargas, poderes e objetivo. As recargas e os rótulos dos poderes só são formatados quando mudam de décimo, estado, vínculo ou eco; o objetivo é recalculado quando muda o estado da sala ou o controle. O minimapa e o mapa geral reutilizam os rótulos das nove salas; o estado de cada selo é consultado ao montar a sala e o rótulo muda quando o selo é revelado. Cabeçalho do minimapa, fase do Arauto, fala de Nima e bússola fora da tela também atualizam apenas quando seus dados visíveis mudam. O smoke inclui checagens de arredondamento e reuso dos textos de recarga, sala, bioma e fase do chefe, mas ainda não foi executado. A compilação batch passou; a redução efetiva de alocações e o custo do Profiler ainda precisam ser medidos em runtime.

Os três ataques do Arauto também têm motivos de aviso próprios: notas descendentes antecipam a salva, notas ascendentes anunciam a queda de memória e pulsos graves identificam a onda de vazio. O som geral de chegada do Arauto continua separado; os motivos são sintetizados no código, sem arquivos de áudio externos. A distinção e o equilíbrio com a trilha ainda precisam ser ouvidos em jogo.

Ao atravessar um limiar de fase durante o aviso, o Arauto ainda conclui o golpe já anunciado. Depois há um breve pulso de transição e 0,9 s sem movimento, com o próximo ataque adiado por 3,1 s, para abrir espaço para ler o novo padrão e se reposicionar.

O HUD mantém a função de cada poder junto do comando e da recarga: Crescente interrompe Ecos comuns, Estouro expõe alvos para golpes seguintes e Rompante afasta Ecos comuns. A vida também aparece numericamente ao lado da barra, como valor atual/máximo, sem animação pulsante. Os guardiões e o Arauto resistem a esses controles de grupo; a dica de aquisição também lembra que os poderes revelam selos opcionais. Se um poder estiver a até 0,18 s do fim da recarga, o comando fica guardado até ele ficar pronto; comandos feitos antes expiram sem disparo atrasado. Ao antecipar um ataque ou poder, Nara conserva a direção anunciada até o impacto; a mira volta a acompanhar o cursor ou analógico logo depois.

Os três Selos de Eco agora aprofundam a build sem adicionar telas ou equipamentos: o selo verde reduz em 20% a recarga do Crescente, o selo do sal prolonga a vulnerabilidade causada pelo Estouro e o selo das badaladas aumenta força e duração do empurrão do Rompante contra Ecos comuns. Cada melhoria permanece salva por sala, aparece como “eco” no HUD e também recupera até 35 pontos de vida. A aplicação visual e o equilíbrio ainda dependem do smoke e de sessões com jogadores.

Após derrotar o terceiro guardião, a lembrança continua pausando o combate até a leitura terminar. Se o selo final ainda estiver disponível, há 12 segundos de exploração segura antes de o Arauto despertar; a janela desaparece se o selo já foi obtido e pode ser pausada no menu.

Nas salas seguras da Bacia e da Costa, Nima aparece depois que Nara liberta os guardiões. Cada lembrança compartilhada na Bacia aumenta gradualmente a opacidade e a escala do seu eco e do reflexo na água; essa conversa continua salva ao sair e voltar.

O ataque básico agora encadeia três cortes dentro de uma janela curta: os dois primeiros mantêm ritmo rápido e o terceiro tem maior alcance e dano, preparação mais longa, breve interrupção de Ecos comuns, arco dourado e um som de acerto próprio. Guardiões e Arauto resistem à interrupção do finalizador. Uma troca de sala, morte ou recarga expirada reinicia a sequência. Começar uma esquiva durante a proteção após receber dano agora preserva o prazo de invulnerabilidade já existente, em vez de encurtá-lo. A sensação do combo ainda precisa de avaliação jogando; a compilação não mede ritmo ou leitura visual.

As três salas centrais agora oferecem um desvio explorável próprio para cada bioma: uma passagem de raízes ao norte no bosque, uma prateleira de sal ao sul e uma vereda de bóias ao norte na costa. Os desvios ligam duas aberturas e retornam à sala, dando uma rota alternativa durante a travessia e o combate; a rota principal ao portal continua aberta. A prateleira de sal esconde o Eco que fortalece Crescente, e a vereda de bóias guarda o Eco de Estouro. O mapa marca a direção do desvio com ↑/↓, selo opcional pendente com ◇ e memória já liberada com ◆; cada memória só pode ser libertada com o poder correspondente. Saves de versões que registraram esses dois Ecos nos locais antigos são migrados automaticamente para os novos trajetos.

Cada bioma também tem um fundo original para sua primeira sala — Vau das Lanternas, Planícies do Vento de Vidro e Calçada da Maré Baixa — além dos fundos exclusivos das salas centrais e das arenas de guardião. As imagens são cenários visuais; caminhos e colisões são controlados pela geometria criada no Unity.

## Requisitos

- Unity Editor `6000.6.9f1` (ver `ProjectSettings/ProjectVersion.txt`) e o módulo Windows Build Support para gerar o executável Windows.
- Para builds Windows com backend IL2CPP, o Unity também exige Visual Studio C++ Build Tools e Windows SDK 10.0.19041 ou superior. São ferramentas de compilação; o código continua sendo editado no VS Code, sem precisar abrir o IDE Visual Studio.
- Para editar e compilar no Editor, a Unity recomenda no mínimo 8 GB de RAM; 16 GB são recomendados para trabalhar com o Unity e o VS Code abertos juntos. O consumo real depende dos programas e assets carregados. Consulte os [requisitos oficiais do Unity 6.6](https://docs.unity.com/en-us/engine/6000.6/manual/get-started/install-and-upgrade/getting-started-installing-unity/system-requirements).
- O projeto usa Input Manager (Old), já configurado em Project Settings. Artes de cenário e inimigos estão em `Assets/Resources/ScadrykArt/`.
- As folhas de arte são importadas sem cópia legível pela CPU: o jogo apenas as desenha e fatia por UV, então manter pixels duplicados em RAM não é necessário. `Tools/Validate-Resources.ps1` confere a existência dos recursos citados nos scripts e essa configuração em todos os PNGs de arte.

### Requisitos do jogo Windows

Os requisitos abaixo para o **jogo executável** são estimativas conservadoras para esta aventura 2D, não mínimos medidos nem garantia de FPS. Ainda faltam sessões de desempenho em máquinas variadas; até lá, trate a coluna recomendada como uma meta de configuração, não como benchmark.

| | Mínimo estimado | Recomendado estimado |
|---|---|---|
| Sistema | Windows 10 64 bits, versão 21H1 ou posterior | Windows 10/11 64 bits atualizado |
| Processador | x64 com SSE2, 2 núcleos | x64 com SSE2, 4 núcleos recentes |
| Memória instalada | 8 GB RAM (estimativa provisória) | 16 GB RAM ou mais |
| Gráficos | GPU integrada ou dedicada com driver do fabricante e suporte a DirectX 10 ou superior | GPU integrada recente ou dedicada, com driver atualizado e suporte a DirectX 11 ou superior |
| Tela | 1280×720 | 1920×1080, proporção 16:9 |
| Armazenamento | Reserve 1 GB livre para instalar/descompactar a build e guardar dados locais | SSD recomendado; a pasta `Build` desta versão ocupa cerca de 161 MB e o tamanho pode mudar entre versões |

A Unity lista Windows 10 21H1+, CPU x86/x64 com SSE2 e GPU DX10/DX11/DX12/Vulkan como compatibilidade de plataforma do Player; isso não substitui a medição dos requisitos deste jogo. A configuração do projeto parte de 1920×1080 e oferece tela cheia ou janela dimensionada a até 1280×720, conforme o monitor. Para o **Editor Unity**, use a seção Requisitos acima: a tabela desta seção é somente para jogar uma build pronta.

## Programar em C# com VS Code

O VS Code é o editor de código do projeto; não é necessário instalar o Visual Studio. O Unity Editor continua necessário para importar scripts, compilar a cena, jogar e gerar a build.

1. Instale a extensão [Unity for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=visualstudiotoolsforunity.vstuc), publicada pela Microsoft. Ela instala as dependências de C# e o C# Dev Kit; siga o aviso da extensão para instalar o runtime .NET exigido, se necessário.
2. No Unity Hub, entre na sua conta Unity e ative uma licença Personal gratuita em **Licenses > Add license > Get a free personal license** (ou ative a licença válida da sua organização). Sem uma licença ativa, o Editor encerra em batch mode e não compila o projeto.
3. Abra o projeto `UnityPort` no Unity Editor e confirme no Package Manager que **Visual Studio Editor** está na versão 2.0.20 ou superior. O projeto usa a versão 2.0.26 em `Packages/manifest.json`.
4. No Unity, abra **Edit > Preferences > External Tools**, selecione **Visual Studio Code** em **External Script Editor**, ative **Generate all .csproj files** e use **Regenerate project files**.
5. No VS Code, abra a pasta `UnityPort`. O workspace recomenda a extensão Unity e C# Dev Kit; aceite a instalação sugerida se ainda não estiverem presentes. Edite os scripts em `Assets/Scripts`; deixe o Unity Editor aberto para recompilar e conferir alterações. Pressione **Ctrl+Shift+B** para importar e compilar com a tarefa padrão **Scadryk: importar e compilar Unity**; use **Terminal > Run Task** e escolha **Scadryk: gerar build Windows** para produzir o executável sem iniciá-lo.

Não instale o pacote Unity antigo `com.unity.ide.vscode`: ele foi descontinuado. O fluxo atual usa a extensão Unity do VS Code e o pacote **Visual Studio Editor** já declarado no projeto.

## Abrir e jogar

### Jogar pelo Unity Editor

1. Instale/abra o Unity Hub e instale o Editor `6000.6.9f1`. O módulo Windows Build Support só é necessário para gerar o executável.
2. Em **Projects**, escolha **Add > Add project from disk** e selecione a pasta `UnityPort` deste repositório (a pasta que contém `Assets`, `Packages` e `ProjectSettings`). Aguarde a primeira importação dos assets terminar.
3. No Unity, abra `Assets/Scenes/ScadrykDemo.unity` e pressione **Play** na barra superior. O VS Code não executa o jogo sozinho; ele serve para editar o código C#.
4. Comece pelo menu. Limpe os Ecos e conclua o objetivo indicado em cada sala; quando o portal surgir, espere a animação de abertura terminar, aproxime-se e use o comando de interação mostrado na tela (padrão **E** no teclado ou **Y** no controle). Complete as nove salas para concluir os três biomas e enfrentar o Arauto.

### Jogar a build Windows pronta

1. Obtenha/descompacte a pasta `Build` inteira de uma versão publicada do projeto.
2. Abra `Build/ScadrykDemo.exe` no Windows. Mantenha junto dele as pastas e arquivos auxiliares (`ScadrykDemo_Data`, `UnityPlayer.dll`, `UnityCrashHandler64.exe` e demais arquivos da pasta); não copie somente o `.exe`.
3. Se a tela cheia não for adequada ao monitor, inicie o jogo e escolha **Opções > Tela cheia** para alternar entre tela cheia e janela ajustada ao monitor.

Para criar um executável Windows, escolha **Scadryk > Build Windows demo** ou use a tarefa de build do VS Code. O build aparece em `Build/ScadrykDemo.exe` dentro do projeto Unity; o método de build da Unity gera também `Build/SCADRYK_BUILD_INFO.txt` com commit, estado da árvore-fonte antes do empacotamento, número de caminhos alterados, versão do Editor e horário UTC, tanto pelo menu quanto pelo script ou CI. A cena e o executável não são iniciados pelo script de build; execute o `.exe` manualmente quando quiser jogar. O smoke da campanha continua pendente.

Para importar e compilar os scripts sem gerar ou abrir o jogo, execute no PowerShell `.\Tools\compile-unity.ps1`. Esse passo não requer Windows Build Support. Para conferir que nomes de imagens referenciados nos scripts têm PNGs em `Resources/ScadrykArt`, rode `Tools/Validate-Resources.ps1`; essa verificação estática não requer Unity. O script procura o Editor `6000.6.9f1` no destino configurado `C:\Outros Arquivos\Unity\Editors` e depois no caminho padrão do Unity Hub; para outro local, informe `-UnityEditorPath 'C:\caminho\para\Unity.exe'`.

### Build e smoke remotos no GitHub Actions

O workflow `.github/workflows/unity-build.yml` cria um executável Windows e executa o smoke das nove salas em um runner Windows hospedado pelo GitHub; não inicia o jogo neste computador. Ele roda automaticamente em PRs internos quando mudam assets/scripts Unity, pacotes, configurações, ferramentas ou o próprio workflow e também pode ser iniciado manualmente depois de chegar à branch padrão. Para habilitá-lo, configure os secrets `UNITY_LICENSE`, `UNITY_EMAIL` e `UNITY_PASSWORD` em **Settings > Secrets and variables > Actions** no repositório. No Windows, uma licença Personal ativada pelo Unity Hub costuma ficar em `C:\ProgramData\Unity\Unity_lic.ulf`; copie o conteúdo do arquivo para `UNITY_LICENSE` e informe o e-mail e a senha da conta Unity nos outros dois secrets. O GameCI usa esses secrets para autenticar o Editor durante o build; não cole os valores no chat, README ou código. Se for usar a mesma licença Personal no runner, saia do Unity Hub depois de copiar o `.ulf` para devolvê-la e liberar o assento de CI; será necessário entrar e ativar a licença novamente antes de usar o Editor localmente. Confira o procedimento e os detalhes de licença na [documentação oficial do GameCI](https://game.ci/docs/github/activation/); a ação de build é comunitária, não publicada pela Unity. Para acionamento manual, acesse **Actions > Build Unity campaign > Run workflow** e escolha a branch desejada. O smoke tem limite de 15 minutos. O artefato `scadryk-unity-windows-<commit>` contém a build jogável; o log do smoke fica em um artefato separado por 14 dias.

O smoke verifica a campanha por automação interna da build Development e encerra quando as nove salas, os três poderes, o Arauto e o registro de vitória passam. Isso não substitui avaliação visual/sonora nem partidas com jogadores. Sem os secrets, o workflow para antes de chamar GameCI. Depois de configurá-los, não é preciso fazer outro commit: no GitHub, abra **Actions > Build Unity campaign**, selecione a execução que falhou no PR e use **Re-run jobs > Re-run all jobs**. Isso repete a validação desse mesmo commit; para validar alterações posteriores, o workflow roda novamente quando os arquivos Unity cobertos mudarem.

Para gerar o executável Windows sem abrir a cena em Play Mode, execute `.\Tools\build-windows.ps1` na pasta `UnityPort`. Esse segundo passo requer o módulo Windows Build Support; procura o Editor nos mesmos caminhos, exige o resultado `Build Finished, Result: Success.` no log do Unity e confirma o executável e os arquivos essenciais do payload (`Assembly-CSharp.dll`, `boot.config` e `globalgamemanagers`). Ambos gravam logs em `%TEMP%`; nenhum inicia o executável. A compilação não substitui a avaliação manual da campanha.

Para uma rodada rápida de desenvolvimento, a sequência é: **Unity Hub → abrir `UnityPort` → cena `Assets/Scenes/ScadrykDemo.unity` → botão Play**. Para jogar sem o Editor, use a build completa em `Build/ScadrykDemo.exe`. A build atual ainda precisa de uma sessão de avaliação de gameplay.

O avanço é salvo automaticamente ao atravessar cada portal. O checkpoint guarda bioma e sala juntos em um registro versionado; saves antigos com esses valores em chaves separadas continuam sendo carregados, e as chaves escalares são mantidas em paralelo para compatibilidade e recuperação. Ao reabrir o jogo, a última sala alcançada é carregada com os poderes das fases já vencidas. A conclusão da campanha apaga o marcador de progresso para permitir uma nova jornada. As métricas ficam na pasta retornada por `Application.persistentDataPath` (no Windows, normalmente `%USERPROFILE%/AppData/LocalLow/<CompanyName>/<ProductName>`). Nenhum dado é enviado pela rede.

O registro atual usa `scadryk-sessions-v9.csv`. Cada linha inclui um `session_id` aleatório compartilhado pelas salas gravadas na mesma execução, `unclear_attacks`, `completed`, os usos dos três poderes, entradas/segundos na corrente costeira, segundos na corrente de sal, selos libertados, segundos em água rasa e ativações bem-sucedidas de ressonância por Pulso. O tempo de exposição conta caminhada e esquiva; pausas, derrota e transições entre salas ficam fora da métrica. `input_mode` registra o último dispositivo da sala (`keyboard_mouse`, `gamepad` ou `unknown`). Mortes e reinícios preservam esses valores durante a tentativa; ao começar uma sala nova, eles reiniciam. O ID aparece como `SCADRYK_SESSION` no log para associar a ficha à campanha sem registrar nome ou conta. Durante um teste, pressione **F8** ao sofrer ou observar um ataque difícil de perceber; F8 fica reservado para esse registro. Além da contagem por sala, `scadryk-attack-clarity-v1.csv` anota fase, sala e padrões ativos ou recentes quando identificáveis; sem padrão próximo, registra **não identificado**, sem adivinhar. O arquivo fica no diretório local `Application.persistentDataPath` e não é enviado pela build. O resumo de playtests aceita também CSVs v8 antigos, atribuindo zero à métrica da corrente de sal que ainda não existia. Os arquivos anteriores permanecem separados como histórico. A sessão é gravada ao trocar de sala, concluir a campanha ou sair do jogo; o cronômetro conta apenas jogo ativo, sem pausas nem travessias. Para comparar builds, jogue cada sala até o portal e examine tempo, mortes, marcas, poderes e exposição ambiental antes de alterar o balanceamento.

Para registrar avaliações de pessoas iniciantes, intermediárias e experientes, use a [ficha de teste de campanha](PLAYTEST.md). Ela combina os dados automáticos do CSV com clareza de objetivos, justiça percebida, compreensão da história e comentários por sala. `Tools/Summarize-Playtests.ps1` consolida sessões v9 (e mantém leitura v8) e marcas F8 por sala, preserva cada `session_id` e pode exportar um CSV para comparar resultados. Com `-RatingsPath PLAYTEST-RATINGS.csv`, também calcula médias separadas por experiência; respostas e comentários continuam sendo registrados por quem observou, sem estimativas automáticas.

Selos de Eco ocupam desvios distintos na entrada da bacia, na chegada à costa e na arena final. De longe, cada um deixa só um brilho discreto; ao se aproximar, sua luz ganha intensidade e Nara recebe uma pista contextual. Se ainda precisar vencer o guardião local, o jogo explica isso antes de indicar o poder e o comando disponíveis. Use Crescente, Estouro ou Rompante junto ao selo correspondente para libertar uma memória: a silhueta de um Eco sobe do selo, toca um som próprio e revela um fragmento de história enquanto Nara recupera vida. Os três encontros são opcionais. Cada lembrança é salva assim que descoberta e continua encontrada depois de morrer, reconstruir a sala ou carregar o checkpoint; começar uma nova jornada limpa os três selos.

As salas têm ritmos diferentes: entradas do bosque e as travessias trazem combate; a comporta do bosque abre com o comando de interação configurado depois da luta (padrão **E** no teclado ou **Y** no controle); as entradas da bacia e da costa são momentos calmos com vestígios para escutar. Na travessia de sal, ressoe dois cristais com o Pulso (padrão **Q** ou clique direito; no controle, **X**). Na costa, faça o farol responder antes do sino do recife; os guardiões encerram cada bioma. O HUD mostra o objetivo e a contagem dos objetos ativados. O minimapa marca Nara com um quadrado dourado e usa um losango ciano contornado para o Eco vivo mais próximo, o próximo sinal ou o portal aberto. Quando esse alvo está fora da visão da câmera, um aviso ciano aponta a direção; ele cede espaço às falas e avisos temporários. Após limpar a sala e terminar mensagens atuais, se ainda houver um sinal ou vestígio pendente, surgem pistas graduais específicas: a primeira após 16 s sem progresso e as seguintes após 11 s de espera ativa cada; pausa e mensagens temporárias suspendem a contagem; na costa, elas explicam a ordem e a posição do farol e do sino do recife. Essa navegação ainda aguarda inspeção visual no Unity Editor.

Em janelas estreitas, uma faixa compacta explica movimento, mira, ataque, esquiva, Pulso, interação, mapa e pausa usando os comandos configurados. Ao escutar uma memória, o jogo agora pausa inimigos, projéteis e avisos até a leitura terminar; um motivo sonoro próprio marca o início de memórias de Nima, segredos, guardiões e do encerramento. Pressione o comando de interação, Enter, Espaço ou Esc no teclado, ou A, interação ou B no controle, para continuar. O painel escurece o fundo, fica centralizado, adapta a altura ao texto e reduz o título/aviso de comandos quando a janela é estreita, sem deixar o mesmo botão avançar acidentalmente a próxima fala.

## Menu, opções e áudio

### Título, pausa e progresso

- Com checkpoint, **Continuar jornada** retoma o último portal salvo; **Nova jornada** começa outra campanha após confirmação. Sem checkpoint, aparece **Começar jornada**. A vitória final também permite iniciar outra jornada.
- **Esc** ou **Start** pausa e retoma. Se a janela perder o foco ou o aplicativo for suspenso, a sala atual é salva, a jornada pausa e continua pausada quando volta.
- **M** abre o mapa durante a exploração; no controle, use **L3** ou escolha **Mapa da jornada** na pausa. O mapa pausa o combate e marca salas concluídas, sala atual, percurso adiante e objetivo.
- **O** abre as opções no título ou na pausa. **Memórias** abre o caderno no título, na pausa e na tela de vitória; ele conserva as entradas já ouvidas mesmo após fechar o jogo.
- O progresso é salvo ao atravessar portais, após derrotar guardiões ou o Arauto e ao sair normalmente. O checkpoint identifica bioma e sala, mantém poderes conquistados e permite retomar portais abertos. Uma nova jornada limpa os marcos da campanha; cancelar a confirmação preserva o save.
- **Salvar e sair ao título** grava a sala atual antes de voltar ao menu. **Sair do jogo** encerra a build e salva o checkpoint; no Editor, esse botão não fecha o Unity.

### Tela, áudio e acessibilidade

- **FPS:** **Auto FPS** sincroniza com a atualização do monitor; **30 FPS** e **60 FPS** aplicam limites fixos imediatamente e ficam salvos. Esses limites desligam VSync e podem causar tearing. Limitar FPS pode reduzir trabalho da GPU/CPU em alguns computadores, mas não garante maior duração da bateria; consumo e fluidez ainda precisam de medição em equipamentos diferentes.
- **Tela:** alterne entre tela cheia e janela. A janela é dimensionada para até 90% do monitor, com limite de 1280×720 e proporção mantida.
- **Acessibilidade:** escala do texto de 100% a 150%, redução de flashes, redução de tremor e alto contraste para os avisos de perigo. Reduzir tremor interrompe também a sacudida que já estiver ocorrendo. A redução de flashes também atenua o golpe de espada alternativo se a arte principal não carregar; sinais de dano iminente continuam fortes para preservar a leitura.
- **Volume:** música e efeitos têm ajustes próprios, aplicados imediatamente. Durante avisos de ataque, a trilha ambiente e a camada de combate baixam suavemente para destacar os sinais sonoros; os efeitos mantêm o volume escolhido.

### Abertura e memórias

A primeira jornada começa com uma abertura narrativa; combate e cronômetro ficam pausados até confirmar **Começar a jornada** com Enter, Espaço, A ou o comando de interação. Nara segue os nomes devolvidos pela água até encontrar a voz de Nima presa às raízes. Cada guardião tentou proteger os vivos do luto de um jeito diferente; o Arauto transformou esse medo em silêncio permanente ao deter a última onda. Nas chegadas tranquilas à bacia e à costa, Nima reaparece depois dos guardiões e suas falas mudam conforme as lembranças retornam; o Caderno identifica quem está falando e mostra um retrato autoral de Nima apenas nas quatro falas dela. Aproxime-se e interaja (padrão E no teclado ou Y no controle). As apresentações das nove salas, falas dos guardiões e diálogos ficam no Caderno de memórias sem duplicatas; saves antigos migram trechos revisados para as novas versões. No final, Nara escolhe lembrar sem manter ninguém preso.

### Teclado e remapeamento

- Setas ou WASD navegam pelos menus; Enter/Espaço confirma e Esc retorna. **Esc**, **F8** e **M** são reservados, respectivamente, para navegação, registro de clareza de ataque e mapa.
- Clique num botão das opções para torná-lo a seleção ativa; teclado e controle mostram o mesmo destaque ao navegar.
- Na aba **Teclas**, remapeie ataque, esquiva, Pulso, poderes, interação e direções. Se uma tecla já estiver em uso, os vínculos trocam de lugar; **Padrões** restaura o esquema padrão.
- Enquanto captura um novo vínculo, Esc cancela a captura; pressione novamente para sair das opções. HUD, pistas e mensagens de interação refletem o dispositivo usado por último.

### Controle

- Analógico esquerdo move e o direito mira. A ataca, B esquiva, X usa Pulso, Y interage, LB/RB/Back ativam os poderes e Start pausa. L3 abre o mapa.
- Quando o controle é o dispositivo ativo, quatro marcas douradas discretas indicam a direção atual do ataque à frente de Nara; elas mostram direção, não o alcance do golpe. A mira acompanha o movimento até o analógico direito ser usado; depois disso, fica independente e conserva a direção ao soltar o analógico.
- Na aba **Controle**, remapeie botões e ajuste a zona morta radial entre 8% e 40% (padrão: 19%). Analógicos navegam, A confirma e B retorna. Start e L3 ficam reservados para pausa e mapa.
- O painel mostra eixos e intensidade útil/bruta para ajudar a ajustar a zona morta. A correspondência pode variar conforme o controle e o driver; ainda faltam sessões com dispositivos físicos variados.

### Música e sinais sonoros

Há trilhas sintetizadas próprias para os três biomas e para o confronto final, com transição cruzada de 1,25 s. Uma camada rítmica cresce conforme mais Ecos entram no encontro e diminui quando a sala é limpa ou pausada. Ações de Nara, avisos, impactos, dano, portais e memórias têm sinais originais; os três poderes também têm preparação e impactos distintos. A mixagem, inclusive o ducking dos avisos, ainda precisa de avaliação auditiva em gameplay. Os sinais visuais de perigo continuam ativos mesmo com o volume zerado.

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

No Bosque Alagado, as margens norte e sul têm poças opcionais que reduzem a velocidade; há espaço para contorná-las. As poças oscilam suavemente para sugerir água em movimento sem competir com os avisos de combate.

O mapa da jornada conecta as três salas principais de cada bioma. Trechos concluídos ficam verdes, a rota até a sala atual fica ciano, próximos encontros aparecem como adiante e biomas ainda inacessíveis ficam marcados como selados. Setas nos nomes das salas indicam desvios; no minimapa, **◇ PEND.** indica selo ainda não obtido e **◆ FEITO**, selo liberado. O mapa completo também explica esses símbolos.

Se Nara tentar interagir com um sinal que responde ao Pulso, o lembrete agora identifica o cristal, o farol ou o sino e reforça a ordem correta da costa.

As fases jogáveis incluídas são **Margens do Primeiro Eco**, **Bacia do Sal Imóvel** e **Costa das Badaladas**. Cada uma tem três salas, caminhos laterais, coberturas e um guardião na última sala. Os limiares entre salas ficam selados para que Nara não entre em uma área antes de o encontro dela começar; o portal aberto após concluir o objetivo é o caminho de progressão. O mapa mostra a posição de Nara e identifica as salas atuais, concluídas e adiante por cores com legenda; quando a saída abre, o cartão da sala atual também exibe **PORTAL**. O progresso das salas anteriores é reconstruído ao continuar de um portal salvo. Em janelas estreitas, o mapa fica abaixo do painel de fase e vida, com a legenda incluída na área reservada. A cada sala limpa, Nara recupera um pouco de vida. Ao zerar a vida, ela retorna ao início da sala atual com vida cheia, poderes aprendidos preservados e todas as recargas de combate prontas. O ataque básico combina sinal de preparação, golpe visível e uma confirmação sonora aguda quando acerta; ataques dos Ecos sustentam uma pose de preparação durante o aviso, seguida de impacto e recuperação.

Ao limpar qualquer sala, um portal ilustrado começa a se abrir para levar Nara ao próximo nível; espere a animação terminar, aproxime-se e use o comando de interação configurado (padrão **E** no teclado ou **Y** no controle). Cada guardião concede um poder com sprite próprio: **Crescente Verdejante**, **Estouro de Sal** e **Rompante da Maré**. Os três também revelam Selos de Eco em rotas opcionais; use o poder indicado perto de um selo para libertar uma lembrança e recuperar vida. Após o terceiro guardião, o **Arauto do Vazio** surge como superchefe, com 1.250 pontos de vida, barra de chefe própria e três padrões de ataque: salva de estrelas, queda de memória marcada no chão e onda de vazio. Os avisos também trazem uma dica de contrajogo: cruzar entre as linhas, sair da marca ou permanecer no centro seguro. Ao derrotá-lo, um portal colossal violeta aparece. Quando terminar de se abrir, aproxime-se e use o comando de interação mostrado na tela: Nima surge diante do portal com um motivo descendente de três notas, atravessa primeiro e diminui até desaparecer; Nara entra em seguida e emerge pelo portal de chegada. Usar o portal final conclui a jornada e registra a escolha de Nara de lembrar.

Os guardiões se apresentam como **Raiz-Mor**, **Vigia do Sal** e **Vigia do Coro**; o nome permanece junto à barra de vida enquanto o guardião está visível. Ao conceder cada poder, a lembrança explica também seu uso tático: Crescente interrompe, Estouro expõe alvos e Rompante afasta Ecos. O aviso de aquisição mostra o comando atual, inclusive após remapeamento. As memórias ligam Nima ao primeiro sino, a Vigia do Sal à Grande Onda suspensa e o Vigia do Coro à decisão do Arauto de calar os sinos. Depois da lembrança do terceiro guardião, o Arauto aparece, dando a Nara tempo para escutar antes de a luta começar.

Os três poderes têm funções complementares: o **Crescente** interrompe e retarda um Eco (chefes resistem por menos tempo); o **Estouro** deixa os alvos expostos por alguns segundos, aumentando em 30% o dano dos golpes seguintes; o **Rompante** empurra Ecos comuns e interrompe sua preparação, mas os guardiões resistem. Enquanto houver alvos expostos, o HUD indica quantos estão sob o efeito e lembra o bônus de 30%; avisos de ataques iminentes têm prioridade quando duas ameaças chegam juntas. Uma sequência útil é Estouro para expor, Crescente para interromper e Rompante para afastar o grupo. O Pulso e cada poder mostram preparação, impacto e recuperação: o Pulso usa 0,08 s antes do impacto; nos poderes a preparação cresce com alcance e efeito, e cada um tem recuperação animada própria. A pose chega ao ápice junto ao dano e depois se recompõe. A mira direcional fica fixada no início da preparação, e o efeito só atinge alvos dentro da forma mostrada.

O ataque básico aceita um novo toque nos últimos 0,2 s antes da recarga terminar e o executa assim que fica disponível. O comando é consumido uma vez e expira se a janela passar, evitando que o golpe pareça ignorar um toque próximo do fim da animação.

A esquiva usa uma janela defensiva menor, de 0,12 s antes do fim da recarga. Pausar, morrer ou reiniciar uma sala limpa comandos pendentes; assim, nenhuma esquiva antiga dispara inesperadamente ao voltar ao jogo.

O Arauto agora muda de fase em 70% e 35% da vida: primeiro retoma as raízes, depois os estilhaços do sal e por fim reúne esses ataques à onda da costa. A barra marca os dois limiares. A linha ou círculo no chão precede o dano; a onda final mostra também um anel interno seguro junto ao chefe.

Os inimigos usam ataques próprios do bioma: raízes que avançam pelo chão e podem prender Nara por um instante, estilhaços de sal que são lançados à distância e ondas circulares de ressonância na costa. Esses ataques usam sprites ilustrados próprios, além do aviso visual antes de causar dano; guardiões ampliam o alcance ou disparam salvas múltiplas. Depois do impacto, o atacante faz uma breve pausa, maior nos chefes, para deixar clara a recuperação e abrir espaço para Nara reagir. Inimigos comuns ficam próximos do tamanho de Nara, e guardiões são maiores. Os avisos em sprite têm uma borda escura discreta para destacá-los tanto nos pisos claros do sal quanto nos fundos escuros da costa; o alto contraste também amplia linhas e clareia seus sinais.

Os Ecos comuns têm quatro papéis distinguíveis pela silhueta, escala e tom: **Caçador** usa a figura do portador de lâmina, aproxima-se e ataca em linha; **Atirador** usa a figura com arco, mantém distância e lança um projétil do bioma; **Ressonador** usa a figura encouraçada e anuncia uma área circular; **Tecelão** usa o portador de lanterna e anuncia um pulso verde que cura aliados próximos. A campanha os introduz por etapas: primeiro Caçador, depois Atirador no bosque, Ressonador na Bacia e, por fim, Tecelão na Costa. A tonalidade também acompanha o bioma — verdes do bosque, âmbar do sal e azuis da costa — sem apagar as diferenças entre papéis. Projéteis distinguem suas consequências: espinhos enraízam no bosque, estilhaços deixam crosta de sal na bacia e fragmentos de sino empurram Nara na costa. O pulso do Ressonador muda de alcance e consequência por região: raízes atrasam o passo no bosque, crosta de sal reduz o movimento por mais tempo na bacia e a onda costeira empurra Nara. O Tecelão só prepara seu pulso quando há um aliado ferido dentro do alcance, evitando sinais sem efeito. As legendas de perigo também nomeiam o papel e a ação: por exemplo, “Atirador — estilhaço de sal” ou “Tecelão — cura dos Ecos”. A equipe completa aparece na sala de combate da Costa, com dois Atiradores, um Ressonador e um Tecelão; a sala da Bacia apresenta o Ressonador ao lado de Caçador e Atirador. Guardiões mantêm arte exclusiva de seu bioma. Ecos em perseguição tentam contornar obstáculos; atiradores reposicionam quando uma rocha bloqueia o disparo. Encostar num Eco não causa dano: ele vem de ataques anunciados ou projéteis. O círculo de aviso do Ressonador e do guardião da costa corresponde ao raio efetivo de dano.

Cada arena de guardião agora tem um cenário pintado próprio, além do fundo e das salas centrais exclusivas dos biomas: o bosque concentra raízes e pedras de escuta em torno de uma clareira; a arena da Bacia usa rocha de sal escurecida para preservar o contraste dos avisos; a Costa enquadra o combate com ruínas de sino, recife e poças profundas. A arte fica sob a geometria e os personagens, sem criar paredes ou alterar colisões. O smoke verifica carregamento e seleção pelo bioma e pela posição exata da sala, além dos fallbacks para as outras salas; a leitura visual em gameplay ainda precisa de avaliação.

Nara também tem sprites para o golpe básico de espada, o pulso especial expansivo e os três poderes conquistados. Os projéteis dos inimigos de sal e do superchefe usam sprites próprios.

As coberturas quadradas foram substituídas por troncos caídos, tocos, cristais e rochas, distribuídos de forma diferente pelas salas e com colisores ajustados ao formato. Nas salas de combate, agrupamentos centrais quebram linhas de tiro e oferecem desvios por lados diferentes; a validação de rota exige manter uma passagem ao portal e espaço de surgimento para os Ecos. A caminhada agora acompanha a velocidade, a esquiva deixa rastros, os ataques têm deslocamento e expansão, e os personagens reagem ao dano e caem antes de desaparecer ou retornar à sala. O jogo dá um breve período de proteção após Nara receber dano para evitar que salvas de projéteis a eliminem de uma vez. Barras de vida flutuam acima de Nara, dos ecos e dos guardiões.

Os obstáculos sólidos também funcionam como **cobertura**: estilhaços inimigos param ao atingir troncos, rochas ou ruínas; o golpe básico, a Crescente Verdejante e o Rompante da Maré não causam dano através deles. O Estouro de Sal e o Pulso circular continuam a atingir através da cobertura, criando uma decisão entre proteger-se de ataques direcionais e sair de áreas anunciadas. Selos de Eco também exigem linha livre para poderes direcionais.

No bosque alagado, as poças rasas reduzem a velocidade de caminhada para 78%, mas não causam dano. A esquiva mantém o impulso normal, permitindo atravessá-las rapidamente quando houver perigo. Na primeira entrada na água, uma mensagem explica esse efeito.

Na Fileira das Bóias, uma faixa de corrente atravessa a sala e empurra Nara para leste sem causar dano. As setas de espuma antecipam a direção e deslizam para leste num ciclo suave; é possível contornar a faixa, avançar contra o fluxo ou usar a esquiva para cruzá-la. Um cue sintetizado e discreto acompanha a entrada na corrente. A dica e o áudio da corrente são independentes dos sinais das poças, então ambos continuam disponíveis na campanha.

Para manter os sinais de combate legíveis, a tela limita efeitos visuais secundários a 24 de cada vez. Avisos de ataques e áreas perigosas têm prioridade e não são descartados por esse limite.

## Como ampliar

- `ScadrykGameDirector.cs`: estado da campanha, conteúdo e nomes das fases/salas, transições por portal, salvamento e fluxo de progressão.
- `ScadrykGameDirector.World.cs`: montagem das salas, cenário, obstáculos, rotas, pontos de surgimento e interações com objetos do mundo.
- `ScadrykGameDirector.UI.cs`: desenho do HUD, minimapa, telas narrativas, menus, mapa da jornada e opções; é uma parte da classe parcial `ScadrykGameDirector`.
- `ScadrykPlayerController.cs`: velocidade, resposta dos controles, ataque, esquiva e pulso.
- `ScadrykEnemyHealth.cs`: vida, perseguição e janela de aviso do ataque inimigo.
- `ScadrykCameraFollow.cs`: acompanhamento suave do jogador.
- `ScadrykDepthSort.cs`: ordenação de desenho por profundidade, para Nara passar à frente ou atrás das árvores e pedras.
- `ScadrykAmbientProp.cs`: movimento sutil para vegetação, cristais, água e lanternas.
- `Editor/ScadrykSceneBuilder.cs`: cria e registra a cena jogável pelo menu da Unity.
- `Editor/ScadrykArtImporter.cs`: prepara as texturas para corte dos quadros e dos sprites ambientais em tempo de execução.

O jogo original permanece no repositório como versão Windows Forms/GDI+. Os scripts dentro de `UnityPort/Assets` são a nova implementação Unity e foram excluídos da compilação Windows pelo `.csproj`.

