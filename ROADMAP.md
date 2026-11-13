# Meta de desenvolvimento — Scadryk

Construir, somente em C#/.NET e com o fluxo de trabalho do VS Code, um RPG de ação 2D de fantasia sombria com a riqueza de combate, exploração, loot e progressão que admiramos em Diablo. Essa é uma meta de longo prazo, não uma promessa de equivalência imediata: cada etapa deve resultar em uma versão jogável, validada e documentada. Scadryk terá cenário, história, personagens, criaturas, itens e identidade visual originais.

## História original planejada

O jogo se passa em Véspera, uma cidade construída sobre o Rio Cego. As pedras desse rio guardam as últimas palavras dos mortos. O Lume de Vigília, um farol abaixo da cidade, usa fragmentos dessas memórias para manter distante o Silêncio de Baixo.

Quando os sinos de Véspera se calam, seus moradores começam a esquecer nomes e vínculos. Ecos violentos aparecem nos lugares onde lembranças foram apagadas. O protagonista, um Escutador que consegue ouvir memórias presas na pedra, entra na cripta atrás da voz de sua irmã desaparecida. Lá enfrenta o Guardião sem Nome, antigo protetor que ainda cumpre um juramento cujo sentido esqueceu.

A investigação leva à Curadora Sileth, guardiã dos arquivos. Ela remove nomes para manter o farol aceso e acredita que a verdade pode libertar a ameaça que dorme abaixo da cidade. O conflito central não será derrotar um mal sem rosto: será decidir se Véspera deve continuar protegida por uma história mutilada ou recuperar suas memórias e enfrentar as consequências.

O prólogo, a cripta inicial e sua lembrança já iniciam essa narrativa. A praça segura de Véspera é explorável e leva ao Arquivo Norte, ao leito do Rio Cego e à escada de cinzas sob a Capela Afogada. O Selo da Vigília revela que o Lume escolhe nomes para consumir; o sino mostra que Nara atravessou o rio por vontade própria. Na Câmara do Lume, o prisma confirma que ela aceitou carregar as lembranças que o farol não consegue guardar. Sua localização e o destino da cidade seguem sem resposta. A primeira decisão da cidade agora é jogável: devolver os nomes ou sustentar o Lume, alterando a praça, um bônus de progressão e os diálogos de Sileth e Edda; a escolha fica no save.

O documento de história com cenário, personagens e resumo dos quatro capítulos está em [STORY.md](STORY.md).

## Estado atual

- A Godot foi removida do projeto. A janela e o desenho usam C#/.NET 8 Windows Forms e GDI+.
- O protótipo mantém treze salas de combate, inimigos comuns, arqueiro, conjurador, elite, Guardião da Cripta, Ecos de Tinta, Escriba Cego, Lembranças Afogadas, Barqueiro Oco, Vestígios de Brasas, Guardião das Cinzas, Mariposas de Nomes, O Nome Vazio e a Tecelã de Nomes.
- Combate: ataque corpo a corpo, ataque especial Redemoinho, esquiva, crítico, alvo marcado e golpes inimigos anunciados.
- Progressão: XP, níveis, escolhas de talentos, ouro, poções, três armas, armadura, relíquia e três talismãs que podem ocupar dois espaços de equipamento.
- Salvamento local em JSON com migração do save ConfigFile da versão Godot.
- Movimento com colisão por eixo, permitindo deslizar pelas quinas.
- Prólogo interativo, duas inscrições opcionais e lembrança de Nara ao recolher o fragmento do Relicário Vital após derrotar o Guardião.
- Praça segura de Véspera com Sileth, Edda, Livro das Ausências, missão de investigação, recompensa e retorno à cripta ou ao Arquivo Norte.
- Arquivo Norte com galeria e cofre exploráveis, inimigo à distância original, chefe com duas fases e Selo da Vigília como item de história e melhoria de dano.
- Leito do Rio Cego e Capela Afogada exploráveis, novo inimigo à distância, chefe com três ataques anunciados e Sino de Nara como item de história e melhoria de vida.
- Escada de Cinzas e Câmara do Lume exploráveis, Vestígios de Brasas, chefe com três ataques anunciados e Prisma da Primeira Vigília como revelação narrativa e melhoria de dano.
- Expedição além do Lume: Margem sem Reflexo e Reservatório das Vozes, Mariposas de Nomes, O Nome Vazio com três ataques anunciados e lembrança de Nara.
- Costa das Vozes Tardias e Claustro do Coro Partido, com a Tecelã de Nomes, três padrões anunciados, loot narrativo e revelação de que o Silêncio é o conjunto de nomes consumidos pelo Lume.
- Elites e chefes deixam talismãs originais com efeitos e compensações diferentes; duas peças únicas podem ser combinadas, com sintonias próprias para Redemoinho, críticos e esquiva; coleção e ambos os espaços persistem no save.
- A primeira escolha muda diálogos, a praça e a força da Tecelã; a nova memória, a etapa da missão e as treze câmaras já limpas ficam no save. Os ecos coletados aparecem na interface.

## Próximas etapas

1. **Consolidar a base C#** — manter compilação Windows e smoke tests, conferir save novo e migração do save antigo.
2. **Transformar a premissa em campanha** — seguir a trilha do Coro para além do Claustro, revelar como o Lume começou a escolher nomes e continuar a busca por Nara.
3. **Aprofundar combate** — mais ataques legíveis, esquiva consistente, inimigos com funções distintas e balanceamento por sala.
4. **Expandir loot e builds** — os dois espaços e as três primeiras sintonias já são jogáveis; adicionar raridades de equipamento, comparação mais rica, mais peças e combinações sem perder legibilidade.
5. **Criar masmorras e narrativa ambiental** — novas regiões, rotas, arquivos, eventos e chefes cujas mecânicas reforcem a história.
6. **Concluir a escolha final** — mostrar como as decisões acumuladas alteram Véspera, Nara, aliados e o destino do Lume de Vigília.
7. **Finalizar apresentação e distribuição** — arte e áudio originais, animações, acessibilidade, builds self-contained e testes em Windows.

Cada etapa deve compilar, incluir uma validação adequada e registrar as limitações conhecidas. A primeira decisão ramificada da campanha já muda o estado persistente de Véspera. As sintonias iniciais tornam a coleta de talismãs uma escolha de build; ainda são necessárias mais combinações, itens de raridade variável e regiões para alcançar o volume e a rejogabilidade de um RPG de ação longo.










