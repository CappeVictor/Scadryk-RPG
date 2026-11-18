# Scadryk — diagnóstico e direção

## Revisão de estado (18/11/2026, após `ac1aad5`)

**Já jogável:** três biomas top-down, nove salas em sequência, portais, três poderes obtidos de guardiões, quatro papéis de Eco comum, Arauto final, selos opcionais, salvamento de sala e métricas locais. Há sprites de cenário, Nara, inimigos, ataques e chefes em `Assets/Resources/ScadrykArt`; `ScadrykDemo.unity` contém câmera e diretor, que criam a fase em tempo de execução. A build Windows compila e abre.

**Ausente ou insuficiente:** as nove salas ainda repetem a condição de eliminar todos os inimigos; exploração e história quase não alteram a progressão. Os papéis dos Ecos usam variações de escala e cor, mas a arte de cada bioma ainda é compartilhada. Faltam sons, música, menu, opções de acessibilidade, controle com mira direita, remapeamento e sessões de QA com jogadores externos. A vitória tem apenas texto de demonstração. A campanha completa não foi validada de ponta a ponta.

**Riscos técnicos atuais:** `ScadrykGameDirector.cs` tem geração, progressão, HUD e persistência no mesmo arquivo; IMGUI usa medidas absolutas; `Input Manager (Old)` não oferece remapeamento automático; `PlayerPrefs` guarda apenas fase/sala; a barra de vida e os ataques não têm teste automatizado de regressão. A próxima etapa deve oferecer acesso confiável às opções e manter o build Windows compilável.

## Diagnóstico do projeto inspecionado (18/11/2026)

**Funciona:** a cena `ScadrykDemo.unity` inicia um jogo top-down; três biomas têm fundos e objetos próprios; nove salas são percorridas por portais; Nara se move, mira, ataca, esquiva e aprende três poderes dos guardiões; há avisos visuais de ataques, barras de vida, minimapa e Arauto do Vazio com portal final.

**Falta:** as salas variam sobretudo em obstáculos e quantidade de inimigos, sem objetivos ou segredos próprios. Os inimigos comuns de um bioma compartilham ataque e comportamento. Não há história jogável, áudio, menu inicial, salvamento, opções, remapeamento, suporte completo a controle, telemetria de sessões nem testes externos documentados. O final ainda se descreve como demonstração.

**Riscos técnicos:** `ScadrykGameDirector` concentra geração, progressão e HUD; a cena usa arte e inimigos criados em tempo de execução; o HUD usa IMGUI com medidas fixas; o Input Manager legado limita remapeamento; golpes do jogador aplicam dano no mesmo quadro do efeito, sem janela de impacto; a recriação da sala restaura toda a vida. Alterações devem manter uma build Windows compilável e testar a transição entre salas.

## Visão original

Nara é uma **Ouvinte** que preserva histórias em três sinos de travessia. Os Ecos são memórias de pessoas e lugares que o silêncio do Arauto separou de seus corpos; libertá-los revela fragmentos da verdade, sem tratá-los como monstros sem identidade. O bosque alagado guarda vozes nas raízes e ensina a abrir caminhos com o Crescente. A bacia de sal cristalizou a última maré e ensina a romper depósitos com o Estouro. A costa das badaladas usa ruínas e sinos como instrumentos para o Rompante. Os três guardiões são zeladores presos ao mesmo voto de silêncio. O Arauto tenta impedir que a memória da catástrofe volte ao mundo; na derrota, o portal colossal reconecta os três lugares. A câmera permanece top-down, com profundidade criada por sombras, oclusão e escala dos objetos.

## Etapas e critérios

1. **Fundação jogável:** leitura dos golpes, esquiva, papéis inimigos, telemetria e salvamento básico. Compilar no Unity e atravessar uma sala.
2. **Nove salas autorais:** trajetória, evento ou desafio próprio por sala, exploração opcional para cada poder, mapa e pistas. Compilar e percorrer nove salas.
3. **Guardiões e Arauto:** ensinar padrões antes de combiná-los, fases justas, portal final. Compilar e vencer a campanha.
4. **Mundo e som:** abertura, vestígios, mudança após guardiões, música e efeitos próprios, encerramento. Compilar e conferir narrativa.
5. **Produto e QA:** menu, pausa/opções, controle e remapeamento, volume, acessibilidade, coleta e análise de métricas, testes com jogadores. Publicar build e registrar limitações.

Cada etapa exige build que abra, progressão possível, poderes disponíveis imediatamente após conquista e sinais legíveis sem explicação externa. As referências de outros jogos orientam apenas princípios de clareza, descoberta e ritmo; nenhum conteúdo deles será reproduzido.
