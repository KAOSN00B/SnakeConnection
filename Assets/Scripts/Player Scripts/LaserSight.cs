using UnityEngine;

public class LaserSight : MonoBehaviour
{

    [SerializeField] private Transform _firePoint;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private float _laserLength = 50f;
    [SerializeField] private Material _laserMaterial;

    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        if (_laserMaterial != null)
        {
            _lineRenderer.material = _laserMaterial;
        }

        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        // Using white so the material's HDR color/emission shows through correctly
        _lineRenderer.startColor = Color.white;
        _lineRenderer.endColor = Color.white;

        _lineRenderer.positionCount = 2;
    }

    void Update()
    {
        Vector3 startPos = _firePoint != null ? _firePoint.position : transform.position;
        Vector3 direction = _firePoint != null ? _firePoint.forward : transform.forward;
        Vector3 endPos;

        if (Physics.Raycast(startPos, direction, out RaycastHit hit, _laserLength))
        {
            endPos = hit.point;
        }
        else
        {
            endPos = startPos + direction * _laserLength;
        }

        _lineRenderer.SetPosition(0, startPos);
        _lineRenderer.SetPosition(1, endPos);
    }
}
