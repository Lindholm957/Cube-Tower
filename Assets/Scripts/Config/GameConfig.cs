using UnityEngine;

namespace CubeTower.Config
{
    [CreateAssetMenu(menuName = "CubeTower/GameConfig", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("=== Bottom Scroll Settings ===")]
        [Tooltip("Number of blocks in bottom horizontal scroll")]
        [SerializeField] private int bottomBlockCount = 20;
        public int BottomBlockCount => bottomBlockCount;

        [Tooltip("Block width in pixels")]
        [SerializeField] private float blockWidth = 120f;
        public float BlockWidth => blockWidth;

        [Tooltip("Block height in pixels")]
        [SerializeField] private float blockHeight = 120f;
        public float BlockHeight => blockHeight;

        [Tooltip("Spacing between blocks in scroll")]
        [SerializeField] private float blockSpacing = 10f;
        public float BlockSpacing => blockSpacing;

        [Tooltip("Sprites for bottom blocks - each block gets sprite by index % length")]
        [SerializeField] private Sprite[] blockSprites = new Sprite[0];
        public Sprite[] BlockSprites => blockSprites;

        [Tooltip("Default sprite for blocks (fallback)")]
        [SerializeField] private Sprite defaultSprite;
        public Sprite DefaultSprite => defaultSprite;

        [Header("=== Tower Settings ===")]
        [Tooltip("Maximum horizontal offset as fraction of block width (0.5 = 50%)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float maxHorizontalOffset = 0.5f;
        public float MaxHorizontalOffset => maxHorizontalOffset;

        [Tooltip("Duration of jump animation when placing block")]
        [SerializeField] private float placeAnimationDuration = 0.3f;
        public float PlaceAnimationDuration => placeAnimationDuration;

        [Tooltip("Height of jump animation in pixels")]
        [SerializeField] private float placeJumpHeight = 50f;
        public float PlaceJumpHeight => placeJumpHeight;

        [Tooltip("Duration of miss/explosion animation")]
        [SerializeField] private float missAnimationDuration = 0.35f;
        public float MissAnimationDuration => missAnimationDuration;

        [Header("=== Screen Areas (as screen fractions) ===")]
        [Tooltip("Minimum Y position for tower area (0.5 = middle of screen)")]
        [SerializeField] private float towerAreaMinY = 0.5f;
        public float TowerAreaMinY => towerAreaMinY;

        [Tooltip("Minimum X position for hole area")]
        [SerializeField] private float holeAreaMinX = 0f;
        public float HoleAreaMinX => holeAreaMinX;

        [Tooltip("Maximum X position for hole area")]
        [SerializeField] private float holeAreaMaxX = 0.25f;
        public float HoleAreaMaxX => holeAreaMaxX;

        [Tooltip("Minimum Y position for hole area")]
        [SerializeField] private float holeAreaMinY = 0.5f;
        public float HoleAreaMinY => holeAreaMinY;

        [Header("=== Save Settings ===")]
        [Tooltip("PlayerPrefs key prefix for saving")]
        [SerializeField] private string saveKey = "CubeTower";
        public string SaveKey => saveKey;

        [Header("=== Localization ===")]
        [Tooltip("Language code: en, ru, etc.")]
        [SerializeField] private string language = "en";
        public string Language => language;
    }
}
