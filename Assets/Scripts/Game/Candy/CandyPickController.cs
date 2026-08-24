using UnityEngine;
using UnityEngine.EventSystems;

namespace CandyShop
{
    // Touch picking: front-most candy under the finger, drag-safe, UI-safe.
    public class CandyPickController : MonoBehaviour
    {
        public Camera pickCamera;
        [Tooltip("Finger travel above this (in pixels) is treated as a scroll, not a pick.")]
        public float maxPickDragPixels = 40f;

        private bool _touchActive;
        private Vector2 _startPos;
        private int _fingerId = -1;

        private void Update()
        {
            var game = GameManager.Instance;
            if (game == null || !game.RunActive) return;

            if (Input.touchCount > 0)
            {
                foreach (var touch in Input.touches)
                {
                    if (touch.phase == TouchPhase.Began)
                    {
                        _touchActive = true;
                        _startPos = touch.position;
                        _fingerId = touch.fingerId;
                    }
                    else if (touch.phase == TouchPhase.Ended && _touchActive && touch.fingerId == _fingerId)
                    {
                        _touchActive = false;
                        Vector2 delta = touch.position - _startPos;
                        if (delta.magnitude <= maxPickDragPixels)
                            TryPick(touch.position);
                        _fingerId = -1;
                    }
                    else if (touch.phase == TouchPhase.Canceled)
                    {
                        _touchActive = false;
                        _fingerId = -1;
                    }
                }
                return;
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            // Editor convenience: left click behaves like a tap.
            if (Input.GetMouseButtonDown(0))
            {
                _touchActive = true;
                _startPos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && _touchActive)
            {
                _touchActive = false;
                Vector2 delta = (Vector2)Input.mousePosition - _startPos;
                if (delta.magnitude <= maxPickDragPixels)
                    TryPick(Input.mousePosition);
            }
#endif
        }

        private void TryPick(Vector2 screenPos)
        {
            var game = GameManager.Instance;
            if (game != null && game.Paused) return; // HUD pause blocks world taps
            var orders = CustomerOrderManager.Instance;
            if (orders == null) return;

            // UI consumes taps: never pick through HUD.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(_fingerId))
                return;

            if (pickCamera == null) pickCamera = Camera.main;
            if (pickCamera == null) return;

            // Physics.Raycast returns the closest hit = front-most candy.
            Ray ray = pickCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                var candy = hit.collider.GetComponentInParent<CandyInstance>();
                if (candy != null && !candy.Picked)
                {
                    orders.Pick(candy);
                }
                // Tap on empty space / non-candy: no star change.
            }
        }
    }
}
