namespace CubeTower.Services
{
    public static class Localization
    {
        private static readonly string[] English = new string[]
        {
            "Block placed!",
            "Block thrown to hole!",
            "Missed!",
            "Height limit reached!",
            "Tower is empty!"
        };

        private static readonly string[] Russian = new string[]
        {
            "Блок установлен!",
            "Бросок в дыру!",
            "Промах мимо башни!",
            "Достигнут предел!",
            "Башня пуста!"
        };

        public enum Key
        {
            BlockPlaced = 0,
            BlockThrownToHole = 1,
            BlockMissed = 2,
            HeightLimit = 3,
            TowerEmpty = 4
        }

        private static string[] _dictionary = English;

        public static void SetLanguage(string languageCode)
        {
            _dictionary = languageCode.ToLower().StartsWith("ru") ? Russian : English;
        }

        public static string Get(Key key)
        {
            return _dictionary[(int)key];
        }

        public static string Get(int index)
        {
            if (index >= 0 && index < _dictionary.Length)
                return _dictionary[index];
            return English[0];
        }
    }
}
