using System.Collections;
using UnityEngine;

// Attach to the follower prefab alongside Health.
// Health.Die() calls TriggerDeath() instead of Destroy() directly,
// so this script handles the flash + shake + sound before destroying.
public class FollowerDeathFeedback : MonoBehaviour
{
    [SerializeField] private int _flashCount = 3;
    [SerializeField] private float _flashDuration = 0.25f;
    [SerializeField] private AudioClip _deathSound;

    public void TriggerDeath()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float flashInterval = _flashDuration / (_flashCount * 2);

        // Snapshot original colors before we touch any materials
        Color[] originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;

        // Play death sound at world position so it doesn't move with the camera
        if (_deathSound != null)
            AudioSource.PlayClipAtPoint(_deathSound, transform.position);

        // Flash white then back to original, repeated _flashCount times
        for (int f = 0; f < _flashCount; f++)
        {
            foreach (Renderer r in renderers)
                r.material.color = Color.white;

            yield return new WaitForSeconds(flashInterval);

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = originalColors[i];

            yield return new WaitForSeconds(flashInterval);
        }

        Destroy(gameObject);
    }
}
