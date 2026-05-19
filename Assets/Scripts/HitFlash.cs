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
    private Color[] _originalColors;
    private Coroutine _activeFlash;

    private void Awake()
    {
        // Try this object first, then walk up the hierarchy (handles player body child)
        _health = GetComponent<Health>() ?? GetComponentInParent<Health>();

        // Grab all renderers in this object and its children (catches multi-mesh characters)
        _renderers = GetComponentsInChildren<Renderer>();

        // Snapshot original material colors now so the flash can always restore correctly
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            _originalColors[i] = _renderers[i].material.color;
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
        // White out all renderers
        foreach (Renderer r in _renderers)
            r.material.color = Color.white;

        yield return new WaitForSeconds(_flashDuration);

        // Restore original colors
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].material.color = _originalColors[i];

        _activeFlash = null;
    }
}
