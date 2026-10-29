using Fusion;
using UnityEngine;
using UnityEngine.UI;
namespace DoubleTroubleSpace
{
  public class SpaceUI : MonoBehaviour
  {
    [SerializeField] private SpaceSession session;
    [SerializeField] private GameObject menu;
    [SerializeField] private GameObject hud;
    [SerializeField] private InputField roomField;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Text connectionText;
    [SerializeField] private Text roomText;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text blueHP;
    [SerializeField] private Text redHP;
    [SerializeField] private Image[] blueHearts = new Image[0];
    [SerializeField] private Image[] redHearts = new Image[0];
    [SerializeField] private Text stateText;
    public void CreateRoom()
    {
      session.Room = roomField.text;
      session.CreateRoom();
    }
    public void JoinRoom()
    {
      session.Room = roomField.text;
      session.JoinRoom();
    }
    public void ExitRoom()
    {
      session.ExitRoom();
    }
    private void Update()
    {
      if (session == null)
      {
        return;
      }
      NetworkRunner runner = session.Runner;
      bool connected = runner != null && runner.IsRunning;
      menu.SetActive(!connected);
      hud.SetActive(connected);
      createButton.interactable = !session.Connecting;
      joinButton.interactable = !session.Connecting;
      roomField.interactable = !session.Connecting;
      connectionText.text = session.Message;
      if (!connected)
      {
        return;
      }
      UpdatePlayers(runner);
      UpdateScore();
    }
    private void UpdatePlayers(NetworkRunner runner)
    {
      SpaceWorld world = SpaceWorld.Instance;
      bool playing = world != null && world.Status == SpaceWorld.Playing;
      int count = 0;
      bool blueFound = false;
      bool redFound = false;
      foreach (PlayerRef player in runner.ActivePlayers)
      {
        count += 1;
        NetworkObject shipObject;
        if (!runner.TryGetPlayerObject(player, out shipObject))
        {
          continue;
        }
        SpaceShip ship = shipObject.GetComponent<SpaceShip>();
        if (ship.Slot == 0)
        {
          blueFound = true;
          ShowLife(blueHearts, blueHP, ship.HP, playing);
        }
        else
        {
          redFound = true;
          ShowLife(redHearts, redHP, ship.HP, playing);
        }
      }
      if (!blueFound)
      {
        ShowLife(blueHearts, blueHP, 0, false);
      }
      if (!redFound)
      {
        ShowLife(redHearts, redHP, 0, false);
      }
      roomText.text = "Sala: " + session.Room + "  Jogadores: " + count + "/2";
    }
    private void UpdateScore()
    {
      SpaceWorld world = SpaceWorld.Instance;
      if (world == null)
      {
        pointsText.text = "";
        stateText.text = "Carregando...";
        return;
      }
      pointsText.text = "Pontos: " + world.Score + "   Fase: " + world.Wave + "/3";
      stateText.text = "";
      if (world.Status == SpaceWorld.Waiting)
      {
        stateText.text = "Esperando o outro jogador...";
      }
      else if (world.Status == SpaceWorld.Victory)
      {
        stateText.text = "Voces ganharam!\nPontos: " + world.Score + "\nR para reiniciar";
      }
      else if (world.Status == SpaceWorld.Defeat)
      {
        stateText.text = "Fim de jogo\nPontos: " + world.Score + "\nR para reiniciar";
      }
    }
    private void ShowLife(Image[] hearts, Text message, int hp, bool playing)
    {
      for (int i = 0; i < hearts.Length; i++)
      {
        if (hearts[i] != null)
        {
          hearts[i].gameObject.SetActive(i < hp);
        }
      }
      message.text = "";
      if (hp <= 0 && playing)
      {
        message.text = "Voce volta na proxima fase";
      }
    }
  }
}
