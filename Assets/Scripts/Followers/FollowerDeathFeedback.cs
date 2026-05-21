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
    [SerializeField] private GameObject _explosionParticlePrefab;

    public void TriggerDeath()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        if (_explosionParticlePrefab != null)
            Instantiate(_explosionParticlePrefab, transform.position, Quaternion.identity);

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float flashInterval = _flashDuration / (_flashCount * 2);

        Material flashMat = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
        Material[][] originalMats = new Material[renderers.Length][];
        Material[][] flashMats    = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMats[i] = renderers[i].sharedMaterials;
            flashMats[i]    = new Material[originalMats[i].Length];
            for (int j = 0; j < flashMats[i].Length; j++)
                flashMats[i][j] = flashMat;
        }

        if (_deathSound != null)
            AudioSource.PlayClipAtPoint(_deathSound, transform.position);

        for (int f = 0; f < _flashCount; f++)
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].materials = flashMats[i];

            yield return new WaitForSeconds(flashInterval);

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterials = originalMats[i];

            yield return new WaitForSeconds(flashInterval);
        }

        Destroy(flashMat);
        Destroy(gameObject);
    }
}
