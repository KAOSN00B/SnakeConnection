using System.Collections;
using UnityEngine;

// Attach to any GameObject that has (or is a child of) a Health component.
// Flashes all renderers white for a single beat whenever the object takes damage.
//
// Works for both the player and followers:
//   - Follower prefab root: Health is on the root, renderers are in children
//   - Player body child:    Health is on the body child, renderers are on the body child
//
// The death flash in FollowerDeathFeedback runs separately and is not replaced by this —
// this is a single quick flash per hit; the death flash is the longer multi-blink on destroy.
public class HitFlash : MonoBehaviour
{
    [Tooltip("How long the white flash lasts in seconds. 0.08-0.12 feels snappy.")]
    [SerializeField] private float _flashDuration = 0.1f;

    private Health _health;
    private Renderer[] _renderers;
    private Material[][] _originalMats;
    private Material[][] _flashMats;
    private Material _flashMat;
    private Coroutine _activeFlash;

    private void Awake()
    {
        _health    = GetComponent<Health>() ?? GetComponentInParent<Health>();
        _renderers = GetComponentsInChildren<Renderer>();

        // Unlit/Color shows as flat white regardless of lighting or shader type
        _flashMat = new Material(Shader.Find("Unlit/Color")) { color = Color.white };

        _originalMats = new Material[_renderers.Length][];
        _flashMats    = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMats[i] = _renderers[i].sharedMaterials;
            _flashMats[i]    = new Material[_originalMats[i].Length];
            for (int j = 0; j < _flashMats[i].Length; j++)
                _flashMats[i][j] = _flashMat;
        }
    }

    private void OnDestroy()
    {
        if (_flashMat != null) Destroy(_flashMat);
    }

    private void OnEnable()
    {
        if (_health != null)
            _health.OnHealthChanged += OnHealthChanged;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnHealthChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int current, int max)
    {
        // Only flash on damage (current dropped), not on healing
        if (current >= max) return;

        // If already flashing, restart it so rapid hits still each register visually
        if (_activeFlash != null)
            StopCoroutine(_activeFlash);

        _activeFlash = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].materials = _flashMats[i];

        yield return new WaitForSeconds(_flashDuration);

        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].sharedMaterials = _originalMats[i];

        _activeFlash = null;
    }
}
