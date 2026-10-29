using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace DoubleTroubleSpace
{
  public class SpaceSession : MonoBehaviour, INetworkRunnerCallbacks
  {
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private NetworkObject worldPrefab;
    [SerializeField] private Transform blueSpawn;
    [SerializeField] private Transform redSpawn;
    [SerializeField] private Transform formationStart;
    private NetworkRunner runner;
    private string room = "DoubleTrouble01";
    private string message = "Crie uma sala ou entre na sala do seu parceiro.";
    private bool connecting;
    public NetworkRunner Runner { get { return runner; } }
    public string Room
    {
      get { return room; }
      set { room = value; }
    }
    public string Message { get { return message; } }
    public bool Connecting { get { return connecting; } }
    private void Awake()
    {
      Application.runInBackground = true;
      DontDestroyOnLoad(gameObject);
    }
    public void CreateRoom()
    {
      Connect(GameMode.Host);
    }
    public void JoinRoom()
    {
      Connect(GameMode.Client);
    }
    public void ExitRoom()
    {
      Leave();
    }
    private async void Connect(GameMode mode)
    {
      if (connecting || runner != null)
      {
        return;
      }
      if (string.IsNullOrWhiteSpace(room))
      {
        message = "Digite o nome da sala.";
        return;
      }
      room = room.Trim();
      connecting = true;
      message = "Conectando... Aguarde.";
      GameObject runnerObject = new GameObject("Fusion Runner");
      DontDestroyOnLoad(runnerObject);
      NetworkRunner newRunner = runnerObject.AddComponent<NetworkRunner>();
      runner = newRunner;
      runner.ProvideInput = true;
      runner.AddCallbacks(this);
      NetworkSceneManagerDefault sceneManager = runnerObject.AddComponent<NetworkSceneManagerDefault>();
      StartGameArgs settings = new StartGameArgs();
      settings.GameMode = mode;
      settings.SessionName = room;
      settings.PlayerCount = 2;
      settings.Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
      settings.SceneManager = sceneManager;
      settings.EnableClientSessionCreation = false;
      try
      {
        StartGameResult result = await newRunner.StartGame(settings);
        if (result.Ok)
        {
          message = "Conectado na sala " + room;
          Debug.Log("SPACE CONNECTED room=" + room + " region=" + newRunner.SessionInfo.Region);
        }
        else
        {
          message = ConnectionError(result.ShutdownReason);
          Destroy(runnerObject);
          runner = null;
        }
      }
      catch (Exception error)
      {
        message = "Erro ao conectar: " + error.Message;
        if (newRunner != null)
        {
          await newRunner.Shutdown();
        }
        if (runnerObject != null)
        {
          Destroy(runnerObject);
        }
        runner = null;
      }
      finally
      {
        connecting = false;
      }
    }
    private string ConnectionError(ShutdownReason reason)
    {
      if (reason == ShutdownReason.GameNotFound)
      {
        return "Sala nao encontrada. Crie a sala na outra janela, espere conectar e use o mesmo nome.";
      }
      if (reason == ShutdownReason.GameIsFull)
      {
        return "Essa sala ja tem dois jogadores.";
      }
      return "Conexao encerrada: " + reason;
    }
    private async void Leave()
    {
      if (runner == null)
      {
        return;
      }
      NetworkRunner oldRunner = runner;
      await oldRunner.Shutdown();
      if (oldRunner != null)
      {
        Destroy(oldRunner.gameObject);
      }
      runner = null;
      message = "Voce saiu da sala.";
    }
    public void OnPlayerJoined(NetworkRunner source, PlayerRef player)
    {
      if (!source.IsServer)
      {
        return;
      }
      if (SpaceWorld.Instance == null)
      {
        source.Spawn(worldPrefab, Vector3.zero, Quaternion.identity, null,
          (network, spawned) =>
          {
            SpaceWorld world = spawned.GetComponent<SpaceWorld>();
            world.FormationOrigin = formationStart.position;
          });
      }
      int slot = 0;
      foreach (PlayerRef otherPlayer in source.ActivePlayers)
      {
        NetworkObject otherObject;
        if (source.TryGetPlayerObject(otherPlayer, out otherObject))
        {
          SpaceShip otherShip = otherObject.GetComponent<SpaceShip>();
          if (otherShip.Slot == 0)
          {
            slot = 1;
          }
        }
      }
      Vector3 startPosition = blueSpawn.position;
      if (slot == 1)
      {
        startPosition = redSpawn.position;
      }
      NetworkObject shipObject = source.Spawn(playerPrefab, startPosition, Quaternion.identity, player,
        (network, spawned) =>
        {
          SpaceShip ship = spawned.GetComponent<SpaceShip>();
          ship.Slot = slot;
          ship.HP = SpaceShip.MaxHP;
          ship.Position = startPosition;
          ship.SpawnPosition = startPosition;
        });
      source.SetPlayerObject(player, shipObject);
      Debug.Log("SPACE PLAYER JOINED " + player + " slot=" + slot);
    }
    public void OnPlayerLeft(NetworkRunner source, PlayerRef player)
    {
      if (!source.IsServer)
      {
        return;
      }
      NetworkObject shipObject;
      if (source.TryGetPlayerObject(player, out shipObject))
      {
        source.Despawn(shipObject);
      }
    }
    public void OnInput(NetworkRunner source, NetworkInput input)
    {
      ShipInput controls = new ShipInput();
      Keyboard keyboard = Keyboard.current;
      if (keyboard != null && Application.isFocused)
      {
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
          controls.Move -= 1;
        }
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
          controls.Move += 1;
        }
        controls.Fire = keyboard.spaceKey.isPressed;
        controls.Restart = keyboard.rKey.isPressed;
      }
      input.Set(controls);
    }
    public void OnShutdown(NetworkRunner source, ShutdownReason reason)
    {
      message = ConnectionError(reason);
      if (runner == source)
      {
        runner = null;
      }
    }
    public void OnDisconnectedFromServer(NetworkRunner source, NetDisconnectReason reason)
    {
      message = "Desconectado: " + reason;
    }
    public void OnConnectFailed(NetworkRunner source, NetAddress address, NetConnectFailedReason reason)
    {
      message = "Nao foi possivel conectar: " + reason;
    }
    public void OnConnectRequest(NetworkRunner source, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
      request.Accept();
    }
    public void OnHostMigration(NetworkRunner source, HostMigrationToken token)
    {
      message = "O anfitriao saiu. Crie outra sala.";
    }
    public void OnInputMissing(NetworkRunner source, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner source) { }
    public void OnSessionListUpdated(NetworkRunner source, List<SessionInfo> list) { }
    public void OnCustomAuthenticationResponse(NetworkRunner source, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner source, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner source, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner source) { }
    public void OnSceneLoadStart(NetworkRunner source) { }
    public void OnObjectExitAOI(NetworkRunner source, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner source, NetworkObject obj, PlayerRef player) { }
  }
}
