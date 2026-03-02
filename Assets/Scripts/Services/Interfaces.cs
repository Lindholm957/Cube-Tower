using CubeTower.Config;

namespace CubeTower.Services
{
    public interface IConfigService
    {
        GameConfig Config { get; }
    }

    public interface ISaveService
    {
        void SaveTowerState(string json);
        string LoadTowerState();
        void ClearSave();
    }
}
