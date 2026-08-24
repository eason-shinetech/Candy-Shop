using System.Collections;
using UnityEngine;

namespace CandyShop
{
    // Spawns power-up particle prefabs. Every power-up must show VFX; silent use is a bug.
    public class PowerUpVfxPlayer : MonoBehaviour
    {
        public static PowerUpVfxPlayer Instance { get; private set; }

        private void Awake() => Instance = this;

        // One-shot burst (magnet). Auto-destroys after the particle lifetime.
        public GameObject PlayOneShot(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                Debug.LogError("PowerUpVfxPlayer: missing one-shot VFX prefab");
                return null;
            }
            var go = Instantiate(prefab, position, Quaternion.identity);
            StartCoroutine(DestroyAfterLifetime(go));
            return go;
        }

        // Looping effect (tornado / freeze). Caller keeps the handle and calls StopLoop.
        public GameObject PlayLoop(GameObject prefab, Vector3 position)
        {
            if (prefab == null)
            {
                Debug.LogError("PowerUpVfxPlayer: missing loop VFX prefab");
                return null;
            }
            return Instantiate(prefab, position, Quaternion.identity);
        }

        public void StopLoop(GameObject handle)
        {
            if (handle == null) return;
            foreach (var ps in handle.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = false;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            StartCoroutine(DestroyAfterLifetime(handle));
        }

        private IEnumerator DestroyAfterLifetime(GameObject go)
        {
            float max = 0f;
            if (go != null)
            {
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
                    max = Mathf.Max(max, ps.main.duration + ps.main.startLifetime.constantMax + 0.5f);
            }
            yield return new WaitForSeconds(Mathf.Max(1.5f, max));
            if (go != null) Destroy(go);
        }
    }
}
