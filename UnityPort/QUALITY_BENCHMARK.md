# Comparação de qualidade — critérios de trabalho

Este documento compara princípios observáveis sem copiar conteúdo. Uma build compilada ou smoke test não prova equivalência de qualidade; isso exige sessões com jogadores e observação de jogo real.

| Critério | Referência e fonte oficial | Scadryk em 19/11/2026 | Próxima verificação |
| --- | --- | --- | --- |
| Leitura do combate | [Hades: Nighty Night Update](https://www.supergiantgames.com/blog/hades-the-nighty-night-update-patch-notes/) descreve impactos direcionais e armadilhas mais visíveis. | Cada linha lateral do Arauto acompanha a trajetória e o alcance da hitbox; papéis inimigos e Arauto têm tons de aviso separados. Smoke cobre geometria e mapeamento, ainda sem execução no Unity nem avaliação auditiva. | Abrir build atual, conferir sinais visuais/sonoros durante movimento e medir reação/clareza com jogadores; validar cobertura e telas menores. |
| Variedade | [Hades: High Speed Update](https://www.supergiantgames.com/blog/gear-up-for-the-high-speed-update/) relaciona inimigos, salas e poderes à variedade. | Nove salas alternam combate, vestígios e desafios de ressonância; quatro papéis de Ecos. Falta medir se cada sala deixa uma lembrança própria. | Teste cego da campanha: jogadores identificam decisão/evento de cada sala? |
| Exploração | [Death's Door](https://www.devolverdigital.com/games/deaths-door) destaca segredos e domínio de habilidades. | Três selos opcionais usam poderes e recompensam com cura; a área necessária para revelar agora coincide com a forma de acerto visual do poder. Smoke cobre poder incorreto, posição fora da forma e cura, inclusive após liberar o terceiro poder. | Rodar smoke no Editor e observar descoberta sem instrução e valor percebido da recompensa com jogadores. |
| História | [Children of Morta](https://11bitstudios.com/games/children-of-morta/) apresenta ação guiada por história. | Abertura, vestígios e encerramento foram implementados; a compreensão dos jogadores não foi avaliada. | Pedir resumo espontâneo do conflito após jogar. |
| Produto | Referências oferecem opções de acessibilidade e controles adequados ao jogo. | Menu, pausa, opções, remapeamento, volume, escala de texto e redução de flashes/tremor implementados. Teste físico de controle e acessibilidade pendentes. | Conferir cada opção em uso e com controle físico. |

**Regra:** registrar evidências na [QA.md](QA.md), atualizar esta tabela após cada etapa e não declarar paridade sem sessões externas comparáveis.
