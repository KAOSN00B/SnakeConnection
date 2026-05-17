using UnityEngine;

[ExecuteInEditMode]
public class LaserSight : MonoBehaviour
{
    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private float _laserLength = 50f;
    [SerializeField] private Material _laserMaterial;
    [SerializeField] private LayerMask _excludeLayers;

    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (_laserMaterial != null)
        {
            _lineRenderer.sharedMaterial = _laserMaterial;
        }

        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.useWorldSpace = true;
        
        // Using white so the material's color shows through
        _lineRenderer.startColor = Color.white;
        _lineRenderer.endColor = Color.white;

        _lineRenderer.positionCount = 2;
    }

    void LateUpdate()
    {
        if (_lineRenderer == null) return;
        UpdateLaser();
    }

    private void UpdateLaser()
    {
        Vector3 startPos = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 direction = _firePoint != null ? _firePoint.forward : transform.forward;

        // Use a raycast that ignores specific layers (like the player itself)
        Vector3 endPos = Physics.Raycast(startPos, direction, out RaycastHit hit, _laserLength, ~_excludeLayers)
            ? hit.point
            : startPos + direction * _laserLength;

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);

        // Update width in case it changed in inspector
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
    }
}
