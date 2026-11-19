# Scadryk — diagnóstico e direção

## Revisão de estado (19/11/2026, após `95886b2`)

**Já implementado:** campanha top-down com três biomas e nove salas; três poderes de guardiões; quatro papéis de Eco; Arauto em fases; objetivos de ressonância, vestígios narrativos, menu, pausa, opções de acessibilidade, remapeamento e áudio procedural. Há salvamento de progresso e telemetria local. A cobertura de cenário bloqueia alguns projéteis e ataques, e passou pelo smoke test de campanha.

**Validação pendente:** a build normal mais recente é Stage9. A mudança de cobertura ainda não tem build normal, porque o Windows bloqueou `Bee.TinyProfiler2.dll` do Unity por política App Control. Não há jogadores externos disponíveis agora; controle físico, legibilidade em telas menores, balanceamento e conclusão manual das nove salas continuam sem evidência suficiente.

**Riscos técnicos:** `ScadrykGameDirector.cs` concentra vários sistemas; o HUD IMGUI usa medidas absolutas; testes automatizados usam dano artificial e não validam sensação ou dificuldade. Não declarar a campanha finalizada nem paridade de qualidade antes da build normal atualizada e sessões de jogo observadas.

## Diagnóstico inicial e direção criativa

Nara é uma Ouvinte que preserva histórias em três sinos de travessia. Os Ecos são memórias separadas pelo silêncio do Arauto. O bosque alagado guarda vozes nas raízes; a bacia de sal cristalizou a última maré; a costa das badaladas usa ruínas e sinos como instrumentos. Cada guardião ensina um poder. O Arauto tenta impedir que a memória da catástrofe volte ao mundo; sua derrota reconecta os lugares por um portal colossal. A câmera permanece top-down, com profundidade criada por sombras, oclusão e escala.

## Critérios por etapa

1. Refinar movimento, mira, esquiva, ataques legíveis e poderes táticos.
2. Dar trajetos e decisões próprios às nove salas e usos de exploração aos poderes.
3. Ensinar padrões com inimigos e guardiões antes de combiná-los no Arauto.
4. Apresentar abertura, vestígios, mudanças no mundo, áudio e encerramento.
5. Completar salvamento, opções, suporte a controle e QA com sessões documentadas.

As referências de outros jogos orientam clareza, descoberta e ritmo. Não reproduzir conteúdo protegido nem alegar paridade sem evidência comparativa.
