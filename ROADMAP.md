# Meta de desenvolvimento — Scadryk

Construir, somente em C#/.NET e com o fluxo de trabalho do VS Code, um RPG de ação 2D de fantasia sombria com a riqueza de combate, exploração, loot e progressão que admiramos em Diablo. Essa é uma meta de longo prazo, não uma promessa de equivalência imediata: cada etapa deve resultar em uma versão jogável, validada e documentada. Scadryk terá cenário, história, personagens, criaturas, itens e identidade visual originais.

## História original planejada

O jogo se passa em Véspera, uma cidade construída sobre o Rio Cego. As pedras desse rio guardam as últimas palavras dos mortos. O Lume de Vigília, um farol abaixo da cidade, usa fragmentos dessas memórias para manter distante o Silêncio de Baixo.

Quando os sinos de Véspera se calam, seus moradores começam a esquecer nomes e vínculos. Ecos violentos aparecem nos lugares onde lembranças foram apagadas. O protagonista, um Escutador que consegue ouvir memórias presas na pedra, entra na cripta atrás da voz de sua irmã desaparecida. Lá enfrenta o Guardião sem Nome, antigo protetor que ainda cumpre um juramento cujo sentido esqueceu.

A investigação leva à Curadora Sileth, guardiã dos arquivos. Ela remove nomes para manter o farol aceso e acredita que a verdade pode libertar a ameaça que dorme abaixo da cidade. O conflito central não será derrotar um mal sem rosto: será decidir se Véspera deve continuar protegida por uma história mutilada ou recuperar suas memórias e enfrentar as consequências.

O prólogo, a cripta inicial e sua lembrança já iniciam essa narrativa. A praça segura de Véspera é explorável e leva ao Arquivo Norte, ao leito do Rio Cego e à escada de cinzas sob a Capela Afogada. O Selo da Vigília revela que o Lume escolhe nomes para consumir; o sino mostra que Nara atravessou o rio por vontade própria. Na Câmara do Lume, o prisma confirma que ela aceitou carregar as lembranças que o farol não consegue guardar. Sua localização e o destino da cidade seguem sem resposta. A primeira decisão da cidade agora é jogável: devolver os nomes ou sustentar o Lume, alterando a praça, um bônus de progressão e os diálogos de Sileth e Edda; a escolha fica no save.

O documento de história com cenário, personagens e resumo dos oito capítulos está em [STORY.md](STORY.md).

## Estado atual

- A Godot foi removida do projeto. A janela e o desenho usam C#/.NET 8 Windows Forms e GDI+.
- O protótipo mantém vinte e uma salas de combate e duas áreas seguras, com inimigos comuns, arqueiro, conjurador, elite, Guardião da Cripta, Ecos de Tinta, Escriba Cego, Lembranças Afogadas, Barqueiro Oco, Vestígios de Brasas, Guardião das Cinzas, Mariposas de Nomes, O Nome Vazio, a Tecelã de Nomes, a Primeira Vigília, Cantores do Lodo, a Cartógrafa do Vão, Espectros de Papel, o Escrivão de Contrato, Ecos Salinos e a Testemunha da Maré.
- Combate: ataque corpo a corpo, ataque especial Redemoinho, Ressonância do Sino que interrompe ecos comuns, esquiva, crítico, alvo marcado e golpes inimigos anunciados.
- Combate à distância: Estilhaço de Voz é um projétil perfurante de recarga própria; percorre a linha de mira, atinge cada inimigo uma vez e para ao encontrar as paredes das salas. A Ressonância do Sino adiciona uma onda de área com interrupção limitada a ecos comuns e proteção contra interrupção nos chefes.
- Progressão: XP, níveis, escolhas de talentos, ouro, poções, três armas com rolagens de raridade e afixos, armadura, relíquia e cinco talismãs únicos que podem ocupar dois espaços de equipamento.
- Salvamento local em JSON com migração do save ConfigFile da versão Godot.
- Movimento com colisão por eixo, permitindo deslizar pelas quinas.
- Prólogo interativo, duas inscrições opcionais e lembrança de Nara ao recolher o fragmento do Relicário Vital após derrotar o Guardião.
- Praça segura de Véspera com Sileth, Edda, Livro das Ausências, missão de investigação, recompensa e retorno à cripta ou ao Arquivo Norte.
- Arquivo Norte com galeria e cofre exploráveis, inimigo à distância original, chefe com duas fases e Selo da Vigília como item de história e melhoria de dano.
- Leito do Rio Cego e Capela Afogada exploráveis, novo inimigo à distância, chefe com três ataques anunciados e Sino de Nara como item de história e melhoria de vida.
- Escada de Cinzas e Câmara do Lume exploráveis, Vestígios de Brasas, chefe com três ataques anunciados e Prisma da Primeira Vigília como revelação narrativa e melhoria de dano.
- Expedição além do Lume: Margem sem Reflexo e Reservatório das Vozes, Mariposas de Nomes, O Nome Vazio com três ataques anunciados e lembrança de Nara.
- Costa das Vozes Tardias e Claustro do Coro Partido, com a Tecelã de Nomes, três padrões anunciados, loot narrativo e revelação de que o Silêncio é o conjunto de nomes consumidos pelo Lume.
- Bacia do Primeiro Nome e Sala da Primeira Vigília, com Ilyra, uma guardiã ecoica que anuncia três ataques; sua memória revela a intenção original do Lume e aponta a trilha de Nara para oeste.
- Vau dos Nomes Escolhidos e Pátio das Testemunhas, com travessia iniciada por Edda, Cantores do Lodo e a Cartógrafa do Vão. O mapa de Nara revela um refúgio fundado por ela para quem escolhe o próprio nome e abre a trilha para oeste sem confirmar onde ela está.
- Refúgio do Veio Calmo, Trilha dos Luminários e Cisterna do Registro Vazio: área segura com três moradores, rota com Espectros de Papel e chefe Escrivão de Contrato. A comunidade guarda o registro compartilhado sob suas próprias regras; o conteúdo confirma apenas que Nara continuou viagem, sem declarar seu nome atual nem seu paradeiro.
- Várzea das Vozes Sopradas e Ponte da Maré Invertida: duas salas a oeste do refúgio, com Ecos Salinos à distância e a Testemunha da Maré, chefe de três ataques anunciados. Sua lembrança confirma que Nara respondeu pelo nome escolhido diante de um grupo que seguiu viagem por vontade própria; não revela o nome nem o destino.
- Elites e chefes deixam cinco talismãs originais com efeitos e compensações diferentes; duas peças únicas podem ser combinadas, com sintonias para Redemoinho, críticos, esquiva e alcance; coleção e ambos os espaços persistem no save.
- A primeira escolha muda diálogos, a praça e a força de cinco chefes; memórias, etapas da missão e as vinte e uma câmaras já limpas ficam no save. Os ecos coletados aparecem na interface.
- A Marca da Vigília especializa dano contra chefes; o Fio da Margem amplia o alcance do Estilhaço com custo de recarga e sintoniza com o Vidro. O Fio de Travessia amplia a onda do Sino e o Vidro reforça seu dano. Domínio reduz a recarga das três técnicas; ataques corpo a corpo e à distância permitem builds de contato, controle, alcance e caça a chefes.

## Próximas etapas

1. **Consolidar a base C#** — manter compilação Windows e smoke tests, conferir save novo e migração do save antigo.
2. **Transformar a premissa em campanha** — Ilyra revela o propósito original do Lume; o Vau e o mapa de Nara introduzem o refúgio escolhido. O Refúgio, a trilha, a cisterna, a Várzea e a Ponte já são jogáveis; continuar além da nascente sem presumir que Nara quer reencontrar o protagonista.
3. **Aprofundar combate** — Estilhaço e Ressonância do Sino ampliam as opções de alcance e controle, com sintonias próprias; evoluir animações, habilidades equipáveis, leitura de ataques, esquiva consistente e balanceamento por sala.
4. **Expandir loot e builds** — armas Comuns, Raras e Relíquias agora recebem bônus de dano e afixos de Trégua, Contracanto ou Maré; as rolagens persistem no save e aparecem no inventário. Cinco talismãs e dois espaços criam especializações; aprofundar famílias de equipamento, afixos combináveis, comparação de itens e habilidades alternativas sem perder legibilidade.
5. **Criar masmorras e narrativa ambiental** — novas regiões, rotas, arquivos, eventos e chefes cujas mecânicas reforcem a história.
6. **Concluir a escolha final** — mostrar como as decisões acumuladas alteram Véspera, Nara, aliados e o destino do Lume de Vigília.
7. **Finalizar apresentação e distribuição** — arte e áudio originais, animações, acessibilidade, builds self-contained e testes em Windows.

Cada etapa deve compilar, incluir uma validação adequada e registrar as limitações conhecidas. A decisão ramificada de Véspera muda o estado persistente da cidade e a dificuldade de cinco chefes. O sétimo capítulo apresenta a comunidade que Nara ajudou a formar; o oitavo acrescenta a décima região e a nascente, sem resolver sua localização, identidade atual ou vontade de voltar. O ataque à distância e a Ressonância do Sino já sustentam abordagens distintas, e as três raridades de arma mudam dano e afixos; ainda são necessários mais conjuntos de habilidades, famílias de itens, variedade de builds e regiões para alcançar o volume e a rejogabilidade de um RPG de ação longo.











## Trabalho em andamento

- Capítulo VIII: a Várzea das Vozes Sopradas e a Ponte da Maré Invertida estão jogáveis. A etapa adiciona os Ecos Salinos, a Testemunha da Maré com três ataques anunciados, a lembrança persistente da nascente e a recompensa de Aven; mantém em aberto o nome escolhido, o paradeiro e a vontade atual de Nara.


