using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Data
{
    public class SaveManager
    {
        private const string HighestLevelKey = "SA_HighestLevel";
        private const string TotalXPKey      = "SA_TotalXP";

        public int GetHighestLevel() => PlayerPrefs.GetInt(HighestLevelKey, 1);

        public void SaveHighestLevel(int level)
        {
            PlayerPrefs.SetInt(HighestLevelKey, level);
            PlayerPrefs.Save();
        }

        public int GetTotalXP() => PlayerPrefs.GetInt(TotalXPKey, 0);

        public void AddTotalXP(int xp)
        {
            PlayerPrefs.SetInt(TotalXPKey, GetTotalXP() + xp);
            PlayerPrefs.Save();
        }

        public void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<color=yellow>[SaveManager] Tüm PlayerPrefs verileri sıfırlandı!</color>");
        }

#if UNITY_EDITOR
        [MenuItem("Tools/Stack Attack/Clear All PlayerPrefs")]
        public static void ClearPlayerPrefsFromMenu()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("<color=green>[SaveManager] PlayerPrefs Unity Menüsünden Sıfırlandı!</color>");
        }
#endif
    }
}
