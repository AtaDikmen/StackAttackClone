using UnityEngine;
using VContainer;
using Core;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private StartScreenPanel startScreenPanel;
        [SerializeField] private PlayScreenPanel    playScreenPanel;
        [SerializeField] private EndGamePanel       endGamePanel;
        [SerializeField] private PerkSelectionPanel perkSelectionPanel;

        private GameManager _gameManager;

        [Inject]
        public void Construct(IObjectResolver resolver, GameManager gameManager)
        {
            _gameManager = gameManager;

            if(startScreenPanel != null) resolver.Inject(startScreenPanel);
            if(playScreenPanel != null) resolver.Inject(playScreenPanel);
            if(endGamePanel != null) resolver.Inject(endGamePanel);
            if(perkSelectionPanel != null) resolver.Inject(perkSelectionPanel);
        }

        private void Start()
        {
            if(_gameManager != null)
            {
                _gameManager.OnStateChanged += HandleStateChanged;
                _gameManager.OnGameEnded    += HandleGameEnded;

                HandleStateChanged(_gameManager.CurrentState);
            }
        }

        private void HandleStateChanged(GameState state)
        {
            if(startScreenPanel != null)
            {
                if(state == GameState.StartScreen) startScreenPanel.Show();
                else startScreenPanel.Hide();
            }

            if(playScreenPanel != null)
            {
                if(state == GameState.Playing) playScreenPanel.Show();
                else playScreenPanel.Hide();
            }

            if(perkSelectionPanel != null)
            {
                if(state == GameState.PausedForPerk) perkSelectionPanel.Show();
                else perkSelectionPanel.Hide();
            }

            if(endGamePanel != null && state != GameState.EndGame)
                endGamePanel.Hide();
        }

        private void HandleGameEnded(bool isWin)
        {
            if(endGamePanel != null)
                endGamePanel.Show(isWin);
        }

        private void OnDestroy()
        {
            if(_gameManager != null)
            {
                _gameManager.OnStateChanged -= HandleStateChanged;
                _gameManager.OnGameEnded    -= HandleGameEnded;
            }
        }
    }
}
