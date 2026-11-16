# Objetivo do Scadryk

Criar um RPG de ação 2D de fantasia sombria, em C#/.NET e desenvolvido no VS Code, com a profundidade de exploração, combate, equipamentos e progressão que torna um ARPG envolvente. Scadryk terá identidade própria: Véspera, o Lume, o Rio Cego, Nara, os personagens, as ameaças e os itens contam uma história original.

## Experiência desejada

O jogador chega a um abrigo habitado, conversa com personagens, recebe pistas e parte por caminhos que precisa percorrer. Encontra regiões grandes e visualmente distintas, desvios, masmorras, inimigos e recompensas. Retorna ao abrigo para conversar, melhorar equipamentos e preparar a próxima viagem. Missões, exploração e combate devem contribuir para a evolução do personagem.

## Como o jogo deve ficar

- **Menu e início:** tela inicial legível e atmosférica, continuação de save, nova jornada, seleção de personagem, história, configurações e saída. Navegação consistente por teclado e mouse; opções de acessibilidade e áudio claras.
- **Mundo:** regiões extensas conectadas, com câmera, pontos de referência, caminhos alternativos, abrigos, entradas de masmorra e retornos compreensíveis. Cada região tem terreno, cores, sons e encontros próprios. O mapa mostra posição e descobertas sem revelar segredos antes da hora.
- **História e personagens:** campanha original com conflitos, personagens recorrentes, diálogos que respondem ao progresso e escolhas com consequências compreensíveis. Missões opcionais enriquecem o mundo e oferecem recompensas sem bloquear a história principal.
- **Combate:** controles responsivos, animações claras, ataques anunciados, esquiva, habilidades com funções diferentes, inimigos que cooperam ou pressionam de maneiras distintas e chefes com padrões reconhecíveis. Obstáculos e terreno devem funcionar de modo coerente para jogadores e inimigos.
- **Personagens e builds:** classes com identidade própria, atributos, talentos e habilidades que permitam escolhas distintas. Os efeitos, custos e formas de redistribuição devem ser explicados no jogo.
- **Equipamentos e loot:** armas e peças com diferenças visíveis de uso, raridades, afixos e combinações interessantes. Comparação de itens, inventário, recompensas de chefes e formas de melhorar equipamentos devem apoiar decisões sem perder itens inesperadamente.
- **Progressão e repetição:** dificuldade crescente e equilibrada, encontros opcionais, objetivos de fim de jogo e motivos para revisitar áreas. O desafio deve variar por mecânicas e combinações de inimigos, além de números.
- **Interface e acessibilidade:** vida, habilidades, objetivos, diário, mapa e recompensas legíveis. Oferecer contraste, áudio e redução de tremor; preservar a orientação em resoluções diferentes.
- **Qualidade e documentação:** saves compatíveis com versões anteriores, recuperação segura de erros e instruções em português para instalar dependências, abrir no VS Code, iniciar e usar controles. Publicar as alterações solicitadas no GitHub com validação automática.

## Critério para concluir uma etapa

Uma etapa precisa ter entrada e saída, cenário e colisões coerentes, encontros e objetivos concluíveis, recompensas, orientação, tratamento de morte e salvamento, e integração com o restante do jogo. Compilação e verificações automatizadas devem passar; aparência, navegação, combate e equilíbrio também precisam ser avaliados em partida antes de declarar a etapa refinada. O usuário pediu que o jogo não seja testado no computador dele sem uma solicitação específica.

## Situação e próximos passos

A campanha principal chega ao epílogo original e oferece dezoito capítulos, quatro classes, variações de técnicas, builds, afixos, Ressonâncias e uma Vigília Ascendente. As Margens do Primeiro Eco, o Vau de Cinza e a Bacia do Sal Imóvel formam uma rota externa conectada; a Bacia inclui o abrigo de Asha e Tavi, uma missão de lanternas, cartografia opcional e a fenda dos Menires. A Costa das Badaladas agora se conecta ao extremo leste da Bacia após a missão de Asha: uma região aberta de 4.200 × 2.400 unidades, com três zonas visuais, a escutadora Nemi, três torres guardadas e o chefe opcional Coro da Maré Falsa, com três padrões telegráficos, arena revelada no mapa, recompensa de arma e retomada do encontro por save. A CI cobre a transição, os encontros, os ataques, as recompensas e a retomada da região; aparência, ritmo e equilíbrio ainda precisam de avaliação em partida. Próximas frentes: ampliar outras regiões para percursos conectados e variados, aprofundar famílias de equipamento e validar a experiência em diferentes resoluções e máquinas sem iniciar o jogo no computador do usuário.

O jogo não será considerado concluído apenas por ter muitos capítulos ou por compilar: todas as partes acima precisam funcionar juntas, ser verificadas no alcance correspondente e oferecer uma jornada completa e coesa.
