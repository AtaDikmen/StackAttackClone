using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using VContainer;
using Core;
using Gameplay.Levels;
using Gameplay.Obstacles;
using Gameplay.Obstacles.Behaviors;
using Systems;
using Cysharp.Threading.Tasks;
using VContainer.Unity;

namespace Gameplay
{
    public class LevelSpawner : MonoBehaviour
    {
        [Header("Prefabs & Spawn Configuration")]
        [SerializeField] private Obstacle obstaclePrefab;
        [SerializeField] private Transform   spawnOrigin;
        [SerializeField] private LevelData[] levels;

        [Header("Boss Final Phase Settings")]
        [SerializeField] private Vector3 bossArenaPosition = new Vector3(0f, 0f, 14f);
        [SerializeField] private float    bossEntranceDuration = 1.3f;
        [SerializeField] private Color    bossPhaseBgColor     = new Color(0.25f, 0.04f, 0.09f, 1f);
        [SerializeField] private float    bgTransitionDuration = 1.2f;
        [SerializeField] private Camera   mainCamera;
        [SerializeField] private Renderer groundRenderer;

        [Header("Boss Phase Obstacle Spam")]
        [SerializeField] private float spamMinInterval = 1.5f;
        [SerializeField] private float spamMaxInterval = 2.5f;

        private GameManager             _gameManager;
        private XPSystem                _xpSystem;
        private IObjectResolver         _resolver;
        private CancellationTokenSource _cts;

        private readonly List<GameObject> _spawnedGroupObjects = new List<GameObject>();
        private readonly List<GameObject> _normalGroupObjects  = new List<GameObject>();
        private          Obstacle         _currentBossObstacle;
        private          GameObject       _bossGroupObject;

        private Color            _originalCameraColor;
        private CameraClearFlags _originalClearFlags;
        private Color            _originalGroundColor;
        private bool             _hasCapturedOriginalColors;

        private GameManager GameManager => _gameManager ??= _resolver?.Resolve<GameManager>();
        private XPSystem    XPSystem    => _xpSystem ??= _resolver?.Resolve<XPSystem>();

        public int AvailableLevelCount => levels != null ? levels.Length : 0;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        private void Awake()
        {
            CaptureOriginalColors();
        }

        private void CaptureOriginalColors()
        {
            if(_hasCapturedOriginalColors) return;

            if(mainCamera == null) mainCamera = Camera.main;
            if(mainCamera != null)
            {
                _originalCameraColor = mainCamera.backgroundColor;
                _originalClearFlags  = mainCamera.clearFlags;
            }

            if(groundRenderer != null && groundRenderer.material != null)
            {
                _originalGroundColor = groundRenderer.material.color;
            }

            _hasCapturedOriginalColors = true;
        }

        public void StartLevel(int levelIndex)
        {
            ClearActiveObstacles();
            RestoreBackgroundColorsInstant();

            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            if(levels == null || levels.Length == 0)
            {
                Debug.LogWarning("LevelSpawner: No LevelData assigned!");
                return;
            }

            int       clampedIndex     = Mathf.Clamp(levelIndex - 1, 0, levels.Length - 1);
            LevelData currentLevelData = levels[clampedIndex];

            XPSystem?.SetXPEnabled(true);

            LevelRoutine(currentLevelData, _cts.Token).Forget();
        }

        public void ClearActiveObstacles()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            foreach(var groupObj in _spawnedGroupObjects)
            {
                if(groupObj != null) Destroy(groupObj);
            }
            _spawnedGroupObjects.Clear();
            _normalGroupObjects.Clear();

            _bossGroupObject     = null;
            _currentBossObstacle = null;
        }

        private async UniTaskVoid LevelRoutine(LevelData levelData, CancellationToken token)
        {
            List<WaveData> normalWaves = new List<WaveData>();
            WaveData       bossWave    = null;

            foreach(var wave in levelData.waves)
            {
                bool isBossWave = wave.obstacles.Exists(o => o.isBossUnit || o.movementPattern == MovementPatternType.BossOrbit);
                if(isBossWave && bossWave == null)
                    bossWave = wave;
                else
                    normalWaves.Add(wave);
            }

            bossWave ??= CreateDefaultBossWave();

            foreach(var wave in normalWaves)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(wave.delayBeforeWave), cancellationToken: token);

                if(GameManager == null || GameManager.CurrentState != GameState.Playing) return;

                SpawnNormalWave(wave, levelData.obstacleFallSpeed);
            }

            await UniTask.WaitUntil(
                () => AreAllNormalObstaclesCleared() || (GameManager != null && GameManager.CurrentState != GameState.Playing),
                cancellationToken: token
            );

            if(GameManager == null || GameManager.CurrentState != GameState.Playing) return;

            await UniTask.Delay(TimeSpan.FromSeconds(0.8f), cancellationToken: token);

            if(GameManager == null || GameManager.CurrentState != GameState.Playing) return;

            await StartBossPhase(bossWave, levelData.obstacleFallSpeed, token);
        }

        private bool AreAllNormalObstaclesCleared()
        {
            _normalGroupObjects.RemoveAll(go => go == null);

            for(int i = _normalGroupObjects.Count - 1; i >= 0; i--)
            {
                var groupObj = _normalGroupObjects[i];
                if(groupObj == null) continue;

                var group = groupObj.GetComponent<ObstacleGroup>();
                if(group == null || group.Members == null || group.Members.Count == 0)
                    _normalGroupObjects.RemoveAt(i);
            }

            return _normalGroupObjects.Count == 0;
        }

        private Vector3 GetGuaranteedCenterSpawnOrigin()
        {
            Vector3 origin = spawnOrigin != null ? spawnOrigin.position : new Vector3(0f, 0f, 30f);
            origin.x = 0f;
            return origin;
        }

        private void SpawnNormalWave(WaveData wave, float fallSpeed)
        {
            Vector3 origin = GetGuaranteedCenterSpawnOrigin();

            var groupedSetups     = new Dictionary<int, List<ObstacleSetup>>();
            var independentSetups = new List<ObstacleSetup>();

            foreach(var setup in wave.obstacles)
            {
                if(setup.groupId >= 0)
                {
                    if(!groupedSetups.ContainsKey(setup.groupId))
                        groupedSetups[setup.groupId] = new List<ObstacleSetup>();
                    groupedSetups[setup.groupId].Add(setup);
                }
                else
                {
                    independentSetups.Add(setup);
                }
            }

            foreach(var kvp in groupedSetups)
            {
                var groupList = kvp.Value;
                if(groupList.Count == 0) continue;

                ObstacleSetup firstConfig = groupList[0];

                float groupCenterX                           = 0f;
                foreach(var setup in groupList) groupCenterX += setup.xPosition;
                groupCenterX /= groupList.Count;

                Vector3 groupSpawnPos = origin + new Vector3(groupCenterX, 0f, 0f);

                var groupObj = new GameObject($"ObstacleGroup_G{kvp.Key}");
                groupObj.transform.position = groupSpawnPos;
                _spawnedGroupObjects.Add(groupObj);
                _normalGroupObjects.Add(groupObj);

                var groupComp = groupObj.AddComponent<ObstacleGroup>();

                foreach(var setup in groupList)
                {
                    Obstacle obstacle = InstantiateObstacle(origin, setup);
                    groupComp.AddMember(obstacle);
                }

                groupComp.Initialize(fallSpeed, firstConfig.movementPattern, firstConfig);
            }

            foreach(var setup in independentSetups)
            {
                var groupObj = new GameObject("ObstacleGroup_Single");
                groupObj.transform.position = origin;
                _spawnedGroupObjects.Add(groupObj);
                _normalGroupObjects.Add(groupObj);

                ObstacleGroup groupComp = groupObj.AddComponent<ObstacleGroup>();

                Obstacle obstacle = InstantiateObstacle(origin, setup);
                groupComp.AddMember(obstacle);

                groupComp.Initialize(fallSpeed, setup.movementPattern, setup);
            }
        }

        public async UniTask StartBossPhase(WaveData bossWave, float fallSpeed, CancellationToken token)
        {
            XPSystem?.SetXPEnabled(false);

            TransitionBackgroundColor(bossPhaseBgColor, bgTransitionDuration, token).Forget();

            Vector3 origin = GetGuaranteedCenterSpawnOrigin();

            var bossGroupObj = new GameObject("ObstacleGroup_BossFinalPhase");
            bossGroupObj.transform.position = origin;
            _spawnedGroupObjects.Add(bossGroupObj);
            _bossGroupObject = bossGroupObj;

            ObstacleGroup groupComp    = bossGroupObj.AddComponent<ObstacleGroup>();
            Obstacle      bossObstacle = null;
            ObstacleSetup bossConfig   = null;

            foreach(var setup in bossWave.obstacles)
            {
                Obstacle obs = InstantiateObstacle(origin, setup);
                groupComp.AddMember(obs);

                if(setup.isBossUnit && bossObstacle == null)
                {
                    bossObstacle = obs;
                    bossConfig   = setup;
                    RegisterBossUnit(obs);
                }
            }

            if(bossConfig == null && bossWave.obstacles.Count > 0)
                bossConfig = bossWave.obstacles[0];

            groupComp.Initialize(fallSpeed, MovementPatternType.BossOrbit, bossConfig ?? new ObstacleSetup());

            if(groupComp.Behavior is BossOrbitBehavior bossBehavior)
                bossBehavior.IsAnchored = true;

            if(bossObstacle != null)
                GameManager?.NotifyBossPhaseStarted(bossObstacle);

            await AnimateBossEntrance(bossGroupObj, origin, bossArenaPosition, bossEntranceDuration, token);

            if(token.IsCancellationRequested || GameManager == null || GameManager.CurrentState != GameState.Playing)
                return;

            if(bossObstacle != null)
                BossPhaseObstacleSpamRoutine(bossObstacle, fallSpeed, token).Forget();
        }

        private async UniTask AnimateBossEntrance(GameObject bossGroup, Vector3 startPos, Vector3 targetPos, float duration, CancellationToken token)
        {
            float elapsed = 0f;

            while(elapsed < duration)
            {
                if(bossGroup == null || token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float easeOut = 1f - Mathf.Pow(1f - t, 3f);
                bossGroup.transform.position = Vector3.Lerp(startPos, targetPos, easeOut);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if(bossGroup != null)
                bossGroup.transform.position = targetPos;
        }

        private async UniTaskVoid BossPhaseObstacleSpamRoutine(Obstacle boss, float fallSpeed, CancellationToken token)
        {
            float[] lanes  = LevelData.STANDARD_LANES;
            Vector3 origin = GetGuaranteedCenterSpawnOrigin();

            while(!token.IsCancellationRequested && boss != null && boss.CurrentHealth > 0)
            {
                float delay = UnityEngine.Random.Range(spamMinInterval, spamMaxInterval);
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

                if(token.IsCancellationRequested || boss == null || boss.CurrentHealth <= 0) break;
                if(GameManager == null || GameManager.CurrentState != GameState.Playing) break;

                float laneX = lanes[UnityEngine.Random.Range(0, lanes.Length)];
                ObstacleSetup singleSetup = new ObstacleSetup
                                            {
                                                xPosition        = laneX,
                                                health           = UnityEngine.Random.Range(2, 4) * 10,
                                                obstacleColor    = new Color(0.9f, 0.4f, 0.2f),
                                                groupId          = -1,
                                                movementPattern  = MovementPatternType.SingleFalling,
                                                xpRewardPerLayer = 0
                                            };

                GameObject groupObj = new GameObject("ObstacleGroup_SpamSingle");
                groupObj.transform.position = origin;
                _spawnedGroupObjects.Add(groupObj);

                ObstacleGroup groupComp = groupObj.AddComponent<ObstacleGroup>();
                Obstacle      obstacle  = InstantiateObstacle(origin, singleSetup);
                groupComp.AddMember(obstacle);
                groupComp.Initialize(fallSpeed * 1.1f, MovementPatternType.SingleFalling, singleSetup);
            }
        }

        private Obstacle InstantiateObstacle(Vector3 origin, ObstacleSetup setup)
        {
            float   yOffset  = setup.isBossUnit ? 0f : 2f;
            Vector3 spawnPos = origin + new Vector3(setup.xPosition, yOffset, 0f);

            Obstacle obstacle;
            if(obstaclePrefab != null)
            {
                obstacle = _resolver != null ?
                    _resolver.Instantiate(obstaclePrefab, spawnPos, Quaternion.identity) :
                    Instantiate(obstaclePrefab, spawnPos, Quaternion.identity);
            }
            else
            {
                GameObject obj = new GameObject("Obstacle");
                obj.transform.position = spawnPos;

                obstacle = obj.AddComponent<Obstacle>();
                _resolver?.InjectGameObject(obj);
            }

            obstacle.Initialize(setup.health, setup.obstacleColor, setup.xpRewardPerLayer, setup.isBossUnit);
            return obstacle;
        }

        private void RegisterBossUnit(Obstacle bossObstacle)
        {
            _currentBossObstacle    =  bossObstacle;
            bossObstacle.OnDefeated += HandleBossDefeated;
        }

        private void HandleBossDefeated()
        {
            _cts?.Cancel();
            RestoreBackgroundColors(bgTransitionDuration).Forget();
            GameManager?.OnBossDefeated();
        }

        private async UniTask TransitionBackgroundColor(Color targetColor, float duration, CancellationToken token)
        {
            if(mainCamera == null) mainCamera = Camera.main;
            if(mainCamera == null) return;

            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            Color startCamColor    = mainCamera.backgroundColor;
            Color startGroundColor = groundRenderer != null && groundRenderer.material != null ? groundRenderer.material.color : targetColor;

            float elapsed = 0f;
            while(elapsed < duration)
            {
                if(token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if(mainCamera != null)
                    mainCamera.backgroundColor = Color.Lerp(startCamColor, targetColor, t);

                if(groundRenderer != null && groundRenderer.material != null)
                    groundRenderer.material.color = Color.Lerp(startGroundColor, targetColor, t);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if(mainCamera != null) mainCamera.backgroundColor                                           = targetColor;
            if(groundRenderer != null && groundRenderer.material != null) groundRenderer.material.color = targetColor;
        }

        private async UniTask RestoreBackgroundColors(float duration)
        {
            if(!_hasCapturedOriginalColors) return;

            try
            {
                await TransitionBackgroundColor(_originalCameraColor, duration, this.GetCancellationTokenOnDestroy());
                if(mainCamera != null) mainCamera.clearFlags = _originalClearFlags;
                if(groundRenderer != null && groundRenderer.material != null) groundRenderer.material.color = _originalGroundColor;
            }
            catch(OperationCanceledException)
            {
            }
        }

        private void RestoreBackgroundColorsInstant()
        {
            if(!_hasCapturedOriginalColors) return;

            if(mainCamera == null) mainCamera = Camera.main;
            if(mainCamera != null)
            {
                mainCamera.backgroundColor = _originalCameraColor;
                mainCamera.clearFlags      = _originalClearFlags;
            }

            if(groundRenderer != null && groundRenderer.material != null)
                groundRenderer.material.color = _originalGroundColor;
        }

        private WaveData CreateDefaultBossWave()
        {
            var wave = new WaveData
                       {
                           delayBeforeWave = 2f,
                           obstacles       = new List<ObstacleSetup>()
                       };

            int groupId = 999;

            wave.obstacles.Add(new ObstacleSetup
                               {
                                   xPosition        = 0f,
                                   health           = 160,
                                   obstacleColor    = new Color(0.9f, 0.15f, 0.2f),
                                   groupId          = groupId,
                                   movementPattern  = MovementPatternType.BossOrbit,
                                   orbitRadius      = 2.2f,
                                   isBossUnit       = true,
                                   xpRewardPerLayer = 0
                               });

            for(int i = 0; i < 4; i++)
            {
                wave.obstacles.Add(new ObstacleSetup
                                   {
                                       xPosition        = 0f,
                                       health           = 30,
                                       obstacleColor    = new Color(0.6f, 0.15f, 0.25f),
                                       groupId          = groupId,
                                       movementPattern  = MovementPatternType.BossOrbit,
                                       orbitRadius      = 2.2f,
                                       isBossUnit       = false,
                                       xpRewardPerLayer = 0
                                   });
            }

            return wave;
        }

        private void OnDrawGizmos()
        {
            Vector3 originPos = GetGuaranteedCenterSpawnOrigin();

            Gizmos.color = Color.green;
            Gizmos.DrawLine(new Vector3(0f, 0f, -5f), new Vector3(0f, 0f, originPos.z + 5f));

            Gizmos.color = Color.cyan;
            foreach(float x in LevelData.STANDARD_LANES)
            {
                Vector3 startPos = new Vector3(x, 2f, 0f);
                Vector3 endPos   = new Vector3(x, 2f, originPos.z);
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireCube(endPos, new Vector3(1f, 1f, 1f));
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            RestoreBackgroundColorsInstant();
        }
    }
}
