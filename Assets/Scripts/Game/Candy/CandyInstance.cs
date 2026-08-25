using System.Collections;
using UnityEngine;

namespace CandyShop
{
    // Attached to every pickable candy in the pile. Holds its type and the hide/remove API.
    public class CandyInstance : MonoBehaviour
    {
        public string candyTypeId;
        public CandyTypeDefinition definition;
        [Tooltip("Prefab this instance was created from (pool key).")]
        public GameObject prefabSource;
        public bool Picked { get; private set; }

        private Collider _collider;
        private CandyPileRestock _pile;

        private Collider Collider =>
            _collider != null ? _collider : (_collider = GetComponent<Collider>());

        private void Start()
        {
            if (_pile == null) _pile = FindObjectOfType<CandyPileRestock>();
        }

        public void MarkRemoved()
        {
            if (Picked) return;
            Picked = true;
            if (Collider != null) Collider.enabled = false;
            StartCoroutine(FlyAwayAndHide());
        }

        // Correct picks fly toward the camera (chip direction); wrong taps just pop out.
        public void MarkRemoved(Vector3 worldTarget) => MarkRemoved();

        // Instant removal used by restock cleanup.
        public void RemoveImmediate()
        {
            Picked = true;
            if (Collider != null) Collider.enabled = false;
            DeactivateToPool();
        }

        public void ResetForReuse()
        {
            Picked = false;
            if (Collider != null) Collider.enabled = true;
            gameObject.SetActive(true);
        }

        private IEnumerator FlyAwayAndHide()
        {
            var cam = Camera.main;
            Vector3 target = transform.position + (cam != null ? cam.transform.forward * 2.5f + Vector3.up * 1.5f : Vector3.up * 2f);
            Vector3 start = transform.position;
            Vector3 scale = transform.localScale;
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.35f);
                transform.position = Vector3.Lerp(start, target, k);
                transform.localScale = scale * (1f - 0.7f * k);
                yield return null;
            }
            transform.localScale = scale;
            DeactivateToPool();
        }

        // Deactivate and hand back to the GameObject pool (supplements 2.0).
        private void DeactivateToPool()
        {
            gameObject.SetActive(false);
            CandyPool.Release(this);
            if (_pile != null) _pile.NotifyInstanceReleased(this);
        }
    }
}
