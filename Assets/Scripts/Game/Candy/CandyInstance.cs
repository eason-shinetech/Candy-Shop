using System.Collections;
using UnityEngine;

namespace CandyShop
{
    // Attached to every pickable candy in the pile. Holds its type and the hide/remove API.
    public class CandyInstance : MonoBehaviour
    {
        public string candyTypeId;
        public CandyTypeDefinition definition;
        public bool Picked { get; private set; }

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        public void MarkRemoved()
        {
            if (Picked) return;
            Picked = true;
            if (_collider != null) _collider.enabled = false;
            StartCoroutine(FlyAwayAndHide());
        }

        // Correct picks fly toward the camera (chip direction); wrong taps just pop out.
        public void MarkRemoved(Vector3 worldTarget) => MarkRemoved();

        // Instant removal used by restock cleanup.
        public void RemoveImmediate()
        {
            Picked = true;
            if (_collider != null) _collider.enabled = false;
            gameObject.SetActive(false);
        }

        public void ResetForReuse()
        {
            Picked = false;
            if (_collider == null) _collider = GetComponent<Collider>();
            if (_collider != null) _collider.enabled = true;
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
            gameObject.SetActive(false);
            transform.localScale = scale;
        }
    }
}
