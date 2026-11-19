# Scadryk — plano e registro de qualidade

## Verificações executadas

- Unity 6000.6.9f1 compilou a build Windows normal nas etapas de menu, salas, Arauto e narrativa.
- A build de desenvolvimento com `-scadryk-smoke` atravessou nove salas, acionou os objetos exigidos, conferiu a obtenção dos três poderes, derrotou o Arauto e alcançou a vitória. Resultado: `SCADRYK_SMOKE: SUCCESS` em `ScadrykSmoke-v2-run.log`. O teste usa dano artificial e **não mede dificuldade, legibilidade nem sensação de controle**.
- O teste não grava salvamento ou métricas do jogador. Na execução `ScadrykSmoke-v3-run.log`, cada poder foi ativado imediatamente após seu guardião e entrou em recarga, além de a campanha terminar com `SUCCESS`.
- O executável normal abriu e não apresentou exceções no `Player.log` consultado. Capturas de tela estão nos pacotes locais em `outputs/ScadrykStage4` a `ScadrykStage8`.
- Após incluir movimento remapeável e reduzir efeitos decorativos, `ScadrykSmoke-v4-run.log` repetiu o resultado `SUCCESS` sem erros de eixos de entrada. A build normal dessa etapa compilou e sua tela inicial abriu. Ainda faltam teste físico com controle, sessão manual de teclado remapeado e inspeção visual do menu de opções em uso.

## Dados observados e limites

O registro local histórico mostrou mortes repetidas na primeira sala em algumas tentativas de aproximadamente 15 segundos; outras passagens tiveram duração de 8 a 14 segundos e nenhuma morte. Os dados não identificam nível de experiência, método de controle ou intervenção de teste. Portanto, não autorizam concluir que o jogo está balanceado. A partir desta versão, `scadryk-sessions-v2.csv` acrescenta marcas de ataques difíceis de perceber pelo jogador com **F8**.

## Sessões com jogadores ainda necessárias

Convidar pelo menos um jogador iniciante, um intermediário e um experiente para jogar sem explicação externa. Para cada um, registrar:

1. tempo e mortes de cada sala no CSV;
2. marcas F8 e descrição do ataque difícil de perceber;
3. uso do Crescente, Estouro e Rompante;
4. sala em que não soube o que fazer e pista que resolveu o bloqueio;
5. se consegue resumir quem é Nara, o que são os Ecos e por que o Arauto se opõe a ela;
6. conforto do volume, legibilidade do texto, flashes, tremor e controle utilizado.

Critérios para revisão: nenhum dano sem aviso reconhecível; nenhum poder fica sem uso por duas salas após ser aprendido; sala repetitiva deve ganhar decisão própria; travamento deve produzir uma pista compreensível; jogadores de níveis diferentes devem concluir a campanha sem ajuda do desenvolvedor. Ainda não há avaliação externa suficiente para declarar essa etapa aprovada.
