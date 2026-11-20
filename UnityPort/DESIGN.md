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
- **Riscos que continuam:** o diretor ainda concentra responsabilidades; o teste de smoke não valida colisões ou sensação em combate real; o layout do HUD usa áreas fixas; não há sessões atuais com participantes iniciantes, intermediários e experientes.
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
