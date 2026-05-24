using UnityEngine;

// Draws a LineRenderer laser from _firePoint forward, stopping at the first physics hit.
// Layers listed in _excludeLayers (e.g. the player's own collider) are ignored so the
// beam does not immediately stop at the player's feet.
[ExecuteInEditMode]
public class LaserSight : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private float _laserLength = 50f;
    [SerializeField] private Material _laserMaterial;
    [SerializeField] private LayerMask _excludeLayers;

    [Header("Performance")]
    [Tooltip("Minimum angle change in the fire point direction before the laser re-raycasts. " +
             "Keeps the laser tracking the mouse on every frame it actually moves while skipping " +
             "the raycast on frames where the aim is perfectly still. 0.1 is nearly invisible.")]
    [SerializeField] private float _minimumFirePointAngleChange = 0.1f;

    private LineRenderer _lineRenderer;

    // Cached state — compared each LateUpdate to decide whether to re-raycast
    private Vector3 _lastLaserDirection;
    private Vector3 _lastLaserStartPosition;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
            _lineRenderer = gameObject.AddComponent<LineRenderer>();

        if (_laserMaterial != null)
            _lineRenderer.sharedMaterial = _laserMaterial;

        // Width and color are visual constants — set once here, not per-frame in UpdateLaser
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth   = _lineWidth;
        _lineRenderer.useWorldSpace  = true;
        _lineRenderer.startColor = Color.white;
        _lineRenderer.endColor   = Color.white;
        _lineRenderer.positionCount = 2;
    }

    void LateUpdate()
    {
        if (_lineRenderer == null) return;
        UpdateLaser();
    }

    private void UpdateLaser()
    {
        Vector3 startPosition = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 aimDirection  = _firePoint != null ? _firePoint.forward  : transform.forward;

        // Only raycast and update the LineRenderer when the fire point has actually moved or rotated.
        // This way the laser tracks the mouse on every frame it moves (no timer lag),
        // and skips the Physics.Raycast only on frames where aim is perfectly still.
        float angleChange           = Vector3.Angle(_lastLaserDirection, aimDirection);
        float startPositionChangeSq = (startPosition - _lastLaserStartPosition).sqrMagnitude;
        bool  aimChangedEnough      = angleChange >= _minimumFirePointAngleChange || startPositionChangeSq > 0.0001f;

        if (!aimChangedEnough) return;

        _lastLaserDirection     = aimDirection;
        _lastLaserStartPosition = startPosition;

        // Cast against all layers except _excludeLayers (typically the player's own collider)
        Vector3 endPosition = Physics.Raycast(startPosition, aimDirection, out RaycastHit raycastHit, _laserLength, ~_excludeLayers)
            ? raycastHit.point
            : startPosition + aimDirection * _laserLength;

        _lineRenderer.SetPosition(0, startPosition);
        _lineRenderer.SetPosition(1, endPosition);
    }
}
