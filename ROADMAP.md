# Meta de desenvolvimento — Scadryk

Construir, somente em C#/.NET e com o fluxo de trabalho do VS Code, um RPG de ação 2D de fantasia sombria com a riqueza de combate, exploração, loot e progressão que admiramos em Diablo. Essa é uma meta de longo prazo, não uma promessa de equivalência imediata: cada etapa deve resultar em uma versão jogável, validada e documentada. Scadryk terá cenário, história, personagens, criaturas, itens e identidade visual originais.

## História original planejada

O jogo se passa em Véspera, uma cidade construída sobre o Rio Cego. As pedras desse rio guardam as últimas palavras dos mortos. O Lume de Vigília, um farol abaixo da cidade, usa fragmentos dessas memórias para manter distante o Silêncio de Baixo.

Quando os sinos de Véspera se calam, seus moradores começam a esquecer nomes e vínculos. Ecos violentos aparecem nos lugares onde lembranças foram apagadas. O protagonista, um Escutador que consegue ouvir memórias presas na pedra, entra na cripta atrás da voz de sua irmã desaparecida. Lá enfrenta o Guardião sem Nome, antigo protetor que ainda cumpre um juramento cujo sentido esqueceu.

A investigação leva à Curadora Sileth, guardiã dos arquivos. Ela remove nomes para manter o farol aceso e acredita que a verdade pode libertar a ameaça que dorme abaixo da cidade. O conflito central não será derrotar um mal sem rosto: será decidir se Véspera deve continuar protegida por uma história mutilada ou recuperar suas memórias e enfrentar as consequências.

O prólogo, a cripta inicial e sua lembrança já iniciam essa narrativa. A praça segura de Véspera é explorável e leva ao Arquivo Norte, ao leito do Rio Cego e à escada de cinzas sob a Capela Afogada. O Selo da Vigília revela que o Lume escolhe nomes para consumir; o sino mostra que Nara atravessou o rio por vontade própria. Na Câmara do Lume, o prisma confirma que ela aceitou carregar as lembranças que o farol não consegue guardar. Sua localização e o destino da cidade seguem sem resposta; as escolhas ainda serão implementadas junto com a campanha.

O documento de história com cenário, personagens e resumo dos três capítulos está em [STORY.md](STORY.md).

## Estado atual

- A Godot foi removida do projeto. A janela e o desenho usam C#/.NET 8 Windows Forms e GDI+.
- O protótipo mantém onze salas de combate, inimigos comuns, arqueiro, conjurador, elite, Guardião da Cripta, Ecos de Tinta, Escriba Cego, Lembranças Afogadas, Barqueiro Oco, Vestígios de Brasas, Guardião das Cinzas, Mariposas de Nomes e O Nome Vazio.
- Combate: ataque corpo a corpo, ataque especial Redemoinho, esquiva, crítico, alvo marcado e golpes inimigos anunciados.
- Progressão: XP, níveis, escolhas de talentos, ouro, poções, três armas, armadura, relíquia e três talismãs que alteram habilidades, esquiva ou ataques críticos.
- Salvamento local em JSON com migração do save ConfigFile da versão Godot.
- Movimento com colisão por eixo, permitindo deslizar pelas quinas.
- Prólogo interativo, duas inscrições opcionais e lembrança de Nara ao recolher o fragmento do Relicário Vital após derrotar o Guardião.
- Praça segura de Véspera com Sileth, Edda, Livro das Ausências, missão de investigação, recompensa e retorno à cripta ou ao Arquivo Norte.
- Arquivo Norte com galeria e cofre exploráveis, inimigo à distância original, chefe com duas fases e Selo da Vigília como item de história e melhoria de dano.
- Leito do Rio Cego e Capela Afogada exploráveis, novo inimigo à distância, chefe com três ataques anunciados e Sino de Nara como item de história e melhoria de vida.
- Escada de Cinzas e Câmara do Lume exploráveis, Vestígios de Brasas, chefe com três ataques anunciados e Prisma da Primeira Vigília como revelação narrativa e melhoria de dano.
- Nova expedição além do Lume: Margem sem Reflexo e Reservatório das Vozes, Mariposas de Nomes, chefe O Nome Vazio com três ataques anunciados e lembrança de Nara como item de história.
- Elites e chefes deixam talismãs originais com efeitos e compensações diferentes; o inventário permite alternar o talismã equipado, e o save registra coleção e equipamento.
- Inscrições, etapa da missão, selo, sino, prisma, rastro de Nara e câmaras já limpas ficam no save; os ecos coletados aparecem na interface.

## Próximas etapas

1. **Consolidar a base C#** — manter compilação Windows e smoke tests, conferir save novo e migração do save antigo.
2. **Transformar a premissa em campanha** — seguir o rastro de Nara além do reservatório, revelar por que o Silêncio de Baixo cresce e dar ao jogador escolhas que afetem Nara, Sileth, Edda e Véspera.
3. **Aprofundar combate** — mais ataques legíveis, esquiva consistente, inimigos com funções distintas e balanceamento por sala.
4. **Expandir loot e builds** — adicionar níveis de raridade, comparação de equipamento, mais espaços e efeitos que combinem entre si.
5. **Criar masmorras e narrativa ambiental** — novas regiões, rotas, arquivos, eventos e chefes cujas mecânicas reforcem a história.
6. **Dar forma à escolha final** — permitir que as decisões do jogador alterem Véspera e o destino do Lume de Vigília.
7. **Finalizar apresentação e distribuição** — arte e áudio originais, animações, acessibilidade, builds self-contained e testes em Windows.

Cada etapa deve compilar, incluir uma validação adequada e registrar as limitações conhecidas. O protótipo atual já tem cinco expedições jogáveis, mas ainda não tem o volume de conteúdo de Diablo; essa escala exige desenvolvimento continuado.










