# Double Trouble Space

Space Invaders cooperativo em Unity 3D com Photon Fusion 2. Dois pilotos enfrentam três fases com 8, 16 e 24 alienígenas. Cada inimigo destruído vale 100 pontos. Cada nave começa com 3 HP e só perde vida quando um tiro inimigo a acerta. A equipe perde quando as duas naves morrem.

## Abrir e jogar

1. Abra a pasta interna `DoubleTroubleSpace` na Unity **6000.5.1f1**.
2. Abra `Assets/Scenes/Game.unity` e pressione Play.
3. Digite um nome de sala e clique em **CRIAR SALA**.
4. Abra `DoubleTroubleSpace/Build/DoubleTroubleSpace.exe`, digite o mesmo nome e clique em **ENTRAR**. Também é possível abrir duas cópias do executável.
5. A partida começa automaticamente quando os dois pilotos estão conectados. É necessário acesso à internet e o mesmo App ID Photon nas duas cópias.

Para gerar o executável, use **Double Trouble > Gerar executavel Windows**. Esse comando salva a cena, atualiza a tabela de prefabs do Fusion e gera a build. A cena e os modelos já estão salvos no projeto e são editados diretamente na Unity.

## Editar a interface no editor

A interface está salva na cena `Game`, em `GameManager > Canvas`. Expanda `MenuInicial` para editar o título, o campo do nome da sala e os botões. Em `HUDPartida` ficam os textos de pontos, fase, HP e estado da partida. Ele começa desativado e é ativado ao conectar. É possível ativá-lo temporariamente no Inspector para editar o posicionamento e depois desativá-lo novamente.

Os botões têm seus eventos `On Click` salvos no Inspector. O componente `SpaceUI` recebe as referências e atualiza os textos durante a partida; não desenha nem cria a interface por código. A vida aparece como três corações por jogador. Os objetos são componentes comuns da cena e podem ser editados normalmente.

## Controles

- **A/D ou setas esquerda/direita:** mover a própria nave.
- **Espaço:** atirar (pode segurar).
- **R:** reiniciar após vitória ou derrota, com dois jogadores conectados.
- **Sair:** desconectar da sala.

A nave azul e a nave vermelha identificam os jogadores. Os tiros verdes são dos inimigos. Tiros que passam reto não causam dano. Quem morre deixa de mover e atirar, mas o parceiro continua. Ao eliminar todos os inimigos da fase, o jogador morto revive e os dois ficam com 3 HP novamente. A primeira fase tem menos inimigos, movimento lento e disparos menos frequentes.

## Como a rede funciona

- `SpaceSession`: cria/entra na sessão Host/Client, recebe entradas e usa `Runner.Spawn` para criar o mundo e uma nave para cada jogador. `SetPlayerObject` associa jogador e nave.
- `SpaceShip`: cada nave recebe sua própria Input Authority. Movimento acontece em `FixedUpdateNetwork`; a posição é uma propriedade `[Networked]`, aplicada ao visual em `Render` com suavização.
- `SpaceWorld`: somente o host decide movimento dos alienígenas, disparos, colisões, dano, pontos e mudança de onda. As propriedades `[Networked]` e os `NetworkArray` replicam o estado dos inimigos e tiros para todos os participantes. Os visuais 3D são gerados localmente a partir desse estado.
- `SpaceUI`: atualiza os textos e recebe os cliques dos componentes de Canvas salvos na cena.
- `SpaceSetup`: atalho de editor para salvar a cena e gerar o executável. As ferramentas antigas de montagem automática foram removidas.

## Organizacao do projeto

- `Assets/Scenes/Game.unity`: GameManager, Canvas, EventSystem e Cenario (chao, camera, luz e pontos de inicio).
- `Assets/Prefabs/Player/SpaceShip.prefab`: nave com Fuselagem e Asas dentro de Modelo.
- `Assets/Prefabs/Enemies`: AlienGreen, AlienYellow e AlienPurple, com corpo e olhos editaveis.
- `Assets/Prefabs/Combat/Laser.prefab`: modelo do disparo.
- `Assets/Prefabs/Network/SpaceWorld.prefab`: formacao com 24 instancias dos prefabs de alienigenas e 64 tiros inicialmente desativados. A simulacao ativa apenas os necessarios.
- `Assets/Materials`: materiais separados em Ships, Enemies e Environment.
- `Assets/Scripts`: cinco scripts do jogo, separados em Network, Player, Combat, UI e Audio.
- `Assets/Tests/Multiplayer`: verificação automática separada dos scripts do jogo, ativada apenas com argumentos de teste.
- `Assets/Editor`: atalho para gerar a build.

Para alterar as formas, abra o prefab com dois cliques e edite seus filhos. Para trocar as cores, edite os materiais. Os pontos `SpawnAzul`, `SpawnVermelho` e `InicioFormacao` estao ligados ao GameManager pelo Inspector. Os scripts foram movidos junto com seus arquivos `.meta` para preservar as referencias existentes.

Os tiros usam colisão por segmento na simulação do host para evitar que atravessem alienígenas entre dois ticks. A partida aguarda dois participantes; se um cliente sai, volta a aguardar. Se o anfitrião sair, é necessário criar uma nova sala (sem migração de host).

## Sons

Os clipes ficam em `Assets/Sounds`. Em `GameManager/Audio`, o componente `SpaceAudio` permite trocar os sons. O filho `Music` tem a musica Raining Bits em loop e volume 0,22. O filho `Effects` tem volume 0,75. O som Laser1 toca nos disparos das duas naves; Explosion1/2/3 variam quando um inimigo ou nave e destruido; VictorySmall toca na vitoria. Os botoes do Canvas chamam `PlayButton`, que usa Blip_Select. Musica e clique sao locais; os eventos de combate sao enviados pelo host para os dois participantes.

## Roteiro para o vídeo

1. Mostre duas instâncias abertas e o mesmo nome de sala.
2. Crie a sala em uma e entre na outra. Mostre os dois pilotos e a partida começando.
3. Mova apenas a nave azul e mostre a mesma movimentação na outra janela.
4. Faça o mesmo com a nave vermelha.
5. Atire nas duas instâncias e mostre os mesmos alienígenas sendo destruídos e a mesma pontuação nas duas telas.
6. Mostre dano individual, um jogador continuando após a morte do parceiro e a recuperação de HP na troca de fase. Se possível, termine a partida e demonstre o reinício.

## Entrega no GitHub

Envie `Assets`, `Packages`, `ProjectSettings`, este guia e o README. O `.gitignore` já exclui Library, Temp e Build. Não é necessário subir essas pastas geradas. O projeto mantém o App ID Fusion já existente; ele identifica a aplicação e não é uma chave de administração.

