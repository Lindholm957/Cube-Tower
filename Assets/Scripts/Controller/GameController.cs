using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CubeTower.Config;
using CubeTower.Model;
using CubeTower.Services;
using CubeTower.View;
using Zenject;

namespace CubeTower.Controller
{
    public class GameController : IInitializable
    {
        private readonly IConfigService _configService;
        private readonly ISaveService _saveService;
        private readonly GameModel _gameModel;
        private readonly BottomScrollView _bottomScrollView;
        private readonly TowerView _towerView;
        private readonly HoleView _holeView;
        private readonly MessageView _messageView;

        private Canvas _canvas;

        public GameController(
            IConfigService configService,
            ISaveService saveService,
            GameModel gameModel,
            BottomScrollView bottomScrollView,
            TowerView towerView,
            HoleView holeView,
            MessageView messageView)
        {
            _configService = configService;
            _saveService = saveService;
            _gameModel = gameModel;
            _bottomScrollView = bottomScrollView;
            _towerView = towerView;
            _holeView = holeView;
            _messageView = messageView;
        }

        // Check whether the visual center of a block (in screen coordinates) is inside the hole area
        private bool IsBlockScreenCenterOverHole(BlockView block)
        {
            if (block == null || _holeView == null || _canvas == null)
                return false;

            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas.worldCamera ?? Camera.main);
            // World position of the block's RectTransform center
            Vector3 worldPos = block.RectTransform.position;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
            return _holeView.ContainsScreenPoint(screenPoint, _canvas);
        }

        public void Initialize()
        {
            var cfg = _configService.Config;

            Localization.SetLanguage(cfg.Language);

            _gameModel.Initialize(cfg.BottomBlockCount, cfg.BlockSprites.Length);

            _towerView.Initialize(cfg);

            _canvas = Object.FindObjectOfType<Canvas>();

            _bottomScrollView.Initialize(_configService);
            _bottomScrollView.SetBlockCallbacks(OnBlockPointerDown, OnBlockDrag, OnBlockPointerUp);

            RestoreProgress();

            Debug.Log("GameController initialized");
        }

        private void RestoreProgress()
        {
            var json = _saveService.LoadTowerState();
            var state = TowerState.FromJson(json);

            _gameModel.LoadFromState(state);

            // Restore saved tower root anchored position (if any)
            if (state != null)
            {
                _towerView.SetTowerRootAnchoredPosition(state.TowerRootAnchoredPosition);
            }

            if (_gameModel.TowerBlocks != null && _gameModel.TowerBlocks.Count > 0)
            {
                var cfg = _configService.Config;
                for (int i = 0; i < _gameModel.TowerBlocks.Count; i++)
                {
                    var blockData = _gameModel.TowerBlocks[i];
                    var block = CreateBlockForRestore(blockData, cfg);
                    _towerView.AddBlock(block, blockData.HorizontalOffset, () => { });
                }
            }
        }

        private BlockView CreateBlockForRestore(BlockData data, GameConfig cfg)
        {
            // Prefer instantiating the block prefab from the bottom scroll; fall back to creating a GameObject if not available.
            BlockView blockView = null;

            var prefab = _bottomScrollView?.GetBlockPrefab();
            Sprite sprite = cfg.DefaultSprite;
            int spriteIndex = 0;

            if (cfg.BlockSprites != null && cfg.BlockSprites.Length > 0)
            {
                spriteIndex = data.SpriteIndex % cfg.BlockSprites.Length;
                sprite = cfg.BlockSprites[spriteIndex];
            }

            if (prefab != null)
            {
                var go = Object.Instantiate(prefab.gameObject, _towerView.TowerRoot, false);
                go.name = "Block_" + data.Id;
                blockView = go.GetComponent<BlockView>();
                if (blockView == null)
                {
                    // If prefab unexpectedly lacks BlockView, add one
                    blockView = go.AddComponent<BlockView>();
                }
                // Tag restored block as tower block (safe if tag not defined)
                try { go.tag = "TowerBlock"; } catch { }
            }
            else
            {
                var blockObj = new GameObject("Block_" + data.Id);
                blockObj.transform.SetParent(_towerView.TowerRoot, false);
                var imageComp = blockObj.AddComponent<Image>();
                imageComp.sprite = sprite;
                var rect = blockObj.GetComponent<RectTransform>() ?? blockObj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(cfg.BlockWidth, cfg.BlockHeight);
                blockView = blockObj.AddComponent<BlockView>();
                try { blockObj.tag = "TowerBlock"; } catch { }
            }

            // ensure block has correct size and sprite
            blockView.SetSize(new Vector2(cfg.BlockWidth, cfg.BlockHeight));
            blockView.Initialize(data, sprite);
            blockView.SetIsFromTower(true);
            blockView.SetCallbacks(OnBlockPointerDown, OnBlockDrag, OnBlockPointerUp);

            return blockView;
        }

        private void OnBlockPointerDown(BlockView block, PointerEventData eventData)
        {
            _bottomScrollView.SetScrollEnabled(false);
        }

        private void OnBlockDrag(BlockView block, PointerEventData eventData)
        {
            // Drag is handled by BlockView
        }

        private void OnBlockPointerUp(BlockView block, PointerEventData eventData)
        {
            _bottomScrollView.SetScrollEnabled(true);

            // Check if dropped on hole.
            // For blocks dragged from scroll require a stricter check (pointer and block center inside hole) to avoid
            // false positives from colliders. For tower blocks allow collider overlap or pointer containment.
            bool onHole;
            if (block.IsFromTower)
            {
                onHole = _holeView.OverlapsWith(block.BlockCollider) || _holeView.ContainsScreenPoint(eventData.position, _canvas);
            }
            else
            {
                // For scroll blocks require both pointer and block center to be inside hole area
                bool pointerInHole = _holeView.ContainsScreenPoint(eventData.position, _canvas);
                bool blockCenterInHole = IsBlockScreenCenterOverHole(block);
                onHole = pointerInHole && blockCenterInHole;
            }
            
            if (onHole)
            {
                HandleBlockDroppedOnHole(block);
                return;
            }

            // Check if dropped on tower (only for scroll blocks)
            if (!block.IsFromTower)
            {
                bool isOverTower = _towerView.IsPointOverTower(eventData.position, _canvas);

                // If tower is empty, allow placement inside TowerArea as well
                if (!isOverTower && _towerView.GetBlockCount() == 0 && _towerView.TowerArea != null)
                {
                    Camera uiCamera = null;
                    if (_canvas != null)
                        uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas.worldCamera ?? Camera.main);
                    else
                        uiCamera = Camera.main;

                    isOverTower = RectTransformUtility.RectangleContainsScreenPoint(_towerView.TowerArea, eventData.position, uiCamera);
                }

                if (isOverTower)
                {
                    HandleBlockToTower(block, eventData);
                    return;
                }
            }

            // If from tower and not on hole - return to tower position
            if (block.IsFromTower)
            {
                // Return tower block to its original position
                ReturnTowerBlockToPosition(block);
                return;
            }

            // Missed - return to scroll
            HandleBlockMissed(block);
        }

        private void HandleBlockDroppedOnHole(BlockView block)
        {
            // Use GameObject tag to decide whether this dropped block should remove from tower.
            // If the block has tag "TowerBlock" it was part of the tower and should be removed.
            // Otherwise (e.g., ScrollBlock) do not remove blocks from the tower.
            if (block != null && block.gameObject != null && block.gameObject.CompareTag("TowerBlock"))
            {
                // Remove from tower and shift
                HandleTowerBlockToHole(block);
            }
            else
            {
                // Scroll block dropped on hole: play miss animation and return to scroll, do NOT remove from tower
                var cfg = _configService.Config;
                float duration = cfg != null ? cfg.MissAnimationDuration : 0.35f;
                block.PlayMissAnimation(duration, () => _bottomScrollView.ReturnBlockToScroll(block));
                _messageView.Show(Localization.Get(Localization.Key.BlockMissed));
            }
        }

        private void HandleScrollBlockToHole(BlockView block)
        {
            var removedBlock = _gameModel.RemoveTopBlock();

            if (removedBlock != null)
            {
                _towerView.RemoveTopBlock((topBlock) =>
                {
                    if (topBlock != null)
                        Object.Destroy(topBlock.gameObject);

                    _towerView.ShiftBlocksDown();

                    // If no blocks left, reset tower root position
                    if (_gameModel.TowerBlocks.Count == 0)
                    {
                        _gameModel.TowerRootAnchoredPosition = UnityEngine.Vector2.zero;
                        _towerView.ResetTowerRootPosition();
                    }

                    _messageView.Show(Localization.Get(Localization.Key.BlockThrownToHole));
                    SaveProgress();
                });
            }
            else
            {
                _messageView.Show(Localization.Get(Localization.Key.TowerEmpty));
            }

            // Return block back to scroll
            _bottomScrollView.ReturnBlockToScroll(block);
        }

        private void HandleTowerBlockToHole(BlockView block)
        {
            int blockIndex = _towerView.GetBlockIndex(block);
            if (blockIndex < 0)
                return;

            // Remove this specific block from model
            _gameModel.RemoveBlockAt(blockIndex);
            
            // Remove from view and shift blocks above
            _towerView.RemoveBlockAt(blockIndex, (removedBlock) =>
            {
                if (removedBlock != null)
                    Object.Destroy(removedBlock.gameObject);

                // If tower became empty, reset tower root position
                if (_gameModel.TowerBlocks.Count == 0)
                {
                    _gameModel.TowerRootAnchoredPosition = UnityEngine.Vector2.zero;
                    _towerView.ResetTowerRootPosition();
                }

                _messageView.Show(Localization.Get(Localization.Key.BlockThrownToHole));
                SaveProgress();
            });
        }

        private void HandleBlockToTower(BlockView block, PointerEventData eventData)
        {
            var cfg = _configService.Config;
            var towerHeight = _towerView.GetBlockCount();

            // Check height limit based on actual screen position
            if (!_towerView.CanAddBlock())
            {
                _messageView.Show(Localization.Get(Localization.Key.HeightLimit));
                _bottomScrollView.ReturnBlockToScroll(block);
                return;
            }

            // Determine placement validity and horizontal offset
            float horizontalOffset = 0f; // fraction of block width (absolute offset)
            if (towerHeight == 0)
            {
                // First block: must be placed inside TowerArea (if provided) and we move towerRoot to this drop position.
                var area = _towerView.TowerArea;
                // Choose camera for UI conversions: null for Overlay, otherwise canvas.worldCamera or Camera.main
                Camera uiCamera = null;
                if (_canvas != null)
                    uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas.worldCamera ?? Camera.main);
                else
                    uiCamera = Camera.main;

                if (area != null)
                {
                    if (!RectTransformUtility.RectangleContainsScreenPoint(area, eventData.position, uiCamera))
                    {
                        // Not inside allowed tower area -> miss
                        block.PlayMissAnimation(cfg.MissAnimationDuration, () => _bottomScrollView.ReturnBlockToScroll(block));
                        _messageView.Show(Localization.Get(Localization.Key.BlockMissed));
                        return;
                    }
                }

                // Move tower root to the screen point using TowerView helper (handles world vs UI correctly)
                _towerView.SetTowerRootPositionFromScreenPoint(eventData.position, uiCamera);
                _gameModel.TowerRootAnchoredPosition = _towerView.TowerRoot.anchoredPosition;

                // Compute position within towerRoot to place the block
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_towerView.TowerRoot, eventData.position, uiCamera, out Vector2 localPointInRoot))
                {
                    // last resort: world point
                    Vector3 wp = (uiCamera != null) ? uiCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, uiCamera.nearClipPlane)) : new Vector3(eventData.position.x, eventData.position.y, 0f);
                    localPointInRoot = _towerView.TowerRoot.InverseTransformPoint(wp);
                }

                Rect rect = _towerView.TowerRoot.rect;
                float halfBlock = cfg.BlockWidth * 0.5f;
                if (localPointInRoot.x < rect.xMin + halfBlock || localPointInRoot.x > rect.xMax - halfBlock)
                {
                    block.PlayMissAnimation(cfg.MissAnimationDuration, () => _bottomScrollView.ReturnBlockToScroll(block));
                    _messageView.Show(Localization.Get(Localization.Key.BlockMissed));
                    return;
                }

                horizontalOffset = localPointInRoot.x / cfg.BlockWidth;
                // Ensure horizontal offset does not exceed 50% of block width
                horizontalOffset = Mathf.Clamp(horizontalOffset, -0.5f, 0.5f);
            }
            else
            {
                // For subsequent blocks, require the drop to be over the top block
                var topBlock = _towerView.GetBlockAt(towerHeight - 1);
                if (topBlock == null)
                {
                    // Defensive: if no top block, treat as miss
                    block.PlayMissAnimation(cfg.MissAnimationDuration, () => _bottomScrollView.ReturnBlockToScroll(block));
                    _messageView.Show(Localization.Get(Localization.Key.BlockMissed));
                    return;
                }

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(topBlock.RectTransform, eventData.position, _canvas.worldCamera, out Vector2 localTopPoint)
                    || !topBlock.RectTransform.rect.Contains(localTopPoint))
                {
                    // Not dropped on top -> miss
                    block.PlayMissAnimation(cfg.MissAnimationDuration, () => _bottomScrollView.ReturnBlockToScroll(block));
                    _messageView.Show(Localization.Get(Localization.Key.BlockMissed));
                    return;
                }

                // Dropped on top: compute random horizontal offset (absolute fraction)
                horizontalOffset = Random.Range(-cfg.MaxHorizontalOffset, cfg.MaxHorizontalOffset);
                // Ensure random offset is not greater than 50% of face
                horizontalOffset = Mathf.Clamp(horizontalOffset, -0.5f, 0.5f);
            }

            var blockData = new BlockData(_gameModel.GetTowerHeight(), block.SpriteIndex);
            blockData.HorizontalOffset = horizontalOffset;

            // Before adding, ensure the new block will be visible on screen (won't be placed off-screen on tall devices)
            var cfg2 = _configService.Config;
            int newIndex = _towerView.GetBlockCount();
            float yOffset = newIndex * cfg2.BlockHeight;

            // local point at top of the new block (relative to towerRoot)
            Vector3 localTop = new Vector3(0f, yOffset + cfg2.BlockHeight, 0f);
            Vector3 worldTop = _towerView.TowerRoot.TransformPoint(localTop);

            Camera camForUi = null;
            if (_canvas != null)
                camForUi = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas.worldCamera ?? Camera.main);
            else
                camForUi = Camera.main;

            Vector2 screenTopPoint = RectTransformUtility.WorldToScreenPoint(camForUi, worldTop);
            if (screenTopPoint.y > Screen.height)
            {
                _messageView.Show(Localization.Get(Localization.Key.HeightLimit));
                _bottomScrollView.ReturnBlockToScroll(block);
                return;
            }

            _gameModel.AddBlockToTower(blockData);

            // Create new block for tower (dragged block returns to scroll)
            var towerBlock = CreateTowerBlock(block.SpriteIndex);
            if (towerBlock == null)
                return;

            // Return original block to scroll
            _bottomScrollView.ReturnBlockToScroll(block);

            _towerView.AddBlock(towerBlock, horizontalOffset, OnBlockPointerDown, OnBlockDrag, OnBlockPointerUp, () =>
            {
                _messageView.Show(Localization.Get(Localization.Key.BlockPlaced));
                SaveProgress();
            });
        }

        private BlockView CreateTowerBlock(int spriteIndex)
        {
            var cfg = _configService.Config;
            if (cfg == null)
            {
                Debug.LogError("GameController: Config is null! Assign GameConfig to ConfigService.");
                return null;
            }

            var blockObj = new GameObject("TowerBlock");
            blockObj.transform.SetParent(_towerView.TowerRoot, false);

            // mark as tower block
            try { blockObj.tag = "TowerBlock"; } catch { }

            var image = blockObj.AddComponent<Image>();
            image.sprite = _bottomScrollView.GetBlockSprite(spriteIndex);

            // RectTransform is automatically added with Image, just set size
            var rect = blockObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cfg.BlockWidth, cfg.BlockHeight);

            var blockView = blockObj.AddComponent<BlockView>();
            var data = new BlockData(_gameModel.GetTowerHeight(), spriteIndex);
            blockView.Initialize(data, image.sprite);
            blockView.SetSize(new Vector2(cfg.BlockWidth, cfg.BlockHeight));
            blockView.SetIsFromTower(true);
            blockView.SetCallbacks(OnBlockPointerDown, OnBlockDrag, OnBlockPointerUp);

            return blockView;
        }

        private void HandleBlockMissed(BlockView block)
        {
            var cfg = _configService.Config;
            _messageView.Show(Localization.Get(Localization.Key.BlockMissed));

            // Play miss animation and then return block back to scroll (infinite blocks)
            if (block != null)
            {
                float duration = cfg != null ? cfg.MissAnimationDuration : 0.35f;
                block.PlayMissAnimation(duration, () => _bottomScrollView.ReturnBlockToScroll(block));
            }
            else
            {
                _bottomScrollView.ReturnBlockToScroll(block);
            }
        }

        private void ReturnTowerBlockToPosition(BlockView block)
        {
            int blockIndex = _towerView.GetBlockIndex(block);
            if (blockIndex < 0)
                return;

            var cfg = _configService.Config;
            float yOffset = blockIndex * cfg.BlockHeight;
            float horizontalOffset = 0f;

            // Get horizontal offset from model
            if (blockIndex < _gameModel.TowerBlocks.Count)
            {
                horizontalOffset = _gameModel.TowerBlocks[blockIndex].HorizontalOffset;
            }

            _towerView.SetBlockPosition(blockIndex, new Vector2(horizontalOffset * cfg.BlockWidth, yOffset));
        }

        private void SaveProgress()
        {
            if (_towerView != null && _towerView.TowerRoot != null && _gameModel != null)
            {
                _gameModel.TowerRootAnchoredPosition = _towerView.TowerRoot.anchoredPosition;
            }

            var state = _gameModel.SaveToState();
            _saveService.SaveTowerState(state.ToJson());
        }
    }
}
