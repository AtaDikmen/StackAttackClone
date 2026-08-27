using UnityEngine;
using VContainer;
using VContainer.Unity;
using Gameplay;

namespace Core
{
    public enum GameState { StartScreen, Playing, EndGame }

    public class GameManager
    {
        public GameState CurrentState { get; private set; } = GameState.StartScreen;
        public int       CurrentLevel { get; private set; } = 1;

        private readonly IObjectResolver  _resolver;
        private readonly PlayerController _playerPrefab;
        private readonly Transform        _spawnPoint;

        private PlayerController _currentPlayer;

        [Inject]
        public GameManager(IObjectResolver resolver, PlayerController playerPrefab, Transform spawnPoint)
        {
            _resolver     = resolver;
            _playerPrefab = playerPrefab;
            _spawnPoint   = spawnPoint;
        }

        public void StartGame()
        {
            if(CurrentState == GameState.StartScreen)
            {
                CurrentState = GameState.Playing;
                SpawnPlayer();
            }
        }

        private void SpawnPlayer()
        {
            if(_currentPlayer == null)
            {
                _currentPlayer = _resolver.Instantiate(_playerPrefab, _spawnPoint.position, _spawnPoint.rotation);
            }
        }

        public void EndGame(bool isWin)
        {
            CurrentState = GameState.EndGame;
            if(isWin) CurrentLevel++;
            // End Game Screen UI tetiklenecek
        }
    }
}
