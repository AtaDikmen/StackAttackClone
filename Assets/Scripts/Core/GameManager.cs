using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Data;
using Gameplay;
using Gameplay.Obstacles;
using Systems;

namespace Core
{
    public enum GameState
    {
        StartScreen,
        Playing,
        PausedForPerk,
        EndGame
    }

    public class GameManager : IStartable
    {
        public GameState CurrentState { get; private set; } = GameState.StartScreen;
        public int       CurrentLevel { get; private set; } = 1;
        public bool      LastGameWon  { get; private set; }

        public event Action<GameState> OnStateChanged;
        public event Action<bool>      OnGameEnded;
        public event Action<Obstacle>  OnBossPhaseStarted;

        public void NotifyBossPhaseStarted(Obstacle bossObstacle)
        {
            OnBossPhaseStarted?.Invoke(bossObstacle);
        }

        private readonly IObjectResolver  _resolver;
        private readonly PlayerController _playerPrefab;
        private readonly Transform        _spawnPoint;
        private readonly LevelSpawner     _levelSpawner;
        private readonly SaveManager      _saveManager;
        private readonly XPSystem         _xpSystem;
        private readonly PerkSystem       _perkSystem;
        private readonly HealthSystem     _healthSystem;

        private PlayerController _currentPlayer;

        [Inject]
        public GameManager(
            IObjectResolver  resolver,
            PlayerController playerPrefab,
            Transform        spawnPoint,
            LevelSpawner     levelSpawner,
            SaveManager      saveManager,
            XPSystem         xpSystem,
            PerkSystem       perkSystem,
            HealthSystem     healthSystem)
        {
            _resolver     = resolver;
            _playerPrefab = playerPrefab;
            _spawnPoint   = spawnPoint;
            _levelSpawner = levelSpawner;
            _saveManager  = saveManager;
            _xpSystem     = xpSystem;
            _perkSystem   = perkSystem;
            _healthSystem = healthSystem;
        }

        public void Start()
        {
            CurrentLevel = _saveManager?.GetHighestLevel() ?? 1;

            if(_xpSystem != null) _xpSystem.OnLevelUp            += HandlePerkLevelUp;
            if(_healthSystem != null) _healthSystem.OnPlayerDied += HandlePlayerDied;

            ChangeState(GameState.StartScreen);
        }

        public void StartGame()
        {
            if(CurrentState == GameState.StartScreen || CurrentState == GameState.EndGame)
            {
                Time.timeScale = 1f;
                _perkSystem?.ResetPerks();
                _xpSystem?.ResetForLevel(100 + (CurrentLevel - 1) * 20);
                _healthSystem?.ResetHealth();

                SpawnPlayer();
                _levelSpawner?.StartLevel(CurrentLevel);

                ChangeState(GameState.Playing);
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            _levelSpawner?.ClearActiveObstacles();
            StartGame();
        }

        public void NextLevel()
        {
            CurrentLevel++;
            StartGame();
        }

        public void GoToStartScreen()
        {
            Time.timeScale = 1f;
            _levelSpawner?.ClearActiveObstacles();
            DespawnPlayer();
            ChangeState(GameState.StartScreen);
        }

        public void ResumeFromPerkSelection()
        {
            Time.timeScale = 1f;
            ChangeState(GameState.Playing);
        }

        public void OnBossDefeated()
        {
            if(CurrentState != GameState.Playing) return;

            LastGameWon = true;
            _saveManager?.SaveHighestLevel(CurrentLevel + 1);

            EndGame(true);
        }

        public void OnObstacleReachedKillPlane()
        {
            if(CurrentState != GameState.Playing) return;

            LastGameWon = false;
            EndGame(false);
        }

        private void HandlePlayerDied()
        {
            if(CurrentState != GameState.Playing) return;

            LastGameWon = false;
            EndGame(false); // Tüm kalpler bittiğinde Level Failed
        }

        private void HandlePerkLevelUp()
        {
            if(CurrentState == GameState.Playing)
            {
                Time.timeScale = 0f;
                ChangeState(GameState.PausedForPerk);
            }
        }

        private void EndGame(bool isWin)
        {
            Time.timeScale = 1f;
            ChangeState(GameState.EndGame);
            OnGameEnded?.Invoke(isWin);
        }

        private void SpawnPlayer()
        {
            if(_currentPlayer == null && _playerPrefab != null)
            {
                Vector3    pos = _spawnPoint != null ? _spawnPoint.position : Vector3.zero;
                Quaternion rot = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

                _currentPlayer = _resolver.Instantiate(_playerPrefab, pos, rot);
            }
        }

        private void DespawnPlayer()
        {
            if(_currentPlayer != null)
            {
                UnityEngine.Object.Destroy(_currentPlayer.gameObject);
                _currentPlayer = null;
            }
        }

        private void ChangeState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
