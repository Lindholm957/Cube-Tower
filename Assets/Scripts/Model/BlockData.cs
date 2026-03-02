using System;
using UnityEngine;

namespace CubeTower.Model
{
    [Serializable]
    public class BlockData
    {
        public int Id;
        public int SpriteIndex;
        public float HorizontalOffset;
        public float TowerY;

        public BlockData(int id, int spriteIndex)
        {
            Id = id;
            SpriteIndex = spriteIndex;
            HorizontalOffset = 0f;
            TowerY = 0f;
        }
    }

    [Serializable]
    public class TowerState
    {
        public BlockData[] Blocks = Array.Empty<BlockData>();
        // Anchored position of the tower root (saved/restored)
        public Vector2 TowerRootAnchoredPosition = Vector2.zero;

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static TowerState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new TowerState();
            try
            {
                return JsonUtility.FromJson<TowerState>(json) ?? new TowerState();
            }
            catch
            {
                return new TowerState();
            }
        }
    }
}
