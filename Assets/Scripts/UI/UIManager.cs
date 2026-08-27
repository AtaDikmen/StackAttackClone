using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VContainer;
using Core;
using Data;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject playPanel;

        [Header("Start Screen UI")]
        [SerializeField] private TextMeshProUGUI startLevelText;
        [SerializeField] private Button tapToStartButton;

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
            if(tapToStartButton != null)
            {
                tapToStartButton.onClick.AddListener(OnTapToStartClicked);
            }

            UpdateUIState(GameState.StartScreen);
        }

        private void OnTapToStartClicked()
        {
            _gameManager.StartGame();
            UpdateUIState(GameState.Playing);
        }

        public void UpdateUIState(GameState state)
        {
            if(startPanel != null) startPanel.SetActive(state == GameState.StartScreen);
            if(playPanel != null) playPanel.SetActive(state == GameState.Playing);

            if(state == GameState.StartScreen && startLevelText != null && _saveManager != null)
            {
                int currentLevel = _saveManager.GetCurrentLevel();
                startLevelText.text = $"Level {currentLevel}";
            }
        }

        private void OnDestroy()
        {
            if(tapToStartButton != null)
            {
                tapToStartButton.onClick.RemoveAllListeners();
            }
        }
    }
}
