using UnityEngine;

namespace CubeTower.Services
{
    public class SaveService : MonoBehaviour, ISaveService
    {
        [SerializeField] private string saveKey = "CubeTower_Save";

        public void SaveTowerState(string json)
        {
            PlayerPrefs.SetString(saveKey + "_tower", json);
            PlayerPrefs.Save();
        }

        public string LoadTowerState()
        {
            return PlayerPrefs.GetString(saveKey + "_tower", "");
        }

        public void ClearSave()
        {
            PlayerPrefs.DeleteKey(saveKey + "_tower");
            PlayerPrefs.Save();
        }
    }
}
