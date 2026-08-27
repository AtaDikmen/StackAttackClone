using UnityEngine;

namespace Data
{
    public class SaveManager
    {
        private const string LevelKey = "PlayerLevel";

        public int GetCurrentLevel()
        {
            return PlayerPrefs.GetInt(LevelKey, 1);
        }

        public void SaveLevel(int level)
        {
            PlayerPrefs.SetInt(LevelKey, level);
            PlayerPrefs.Save();
        }
    }
}
