# Ficha de sessão de teste — Scadryk

Use este roteiro em uma build identificada; copie `Commit` e `Source tree` de `Build/SCADRYK_BUILD_INFO.txt`, ao lado do executável, para a ficha e mantenha esse arquivo com a build compartilhada. Para comparar participantes na mesma versão, use uma build marcada `Source tree: clean`; mantenha builds `modified` como amostras separadas e não misture seus resultados. Não ensine objetivos, segredos ou estratégias durante a primeira tentativa; apresente apenas os controles. Peça autorização antes de registrar comentários e use um apelido ou código, sem dados pessoais. O jogo grava duração, mortes, ataques difíceis de perceber, usos de poderes, exposição à corrente costeira, à corrente de sal e à água rasa, ativações de ressonância por Pulso e selos opcionais revelados por sala em `scadryk-sessions-v9.csv` dentro de `Application.persistentDataPath`. O mesmo `session_id` agrupa as salas gravadas em uma execução; ele também aparece no log como `SCADRYK_SESSION`. A ação remapeável **Marcar ataque pouco claro** (F8 no teclado e R3 no controle por padrão) também grava em `scadryk-attack-clarity-v1.csv` fase, sala e padrões inimigos recentes quando identificáveis.

## Participante e build

- Código/apelido: `____________________`
- Experiência declarada: `iniciante / intermediária / experiente`
- Familiaridade com jogos de ação top-down/ARPG: `____________________`
- Build/commit: `____________________`
- Data, `session_id` do CSV/log e dispositivo de entrada: `____________________`
- Se usou controle: modelo, conexão (USB/Bluetooth), zona morta exibida nas opções (`8–40%`) e se cada analógico ficou parado sem mover Nara, mirar ou navegar menus: `____________________`
- Controle: drift percebido, mira/movimento imprecisos ou resposta atrasada; anote em qual analógico e em que situação: `____________________`
- Observador: `____________________`

Convide ao menos duas pessoas por nível de experiência (seis sessões) antes de comparar padrões. Cada pessoa deve começar sem save anterior e jogar até concluir ou decidir parar. Não combine métricas de builds diferentes como se fossem a mesma versão.

## Protocolo uniforme

1. Use a mesma build identificada por commit para todas as seis sessões. Se o smoke da campanha não passar nessa build, adie o estudo com jogadores até corrigir o bloqueio.
2. Separe os saves dos participantes da conta pessoal do computador. Prefira um perfil local de Windows dedicado às sessões, pois o progresso usa `PlayerPrefs`; não apague saves preexistentes para preparar a máquina.
3. Peça consentimento antes de anotar respostas. Registre só o código/apelido e as informações necessárias à comparação. Não grave áudio, vídeo ou tela sem autorização específica.
4. Dê a folha de controles e mostre o vínculo atual de **Marcar ataque pouco claro** (F8/R3 por padrão); explique que ele registra um sinal/ataque que a pessoa não percebeu com clareza. Não explique salas, chefes, selos ou combinações, e não interrompa para ensinar estratégias.
5. Observe em silêncio. Registre a sala e o contexto do golpe confuso imediatamente; se o participante usar a marcação configurada, associe a linha do CSV à descrição dele sem tentar adivinhar a causa.
6. Depois de cada guardião, observe se a pessoa experimenta o novo poder por conta própria. Registre o alvo/contexto, se usou um poder para preparar outro golpe e se percebeu o efeito anunciado. Depois da sessão, peça que descreva a função de cada poder sem sugerir a resposta.
7. Encerre quando a campanha terminar ou quando a pessoa escolher parar. Registre sala final, tempo total, mortes, motivo de parada e se conseguiu retomar do save; não pressione para continuar após frustração.
8. Em uma das duas sessões de cada nível, peça para salvar e sair ao título depois de derrotar um guardião e antes de cruzar o portal. Ao continuar, confirme que o poder e a passagem permanecem e que o guardião não reaparece. Deixe a outra sessão sem essa interrupção para observar o fluxo contínuo.

### Resumo da sessão

- Resultado: `concluiu / parou` — sala final e motivo: `____________________`
- Tempo total e mortes: `____________________`
- Ataques que causaram dano sem motivo reconhecido: `____________________`
- Poder menos usado e razão observada/declarada: `____________________`
- Primeiro uso de Crescente, Estouro e Rompante (alvo e contexto): `____________________`
- Alguma combinação espontânea de poderes? Quais efeitos percebeu? `____________________`
- Encontrou algum Selo de Eco? Qual melhoria percebeu e como ela mudou seu uso do poder? `____________________`
- Depois do terceiro guardião, percebeu tempo suficiente para explorar o selo final antes do Arauto? `____________________`
- Save/retomada e opções de acessibilidade usadas: `____________________`

Se houver controle, antes da partida observe alguns segundos com os dois analógicos em repouso e registre qualquer movimento, mira ou navegação involuntária; anote o percentual atual, modelo e conexão para reproduzir o caso. Não altere a zona morta antes de registrar essa observação. Se aparecer drift ou a mira/movimento parecerem lentos, peça à pessoa para ajustar a zona morta nas opções e anote os percentuais antes e depois, além do efeito percebido. Durante cada sessão, peça à pessoa para abrir uma lembrança primeiro com teclado/mouse e depois, se houver controle disponível, trocar de dispositivo com o painel aberto. Observe se os comandos mostrados acompanham a troca, se a pausa permite ler sem pressão e se o botão usado para continuar não pula a fala seguinte. Registre se o cue sonoro da lembrança se distingue da trilha e se o toque ascendente de um selo é percebido sem se confundir com a lembrança que vem em seguida.

## Registro por sala

Depois da sessão, filtre os dois CSVs pelo `session_id` e copie os valores automáticos abaixo. O jogo preserva mortes e usos de poderes ao reiniciar a sala, então os valores da linha já cobrem as tentativas daquela sala na campanha. Se a pessoa repetir a campanha em outra execução, o ID muda; mantenha cada tentativa separada. Para cada marcação, compare o padrão salvo com o relato do jogador. Se constar `não identificado`, peça ao jogador ou observador uma descrição apenas se ainda lembrar do golpe; não adivinhe nem complete a informação depois.

Para consolidar os registros no Windows, abra PowerShell na pasta do projeto e use `.\UnityPort\Tools\Summarize-Playtests.ps1 -SessionsPath "CAMINHO\scadryk-sessions-v9.csv" -AttackClarityPath "CAMINHO\scadryk-attack-clarity-v1.csv"`. Acrescente `-SessionId "ID_DA_SESSAO"` para filtrar uma tentativa ou `-OutputPath ".\playtest-summary.csv"` para salvar as métricas por sala; sem esses parâmetros, o script mostra todas as sessões e mantém cada `session_id` separado. CSVs v8 antigos também podem ser resumidos; a nova exposição à corrente de sal fica zerada neles.

Após a sessão, registre as respostas de 1 a 5 numa linha de `PLAYTEST-RATINGS.csv`, identificada pelo `session_id` e nível `iniciante`, `intermediária` ou `experiente`; deixe sem nota as perguntas não respondidas. Os campos sobre redução de flashes/tremor e alto contraste só devem ser preenchidos se a pessoa realmente usou essas opções; não peça a ninguém para ativá-las se isso causar desconforto. Para testar a alternativa visual sem som, combine com a pessoa e silencie efeitos numa pausa segura depois da primeira tentativa, então observe uma sala de combate sem alterar a build. Não acrescente nome, email ou outros dados pessoais. Passe também `-RatingsPath .\UnityPort\PLAYTEST-RATINGS.csv` ao resumo para calcular médias separadas por nível; cada sessão aceita uma única ficha. O relatório mostra quantas respostas há em cada nível e quantas faltam para a meta de duas, e avisa quando uma amostra ainda é pequena demais para comparar médias.

Para validar o leitor sem usar dados de participantes, rode `.\UnityPort\Tools\Test-Summarize-Playtests.ps1` no PowerShell. O comando gera e remove fixtures temporários e verifica CSVs v9 e v8, cobertura parcial, ausência de respostas, fichas duplicadas e notas fora da escala. O workflow C# da raiz também executa essa checagem quando os scripts mudam; ela valida o relatório e sua compatibilidade histórica, mas não substitui o build/smoke do Unity.

| Fase / sala | Segundos ativos | Mortes | Marcações / sinais pouco claros | Crescente | Estouro | Rompante | Entradas na corrente | Segundos na corrente | Segundos no vento de sal | Segundos na água | Ressonâncias por Pulso | Selos revelados | Objetivo compreendido sem ajuda? | Comentário curto |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | --- | --- |
| Bosque alagado 1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Bosque alagado 2 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Bosque alagado 3 / guardião |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Bacia de sal 1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Bacia de sal 2 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Bacia de sal 3 / guardião |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Costa das badaladas 1 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Costa das badaladas 2 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |
| Costa das badaladas 3 / guardião e Arauto |  |  |  |  |  |  |  |  |  |  |  |  |  |  |

## Perguntas após a sessão

Respostas de 1 (discordo) a 5 (concordo), com observações:

- Eu sabia para onde ir na maior parte do tempo: `1  2  3  4  5` — nota: `____________________`
- Encontrei e entendi as opções de acessibilidade sem ajuda: `1  2  3  4  5` — quais foram usadas? `____________________`
- Com “Reduzir flashes” ativo, os efeitos ficaram confortáveis e continuaram fáceis de ler: `1  2  3  4  5` — deixe em branco se não usou.
- Com “Reduzir tremor” ativo, o movimento da câmera ficou confortável: `1  2  3  4  5` — deixe em branco se não usou.
- O alto contraste tornou os avisos mais fáceis de distinguir: `1  2  3  4  5` — deixe em branco se não usou.
- O ajuste de escala de texto tornou as instruções e lembranças mais legíveis: `1  2  3  4  5` — deixe em branco se não alterou a escala.
- Mesmo com efeitos sonoros no volume zero, os sinais visuais deixaram os ataques perigosos claros: `1  2  3  4  5`.
- Encontrei ao menos um selo opcional sem receber instruções do observador: `1  2  3  4  5` — qual pista levou até ele? `____________________`
- Consegui reconhecer os avisos antes de receber dano: `1  2  3  4  5` — nota: `____________________`
- A esquiva e os poderes responderam como eu esperava: `1  2  3  4  5` — nota: `____________________`
- Percebi quando o combo chegou ao terceiro corte, pelo arco dourado, animação e som: `1  2  3  4  5` — nota: `____________________`
- Entendi a direção da corrente e escolhi quando atravessar ou evitá-la: `1  2  3  4  5` — nota: `____________________`
- Entendi como as poças alteram o passo e como ressoar cristais/farol/sino com o Pulso: `1  2  3  4  5` — nota: `____________________`
- Os avisos sonoros continuaram distinguíveis com música e mais de um Eco atacando: `1  2  3  4  5` — nota: `____________________`
- Consegui ler cada lembrança com calma; o combate parou e os comandos do painel acompanharam o dispositivo usado: `1  2  3  4  5` — nota: `____________________`
- O sinal sonoro de abertura da lembrança ficou claro sem competir com a música: `1  2  3  4  5` — nota: `____________________`
- Percebi o toque ascendente ao libertar um selo e o distingui do som da lembrança: `1  2  3  4  5` — nota: `____________________`
- Entendi quando o portal terminou de abrir e quando podia atravessá-lo: `1  2  3  4  5` — nota: `____________________`
- As mortes pareceram evitáveis quando aconteceram: `1  2  3  4  5` — nota: `____________________`
- Qual sala foi mais interessante e por quê? `________________________________________`
- Qual sala foi mais confusa ou repetitiva? `________________________________________`
- As entradas do bosque, da bacia de sal e da costa pareceram lugares distintos? O caminho e os marcos de cada uma ficaram claros? `________________________________________`
- Que ataque causou dano sem você entender o motivo? `________________________________________`
- Que poder você usou menos e por quê? `________________________________________`
- O painel de memória mostrou um comando incorreto ou avançou uma fala sem você querer? `________________________________________`
- O que entendeu sobre Nara, Nima e o silêncio dos sinos? `________________________________________`
- Onde parou, se não terminou, e o que tentou fazer? `________________________________________`

## Revisão e ação

1. Reúna os seis registros e compare primeiro cada sala entre os três níveis de experiência.
2. Investigue toda marca de ataque pouco claro com o relato do jogador ou a descrição do observador. Corrija qualquer dano reproduzível sem aviso legível e qualquer bloqueio que impeça concluir a sala.
3. Se duas pessoas ou mais ficarem presas no mesmo objetivo, ajuste a pista ou o mapa antes de aumentar a dificuldade.
4. Se um poder ficar sem uso por um grupo, procure a causa na sala, na recarga ou na clareza do efeito; não aumente o dano automaticamente.
5. Registre a mudança, a build seguinte e repita a sala afetada. Não preencha esta ficha com resultados hipotéticos: os registros começam vazios até que pessoas joguem a build.
6. Se o terceiro corte não ficar claro, ajuste contraste, som ou animação antes de alterar dano e recarga. Se os avisos simultâneos ficarem difíceis de distinguir, ajuste a mixagem e repita uma luta com vários Ecos.
7. Se as pessoas tentarem atravessar antes do portal abrir e não entenderem a espera, reveja a mensagem de estado e a duração visual antes de alterar o bloqueio de progressão.
