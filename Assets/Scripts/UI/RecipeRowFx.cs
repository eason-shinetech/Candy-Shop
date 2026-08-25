using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyShop
{
    // Per-rank recipe row FX (supplements 2.0 "star-rank display & VFX").
    // One parametric preset scaled by rank — density, motion and accent all increase,
    // so a 5-star row clearly reads cooler than a 1-star row.
    //   rank 1: no idle loop, tiny burst on unlock only
    //   rank 2: sparse idle sparkle
    //   rank 3: light continuous sparkle
    //   rank 4: richer sparkle, faster motion, dual accent
    //   rank 5: dense glitter loop + big unlock burst
    public class RecipeRowFx : MonoBehaviour
    {
        public int rank = 1;
        public bool unlocked;

        private Coroutine _loop;
        private static readonly Color[] AccentByRank =
        {
            UIKit.Cream, UIKit.Lemon, UIKit.Grape, UIKit.Lemon, UIKit.SugarPink
        };

        private float IdleInterval => Mathf.Lerp(2.6f, 0.45f, (rank - 1) / 4f);
        private int BurstCount => 6 + rank * 5;
        private float DotSize => 10f + rank * 2f;

        private void OnEnable()
        {
            if (unlocked && rank >= 2)
                _loop = StartCoroutine(IdleLoop());
        }

        private void OnDisable()
        {
            if (_loop != null) StopCoroutine(_loop);
            _loop = null;
        }

        public void Setup(int starRank, bool isUnlocked)
        {
            rank = Mathf.Clamp(starRank, 1, 5);
            unlocked = isUnlocked;
        }

        // Short burst (purchase / grant). Density and speed scale with rank.
        public void PlayUnlockBurst()
        {
            StartCoroutine(BurstRoutine());
        }

        private IEnumerator IdleLoop()
        {
            var wait = new WaitForSeconds(IdleInterval);
            while (true)
            {
                SpawnDot(randomX: true);
                if (rank >= 4) SpawnDot(randomX: true); // richer sparkle
                yield return wait;
            }
        }

        private IEnumerator BurstRoutine()
        {
            int dots = BurstCount;
            for (int i = 0; i < dots; i++)
            {
                SpawnDot(randomX: false, burst: true);
                if (i % 3 == 0) yield return null; // spread over a few frames
            }
        }

        private void SpawnDot(bool randomX, bool burst = false)
        {
            var dotGo = new GameObject("FxDot", typeof(Image));
            dotGo.transform.SetParent(transform, false);
            var img = dotGo.GetComponent<Image>();
            img.sprite = UIKit.RoundedSprite();
            img.raycastTarget = false;

            Color accent = AccentByRank[rank - 1];
            if (rank >= 4 && Random.value < 0.5f) accent = UIKit.SugarPink; // dual accent
            img.color = accent;

            var rt = (RectTransform)dotGo.transform;
            rt.sizeDelta = Vector2.one * DotSize;
            float x = randomX ? Random.Range(60f, 640f) : 100f + Random.Range(0f, 560f);
            rt.anchoredPosition = new Vector2(x, burst ? Random.Range(20f, 150f) : 20f);

            float rise = burst ? 160f : 90f + rank * 10f;
            float dur = burst ? 0.7f : 1.1f;
            StartCoroutine(AnimateDot(rt, img, rise, dur));
        }

        private IEnumerator AnimateDot(RectTransform rt, Image img, float rise, float dur)
        {
            Vector2 start = rt.anchoredPosition;
            Color c = img.color;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                rt.anchoredPosition = start + Vector2.up * (rise * k);
                c.a = 1f - k;
                img.color = c;
                yield return null;
            }
            Destroy(rt.gameObject);
        }
    }
}
