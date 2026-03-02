using UnityEngine;
using CubeTower.Config;
using CubeTower.Services;

namespace CubeTower.Services
{
    public class ConfigService : MonoBehaviour, IConfigService
    {
        [SerializeField] private GameConfig config;

        public GameConfig Config => config;
    }
}
