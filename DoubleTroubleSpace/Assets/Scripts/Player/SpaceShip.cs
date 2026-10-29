using Fusion;
using UnityEngine;
namespace DoubleTroubleSpace
{
  public struct ShipInput : INetworkInput
  {
    public float Move;
    public NetworkBool Fire;
    public NetworkBool Restart;
  }
  public class SpaceShip : NetworkBehaviour
  {
    [Networked] public Vector3 Position { get; set; }
    [Networked] public Vector3 SpawnPosition { get; set; }
    [Networked] public int Slot { get; set; }
    [Networked] public int HP { get; set; }
    [Networked] private TickTimer DamageTimer { get; set; }
    [Networked] private TickTimer ShotTimer { get; set; }
    public const int MaxHP = 3;
    [SerializeField] private Transform visual;
    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Material blueMaterial;
    [SerializeField] private Material redMaterial;
    public override void Spawned()
    {
      Material shipMaterial = blueMaterial;
      if (Slot == 1)
      {
        shipMaterial = redMaterial;
      }
      foreach (Renderer body in bodyRenderers)
      {
        body.sharedMaterial = shipMaterial;
      }
      transform.position = Position;
    }
    public override void FixedUpdateNetwork()
    {
      ShipInput controls;
      if (!GetInput(out controls))
      {
        return;
      }
      SpaceWorld world = SpaceWorld.Instance;
      if (world == null || world.Status != SpaceWorld.Playing || HP <= 0)
      {
        return;
      }
      float newX = Position.x + controls.Move * 10 * Runner.DeltaTime;
      newX = Mathf.Clamp(newX, -10, 10);
      Position = new Vector3(newX, SpawnPosition.y, SpawnPosition.z);
      if (Object.HasStateAuthority && controls.Fire && ShotTimer.ExpiredOrNotRunning(Runner))
      {
        world.Fire(Position + Vector3.forward, Slot);
        ShotTimer = TickTimer.CreateFromSeconds(Runner, 0.22f);
      }
    }
    public override void Render()
    {
      transform.position = Vector3.Lerp(transform.position, Position, Time.deltaTime * 20);
      if (visual != null)
      {
        visual.gameObject.SetActive(HP > 0);
      }
    }
    public void RestoreHP()
    {
      if (!Object.HasStateAuthority)
      {
        return;
      }
      HP = MaxHP;
      Position = SpawnPosition;
      DamageTimer = default;
      ShotTimer = default;
    }
    public void TakeHit()
    {
      if (!Object.HasStateAuthority || HP <= 0)
      {
        return;
      }
      if (!DamageTimer.ExpiredOrNotRunning(Runner))
      {
        return;
      }
      HP -= 1;
      if (HP == 0 && SpaceWorld.Instance != null)
      {
        SpaceWorld.Instance.RPC_PlayExplosion(Random.Range(0, 3));
      }
      DamageTimer = TickTimer.CreateFromSeconds(Runner, 0.7f);
    }
  }
}
