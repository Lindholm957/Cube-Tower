using UnityEngine;
using UnityEngine.UI;

namespace CubeTower.View
{
    public class HoleView : MonoBehaviour
    {
        [SerializeField] private RectTransform holeRect;
        [SerializeField] private BoxCollider2D holeCollider;

        public RectTransform HoleRect => holeRect;
        public BoxCollider2D HoleCollider => holeCollider;

        private void Awake()
        {
            SetupCollider();
        }

        private void SetupCollider()
        {
            if (holeCollider == null)
                holeCollider = GetComponent<BoxCollider2D>();
            
            if (holeCollider == null)
                holeCollider = gameObject.AddComponent<BoxCollider2D>();

            // Configure collider as trigger
            holeCollider.isTrigger = true;
            
            // Update size
            UpdateColliderSize();
        }

        private void UpdateColliderSize()
        {
            if (holeRect != null && holeCollider != null)
            {
                holeCollider.size = holeRect.sizeDelta;
                holeCollider.offset = Vector2.zero;
            }
        }

        private void Update()
        {
            // Keep collider updated
            UpdateColliderSize();
        }
        public bool OverlapsWith(BlockView block)
        {
            if (holeCollider == null || block?.BlockCollider == null)
                return false;

            return block.BlockCollider.IsTouching(holeCollider);
        }

        public bool OverlapsWith(Collider2D otherCollider)
        {
            if (holeCollider == null || otherCollider == null)
                return false;

            return otherCollider.IsTouching(holeCollider);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Canvas canvas)
        {
            if (holeRect == null || canvas == null)
                return false;

            // Use RectTransformUtility to test screen point against the hole RectTransform.
            // This works for all canvas render modes (ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace).
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(holeRect, screenPoint, cam, out Vector2 localPoint);
            return holeRect.rect.Contains(localPoint);
        }

        public Vector2 GetHolePosition()
        {
            if (holeRect == null)
                return Vector2.zero;
            return holeRect.position;
        }
    }
}
