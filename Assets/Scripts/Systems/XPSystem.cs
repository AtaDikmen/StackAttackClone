using System;
using UnityEngine;

namespace Systems
{
    public class XPSystem
    {
        public int   CurrentXP        { get; private set; }
        public int   CurrentPerkLevel { get; private set; } = 1;
        public int   XPToNextLevel    { get; private set; } = 100;
        public int   BaseXP           { get; private set; } = 100;
        public float GrowthFactor     { get; set; }         = 1.25f;
        public bool  IsXPDisabled     { get; set; }         = false;

        public event Action<float> OnXPChanged;

        public event Action OnLevelUp;

        public void SetXPEnabled(bool enabled)
        {
            IsXPDisabled = !enabled;
        }

        public void ResetForLevel(int baseXPRequired = 100)
        {
            CurrentPerkLevel = 1;
            BaseXP           = baseXPRequired > 0 ? baseXPRequired : 100;
            XPToNextLevel    = CalculateThresholdForLevel(CurrentPerkLevel);
            CurrentXP        = 0;
            IsXPDisabled     = false;
            OnXPChanged?.Invoke(0f);
        }

        public int CalculateThresholdForLevel(int perkLevel)
        {
            float scaled = BaseXP * Mathf.Pow(GrowthFactor, Mathf.Max(0, perkLevel - 1));
            return Mathf.Max(20, Mathf.RoundToInt(scaled));
        }

        public void AddXP(int amount)
        {
            if(IsXPDisabled || amount <= 0) return;

            CurrentXP += amount;

            while(CurrentXP >= XPToNextLevel)
            {
                CurrentXP -= XPToNextLevel;
                CurrentPerkLevel++;
                XPToNextLevel = CalculateThresholdForLevel(CurrentPerkLevel);

                OnXPChanged?.Invoke(1f);
                OnLevelUp?.Invoke();
            }

            float normalizedProgress = XPToNextLevel > 0 ? Mathf.Clamp01((float)CurrentXP / XPToNextLevel) : 0f;

            OnXPChanged?.Invoke(normalizedProgress);
        }
    }
}
