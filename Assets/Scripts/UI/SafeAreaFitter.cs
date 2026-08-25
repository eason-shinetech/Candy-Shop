using UnityEngine;

namespace CandyShop
{
    // Keeps the HUD inside Android safe areas (punch-hole / gesture nav).
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private Rect _applied = Rect.zero;

        private void Awake() => Apply();
        private void OnRectTransformDimensionsChange() => Apply();

        private void Update()
        {
            // Cheap guard: re-apply when the safe area changes (rotation is locked but resolutions differ).
            if (Screen.safeArea != _applied) Apply();
        }

        private void Apply()
        {
            var rt = (RectTransform)transform;
            Rect sa = Screen.safeArea;
            // Writing the anchors below fires OnRectTransformDimensionsChange re-entrantly;
            // the _applied check makes that nested call a no-op and stops the recursion.
            if (sa == _applied) return;
            _applied = sa;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0) return;

            Vector2 anchorMin = new Vector2(sa.x / screenSize.x, sa.y / screenSize.y);
            Vector2 anchorMax = new Vector2((sa.x + sa.width) / screenSize.x, (sa.y + sa.height) / screenSize.y);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
