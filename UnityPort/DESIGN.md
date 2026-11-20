# Scadryk — diagnóstico e direção

## Revisão de estado (19/11/2026, após `95886b2`)

**Já implementado:** campanha top-down com três biomas e nove salas; três poderes de guardiões; quatro papéis de Eco; Arauto em fases; objetivos de ressonância, vestígios narrativos, menu, pausa, opções de acessibilidade, remapeamento e áudio procedural. Há salvamento de progresso e telemetria local. A cobertura de cenário bloqueia alguns projéteis e ataques, e passou pelo smoke test de campanha.

**Validação pendente:** a build normal mais recente é Stage9. A mudança de cobertura ainda não tem build normal, porque o Windows bloqueou `Bee.TinyProfiler2.dll` do Unity por política App Control. Não há jogadores externos disponíveis agora; controle físico, legibilidade em telas menores, balanceamento e conclusão manual das nove salas continuam sem evidência suficiente.

**Riscos técnicos:** `ScadrykGameDirector.cs` concentra vários sistemas; o HUD IMGUI usa medidas absolutas; testes automatizados usam dano artificial e não validam sensação ou dificuldade. Não declarar a campanha finalizada nem paridade de qualidade antes da build normal atualizada e sessões de jogo observadas.

## Inspeção de código e conteúdo (19/11/2026, branch `codex/unity-playable-phase-maps`)

- O port é um projeto Unity 6.6.9f1 válido: uma cena serializada, 16 scripts de runtime, quatro ferramentas de editor, `ProjectSettings`, pacotes e 40 PNG originais em `Resources/ScadrykArt` (cerca de 68 MB). As nove salas são construídas em runtime pelo diretor; não são nove cenas separadas.
- `ScadrykPlayerController` usa `Rigidbody2D`, aceleração, mira por mouse/analógico, ataques com janela de 0,12 s, esquiva invulnerável, pulso e três poderes. `ScadrykEnemyHealth` contém quatro papéis de Eco, avisos de ataque, cobertura por raycast, guardiões e três fases do Arauto. `ScadrykGameDirector` concentra geração de salas, portais, objetivos, saves, métricas, menus e smoke test (1.148 linhas na inspeção).
- O contrato para poderes, exploração, narrativa, opções, telemetria e cenas consta no README; a cobertura sólida e o teste da campanha existem no código. Smoke test usa dano artificial e, mesmo passando, não substitui combate jogado nem observação humana.
- **Defeito encontrado nesta revisão:** os projéteis eram objetos de cena sem pai. Ao trocar ou reiniciar a sala, o mapa antigo era destruído, mas um projétil em voo podia sobreviver até seu timeout e atingir Nara na sala seguinte. A correção associa cada projétil à sala ativa, limpa os restantes na passagem e estende o smoke test para conferir ambas as condições.
- **Etapa de acessibilidade implementada no código, ainda sem aceite:** menus agora recebem navegação pelo analógico, A confirma, B volta, Start pausa; opções têm abas de teclado e gamepad e remapeamento dos sete comandos de combate. Associações duplicadas são trocadas. A compilação no Editor e a verificação com um controle físico continuam pendentes; não contar como suporte validado.
- **Etapa narrativa em andamento:** Nima, uma memória libertada no bosque, reaparece nas chegadas calmas da bacia e da costa com falas que refletem os guardiões já vencidos. A indicação de conversa é contextual, aceita interação de teclado ou gamepad, tem uma resposta sonora própria e o smoke test verifica que a personagem aparece e responde nas duas salas. Build/execução Unity continuam pendentes.
- **Correção de progressão do mapa:** ao carregar um portal salvo, as salas anteriores da fase agora são reconstruídas como concluídas. O smoke de campanha também verifica esse estado após cada travessia. Ainda sem execução no Editor.
- **Decisão ambiental do bosque:** poças rasas reduzem a caminhada a 78%, sem causar dano; a esquiva mantém o impulso. Isso faz a água já presente no cenário influenciar o combate sem virar uma armadilha invisível. A primeira entrada exibe uma pista. A alteração e a checagem de smoke ainda aguardam compilação e execução no Editor.
- **Legibilidade visual de combate:** efeitos secundários agora têm teto de 24 simultâneos; quando a tela enche, o mais antigo é removido primeiro. Avisos de ataque e áreas de impacto ficam fora desse orçamento para preservar sinais de perigo. O smoke verifica o limite, mas ainda não foi executado no Editor.
- **Transição de trilha:** a música ambiente agora cruza suavemente entre biomas e o tema do Arauto em 1,25 s, com tempo não escalado para não parar ao pausar. A mudança ainda precisa ser compilada e escutada no Editor.
- **Ritmo da costa:** a travessia da sala 8 agora pede uma sequência ensinada no texto de entrada: o farol antes do sino do recife. O smoke recusa a ordem errada e depois percorre a correta. A etapa ainda não foi compilada nem rodada no Editor.
- **Função dos poderes:** o smoke agora ataca um alvo de vida alta logo após cada guardião e confere dano, recarga e o efeito característico do poder (interrupção, exposição ou empurrão). A cobertura ainda não foi compilada ou executada no Editor.
- **Ajuda para exploração:** as salas de objetivo agora oferecem pistas graduais específicas; a travessia da costa nomeia farol e sino e revela primeiro a ordem, depois a posição de cada sinal. Ainda aguarda compilação e leitura visual no Editor.
- **Área de dano do Arauto:** o aviso, o efeito de impacto e o dano da onda final agora usam o mesmo limite externo e o mesmo raio interno seguro. A mudança aguarda validação visual e de combate no Editor.
- **Transição de fase durante aviso:** cada golpe do Arauto mantém a fase e o padrão vistos no telegráfico até o impacto; mudar o limiar de vida no meio do aviso afeta o próximo golpe. O smoke agora cruza o limiar durante a primeira salva e exige os três projéteis anunciados; ainda falta executar no Editor.
- **Largura dos telegráficos:** linhas de raízes e salvas do Arauto/Ecos foram alinhadas à largura efetiva das hitboxes e projéteis. A legibilidade e a sensação em movimento ainda aguardam validação no Editor.
- **Alcance do Pulso na exploração:** sinais ressoam apenas dentro do raio configurado no efeito visual; o smoke tenta ativá-los logo fora desse alcance. Ainda falta executar no Editor.
- **HUD estreito:** o minimapa passa para baixo do painel principal se a largura ficar insuficiente; uma asserção de layout cobre uma janela 700×600 com escala de texto máxima. Falta validar visualmente no Editor.
- **Opções em baixa altura:** o remapeamento compacta o espaçamento das linhas abaixo de 720px, mantendo a lista de teclas e o botão Voltar dentro de uma janela de 600px. O smoke mede o limite, mas ainda não foi executado no Editor.
- **Menu principal compacto:** texto e três botões reduzem largura e espaçamento em janelas baixas; o smoke verifica que os blocos cabem a 400×360px. Falta observar a legibilidade no Editor.
- **Navegação do mapa:** o minimapa agora mostra o nome do bioma e nove rótulos distintos, em vez de repetir apenas “Entrada”, “Travessia” e “Guardião”. O smoke confere a unicidade; ainda falta avaliar legibilidade e orientação em jogo.
- **Escala do texto:** estilos de interface agora são reconstruídos quando a preferência muda, para que o ajuste de 100–150% tenha efeito imediato sem reiniciar o jogo. Falta verificar tamanhos e quebras de linha no Editor.
- **Registro de mortes:** reiniciar a sala após uma derrota mantém tempo ativo, número de mortes e uso de poderes na mesma sessão CSV; a asserção foi incluída no smoke. Ainda falta executar e conferir uma sessão real no Editor.
- **Proteção do checkpoint:** “Nova jornada” pede confirmação quando há progresso salvo; cancelar volta ao menu sem limpar o save. O smoke verifica que a confirmação abre e pode ser cancelada. Ainda falta validar a interação no Editor.
- **Área do golpe de raízes:** o aviso linear e o impacto agora incluem o raio real do colisor de Nara; a mesma largura define a borda que causa dano. O smoke verifica alvos logo dentro e fora dessa borda. Ainda falta observação visual e execução no Editor.
- **Pegada dos poderes:** Crescente e Rompante agora limitam o acerto ao retângulo orientado dos sprites de efeito, incluindo a dimensão do colisor inimigo. Isso substitui os antigos setores circulares amplos que podiam acertar fora da arte visível. O smoke verifica bordas longitudinal e lateral; falta ajustar alcance em combate manual.
- **Pulso circular:** o anel agora é desenhado com o diâmetro completo do raio de dano; antes, o sprite era reduzido a 72% do tamanho. O smoke confere que o raio visível e o raio efetivo coincidem. Falta inspeção visual no Editor.
- **Golpe básico:** a área de acerto agora segue o retângulo orientado do arco de espada, com expansão pelo colisor do Eco; o aviso visual e a hitbox compartilham dimensões. O smoke confere limites frontal e lateral. Falta ajustar a sensação e o alcance em combate manual.
- **Queda de estrelas do Arauto:** raio de aviso, impacto e dano agora é 1,75 unidade. O smoke testa a borda interna e externa; ainda falta observar o tempo de reação durante uma luta real.
- **Projéteis extras da queda de estrelas:** as fases 2 e 3 do Arauto agora anunciam cada projétil lateral com uma linha; largura, quantidade e ângulos vêm dos mesmos dados usados para disparar. O smoke verifica 1 aviso/1 projétil e 3 avisos/3 projéteis, incluindo a largura. Falta execução e revisão visual no Editor.
- **Identidade da abertura:** nova arte original liga visualmente o bosque alagado, o sal e a costa das badaladas; a tela inicial a escurece sob o menu. A verificação de carga existe no smoke, mas falta compilação e revisão visual no Editor.
- **Riscos que continuam:** o diretor ainda concentra responsabilidades; o teste de smoke não valida colisões ou sensação em combate real; o layout do HUD usa áreas fixas; não há sessões atuais com participantes iniciantes, intermediários e experientes.
- **Avisos do Arauto (20/11/2026):** trajetórias laterais e salvas agora recebem linhas individuais. O alcance dos disparos termina antes de a hitbox sair do trecho anunciado, e os volleys de cristal param no alvo sinalizado. As asserções do smoke verificam contagem, ângulos, centros, comprimentos, larguras e alcances nas três fases. Isso melhora a previsibilidade do combate no código; build, visibilidade em movimento e reação humana seguem sem validação no Editor Unity.
- **Selos opcionais (20/11/2026):** a automação agora confere que a bacia, a chegada à costa e a sala final só libertam a recompensa com o poder correto e perto do selo, e que a cura realmente ocorre. O teste da sala final respeita a ordem em que Nara conquista Rompante, após o guardião costeiro. Descoberta e valor da recompensa continuam exigindo jogadores sem orientação externa.
- **Limite do CI:** a workflow `.github/workflows/validate.yml` valida apenas o aplicativo .NET da raiz, não os scripts/cena do UnityPort. Portanto, checks ausentes no commit Unity não são sinal de compilação Unity bem-sucedida.
- A versão normal mais recente registrada é Stage9. Este ambiente não localizou o Unity Editor nos caminhos de instalação usuais; a build normal atual e a captura desta revisão, portanto, continuam pendentes. O App Control já havia bloqueado uma DLL do Editor numa tentativa anterior. Não confundir o smoke test anterior com build normal ou apresentação visual desta revisão.

## Diagnóstico inicial e direção criativa

Nara é uma Ouvinte que preserva histórias em três sinos de travessia. Os Ecos são memórias separadas pelo silêncio do Arauto. O bosque alagado guarda vozes nas raízes; a bacia de sal cristalizou a última maré; a costa das badaladas usa ruínas e sinos como instrumentos. Cada guardião ensina um poder. O Arauto tenta impedir que a memória da catástrofe volte ao mundo; sua derrota reconecta os lugares por um portal colossal. A câmera permanece top-down, com profundidade criada por sombras, oclusão e escala.

## Critérios por etapa

1. Refinar movimento, mira, esquiva, ataques legíveis e poderes táticos.
2. Dar trajetos e decisões próprios às nove salas e usos de exploração aos poderes.
3. Ensinar padrões com inimigos e guardiões antes de combiná-los no Arauto.
4. Apresentar abertura, vestígios, mudanças no mundo, áudio e encerramento.
5. Completar salvamento, opções, suporte a controle e QA com sessões documentadas.

As referências de outros jogos orientam clareza, descoberta e ritmo. Não reproduzir conteúdo protegido nem alegar paridade sem evidência comparativa.
