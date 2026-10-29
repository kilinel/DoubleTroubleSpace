# Como entender o codigo

O jogo tem cinco scripts principais. Os comentários dentro deles estão em inglês; este guia explica o fluxo em português.

## SpaceSession: entrar na partida

1. O botão chama `CreateRoom` ou `JoinRoom`.
2. `Connect` pede ao Photon para criar ou encontrar a sala e espera a resposta.
3. Quando um jogador entra, `OnPlayerJoined` cria a nave pelo Fusion.
4. Cada nave fica ligada ao jogador que a controla.
5. `OnInput` lê o teclado e envia movimento, tiro e reinício para a simulação.

`async` e `await` permitem aguardar a rede sem travar o jogo. Os métodos vazios no final são exigidos pela interface de callbacks do Fusion.

## SpaceShip: controlar a nave

`Spawned` aplica a cor. `FixedUpdateNetwork` recebe os comandos, move a nave e limita a frequência dos tiros. `TakeHit` desconta vida. `RestoreHP` devolve os três HP e a posição inicial. `Render` suaviza o movimento e esconde a nave morta.

## SpaceWorld: regras e combate

`FixedUpdateNetwork` chama cada parte da partida em ordem:

1. Esperar dois jogadores ou aceitar o reinício.
2. `MoveEnemies`: mover a formação.
3. `CountEnemies`: verificar se a fase acabou.
4. `EnemyShoot`: controlar os tiros inimigos.
5. `MoveBullets`: mover os tiros e chamar `HitEnemy` ou `HitPlayer`.
6. `CheckDefeat`: perder apenas quando não há jogador vivo.

`NextWave` recupera o HP dos dois e prepara 8, 16 ou 24 inimigos. Depois da terceira fase, a equipe vence. Um inimigo destruído vale 100 pontos.

Os acertos verificam o caminho percorrido pelo tiro desde a atualização anterior. Isso evita que um tiro rápido atravesse um alvo sem registrar o acerto.

## SpaceUI: atualizar o Canvas

Os componentes visuais estão salvos na cena. `UpdatePlayers` consulta cada nave e mostra os corações. `UpdateScore` mostra pontos, fase e resultado. `ShowLife` deixa visível um coração para cada HP restante.

## O que precisa continuar sendo do Fusion

- `[Networked]`: sincroniza uma informação.
- `NetworkArray`: sincroniza os estados dos inimigos e dos tiros.
- `FixedUpdateNetwork`: executa a lógica na simulação de rede.
- `HasStateAuthority`: impede o cliente de decidir os acertos e a pontuação por conta própria.
- `TickTimer`: mede os intervalos de tiro e de dano usando o tempo da simulação.
- `Runner.Spawn`: cria um objeto de rede para os dois participantes.

Os modelos e materiais ficam nos prefabs e na cena. Os scripts principais não montam o cenário nem desenham a interface.

## SpaceAudio: musica e efeitos

O objeto `GameManager/Audio` guarda os clipes e duas fontes de audio: `Music`, com a musica em loop, e `Effects`, para tocar efeitos sem cortar a musica. Os botoes chamam `PlayButton` pelo Inspector. Quando uma nave atira, um inimigo morre, uma nave morre ou a equipe ganha, o host usa um RPC do Fusion para avisar as duas copias do jogo. Cada copia toca o efeito localmente. O laser e usado apenas pelos tiros dos jogadores. A explosao varia entre os tres arquivos enviados.
