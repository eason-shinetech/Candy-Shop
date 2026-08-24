using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // Builds and maintains the pre-placed candy pile; refills unlocked types so valid orders never starve.
    public class CandyPileRestock : MonoBehaviour
    {
        [Header("References")]
        public Transform pileRoot;
        public GameManager game;

        [Header("Layout")]
        public float pileRadius = 2.6f;
        public float stackHeight = 1.4f;
        public int dropRows = 3;

        private readonly List<CandyInstance> _instances = new List<CandyInstance>();
        private readonly Dictionary<string, List<CandyInstance>> _byType = new Dictionary<string, List<CandyInstance>>();

        // Tornado effect state
        private bool _lifting;
        private float _liftTimer;
        private float _liftDuration;
        private readonly Dictionary<Transform, Vector3> _origLocalPos = new Dictionary<Transform, Vector3>();

        public bool IsBusyLifting => _lifting;

        private void Awake()
        {
            if (pileRoot == null) pileRoot = transform;
            if (game == null) game = FindObjectOfType<GameManager>();
        }

        public void EnsurePile()
        {
            var cfg = game.orderConfig;
            var orders = CustomerOrderManager.Instance;
            var unlocked = orders != null ? orders.GetUnlockedTypes() : new List<CandyTypeDefinition>();

            foreach (var type in unlocked)
                FillType(type, cfg.targetInstancesPerType);
        }

        private void FillType(CandyTypeDefinition type, int target)
        {
            if (type == null || type.prefab == null)
            {
                Debug.LogError("CandyTypeDefinition missing prefab: " + (type != null ? type.typeId : "null"));
                return;
            }

            if (!_byType.TryGetValue(type.typeId, out var list))
            {
                list = new List<CandyInstance>();
                _byType[type.typeId] = list;
            }

            int toAdd = target - ActiveCount(list);
            for (int i = 0; i < toAdd; i++)
            {
                Vector2 r = Random.insideUnitCircle * pileRadius;
                Vector3 finalPos = new Vector3(r.x, Random.Range(0f, stackHeight), r.y);
                var go = Instantiate(type.prefab, finalPos + Vector3.up * 2.5f, Random.rotation, pileRoot);
                go.name = "Candy_" + type.typeId + "_" + i;

                // One sphere collider per instance keeps picking simple and cheap.
                var bounds = ComputeBounds(go);
                float radius = Mathf.Max(0.18f, Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z)) * 0.9f);
                var col = go.AddComponent<SphereCollider>();
                col.radius = radius / Mathf.Max(Mathf.Epsilon, MaxScale(go.transform));
                col.center = go.transform.InverseTransformPoint(bounds.center);

                var inst = go.AddComponent<CandyInstance>();
                inst.candyTypeId = type.typeId;
                inst.definition = type;
                list.Add(inst);
                _instances.Add(inst);

                StartCoroutine(DropIn(go.transform, finalPos));
            }
        }

        // Simple drop-in so new candies lerp down from above.
        private IEnumerator DropIn(Transform t, Vector3 finalPos)
        {
            Vector3 start = t.localPosition;
            float dur = 0.45f;
            float time = 0f;
            while (time < dur)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / dur);
                k = 1f - (1f - k) * (1f - k); // ease-out
                t.localPosition = Vector3.Lerp(start, finalPos, k);
                yield return null;
            }
            t.localPosition = finalPos;
        }

        private static float MaxScale(Transform t)
        {
            var s = t.lossyScale;
            return Mathf.Max(Mathf.Abs(s.x), Mathf.Max(Mathf.Abs(s.y), Mathf.Abs(s.z)));
        }

        private static Bounds ComputeBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.3f);
            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        private static int ActiveCount(List<CandyInstance> list)
        {
            int n = 0;
            foreach (var inst in list) if (!inst.Picked && inst.gameObject.activeSelf) n++;
            return n;
        }

        // Refill all unlocked types after serving a customer or starting an order.
        public void RestockForOrder(CustomerOrderState order)
        {
            EnsurePile();
        }

        // Called when remaining pickable candies of a requested type hit 0.
        public void RestockIfNeeded(CustomerOrderState order, string typeId)
        {
            foreach (var t in order.types)
            {
                if (_byType.TryGetValue(t.typeId, out var list) && ActiveCount(list) == 0 && order.RemainingOf(t.typeId) > 0)
                    FillType(t, game.orderConfig.targetInstancesPerType);
            }
            if (_byType.TryGetValue(typeId, out var l) && ActiveCount(l) == 0)
                RestockForOrder(order);
        }

        public IEnumerable<CandyInstance> GetUnpickedInstances()
        {
            foreach (var inst in _instances)
                if (!inst.Picked && inst.gameObject.activeSelf)
                    yield return inst;
        }

        // ---- Tornado gameplay: lift candies so buried ones are tappable ----
        public void LiftFor(float seconds)
        {
            if (_lifting) return;
            _origLocalPos.Clear();
            foreach (var inst in _instances)
            {
                if (inst == null) continue;
                _origLocalPos[inst.transform] = inst.transform.localPosition;
            }
            _lifting = true;
            _liftTimer = 0f;
            _liftDuration = seconds;
        }

        private void Update()
        {
            if (!_lifting) return;
            _liftTimer += Time.deltaTime;

            if (_liftTimer >= _liftDuration)
            {
                foreach (var kvp in _origLocalPos)
                    if (kvp.Key != null) kvp.Key.localPosition = kvp.Value;
                _lifting = false;
                return;
            }

            float k = _liftTimer / _liftDuration;
            // Rise, gentle orbit while lifted, then settle back down.
            float liftK = k < 0.15f ? k / 0.15f : (k > 0.8f ? (1f - k) / 0.2f : 1f);
            float orbit = Time.time * 2.2f;
            foreach (var kvp in _origLocalPos)
            {
                if (kvp.Key == null) continue;
                Vector3 p = kvp.Value;
                p.y += liftK * 1.6f;
                p.x += Mathf.Cos(orbit + p.z * 2f) * 0.12f * liftK;
                p.z += Mathf.Sin(orbit + p.x * 2f) * 0.12f * liftK;
                kvp.Key.localPosition = p;
            }
        }
    }
}
