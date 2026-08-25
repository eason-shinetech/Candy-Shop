using System.Collections.Generic;
using UnityEngine;

namespace CandyShop
{
    // GameObject pool for pile candies (supplements 2.0).
    // Free-list design: an inactive CandyInstance is "released" and can be reacquired by
    // prefab; pick/remove deactivates, restock reacquires — no Instantiate/Destroy churn.
    public static class CandyPool
    {
        private static readonly Dictionary<GameObject, List<CandyInstance>> Free =
            new Dictionary<GameObject, List<CandyInstance>>();

        // Acquire an instance of the prefab: reuse a released one or instantiate a new copy.
        // New copies get the pick collider + CandyInstance wired exactly like first spawn.
        public static CandyInstance Acquire(
            CandyTypeDefinition type, Vector3 localPosition, Quaternion rotation,
            Transform parent, System.Action<CandyInstance> onNewInstance)
        {
            if (type == null || type.prefab == null) return null;

            if (!Free.TryGetValue(type.prefab, out var list))
            {
                list = new List<CandyInstance>();
                Free[type.prefab] = list;
            }

            // Pop the last free instance of this prefab (also drops destroyed entries).
            while (list.Count > 0)
            {
                int last = list.Count - 1;
                var candidate = list[last];
                list.RemoveAt(last);
                if (candidate == null) continue; // destroyed with a scene unload

                candidate.transform.SetParent(parent, false);
                candidate.transform.localPosition = localPosition;
                candidate.transform.localRotation = rotation;
                candidate.ResetForReuse();
                return candidate;
            }

            var go = Object.Instantiate(type.prefab, parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = rotation;
            var inst = go.AddComponent<CandyInstance>();
            inst.prefabSource = type.prefab;
            onNewInstance?.Invoke(inst);
            return inst;
        }

        // Called when an instance becomes inactive (pick/remove) — back to the free list.
        public static void Release(CandyInstance instance)
        {
            if (instance == null || instance.prefabSource == null) return;
            if (!Free.TryGetValue(instance.prefabSource, out var list))
            {
                list = new List<CandyInstance>();
                Free[instance.prefabSource] = list;
            }
            if (!list.Contains(instance)) list.Add(instance);
        }

        // Scene teardown (scene unload destroys all objects; clear stale references).
        public static void Clear()
        {
            Free.Clear();
        }
    }
}
