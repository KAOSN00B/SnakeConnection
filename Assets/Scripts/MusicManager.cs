using UnityEngine;

// Attach to a dedicated MusicManager GameObject in the scene.
// Persists across scene reloads so music doesn't restart on retry.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioClip _musicClip;
    [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.clip = _musicClip;
        _audioSource.loop = true;
        _audioSource.volume = _volume;
        _audioSource.playOnAwake = false;

        if (_musicClip != null)
            _audioSource.Play();
    }
}
