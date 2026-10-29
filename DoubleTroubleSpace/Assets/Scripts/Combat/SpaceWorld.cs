using Fusion;
using UnityEngine;
namespace DoubleTroubleSpace
{
  public struct SpaceBullet : INetworkStruct
  {
    public Vector3 Position;
    public int Kind;
  }
  public class SpaceWorld : NetworkBehaviour
  {
    public const int Waiting = 0;
    public const int Playing = 1;
    public const int Victory = 2;
    public const int Defeat = 3;
    public static SpaceWorld Instance { get; private set; }
    [Networked] public int Status { get; set; }
    [Networked] public int Wave { get; set; }
    [Networked] public int Score { get; set; }
    [Networked] public Vector3 Formation { get; set; }
    [Networked] public Vector3 FormationOrigin { get; set; }
    [Networked] private int Direction { get; set; }
    [Networked] private TickTimer AlienShot { get; set; }
    [Networked, Capacity(24)] public NetworkArray<NetworkBool> Aliens => default;
    [Networked, Capacity(64)] public NetworkArray<SpaceBullet> Bullets => default;
    [SerializeField] private Transform[] aliens = new Transform[24];
    [SerializeField] private Transform[] bullets = new Transform[64];
    [SerializeField] private Material[] bulletMaterials;
    public override void Spawned()
    {
      Instance = this;
      if (Object.HasStateAuthority)
      {
        Direction = 1;
      }
    }
    public void Begin()
    {
      if (!Object.HasStateAuthority) return;
      Score = 0;
      Wave = 0;
      Status = Playing;
      NextWave();
    }
    private void NextWave()
    {
      Wave += 1;
      foreach (PlayerRef player in Runner.ActivePlayers)
      {
        NetworkObject shipObject;
        if (Runner.TryGetPlayerObject(player, out shipObject))
        {
          shipObject.GetComponent<SpaceShip>().RestoreHP();
        }
      }
      if (Wave > 3)
      {
        Wave = 3;
        Status = Victory;
        RPC_PlayVictory();
        return;
      }
      Formation = FormationOrigin;
      Direction = 1;
      int enemyCount = Wave * 8;
      for (int i = 0; i < Aliens.Length; i++)
      {
        Aliens.Set(i, i < enemyCount);
      }
      for (int i = 0; i < Bullets.Length; i++)
      {
        Bullets.Set(i, new SpaceBullet());
      }
      AlienShot = TickTimer.CreateFromSeconds(Runner, 3);
    }
    public Vector3 AlienPosition(int index)
    {
      int column = index % 8;
      int row = index / 8;
      float x = (column - 3.5f) * 2;
      float z = row * 2;
      return Formation + new Vector3(x, 0, z);
    }
    public void Fire(Vector3 position, int slot)
    {
      if (Object.HasStateAuthority)
      {
        if (AddBullet(position, slot + 1))
        {
          RPC_PlayLaser();
        }
      }
    }
    private bool AddBullet(Vector3 position, int kind)
    {
      for (int i = 0; i < Bullets.Length; i++)
      {
        if (Bullets[i].Kind == 0)
        {
          SpaceBullet shot = new SpaceBullet();
          shot.Position = position;
          shot.Kind = kind;
          Bullets.Set(i, shot);
          return true;
        }
      }
      return false;
    }
    public override void FixedUpdateNetwork()
    {
      if (!Object.HasStateAuthority) return;
      int playerCount = 0;
      bool restartPressed = false;
      foreach (PlayerRef player in Runner.ActivePlayers)
      {
        playerCount += 1;
        ShipInput controls;
        if (Runner.TryGetInputForPlayer(player, out controls) && controls.Restart)
        {
          restartPressed = true;
        }
      }
      if (playerCount < 2)
      {
        if (Status == Playing) Status = Waiting;
        return;
      }
      if (Status == Waiting || (restartPressed && Status != Playing))
      {
        Begin();
      }
      if (Status != Playing) return;
      MoveEnemies();
      if (CountEnemies() == 0)
      {
        NextWave();
        return;
      }
      EnemyShoot();
      MoveBullets();
      CheckDefeat();
    }
    private void MoveEnemies()
    {
      Vector3 position = Formation;
      float speed = 0.5f + Wave * 0.4f;
      position.x += Direction * speed * Runner.DeltaTime;
      if (Mathf.Abs(position.x) >= 3)
      {
        position.x = Mathf.Clamp(position.x, -3, 3);
        Direction = -Direction;
        position.z = Mathf.Max(-5, position.z - 0.45f);
      }
      Formation = position;
    }
    private int CountEnemies()
    {
      int count = 0;
      for (int i = 0; i < Aliens.Length; i++)
      {
        if (Aliens[i]) count += 1;
      }
      return count;
    }
    private void EnemyShoot()
    {
      if (!AlienShot.ExpiredOrNotRunning(Runner)) return;
      int start = (Runner.Tick.Raw / 7) % Aliens.Length;
      for (int offset = 0; offset < Aliens.Length; offset++)
      {
        int index = (start + offset) % Aliens.Length;
        if (Aliens[index])
        {
          AddBullet(AlienPosition(index) - Vector3.forward, 3);
          break;
        }
      }
      float delay = 2.2f;
      if (Wave == 2) delay = 1.3f;
      else if (Wave == 3) delay = 0.85f;
      AlienShot = TickTimer.CreateFromSeconds(Runner, delay);
    }
    private void MoveBullets()
    {
      for (int i = 0; i < Bullets.Length; i++)
      {
        SpaceBullet shot = Bullets[i];
        if (shot.Kind == 0) continue;
        Vector3 previousPosition = shot.Position;
        if (shot.Kind == 3)
        {
          shot.Position.z -= (4 + Wave) * Runner.DeltaTime;
          if (HitPlayer(shot.Position, previousPosition)) shot.Kind = 0;
        }
        else
        {
          shot.Position.z += 24 * Runner.DeltaTime;
          if (HitEnemy(shot.Position, previousPosition)) shot.Kind = 0;
        }
        if (shot.Position.z > 17 || shot.Position.z < -12) shot.Kind = 0;
        Bullets.Set(i, shot);
      }
    }
    private bool HitEnemy(Vector3 position, Vector3 previousPosition)
    {
      for (int i = 0; i < Aliens.Length; i++)
      {
        if (!Aliens[i]) continue;
        Vector3 enemy = AlienPosition(i);
        bool sameColumn = Mathf.Abs(enemy.x - position.x) < 0.85f;
        bool crossedEnemy = enemy.z >= previousPosition.z - 0.6f && enemy.z <= position.z + 0.6f;
        if (sameColumn && crossedEnemy)
        {
          Aliens.Set(i, false);
          Score += 100;
          RPC_PlayExplosion(Random.Range(0, 3));
          return true;
        }
      }
      return false;
    }
    private bool HitPlayer(Vector3 position, Vector3 previousPosition)
    {
      foreach (PlayerRef player in Runner.ActivePlayers)
      {
        NetworkObject shipObject;
        if (!Runner.TryGetPlayerObject(player, out shipObject)) continue;
        SpaceShip ship = shipObject.GetComponent<SpaceShip>();
        if (ship == null || ship.HP <= 0) continue;
        bool sameColumn = Mathf.Abs(ship.Position.x - position.x) < 0.8f;
        bool crossedShip = position.z <= ship.Position.z + 0.6f && previousPosition.z >= ship.Position.z - 0.6f;
        if (sameColumn && crossedShip)
        {
          ship.TakeHit();
          return true;
        }
      }
      return false;
    }
    private void CheckDefeat()
    {
      foreach (PlayerRef player in Runner.ActivePlayers)
      {
        NetworkObject shipObject;
        if (Runner.TryGetPlayerObject(player, out shipObject))
        {
          if (shipObject.GetComponent<SpaceShip>().HP > 0) return;
        }
      }
      Status = Defeat;
    }
    public override void Render()
    {
      for (int i = 0; i < aliens.Length; i++)
      {
        aliens[i].gameObject.SetActive(Aliens[i]);
        aliens[i].position = AlienPosition(i);
      }
      for (int i = 0; i < bullets.Length; i++)
      {
        SpaceBullet shot = Bullets[i];
        bullets[i].gameObject.SetActive(shot.Kind != 0);
        bullets[i].position = shot.Position;
        if (shot.Kind != 0)
        {
          Renderer model = bullets[i].GetComponent<Renderer>();
          model.sharedMaterial = bulletMaterials[shot.Kind - 1];
        }
      }
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
      if (Instance == this) Instance = null;
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayLaser()
    {
      if (SpaceAudio.Instance != null)
      {
        SpaceAudio.Instance.PlayLaser();
      }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayExplosion(int soundIndex)
    {
      if (SpaceAudio.Instance != null)
      {
        SpaceAudio.Instance.PlayExplosion(soundIndex);
      }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayVictory()
    {
      if (SpaceAudio.Instance != null)
      {
        SpaceAudio.Instance.PlayVictory();
      }
    }
  }
}
