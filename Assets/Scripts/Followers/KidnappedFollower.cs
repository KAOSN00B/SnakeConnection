using System.Collections;
using UnityEngine;

// Attach to every follower prefab. Stays completely dormant until GetKidnapped() is called.
// While kidnapped: detaches from the chain, follows the hacker, and flashes orange-red as a distress signal.
// If the hacker is killed within _escapeDuration seconds, Rescue() puts the follower back at the tail.
// If the timer expires, the follower is lost forever and the hacker immediately hunts the next tail.
public class KidnappedFollower : MonoBehaviour
{
    [Tooltip("Seconds the player has to kill the hacker before the follower is lost permanently.")]
    [SerializeField] private float _escapeDuration = 5f;

    [Tooltip("How fast the follower flashes while kidnapped — lower value means faster flashing.")]
    [SerializeField] private float _distressFlashInterval = 0.15f;

    [Tooltip("X and Z offsets in world units beside/behind the hacker. Y is ignored — the follower keeps its own height.")]
    [SerializeField] private Vector3 _followOffset = new Vector3(0f, 0f, -1.5f);

    [Header("Audio")]
    [SerializeField] private AudioClip _kidnappedSound;

    // Cached component references
    private HackerMovement _hackerMovement;
    private FollowerMovement _followerMovement;
    private FolloweAttack _followerAttack;
    private Rigidbody _rb;

    // Material swap state
    private Renderer[] _renderers;
    private Material _distressMaterial;
    private Material[][] _originalMaterials;
    private Material[][] _distressMaterials;

    private int _originalLayer;
    private bool _originalIsKinematic;
    private Coroutine _countdownCoroutine;
    private Coroutine _distressFlashCoroutine;
    private bool _isKidnapped;
    public bool IsKidnapped => _isKidnapped;

    private Vector3 _originalLocalScale;

    private void Awake()
    {
        _followerMovement   = GetComponent<FollowerMovement>();
        _followerAttack     = GetComponent<FolloweAttack>();
        _rb                 = GetComponent<Rigidbody>();
        _renderers          = GetComponentsInChildren<Renderer>();
        _originalLocalScale = transform.localScale;

        // Orange-red distress color
        _distressMaterial = new Material(Shader.Find("Unlit/Color")) { color = new Color(1f, 0.3f, 0f) };

        // Pre-build all material arrays
        _originalMaterials = new Material[_renderers.Length][];
        _distressMaterials = new Material[_renderers.Length][];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _originalMaterials[i] = _renderers[i].sharedMaterials;
            _distressMaterials[i] = new Material[_originalMaterials[i].Length];
            for (int j = 0; j < _distressMaterials[i].Length; j++)
                _distressMaterials[i][j] = _distressMaterial;
        }
    }

    private void OnDestroy()
    {
        if (_distressMaterial != null) Destroy(_distressMaterial);
    }

    // Called by HackerMovement the moment it reaches the tail follower.
    public void GetKidnapped(HackerMovement hackerMovement)
    {
        if (_isKidnapped) return;
        _isKidnapped = true;

        _hackerMovement = hackerMovement;

        // Detach from the chain
        if (_followerMovement != null)
        {
            ChainManager.Instance?.RemoveFollower(_followerMovement);
            _followerMovement.enabled = false;
        }

        if (_followerAttack != null)
            _followerAttack.enabled = false;

        // Switch to the KidnappedFollower physics layer
        _originalLayer = gameObject.layer;
        gameObject.layer = LayerMask.NameToLayer("KidnappedFollower");

        // Disable physics while kidnapped so the follower doesn't fall through the floor
        if (_rb != null)
        {
            _originalIsKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
        }

        // Parent to the hacker so the follower moves with it automatically.
        Transform hackerRoot = _hackerMovement.transform.root;

        // Save world Y before parenting — the hacker root may have a different height or
        // non-unit scale, which would distort any localPosition-based Y offset.
        float savedFollowerWorldY = transform.position.y;

        transform.SetParent(hackerRoot);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Place behind the hacker using world-space math (rotation only, no scale).
        // hackerRoot.rotation * vector rotates the offset without scaling it,
        // so _followOffset.z = -1.5 always means exactly 1.5 world units behind the hacker.
        Vector3 worldFollowPosition = hackerRoot.position + (hackerRoot.rotation * new Vector3(_followOffset.x, 0f, _followOffset.z));
        worldFollowPosition.y = savedFollowerWorldY;
        transform.position = worldFollowPosition;

        if (_kidnappedSound != null)
            AudioSource.PlayClipAtPoint(_kidnappedSound, transform.position);

        _distressFlashCoroutine = StartCoroutine(DistressFlashRoutine());
        _countdownCoroutine     = StartCoroutine(EscapeCountdownRoutine());
    }

    // Called by HackerDeathFeedback when the hacker is destroyed before the timer expires.
    public void Rescue()
    {
        if (!_isKidnapped) return;
        _isKidnapped = false;

        StopAllCoroutines();
        _countdownCoroutine     = null;
        _distressFlashCoroutine = null;

        // Restore materials
        for (int i = 0; i < _renderers.Length; i++)
            _renderers[i].sharedMaterials = _originalMaterials[i];

        // Restore original physics layer and state
        gameObject.layer = _originalLayer;
        if (_rb != null)
            _rb.isKinematic = _originalIsKinematic;

        // Detach from the hacker and restore original scale
        transform.SetParent(null);
        transform.localScale = _originalLocalScale;

        // Bring the follower back online
        if (_followerMovement != null) _followerMovement.enabled = true;
        if (_followerAttack   != null) _followerAttack.enabled   = true;

        // Rejoin at the end of the current chain tail
        if (_followerMovement != null)
            ChainManager.Instance?.AddFollower(_followerMovement);

        _hackerMovement = null;
    }

    private IEnumerator DistressFlashRoutine()
    {
        bool showingDistress = false;
        while (true)
        {
            showingDistress = !showingDistress;
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].materials = showingDistress ? _distressMaterials[i] : _originalMaterials[i];

            yield return new WaitForSeconds(_distressFlashInterval);
        }
    }

    private IEnumerator EscapeCountdownRoutine()
    {
        yield return new WaitForSeconds(_escapeDuration);
        LostForever();
    }

    private void LostForever()
    {
        _isKidnapped = false;

        if (_hackerMovement != null)
            _hackerMovement.OnKidnappedFollowerLost();

        FollowerDeathFeedback deathFeedback = GetComponent<FollowerDeathFeedback>();
        if (deathFeedback != null)
            deathFeedback.TriggerDeath();
        else
            Destroy(gameObject);
    }
}
