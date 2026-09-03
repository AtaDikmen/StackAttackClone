using System;
using Audio;
using UnityEngine;
using VContainer;

namespace Systems
{
    public class HealthSystem
    {
        public int MaxHealth     { get; private set; } = 3;
        public int CurrentHealth { get; private set; }

        public event Action<int, int> OnHealthChanged;
        public event Action           OnPlayerDied;

        private readonly AudioManager _audioManager;

        [Inject]
        public HealthSystem(AudioManager audioManager = null)
        {
            _audioManager = audioManager;
        }

        public void ResetHealth(int max = 3)
        {
            MaxHealth     = max;
            CurrentHealth = max;
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        }

        public void TakeDamage(int amount = 1)
        {
            if(CurrentHealth <= 0) return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            _audioManager?.PlayDamage();
            OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);

            if(CurrentHealth <= 0)
            {
                OnPlayerDied?.Invoke();
            }
        }
    }
}
