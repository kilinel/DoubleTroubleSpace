using System;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
namespace DoubleTroubleSpace
{
  public class SpaceRulesCheck : MonoBehaviour
  {
    private bool host;
    private bool checkRules;
    private bool finished;
    private int step;
    private float nextStep;
    private float nextLog;
    private SpaceSession session;
    private SpaceShip first;
    private SpaceShip second;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromArguments()
    {
      string[] arguments = Environment.GetCommandLineArgs();
      bool isHost = Array.IndexOf(arguments, "--space-smoke-host") >= 0;
      bool isClient = Array.IndexOf(arguments, "--space-smoke-client") >= 0;
      if (!isHost && !isClient) return;
      SpaceRulesCheck test = new GameObject("Multiplayer test").AddComponent<SpaceRulesCheck>();
      test.host = isHost;
      bool rulesRequested = Array.IndexOf(arguments, "--space-rules-test") >= 0;
      test.checkRules = isHost && rulesRequested;
      test.finished = !rulesRequested;
      DontDestroyOnLoad(test.gameObject);
    }
    private void Start()
    {
      session = FindAnyObjectByType<SpaceSession>();
      session.Room = "DTS-SimpleCode-Test";
      if (host) session.CreateRoom();
      else session.JoinRoom();
      NetworkEvents events = session.Runner.gameObject.AddComponent<NetworkEvents>();
      events.OnInput = new NetworkEvents.InputEvent();
      events.OnInput.AddListener(SendTestInput);
      session.Runner.AddCallbacks(events);
    }
    private void SendTestInput(NetworkRunner runner, NetworkInput input)
    {
      ShipInput controls = new ShipInput();
      if (finished)
      {
        controls.Fire = true;
        if (Mathf.Sin(Time.unscaledTime * 1.2f) > 0) controls.Move = 1;
        else controls.Move = -1;
      }
      input.Set(controls);
    }
    private void Update()
    {
      if (Time.unscaledTime > 90) Application.Quit();
      SpaceWorld world = SpaceWorld.Instance;
      if (world == null) return;
      if (!host && world.Status == SpaceWorld.Victory) finished = true;
      if (Time.unscaledTime > nextLog)
      {
        nextLog = Time.unscaledTime + 5;
        Debug.Log("SPACE AUDIT status=" + world.Status + " score=" + world.Score + " wave=" + world.Wave);
      }
      if (!checkRules || finished || !world.Object.HasStateAuthority || world.Status == SpaceWorld.Waiting || Time.time < nextStep) return;
      RunRules(world);
      nextStep = Time.time + 1;
    }
    private void RunRules(SpaceWorld world)
    {
      if (step == 0)
      {
        foreach (PlayerRef player in world.Runner.ActivePlayers)
        {
          NetworkObject obj;
          if (!world.Runner.TryGetPlayerObject(player, out obj)) continue;
          SpaceShip ship = obj.GetComponent<SpaceShip>();
          if (ship.Slot == 0) first = ship;
          else second = ship;
        }
        if (first == null || second == null) return;
        SpaceAudio audio = FindAnyObjectByType<SpaceAudio>();
        Check(audio != null, "audio manager is present");
        AudioSource[] sources = audio.GetComponentsInChildren<AudioSource>();
        bool musicReady = false;
        foreach (AudioSource source in sources)
          if (source.loop && source.clip != null && source.clip.name == "Raining Bits_ost") musicReady = true;
        Check(musicReady, "looping music clip is assigned");
        Check(EnemyCount(world) == 8, "first wave has eight enemies");
        first.HP = 1;
        second.HP = 1;
        second.TakeHit();
      }
      else if (step == 1)
      {
        Check(second.HP == 0 && first.HP == 1 && world.Status == SpaceWorld.Playing, "survivor keeps playing");
        SpaceUI ui = FindAnyObjectByType<SpaceUI>();
        Transform heart = ui.transform.Find("HUDPartida/VidasVermelho/Coracao1");
        Check(!heart.gameObject.activeSelf, "dead player has no hearts");
        ClearEnemies(world);
      }
      else if (step == 2)
      {
        Check(world.Wave == 2 && first.HP == 3 && second.HP == 3, "new wave restores both ships");
        Check(EnemyCount(world) == 16, "second wave has sixteen enemies");
        SpaceUI ui = FindAnyObjectByType<SpaceUI>();
        Check(ui.transform.Find("HUDPartida/VidasVermelho/Coracao3").gameObject.activeSelf, "revived player has three hearts");
        first.HP = 1;
        second.HP = 1;
        first.TakeHit();
        second.TakeHit();
      }
      else if (step == 3)
      {
        Check(world.Status == SpaceWorld.Defeat && first.HP == 0 && second.HP == 0, "both dead ends the match");
        CheckResultText();
        world.Begin();
      }
      else if (step == 4)
      {
        Check(world.Status == SpaceWorld.Playing && world.Wave == 1 && first.HP == 3 && second.HP == 3, "restart resets health and wave");
        ClearEnemies(world);
      }
      else if (step == 5)
      {
        Check(world.Wave == 2, "wave one completes");
        ClearEnemies(world);
      }
      else if (step == 6)
      {
        Check(world.Wave == 3 && EnemyCount(world) == 24, "third wave has twenty four enemies");
        ClearEnemies(world);
      }
      else
      {
        Check(world.Status == SpaceWorld.Victory, "clearing three waves wins");
        CheckResultText();
        Debug.Log("SPACE RULES ALL PASSED");
        world.Begin();
        finished = true;
      }
      step += 1;
    }
    private void CheckResultText()
    {
      SpaceUI ui = FindAnyObjectByType<SpaceUI>();
      Text result = ui.transform.Find("HUDPartida/EstadoPartida").GetComponent<Text>();
      Check(result.text.Contains("Pontos:"), "result displays points");
    }
    private int EnemyCount(SpaceWorld world)
    {
      int count = 0;
      for (int i = 0; i < world.Aliens.Length; i++)
        if (world.Aliens[i]) count += 1;
      return count;
    }
    private void ClearEnemies(SpaceWorld world)
    {
      for (int i = 0; i < world.Aliens.Length; i++) world.Aliens.Set(i, false);
    }
    private void Check(bool valid, string rule)
    {
      if (!valid)
      {
        finished = true;
        throw new InvalidOperationException("SPACE RULE FAIL: " + rule);
      }
      Debug.Log("SPACE RULE PASS: " + rule);
    }
  }
}
