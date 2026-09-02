using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Core;
using Systems;
using Gameplay.Obstacles;

namespace UI
{
    public class PlayScreenPanel : MonoBehaviour
    {
        [Header("Level & XP UI")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject xpBarContainer;
        [SerializeField] private Image      xpFillImage;
        [SerializeField] private Slider     xpSlider;

        [Header("Health UI")]
        [SerializeField] private Image[] heartImages;

        [Header("Boss Health Bar UI")]
        [SerializeField] private GameObject bossBarContainer;
        [SerializeField] private Slider bossHealthSlider;
        [SerializeField] private Image  bossHealthFillImage;

        private GameManager  _gameManager;
        private XPSystem     _xpSystem;
        private HealthSystem _healthSystem;
        private Obstacle     _currentBossObstacle;

        [Inject]
        public void Construct(GameManager gameManager, XPSystem xpSystem, HealthSystem healthSystem)
        {
            _gameManager  = gameManager;
            _xpSystem     = xpSystem;
            _healthSystem = healthSystem;
        }

        private void OnEnable()
        {
            if(_xpSystem != null) _xpSystem.OnXPChanged              += UpdateXPBar;
            if(_healthSystem != null) _healthSystem.OnHealthChanged  += UpdateHeartsDisplay;
            if(_gameManager != null) _gameManager.OnBossPhaseStarted += HandleBossPhaseStarted;
        }

        private void OnDisable()
        {
            if(_xpSystem != null) _xpSystem.OnXPChanged              -= UpdateXPBar;
            if(_healthSystem != null) _healthSystem.OnHealthChanged  -= UpdateHeartsDisplay;
            if(_gameManager != null) _gameManager.OnBossPhaseStarted -= HandleBossPhaseStarted;
            UnbindCurrentBoss();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateLevelDisplay();
            UpdateXPBar(0f);

            if(_healthSystem != null)
                UpdateHeartsDisplay(_healthSystem.CurrentHealth, _healthSystem.MaxHealth);

            SetBossBarVisible(false);
            SetXPBarVisible(true);
        }

        public void Hide()
        {
            UnbindCurrentBoss();
            gameObject.SetActive(false);
        }

        private void UpdateLevelDisplay()
        {
            if(levelText != null && _gameManager != null)
                levelText.text = $"Level {_gameManager.CurrentLevel}";
        }

        private void UpdateXPBar(float progressNormalized)
        {
            progressNormalized = Mathf.Clamp01(progressNormalized);

            if(xpFillImage != null) xpFillImage.fillAmount = progressNormalized;
            if(xpSlider != null) xpSlider.value            = progressNormalized;
        }

        private void UpdateHeartsDisplay(int currentHealth, int maxHealth)
        {
            if(heartImages == null) return;

            for(int i = 0; i < heartImages.Length; i++)
            {
                if(heartImages[i] != null)
                    heartImages[i].enabled = i < currentHealth;
            }
        }

        private void HandleBossPhaseStarted(Obstacle bossObstacle)
        {
            UnbindCurrentBoss();
            _currentBossObstacle = bossObstacle;

            SetXPBarVisible(false);
            SetBossBarVisible(true);

            if(bossObstacle != null)
            {
                bossObstacle.OnHealthChanged += UpdateBossHealth;
                bossObstacle.OnDefeated      += HandleBossDefeated;
                UpdateBossHealth(bossObstacle.CurrentHealth, bossObstacle.MaxHealth);
            }
        }

        private void UpdateBossHealth(int currentHealth, int maxHealth)
        {
            float progress = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

            if(bossHealthSlider != null)
                bossHealthSlider.value = progress;

            if(bossHealthFillImage != null)
                bossHealthFillImage.fillAmount = progress;
        }

        private void HandleBossDefeated()
        {
            UpdateBossHealth(0, 100);
            UnbindCurrentBoss();
        }

        private void UnbindCurrentBoss()
        {
            if(_currentBossObstacle != null)
            {
                _currentBossObstacle.OnHealthChanged -= UpdateBossHealth;
                _currentBossObstacle.OnDefeated      -= HandleBossDefeated;
                _currentBossObstacle                 =  null;
            }
        }

        private void SetXPBarVisible(bool isVisible)
        {
            if(xpBarContainer != null)
                xpBarContainer.SetActive(isVisible);
        }

        private void SetBossBarVisible(bool isVisible)
        {
            if(bossBarContainer != null)
                bossBarContainer.SetActive(isVisible);
        }

        private void OnDestroy()
        {
            UnbindCurrentBoss();
            if(_gameManager != null) _gameManager.OnBossPhaseStarted -= HandleBossPhaseStarted;
        }
    }
}
