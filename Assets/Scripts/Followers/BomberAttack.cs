using UnityEngine;

// Follower attack variant: lobs grenades in a slow arc rather than firing bullets.
// Draws a curved arc preview showing the grenade's trajectory — same visual approach
// as LaserSight but with multiple points to approximate the parabola.
// Much slower fire rate than the basic follower; compensated by AoE damage on detonation.
[RequireComponent(typeof(LineRenderer))]
public class BomberAttack : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("Maximum range at which the bomber will acquire an enemy target.")]
    [SerializeField] private float _attackRange = 12f;

    [Tooltip("Seconds between each nearest-enemy scan. Lower = more responsive.")]
    [SerializeField] private float _targetRefreshInterval = 0.2f;

    [Header("Firing")]
    [Tooltip("Seconds between grenade launches — should be slow to match the high damage.")]
    [SerializeField] private float _fireRate = 3f;

    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _grenadePrefab;

    [Header("Arc Preview")]
    [Tooltip("Arc height used both for the preview line and passed to the grenade. Keep in sync with GrenadeProjectile._arcHeight.")]
    [SerializeField] private float _arcHeight = 3f;

    [Tooltip("How wide the arc preview line is.")]
    [SerializeField] private float _lineWidth = 0.08f;

    [Tooltip("Material applied to the arc preview line — assign the same laser material for visual consistency.")]
    [SerializeField] private Material _arcLineMaterial;

    [Tooltip("Number of points along the arc line — more points = smoother curve.")]
    [SerializeField] private int _arcLineResolution = 24;

    [Tooltip("How long the arc preview stays visible after a grenade is thrown.")]
    [SerializeField] private float _arcLineDisplayDuration = 1f;

    [Header("Audio")]
    [SerializeField] private AudioClip _throwSound;

    private Transform _enemyTarget;
    private float _fireCooldown;
    private float _targetRefreshTimer;
    private float _arcLineHideTime = -1f;
    private LineRenderer _arcLine;

    public bool HasTarget => _enemyTarget != null;

    private void Awake()
    {
        _arcLine = GetComponent<LineRenderer>();

        // Mirror the LaserSight setup so the arc line looks consistent with the player's laser
        _arcLine.positionCount = _arcLineResolution;
        _arcLine.useWorldSpace = true;
        _arcLine.startWidth    = _lineWidth;
        _arcLine.endWidth      = _lineWidth;
        _arcLine.startColor    = Color.white;
        _arcLine.endColor      = Color.white;
        if (_arcLineMaterial != null)
            _arcLine.sharedMaterial = _arcLineMaterial;

        _arcLine.enabled = false;
    }

    private void Update()
    {
        _targetRefreshTimer -= Time.deltaTime;
        if (_targetRefreshTimer <= 0f)
        {
            RefreshTarget();
            _targetRefreshTimer = _targetRefreshInterval;
        }

        if (_enemyTarget != null)
        {
            // Show the arc preview only for _arcLineDisplayDuration seconds after each throw
            if (Time.time < _arcLineHideTime)
                DrawArcPreview();
            else
                _arcLine.enabled = false;

            HandleFiring();
        }
        else
        {
            _arcLine.enabled = false;
        }
    }

    private void RefreshTarget()
    {
        float squaredRange = _attackRange * _attackRange;
        _enemyTarget = EnemyRegistry.GetNearest(transform.position, squaredRange);
    }

    private void HandleFiring()
    {
        _fireCooldown -= Time.deltaTime;
        if (_fireCooldown > 0f) return;

        LaunchGrenade();
        _fireCooldown = _fireRate;
    }

    private void LaunchGrenade()
    {
        if (_grenadePrefab == null || _enemyTarget == null) return;

        if (_throwSound != null)
            AudioSource.PlayClipAtPoint(_throwSound, transform.position);

        Vector3 spawnPosition = _firePoint != null
            ? _firePoint.position
            : transform.position + Vector3.up * 0.8f;

        GameObject grenadeObject = Instantiate(_grenadePrefab, spawnPosition, Quaternion.identity);
        GrenadeProjectile grenadeProjectile = grenadeObject.GetComponent<GrenadeProjectile>();
        if (grenadeProjectile != null)
            grenadeProjectile.Launch(_enemyTarget.position);

        // Start the arc preview window — hides automatically after _arcLineDisplayDuration seconds
        _arcLineHideTime = Time.time + _arcLineDisplayDuration;
    }

    private void DrawArcPreview()
    {
        Vector3 startPosition = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 endPosition   = _enemyTarget.position;

        _arcLine.enabled        = true;
        _arcLine.positionCount  = _arcLineResolution;
        _arcLine.startWidth     = _lineWidth;
        _arcLine.endWidth       = _lineWidth;

        for (int pointIndex = 0; pointIndex < _arcLineResolution; pointIndex++)
        {
            // t goes 0 → 1 across all points, matching the same formula used in GrenadeProjectile.Update
            float t = (float)pointIndex / (_arcLineResolution - 1);

            float pointX = Mathf.Lerp(startPosition.x, endPosition.x, t);
            float pointZ = Mathf.Lerp(startPosition.z, endPosition.z, t);
            float pointY = Mathf.Lerp(startPosition.y, endPosition.y, t)
                         + _arcHeight * Mathf.Sin(t * Mathf.PI);

            _arcLine.SetPosition(pointIndex, new Vector3(pointX, pointY, pointZ));
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 gizmoCenter = _firePoint != null ? _firePoint.position : transform.position;
        Gizmos.DrawWireSphere(gizmoCenter, _attackRange);
    }
}
