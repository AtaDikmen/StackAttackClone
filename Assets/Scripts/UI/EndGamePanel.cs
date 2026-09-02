using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using Core;

namespace UI
{
    public class EndGamePanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI resultTitleText;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private GameManager _gameManager;

        [Inject]
        public void Construct(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Start()
        {
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        public void Show(bool isWin)
        {
            gameObject.SetActive(true);

            if (resultTitleText != null)
            {
                resultTitleText.text = isWin ? "VICTORY!" : "GAME OVER";
                resultTitleText.color = isWin ? new Color(0.2f, 0.8f, 0.3f) : new Color(0.9f, 0.2f, 0.2f);
            }

            if (nextLevelButton != null)
                nextLevelButton.gameObject.SetActive(isWin);

            if (retryButton != null)
                retryButton.gameObject.SetActive(!isWin);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnNextLevelClicked()
        {
            _gameManager?.NextLevel();
        }

        private void OnRetryClicked()
        {
            _gameManager?.RestartLevel();
        }

        private void OnMainMenuClicked()
        {
            _gameManager?.GoToStartScreen();
        }

        private void OnDestroy()
        {
            if (nextLevelButton != null) nextLevelButton.onClick.RemoveAllListeners();
            if (retryButton != null) retryButton.onClick.RemoveAllListeners();
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
        }
    }
}
