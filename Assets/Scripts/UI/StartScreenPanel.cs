using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Core;
using Data;

namespace UI
{
    public class StartScreenPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Button          tapToStartButton;

        private GameManager _gameManager;
        private SaveManager _saveManager;

        [Inject]
        public void Construct(GameManager gameManager, SaveManager saveManager)
        {
            _gameManager = gameManager;
            _saveManager = saveManager;
        }

        private void Start()
        {
            if(tapToStartButton == null)
                return;
            tapToStartButton.onClick.RemoveAllListeners();
            tapToStartButton.onClick.AddListener(OnTapToStartClicked);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateLevelDisplay();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateLevelDisplay()
        {
            if(levelText == null)
                return;
            int currentLevel = _gameManager?.CurrentLevel ?? (_saveManager?.GetHighestLevel() ?? 1);
            levelText.text = $"Level {currentLevel}";
        }

        private void OnTapToStartClicked()
        {
            _gameManager?.StartGame();
        }

        private void OnDestroy()
        {
            if(tapToStartButton != null)
                tapToStartButton.onClick.RemoveAllListeners();
        }
    }
}
