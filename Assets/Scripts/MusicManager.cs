using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a dedicated MusicManager GameObject in the main menu scene.
// Persists across all scene loads via DontDestroyOnLoad.
// Automatically switches between menu and game music based on the active scene name.
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioClip _menuMusicClip;
    [SerializeField] private AudioClip _musicClip;
    [SerializeField] private AudioClip _gameOverClip;
    [SerializeField] [Range(0f, 1f)] private float _volume = 0.5f;

    [Tooltip("Exact name of the main menu scene. All other scenes play the game music clip.")]
    [SerializeField] private string _menuSceneName = "MainMenu";

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
        _audioSource.loop = true;
        _audioSource.volume = _volume;
        _audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // sceneLoaded doesn't fire for the first scene since we subscribe in OnEnable
        // which runs after the scene is already loaded — handle it manually here
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip clip = sceneName == _menuSceneName ? _menuMusicClip : _musicClip;
        if (clip == null) return;

        // Don't restart if this clip is already playing (e.g. continuous game music across non-menu scenes)
        if (_audioSource.clip == clip && _audioSource.isPlaying) return;

        _audioSource.Stop();
        _audioSource.clip = clip;
        _audioSource.loop = true;
        _audioSource.Play();
    }

    public void PlayGameOver()
    {
        if (_gameOverClip == null) return;
        _audioSource.Stop();
        _audioSource.clip = _gameOverClip;
        _audioSource.loop = false;
        _audioSource.Play();
    }
}
