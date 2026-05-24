using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to any active GameObject in the main menu scene.
// Loads the game scene on the first input from any device —
// keyboard, mouse click, or controller button.
public class MainMenuInput : MonoBehaviour
{
    [Tooltip("Exact name of the game scene to load (must match the scene name in Build Settings).")]
    [SerializeField] private string _gameSceneName = "GameScene";

    [Tooltip("Block input for this many seconds after the scene starts, " +
             "so a held button from a previous screen doesn't skip instantly.")]
    [SerializeField] private float _inputDelay = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip _pressSound;
    [SerializeField] [Range(0f, 1f)] private float _pressSoundVolume = 1f;

    private AudioSource _audioSource;
    private bool _inputEnabled;
    private bool _loading;

    private void Awake()
    {
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake = false;
    }

    private void Start()
    {
        Invoke(nameof(EnableInput), _inputDelay);
    }

    private void EnableInput() => _inputEnabled = true;

    private void Update()
    {
        if (!_inputEnabled || _loading) return;
        if (Input.anyKeyDown)
            StartCoroutine(LoadGameAfterSound());
    }

    private IEnumerator LoadGameAfterSound()
    {
        _loading = true;

        if (_pressSound != null)
        {
            _audioSource.PlayOneShot(_pressSound, _pressSoundVolume);
            yield return new WaitForSeconds(_pressSound.length);
        }

        SceneManager.LoadScene(_gameSceneName);
    }
}
