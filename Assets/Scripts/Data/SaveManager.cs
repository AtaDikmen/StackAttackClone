using UnityEngine;

namespace Data
{
    public class SaveManager
    {
        private const string HighestLevelKey = "SA_HighestLevel";
        private const string TotalXPKey      = "SA_TotalXP";

        public int GetHighestLevel() => PlayerPrefs.GetInt(HighestLevelKey, 1);

        public void SaveHighestLevel(int level)
        {
            if(level > GetHighestLevel())
            {
                PlayerPrefs.SetInt(HighestLevelKey, level);
                PlayerPrefs.Save();
            }
        }

        public int GetTotalXP() => PlayerPrefs.GetInt(TotalXPKey, 0);

        public void AddTotalXP(int xp)
        {
            PlayerPrefs.SetInt(TotalXPKey, GetTotalXP() + xp);
            PlayerPrefs.Save();
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
