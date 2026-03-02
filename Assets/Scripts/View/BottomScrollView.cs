using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CubeTower.Config;
using CubeTower.Model;
using CubeTower.Services;
using UnityEngine.EventSystems;

namespace CubeTower.View
{
    public class BottomScrollView : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRect;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private BlockView blockPrefab;

        private IConfigService _configService;
        private List<BlockView> _blocks = new List<BlockView>();
        private Vector2 _blockSize;

        public void Initialize(IConfigService configService)
        {
            _configService = configService;
            var cfg = _configService.Config;

            _blockSize = new Vector2(cfg.BlockWidth, cfg.BlockHeight);
            CreateBlocks(cfg);
        }

        private void CreateBlocks(GameConfig cfg)
        {
            foreach (Transform child in contentRect)
            {
                Destroy(child.gameObject);
            }
            _blocks.Clear();

            for (int i = 0; i < cfg.BottomBlockCount; i++)
            {
                var blockObj = Instantiate(blockPrefab, contentRect);
                blockObj.SetSize(_blockSize);

                int spriteIndex = i % cfg.BlockSprites.Length;
                var data = new BlockData(i, spriteIndex);
                var sprite = cfg.BlockSprites.Length > 0 ? cfg.BlockSprites[spriteIndex] : cfg.DefaultSprite;

                blockObj.Initialize(data, sprite);
                _blocks.Add(blockObj);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }

        public void SetScrollEnabled(bool enabled)
        {
            if (scrollRect != null)
                scrollRect.enabled = enabled;
        }

        public void SetBlockCallbacks(
            System.Action<BlockView, PointerEventData> onPointerDown,
            System.Action<BlockView, PointerEventData> onDrag,
            System.Action<BlockView, PointerEventData> onPointerUp)
        {
            foreach (var block in _blocks)
            {
                block.SetCallbacks(onPointerDown, onDrag, onPointerUp);
            }
        }

        public BlockView CreateBlockForTower(BlockView sourceBlock)
        {
            var blockObj = Instantiate(blockPrefab, contentRect);
            blockObj.SetSize(_blockSize);
            blockObj.Initialize(sourceBlock.Model, sourceBlock.BlockImage.sprite);
            return blockObj;
        }

        public Sprite GetBlockSprite(int spriteIndex)
        {
            var cfg = _configService.Config;
            if (cfg.BlockSprites.Length > 0)
                return cfg.BlockSprites[spriteIndex % cfg.BlockSprites.Length];
            return cfg.DefaultSprite;
        }

        public RectTransform GetContentRect()
        {
            return contentRect;
        }

        public BlockView GetBlockPrefab()
        {
            return blockPrefab;
        }

        public void ReturnBlockToScroll(BlockView block)
        {
            block.SetParent(contentRect);
            block.RectTransform.anchoredPosition = Vector2.zero;
            block.RectTransform.sizeDelta = _blockSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }
}
