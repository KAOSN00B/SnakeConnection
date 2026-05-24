using System.Collections.Generic;
using UnityEngine;

// Draws a line from the player through each follower in chain order.
// Creates its own child GameObject for the LineRenderer so it never
// conflicts with LaserSight or any other LineRenderer on the player.
// Attach to the Player root.
public class ChainRenderer : MonoBehaviour
{
    [SerializeField] private float _lineWidth = 0.12f;
    [SerializeField] private Material _lineMaterial;
    [SerializeField] private Color _lineColor = new Color(0.3f, 0.85f, 1f, 0.8f);

    private LineRenderer _lineRenderer;

    private void Awake()
    {
        var chainLineObject = new GameObject("ChainLine");
        chainLineObject.transform.SetParent(transform);
        chainLineObject.transform.localPosition = Vector3.zero;

        _lineRenderer = chainLineObject.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace        = true;
        _lineRenderer.startWidth           = _lineWidth;
        _lineRenderer.endWidth             = _lineWidth;
        _lineRenderer.startColor           = _lineColor;
        _lineRenderer.endColor             = _lineColor;
        _lineRenderer.positionCount        = 0;
        _lineRenderer.receiveShadows       = false;
        _lineRenderer.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        _lineRenderer.generateLightingData = false;

        if (_lineMaterial != null)
            _lineRenderer.sharedMaterial = _lineMaterial;
        else
        {
            // Use the same unlit shader the player trail uses so it renders correctly without a custom material
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = _lineColor;
            _lineRenderer.endColor   = _lineColor;
        }
    }

    private void LateUpdate()
    {
        if (ChainManager.Instance == null || !ChainManager.Instance.HasFollowers)
        {
            _lineRenderer.positionCount = 0;
            return;
        }

        IReadOnlyList<FollowerMovement> followers = ChainManager.Instance.GetFollowersInOrder();

        int pointCount = followers.Count + 1;
        _lineRenderer.positionCount = pointCount;
        _lineRenderer.SetPosition(0, transform.position);

        for (int i = 0; i < followers.Count; i++)
            _lineRenderer.SetPosition(i + 1, followers[i].transform.position);
    }
}
