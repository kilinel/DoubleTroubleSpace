using UnityEngine;
namespace DoubleTroubleSpace
{
  public class SpaceAudio : MonoBehaviour
  {
    public static SpaceAudio Instance { get; private set; }
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioClip buttonSound;
    [SerializeField] private AudioClip laserSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip[] explosionSounds;
    private void Awake()
    {
      Instance = this;
    }
    private void Start()
    {
      if (musicSource.clip != null && !musicSource.isPlaying)
      {
        musicSource.Play();
      }
    }
    public void PlayButton()
    {
      effectsSource.PlayOneShot(buttonSound);
    }
    public void PlayLaser()
    {
      effectsSource.PlayOneShot(laserSound, 0.6f);
    }
    public void PlayVictory()
    {
      effectsSource.PlayOneShot(victorySound);
    }
    public void PlayExplosion(int index)
    {
      if (index >= 0 && index < explosionSounds.Length)
      {
        effectsSource.PlayOneShot(explosionSounds[index]);
      }
    }
    private void OnDestroy()
    {
      if (Instance == this)
      {
        Instance = null;
      }
    }
  }
}
