using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using CubeTower.Config;

namespace CubeTower.View
{
    public class TowerView : MonoBehaviour
    {

        [SerializeField] private RectTransform towerArea;
        [SerializeField] private RectTransform towerRoot;

        private List<BlockView> _towerBlocks = new List<BlockView>();
        private Vector2 _blockSize;
        private GameConfig _config;
        private Vector2 _initialTowerRootAnchoredPos;
        private Vector2 _initialTowerRootSize;

        public RectTransform TowerRoot => towerRoot;
        public RectTransform TowerArea => towerArea;

        public void Initialize(GameConfig config)
        {
            _config = config;
            _blockSize = new Vector2(config.BlockWidth, config.BlockHeight);
            // store initial anchored position to allow reset
            if (towerRoot != null)
                _initialTowerRootAnchoredPos = towerRoot.anchoredPosition;
            if (towerRoot != null)
                _initialTowerRootSize = towerRoot.sizeDelta;

            ClearTower();
        }

        public void SetTowerRootAnchoredPosition(Vector2 anchoredPos)
        {
            if (towerRoot == null)
                return;

            // Ensure tower root stays within parent bounds so the tower top is visible on all screens
            var parentRect = towerRoot.parent as RectTransform;
            if (parentRect != null)
            {
                float parentHeight = parentRect.rect.height;
                Vector2 size = towerRoot.sizeDelta;
                // compute top position taking pivot into account
                float pivotTopOffset = (1f - towerRoot.pivot.y) * size.y;
                float maxY = parentHeight - pivotTopOffset;
                // clamp between 0 and maxY (if maxY negative, just allow 0)
                if (maxY < 0f) maxY = 0f;
                anchoredPos.y = Mathf.Clamp(anchoredPos.y, 0f, maxY);
            }
            else
            {
                anchoredPos.y = Mathf.Max(0f, anchoredPos.y);
            }

            towerRoot.anchoredPosition = anchoredPos;
        }

        public void SetTowerRootPositionFromScreenPoint(Vector2 screenPoint, Camera cam)
        {
            if (towerRoot == null)
                return;

            var parentRect = towerRoot.parent as RectTransform;
            Vector3 worldPoint;

            if (parentRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(parentRect, screenPoint, cam, out worldPoint))
            {
                towerRoot.position = worldPoint;
            }
            else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(towerRoot, screenPoint, cam, out worldPoint))
            {
                towerRoot.position = worldPoint;
            }
            else
            {
                var cameraToUse = cam ?? Camera.main;
                if (cameraToUse != null)
                {
                    worldPoint = cameraToUse.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, cameraToUse.nearClipPlane));
                    towerRoot.position = worldPoint;
                }
            }

            // Ensure anchored Y is not below zero after moving
            var anchored = towerRoot.anchoredPosition;
            // apply additional downward offset requested (move tower lower by 75 units)
            anchored.y -= 75f;
            anchored.y = Mathf.Max(0f, anchored.y);
            towerRoot.anchoredPosition = anchored;
        }

        public void ResetTowerRootPosition()
        {
            if (towerRoot == null)
                return;

            // Reset to initial but keep Y >= 0
            var pos = _initialTowerRootAnchoredPos;
            pos.y = Mathf.Max(0f, pos.y);
            towerRoot.anchoredPosition = pos;
        }

        public void ClearTower()
        {
            foreach (Transform child in towerRoot)
            {
                Destroy(child.gameObject);
            }
            _towerBlocks.Clear();
            // reset root size
            if (towerRoot != null)
                towerRoot.sizeDelta = _initialTowerRootSize;
        }

        public void AddBlock(BlockView block, float horizontalOffset, System.Action onComplete)
        {
            AddBlock(block, horizontalOffset, null, null, null, onComplete);
        }

        public void AddBlock(BlockView block, float horizontalOffset, 
            System.Action<BlockView, PointerEventData> onPointerDown,
            System.Action<BlockView, PointerEventData> onDrag,
            System.Action<BlockView, PointerEventData> onPointerUp,
            System.Action onComplete)
        {
            int index = _towerBlocks.Count;
            float yOffset = index * _blockSize.y;
            
            block.SetParent(towerRoot);
            block.RectTransform.pivot = new Vector2(0.5f, 0f);
            block.RectTransform.anchorMin = new Vector2(0.5f, 0f);
            block.RectTransform.anchorMax = new Vector2(0.5f, 0f);
            block.RectTransform.anchoredPosition = new Vector2(horizontalOffset * _blockSize.x, yOffset);
            block.RectTransform.sizeDelta = _blockSize;
            block.SetSize(_blockSize);

            if (onPointerDown != null)
                block.SetCallbacks(onPointerDown, onDrag, onPointerUp);

            _towerBlocks.Add(block);

            // Update tower root size to fit new block
            UpdateTowerRootSize();

            block.PlayPlaceAnimation(_config.PlaceJumpHeight, _config.PlaceAnimationDuration, onComplete);
        }

        public void RemoveTopBlock(System.Action<BlockView> onRemoved)
        {
            if (_towerBlocks.Count == 0)
            {
                onRemoved?.Invoke(null);
                return;
            }

            var topBlock = _towerBlocks[_towerBlocks.Count - 1];
            _towerBlocks.RemoveAt(_towerBlocks.Count - 1);
            // shrink root to fit
            UpdateTowerRootSize();
            onRemoved?.Invoke(topBlock);
        }

        public void RemoveBlockAt(int index, System.Action<BlockView> onRemoved)
        {
            if (index < 0 || index >= _towerBlocks.Count)
            {
                onRemoved?.Invoke(null);
                return;
            }

            var block = _towerBlocks[index];
            _towerBlocks.RemoveAt(index);

            // Shift blocks above down
            ShiftBlocksAbove(index);

            // update root size after removal
            UpdateTowerRootSize();

            onRemoved?.Invoke(block);
        }

        private void UpdateTowerRootSize()
        {
            if (towerRoot == null)
                return;

            float neededHeight = Mathf.Max(_initialTowerRootSize.y, _towerBlocks.Count * _blockSize.y);
            Vector2 size = towerRoot.sizeDelta;
            size.y = neededHeight;
            size.x = Mathf.Max(size.x, _initialTowerRootSize.x);
            towerRoot.sizeDelta = size;
        }

        private void ShiftBlocksAbove(int fromIndex)
        {
            float blockHeight = _blockSize.y;
            float duration = 0.3f;

            for (int i = fromIndex; i < _towerBlocks.Count; i++)
            {
                float targetY = i * blockHeight;
                _towerBlocks[i].PlayFallAnimation(targetY, duration);
            }
        }

        public void ShiftBlocksDown(System.Action onComplete = null)
        {
            if (_towerBlocks.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            float blockHeight = _blockSize.y;
            float duration = 0.3f;

            for (int i = 0; i < _towerBlocks.Count; i++)
            {
                float targetY = i * blockHeight;
                _towerBlocks[i].PlayFallAnimation(targetY, duration);
            }

            if (onComplete != null)
                Invoke(nameof(CompleteShift), duration);
        }

        private void CompleteShift() { }

        public int GetBlockCount() => _towerBlocks.Count;

        public BlockView GetBlockAt(int index)
        {
            if (index >= 0 && index < _towerBlocks.Count)
                return _towerBlocks[index];
            return null;
        }

        public int GetBlockIndex(BlockView block)
        {
            return _towerBlocks.IndexOf(block);
        }

        public void SetBlockCallbacks(int blockIndex, System.Action<BlockView, PointerEventData> onPointerDown, 
            System.Action<BlockView, PointerEventData> onDrag, System.Action<BlockView, PointerEventData> onPointerUp)
        {
            if (blockIndex >= 0 && blockIndex < _towerBlocks.Count)
            {
                _towerBlocks[blockIndex].SetCallbacks(onPointerDown, onDrag, onPointerUp);
            }
        }

        public Vector2 GetBlockPosition(int index)
        {
            if (index >= 0 && index < _towerBlocks.Count)
                return _towerBlocks[index].RectTransform.anchoredPosition;
            return Vector2.zero;
        }

        public bool CanAddBlock()
        {
            if (towerRoot == null)
                return false;

            float towerTopY = towerRoot.position.y + (_towerBlocks.Count * _blockSize.y);
            float screenTopY = Screen.height;

            return towerTopY < screenTopY;
        }

        public bool IsPointOverTower(Vector2 screenPoint, Canvas canvas)
        {
            if (towerRoot == null || canvas == null)
                return false;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                towerRoot, screenPoint, canvas.worldCamera, out Vector2 localPoint);

            Rect bounds = towerRoot.rect;
            return bounds.Contains(localPoint);
        }

        public Vector2 GetBlockSize() => _blockSize;

        public void SetBlockPosition(int index, Vector2 position)
        {
            if (index >= 0 && index < _towerBlocks.Count)
            {
                _towerBlocks[index].RectTransform.anchoredPosition = position;
            }
        }
    }
}
