using System.Collections.Generic;
using CubeTower.Model;

namespace CubeTower.Model
{
    public class GameModel
    {
        public List<BlockData> TowerBlocks { get; } = new List<BlockData>();
        // Anchored position of tower root in UI coordinates
        public UnityEngine.Vector2 TowerRootAnchoredPosition { get; set; } = UnityEngine.Vector2.zero;
        
        public int BottomBlockCount { get; private set; }
        public int MaxSpriteIndex { get; private set; }

        public void Initialize(int bottomBlockCount, int spriteCount)
        {
            BottomBlockCount = bottomBlockCount;
            MaxSpriteIndex = spriteCount > 0 ? spriteCount - 1 : 0;
        }

        public void AddBlockToTower(BlockData block)
        {
            TowerBlocks.Add(block);
        }

        public BlockData RemoveTopBlock()
        {
            if (TowerBlocks.Count == 0)
                return null;

            var block = TowerBlocks[TowerBlocks.Count - 1];
            TowerBlocks.RemoveAt(TowerBlocks.Count - 1);
            return block;
        }

        public BlockData RemoveBlockAt(int index)
        {
            if (index < 0 || index >= TowerBlocks.Count)
                return null;

            var block = TowerBlocks[index];
            TowerBlocks.RemoveAt(index);
            return block;
        }

        public int GetTowerHeight()
        {
            return TowerBlocks.Count;
        }

        public bool CanPlaceOnTower(int currentHeight, float screenHeight, float blockHeight, float minY)
        {
            float towerTopY = currentHeight * blockHeight;
            float screenTopY = screenHeight * minY;
            return towerTopY < (screenHeight - screenTopY);
        }

        public void LoadFromState(TowerState state)
        {
            TowerBlocks.Clear();
            if (state.Blocks != null)
            {
                foreach (var block in state.Blocks)
                {
                    TowerBlocks.Add(block);
                }
            }

            // restore tower root position
            TowerRootAnchoredPosition = state != null ? state.TowerRootAnchoredPosition : UnityEngine.Vector2.zero;
        }

        public TowerState SaveToState()
        {
            return new TowerState
            {
                Blocks = TowerBlocks.ToArray()
                ,
                TowerRootAnchoredPosition = TowerRootAnchoredPosition
            };
        }
    }
}
