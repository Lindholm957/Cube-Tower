using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CubeTower.Model;

namespace CubeTower.View
{
    public interface IBlockView
    {
        RectTransform RectTransform { get; }
        BlockData Model { get; }
        int SpriteIndex { get; }
        void Initialize(BlockData data, Sprite sprite);
        void SetSprite(Sprite sprite);
    }

    public class BlockView : MonoBehaviour, IBlockView, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform RectTransform { get; private set; }
        public BlockData Model { get; private set; }
        public int SpriteIndex { get; private set; }

        [SerializeField] private Image blockImage;
        public Image BlockImage => blockImage;

        [SerializeField] private BoxCollider2D blockCollider;
        public BoxCollider2D BlockCollider => blockCollider;

        private System.Action<BlockView, PointerEventData> _onPointerDown;
        private System.Action<BlockView, PointerEventData> _onDrag;
        private System.Action<BlockView, PointerEventData> _onPointerUp;

        private Vector2 _dragOffset;
        private bool _isDragEnabled = true;
        public bool IsFromTower { get; private set; }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();
            
            // Add BoxCollider2D if not present
            if (blockCollider == null)
            {
                blockCollider = GetComponent<BoxCollider2D>();
                if (blockCollider == null)
                {
                    blockCollider = gameObject.AddComponent<BoxCollider2D>();
                }
            }
        }

        public void SetSize(Vector2 size)
        {
            RectTransform.sizeDelta = size;
            if (blockCollider != null)
            {
                blockCollider.size = size;

                // Align collider center with the visual rect center taking into account RectTransform pivot.
                // If pivot is (0.5,0.5) offset should be zero. For pivot at bottom (y=0) offset should be +size.y*0.5.
                Vector2 pivot = RectTransform != null ? RectTransform.pivot : new Vector2(0.5f, 0.5f);
                float offsetX = (0.5f - pivot.x) * size.x;
                float offsetY = (0.5f - pivot.y) * size.y;
                blockCollider.offset = new Vector2(offsetX, offsetY);
            }
        }

        public void SetIsFromTower(bool isFromTower)
        {
            IsFromTower = isFromTower;
        }

        public void SetDragEnabled(bool enabled)
        {
            _isDragEnabled = enabled;
        }

        public void Initialize(BlockData data, Sprite sprite)
        {
            Model = data;
            SpriteIndex = data.SpriteIndex;
            SetSprite(sprite);
        }

        public void SetSprite(Sprite sprite)
        {
            if (blockImage != null)
                blockImage.sprite = sprite;
        }

        public void SetCallbacks(
            System.Action<BlockView, PointerEventData> onPointerDown,
            System.Action<BlockView, PointerEventData> onDrag,
            System.Action<BlockView, PointerEventData> onPointerUp)
        {
            _onPointerDown = onPointerDown;
            _onDrag = onDrag;
            _onPointerUp = onPointerUp;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isDragEnabled) return;
            _dragOffset = (Vector2)RectTransform.position - eventData.position;
            _onPointerDown?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragEnabled) return;
            RectTransform.position = eventData.position + _dragOffset;
            _onDrag?.Invoke(this, eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isDragEnabled) return;
            _onPointerUp?.Invoke(this, eventData);
        }

        // SetSize implementation above handles collider as well

        public void SetParent(Transform parent)
        {
            RectTransform.SetParent(parent, false);
        }

        /// <summary>
        /// Jump animation when placing on tower
        /// Satisfies requirement: "должна использоваться как минимум 1 анимация"
        /// </summary>
        public void PlayPlaceAnimation(float jumpHeight, float duration, System.Action onComplete)
        {
            StartCoroutine(PlaceAnimationRoutine(jumpHeight, duration, onComplete));
        }

        private System.Collections.IEnumerator PlaceAnimationRoutine(float jumpHeight, float duration, System.Action onComplete)
        {
            Vector2 startPos = RectTransform.anchoredPosition;
            Vector2 peakPos = startPos + Vector2.up * jumpHeight;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float curve = Mathf.Sin(t * Mathf.PI);
                RectTransform.anchoredPosition = Vector2.Lerp(startPos, peakPos, curve);
                yield return null;
            }

            RectTransform.anchoredPosition = startPos;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Miss/explosion animation
        /// Satisfies requirement: "если игрок перенес кубик и промазал мимо башни, кубик с анимацией исчезает или взрывается"
        /// </summary>
        public void PlayMissAnimation(float duration, System.Action onComplete)
        {
            StartCoroutine(MissAnimationRoutine(duration, onComplete));
        }

        private System.Collections.IEnumerator MissAnimationRoutine(float duration, System.Action onComplete)
        {
            Vector3 startScale = RectTransform.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                RectTransform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            RectTransform.localScale = startScale;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Fall down animation when blocks shift after removing top
        /// </summary>
        public void PlayFallAnimation(float targetY, float duration)
        {
            StartCoroutine(FallAnimationRoutine(targetY, duration));
        }

        private System.Collections.IEnumerator FallAnimationRoutine(float targetY, float duration)
        {
            Vector2 startPos = RectTransform.anchoredPosition;
            Vector2 endPos = new Vector2(startPos.x, targetY);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = t * t * (3f - 2f * t);
                RectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                yield return null;
            }

            RectTransform.anchoredPosition = endPos;
        }
    }
}
