# Meta de desenvolvimento — Scadryk

Construir um RPG de ação 2D em C# para Windows, com combate responsivo, masmorras, loot e builds com identidade própria. Diablo orienta a sensação e a ambição de qualidade; conteúdo, personagens e apresentação continuam originais.

## Estado atual

- A Godot foi removida do projeto. A janela e o desenho usam C#/.NET 8 Windows Forms e GDI+.
- O protótipo mantém três salas, inimigos comuns, arqueiro, conjurador, elite e Guardião da Cripta.
- Combate: ataque corpo a corpo, ataque especial Redemoinho, esquiva, crítico, alvo marcado e golpes inimigos anunciados.
- Progressão: XP, níveis, escolhas de talentos, ouro, poções, três armas, armadura e relíquia.
- Salvamento local em JSON com migração do save ConfigFile da versão Godot.
- Movimento com colisão por eixo, permitindo deslizar pelas quinas.

## Próximas etapas

1. **Validar a base C#** — compilar no Windows, executar os smoke tests, conferir save novo e migração do save antigo.
2. **Aprofundar combate** — mais ataques legíveis, esquiva consistente, inimigos com funções distintas e balanceamento por sala.
3. **Expandir loot e builds** — raridades com efeitos reais, comparação de equipamento e escolhas de habilidades relevantes.
4. **Criar masmorras** — mais salas, rotas, eventos, chefes e recompensas.
5. **Finalizar apresentação** — arte original, animações, áudio e opções de acessibilidade.
6. **Preparar distribuição** — builds self-contained, testes em Windows e instruções de atualização do save.

Cada etapa deve compilar, incluir uma validação adequada e registrar as limitações conhecidas. O protótipo atual ainda não tem o volume de conteúdo de Diablo; essa escala exige desenvolvimento continuado.

