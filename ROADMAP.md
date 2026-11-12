# Meta de desenvolvimento — Scadryk

Construir, somente em C#/.NET e com o fluxo de trabalho do VS Code, um RPG de ação 2D de fantasia sombria com a riqueza de combate, exploração, loot e progressão que admiramos em Diablo. Essa é uma meta de longo prazo, não uma promessa de equivalência imediata: cada etapa deve resultar em uma versão jogável, validada e documentada. Scadryk terá cenário, história, personagens, criaturas, itens e identidade visual originais.

## História original planejada

O jogo se passa em Véspera, uma cidade construída sobre o Rio Cego. As pedras desse rio guardam as últimas palavras dos mortos. O Lume de Vigília, um farol abaixo da cidade, usa fragmentos dessas memórias para manter distante o Silêncio de Baixo.

Quando os sinos de Véspera se calam, seus moradores começam a esquecer nomes e vínculos. Ecos violentos aparecem nos lugares onde lembranças foram apagadas. O protagonista, um Escutador que consegue ouvir memórias presas na pedra, entra na cripta atrás da voz de sua irmã desaparecida. Lá enfrenta o Guardião sem Nome, antigo protetor que ainda cumpre um juramento cujo sentido esqueceu.

A investigação leva à Curadora Sileth, guardiã dos arquivos. Ela remove nomes para manter o farol aceso e acredita que a verdade pode libertar a ameaça que dorme abaixo da cidade. O conflito central não será derrotar um mal sem rosto: será decidir se Véspera deve continuar protegida por uma história mutilada ou recuperar suas memórias e enfrentar as consequências.

O prólogo, a masmorra inicial e sua lembrança já iniciam essa narrativa. A praça segura de Véspera também é explorável: o jogador conversa com Edda e Sileth, investiga o Livro das Ausências e recebe a pista do Arquivo Norte. As regiões seguintes e as escolhas ainda serão implementadas e refinadas junto com a campanha.

O documento de história com cenário, personagens e resumo dos três capítulos está em [STORY.md](STORY.md).

## Estado atual

- A Godot foi removida do projeto. A janela e o desenho usam C#/.NET 8 Windows Forms e GDI+.
- O protótipo mantém três salas, inimigos comuns, arqueiro, conjurador, elite e Guardião da Cripta.
- Combate: ataque corpo a corpo, ataque especial Redemoinho, esquiva, crítico, alvo marcado e golpes inimigos anunciados.
- Progressão: XP, níveis, escolhas de talentos, ouro, poções, três armas, armadura e relíquia.
- Salvamento local em JSON com migração do save ConfigFile da versão Godot.
- Movimento com colisão por eixo, permitindo deslizar pelas quinas.
- Prólogo interativo, duas inscrições opcionais e lembrança de Nara ao recolher o fragmento do Relicário Vital após derrotar o Guardião.
- Praça segura de Véspera com Sileth, Edda, Livro das Ausências, missão de investigação, recompensa e retorno à cripta.
- Inscrições e etapa da missão ficam no save; os ecos coletados aparecem na interface.

## Próximas etapas

1. **Consolidar a base C#** — manter compilação Windows e smoke tests, conferir save novo e migração do save antigo.
2. **Transformar a premissa em campanha** — desenvolver o Arquivo Norte, aprofundar Sileth e Nara e revelar o funcionamento do Lume por meio de regiões e cenas jogáveis.
3. **Aprofundar combate** — mais ataques legíveis, esquiva consistente, inimigos com funções distintas e balanceamento por sala.
4. **Expandir loot e builds** — raridades com efeitos reais, comparação de equipamento e escolhas de habilidades relevantes.
5. **Criar masmorras e narrativa ambiental** — novas regiões, rotas, arquivos, eventos e chefes cujas mecânicas reforcem a história.
6. **Dar forma à escolha final** — permitir que as decisões do jogador alterem Véspera e o destino do Lume de Vigília.
7. **Finalizar apresentação e distribuição** — arte e áudio originais, animações, acessibilidade, builds self-contained e testes em Windows.

Cada etapa deve compilar, incluir uma validação adequada e registrar as limitações conhecidas. O protótipo atual ainda não tem o volume de conteúdo de Diablo; essa escala exige desenvolvimento continuado.





