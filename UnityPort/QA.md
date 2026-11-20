# Scadryk — plano e registro de qualidade

## Verificações executadas

- Unity 6000.6.9f1 compilou a build Windows normal nas etapas de menu, salas, Arauto e narrativa.
- A build de desenvolvimento com `-scadryk-smoke` atravessou nove salas, acionou os objetos exigidos, conferiu a obtenção dos três poderes, derrotou o Arauto e alcançou a vitória. Resultado: `SCADRYK_SMOKE: SUCCESS` em `ScadrykSmoke-v2-run.log`. O teste usa dano artificial e **não mede dificuldade, legibilidade nem sensação de controle**.
- O teste não grava salvamento ou métricas do jogador. Na execução `ScadrykSmoke-v3-run.log`, cada poder foi ativado imediatamente após seu guardião e entrou em recarga, além de a campanha terminar com `SUCCESS`.
- O executável normal abriu e não apresentou exceções no `Player.log` consultado. Capturas de tela estão nos pacotes locais em `outputs/ScadrykStage4` a `ScadrykStage8`.
- Após incluir movimento remapeável e reduzir efeitos decorativos, `ScadrykSmoke-v4-run.log` repetiu o resultado `SUCCESS` sem erros de eixos de entrada. A build normal dessa etapa compilou e sua tela inicial abriu. Ainda faltam teste físico com controle, sessão manual de teclado remapeado e inspeção visual do menu de opções em uso.
- A alteração de cobertura passou por `ScadrykSmoke-v5-run.log` com `SUCCESS`. A tentativa de build normal em 19/11/2026 foi interrompida pelo Controle de Aplicativo do Windows: `Bee.TinyProfiler2.dll` do Unity foi bloqueada pela política de integridade de código (eventos 3077/3033). A última build normal validada continua sendo `ScadrykStage9`. A alteração de cobertura ainda não satisfaz o critério de build normal nem tem captura jogável própria.
- Na revisão seguinte, foi encontrado e corrigido um caso em que projéteis inimigos sem parent podiam persistir entre salas ou após reiniciar. `ScadrykEnemyProjectile` agora é filho da raiz da sala ativa; troca de sala, reinício e conclusão limpam projéteis remanescentes. O smoke test agora cria um projétil em cada sala e verifica associação e remoção. Esta alteração ainda precisa rodar no Unity Editor; não há resultado de build/smoke novo nem captura para ela.
- Etapa de navegação/remapeamento de gamepad implementada na fonte: analógico navega, A confirma, B volta, Start pausa; a aba Controle permite associar botões aos sete comandos de combate. Checagem com controle físico, compilação e inspeção de foco visual ainda pendentes; sem resultado de build ou aceite desta etapa.
- Escopo de CI conferido: `.github/workflows/validate.yml` compila, testa e publica somente `Scadryk.csproj` (Windows Forms/.NET da raiz). O commit Unity atual não tem checks associados, e esse workflow não compila `UnityPort`. Uma build no Editor Unity continua sendo necessária para validar os scripts do port.
- Nova etapa de narrativa: Nima reaparece nos dois espaços calmos após derrotar os guardiões da floresta e do sal; cada visita oferece duas falas contextuais e um som próprio. O smoke test agora verifica presença e primeira resposta de interação. A fonte e a verificação foram preparadas, ainda sem execução no Editor, inspeção de legibilidade ou captura.
- Correção do minimapa: `BuildPhase` reconstrói como concluídas as salas anteriores ao portal salvo e o smoke verifica as marcas após cada transição. Fonte alterada; ainda sem execução no Unity.
- Etapa ambiental do bosque: poças rasas agora têm zonas de terreno que reduzem a velocidade de caminhada para 78%; a esquiva preserva seu impulso. Uma pista aparece no primeiro contato. O smoke foi ampliado para confirmar zonas ativas nas três salas do bosque e que elas aplicam redução. Ainda sem compilação ou execução no Unity Editor; não tratar a asserção de código como resultado de teste.
- Limite de efeitos secundários: a fonte mantém no máximo 24 efeitos não prioritários simultâneos e protege avisos/áreas de perigo desse orçamento. O smoke cria 40 efeitos e verifica o limite. Ainda sem execução no Unity Editor.
- Música dinâmica: mudanças de bioma e entrada do Arauto agora usam transição cruzada de 1,25 s. A fonte ainda aguarda compilação e avaliação auditiva no Unity Editor.
- Sequência de ressonância da costa: o smoke tenta o sino do recife antes do farol e exige que nada seja ativado; depois conclui na ordem ensinada pelo texto. Ainda sem execução no Editor.
- Cobertura dos poderes: depois de cada guardião, o smoke cria um alvo de vida alta, usa o poder recém-aprendido e exige dano mais seu efeito tático correspondente (interrupção, exposição ou empurrão), além de confirmar a recarga. Essa asserção está apenas na fonte e ainda precisa de compilação e execução no Unity Editor.
- Pistas de exploração: mensagens de bloqueio agora variam por sala e ficam mais específicas; a costa explica a ordem farol à esquerda → sino do recife à direita. O smoke confere que as etapas não repetem o mesmo texto. Alteração ainda sem compilação ou observação no Editor.
- Leitura do golpe de onda do Arauto: aviso, área visual no impacto e alcance de dano agora compartilham os mesmos raios (6,7 externo e 2,2 seguro), removendo uma faixa em que o jogador podia receber dano fora do efeito mostrado. Correção de código ainda sem compilação ou verificação em jogo.
- Transição entre fases do Arauto: a fase e o padrão anunciados ficam registrados até o golpe resolver, mesmo se Nara cruzar um limiar de vida durante o aviso. O smoke cruza o limiar durante a primeira salva e confere que continuam sendo disparados três projéteis. Esta nova asserção ainda não foi executada no Editor.
- Espessura dos avisos lineares: linhas de raízes e salvas agora acompanham a largura das faixas de dano e dos projéteis correspondentes, reduzindo ataques cuja área real excedia o sinal. Ainda precisa de inspeção visual e teste no Editor.
- Alcance dos objetos de ressonância: cristais e sinais da costa agora respondem dentro do raio real do Pulso; o smoke tenta ativá-los a 0,02 unidade fora do alcance e exige que continuem intactos antes de interagir dentro dele. Ainda sem execução no Editor.
- HUD em janela estreita: o minimapa é reposicionado abaixo do painel principal quando não há largura para exibi-los lado a lado; o smoke confere ausência de sobreposição e limites da janela a 700×600 com texto em 150%. Ainda sem execução ou inspeção visual no Editor.
- Opções em janela baixa: abaixo de 720px o menu reduz o espaçamento das 11 linhas de remapeamento para manter teclado, Voltar e instruções visíveis; o smoke confere o limite vertical em 600px. Ainda sem execução/inspeção no Editor.
- Menu inicial compacto: caixas de título, história, botões e rodapé agora respeitam a largura e a altura disponíveis; o smoke mede 400×360px. Ainda sem execução ou inspeção visual no Editor.
- Minimapa das nove salas: cabeçalho mostra o bioma atual e cada sala recebe um rótulo próprio; o smoke confere que os nove são únicos. Ainda sem execução ou avaliação de legibilidade no Editor.
- Escala do texto: a interface reconstrói os estilos quando a escala é alterada; a preferência agora deve surtir efeito imediatamente. A mudança ainda precisa de compilação e inspeção em 100%, 125% e 150% no Editor.
- Telemetria após morte: reiniciar a mesma sala preserva cronômetro, contagem acumulada de mortes, marcas de perigo e uso de poderes até a sala terminar; o smoke agora verifica tempo, morte e uso de poder. Esta checagem ainda não foi executada no Unity Editor.
- Proteção do save: solicitar uma nova jornada com checkpoint abre confirmação; cancelar mantém o menu e o progresso sem alterar PlayerPrefs. O smoke verifica a confirmação e o cancelamento, ainda sem execução no Editor.
- Aviso de raízes: telegráfico, efeito no chão e área de dano usam a mesma margem de colisão do jogador. O smoke verifica alvos imediatamente dentro/fora da borda; ainda sem execução ou revisão visual no Editor.
- Crescente e Rompante: dano e efeitos de controle usam os retângulos orientados dos sprites exibidos, com expansão pelo raio do colisor do inimigo; o smoke verifica as bordas frontal e lateral. Ainda sem execução, compilação ou balanceamento manual no Editor.
- Pulso: o diâmetro da arte coincide com o diâmetro do raio efetivo de dano; o smoke confere a relação. Ainda sem execução ou comparação visual no Editor.
- Ataque básico: dano e arco de espada usam a mesma pegada orientada, expandida pelo colisor inimigo; o smoke verifica limites frontal e lateral. Ainda sem execução ou avaliação manual da sensação de alcance.
- Queda de estrelas do Arauto: aviso e impacto têm o mesmo raio de 1,75 unidade do dano; o smoke verifica alvos dentro/fora da borda. Ainda sem execução ou teste de reação no Editor.
- Projéteis laterais da queda de estrelas: cada fase usa as mesmas quantidades/ângulos no anúncio e no disparo; o smoke exige um aviso e um projétil na fase 2 e três de cada na fase 3, com largura equivalente ao colisor. Ainda sem execução ou inspeção visual no Editor.
- Alcance dos avisos de projéteis: cada linha da salva do Arauto agora corresponde ao ângulo real do projétil. Os disparos da salva e da queda de estrelas expiram antes que a hitbox ultrapasse o trecho anunciado; projéteis de cristal comuns também param no alvo avisado. O smoke compara os limites configurados nas fases 1, 2 e 3. Ainda sem execução no Unity Editor ou verificação visual da duração e legibilidade das linhas.
- Arte de abertura: nova imagem `ScadrykTitleBackdrop.png` conecta os três biomas e é desenhada atrás do menu inicial com uma camada escura para contraste. O smoke exige que a textura seja carregada; a integração visual ainda não foi compilada ou inspecionada no Editor.

## Dados observados e limites

O registro local histórico mostrou mortes repetidas na primeira sala em algumas tentativas de aproximadamente 15 segundos; outras passagens tiveram duração de 8 a 14 segundos e nenhuma morte. Os dados não identificam nível de experiência, método de controle ou intervenção de teste. Portanto, não autorizam concluir que o jogo está balanceado. A telemetria atual usa `scadryk-sessions-v3.csv`, acrescenta a marca F8 para ataques difíceis de perceber, distingue salas concluídas de registros interrompidos e conta tempo ativo sem pausas.

## Sessões com jogadores ainda necessárias

Convidar pelo menos um jogador iniciante, um intermediário e um experiente para jogar sem explicação externa. O responsável pelo projeto informou em 19/11/2026 que não tem jogadores disponíveis agora; esta validação permanece pendente. Para cada um, registrar:

1. tempo e mortes de cada sala no CSV;
2. marcas F8 e descrição do ataque difícil de perceber;
3. uso do Crescente, Estouro e Rompante;
4. sala em que não soube o que fazer e pista que resolveu o bloqueio;
5. se consegue resumir quem é Nara, o que são os Ecos e por que o Arauto se opõe a ela;
6. conforto do volume, legibilidade do texto, flashes, tremor e controle utilizado.
7. se percebeu a redução de velocidade nas poças e usou a esquiva para atravessá-las.

Critérios para revisão: nenhum dano sem aviso reconhecível; nenhum poder fica sem uso por duas salas após ser aprendido; sala repetitiva deve ganhar decisão própria; travamento deve produzir uma pista compreensível; jogadores de níveis diferentes devem concluir a campanha sem ajuda do desenvolvedor. Ainda não há avaliação externa suficiente para declarar essa etapa aprovada.
