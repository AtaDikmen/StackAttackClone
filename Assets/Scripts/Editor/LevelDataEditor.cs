#if UNITY_EDITOR
using Gameplay.Levels;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Editor
{
    [CustomEditor(typeof(LevelData))]
    public class LevelDataEditor : UnityEditor.Editor
    {
        private float _laneSpacing = LevelData.MIN_OBSTACLE_SPACING;

        public override void OnInspectorGUI()
        {
            LevelData levelData = (LevelData)target;

            EditorGUILayout.LabelField("Level Configuration", EditorStyles.boldLabel);
            levelData.levelIndex        = EditorGUILayout.IntField("Level Index", levelData.levelIndex);
            levelData.obstacleFallSpeed = EditorGUILayout.FloatField("Fall Speed", levelData.obstacleFallSpeed);
            levelData.xpToLevelUp       = EditorGUILayout.IntField("XP Required for Perk", levelData.xpToLevelUp);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Wave Generator (Min 2.0 Birim Mesafe)", EditorStyles.boldLabel);

            _laneSpacing = EditorGUILayout.FloatField("Lane Spacing (Min 2.0)", Mathf.Max(2.0f, _laneSpacing));

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Sol Dalgası (Left)", GUILayout.Height(26)))
            {
                GenerateSideWave(levelData, -2.0f, 2);
            }
            if(GUILayout.Button("Sağ Dalgası (Right)", GUILayout.Height(26)))
            {
                GenerateSideWave(levelData, 2.0f, 2);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Çift Taraflı (Dual-Side)", GUILayout.Height(26)))
            {
                GenerateDualSideWave(levelData);
            }
            if(GUILayout.Button("Rastgele Şerit Dalgası", GUILayout.Height(26)))
            {
                GenerateRandomLaneWave(levelData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Dairesel Dalga (Circular)", GUILayout.Height(26)))
            {
                GenerateCircularWave(levelData);
            }
            if(GUILayout.Button("Ping-Pong Dalgası (Horizontal)", GUILayout.Height(26)))
            {
                GeneratePingPongWave(levelData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Boss Dalgası (Merkez)", GUILayout.Height(26)))
            {
                GenerateBossWave(levelData);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if(GUILayout.Button("Validate & Fix Overlaps (Sanitize)", GUILayout.Height(30)))
            {
                levelData.SanitizeAllWaves();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Raw Data Inspector", EditorStyles.boldLabel);

            DrawDefaultInspectorWithoutScriptField();

            if(GUI.changed)
            {
                EditorUtility.SetDirty(levelData);
            }
        }

        private void GenerateSideWave(LevelData levelData, float centerPositionX, int count)
        {
            WaveData wave = new WaveData
                            {
                                delayBeforeWave = Random.Range(1.8f, 2.5f),
                                obstacles       = new List<ObstacleSetup>()
                            };

            int groupId = levelData.waves.Count * 10;

            for(int i = 0; i < count; i++)
            {
                float xOffset = centerPositionX + (i - (count - 1) / 2f) * _laneSpacing;

                wave.obstacles.Add(new ObstacleSetup
                                   {
                                       xPosition        = xOffset,
                                       health           = Random.Range(2, 5) * 10,
                                       obstacleColor    = RandomColor(),
                                       groupId          = groupId,
                                       movementPattern  = MovementPatternType.SingleFalling,
                                       xpRewardPerLayer = 30
                                   });
            }

            LevelData.SanitizeWaveOverlaps(wave, _laneSpacing);
            levelData.waves.Add(wave);
            EditorUtility.SetDirty(levelData);
        }

        private void GenerateDualSideWave(LevelData levelData)
        {
            WaveData wave = new WaveData
                            {
                                delayBeforeWave = 2.5f,
                                obstacles       = new List<ObstacleSetup>()
                            };

            int groupId = levelData.waves.Count * 10;

            wave.obstacles.Add(new ObstacleSetup
                               {
                                   xPosition       = -3.0f,
                                   health          = 30,
                                   obstacleColor   = RandomColor(),
                                   groupId         = groupId,
                                   movementPattern = MovementPatternType.SingleFalling
                               });

            wave.obstacles.Add(new ObstacleSetup
                               {
                                   xPosition       = 3.0f,
                                   health          = 30,
                                   obstacleColor   = RandomColor(),
                                   groupId         = groupId + 1,
                                   movementPattern = MovementPatternType.SingleFalling
                               });

            LevelData.SanitizeWaveOverlaps(wave, _laneSpacing);
            levelData.waves.Add(wave);
            EditorUtility.SetDirty(levelData);
        }

        private void GenerateRandomLaneWave(LevelData levelData)
        {
            WaveData wave = new WaveData
                            {
                                delayBeforeWave = 2f,
                                obstacles       = new List<ObstacleSetup>()
                            };

            float[]   lanes         = LevelData.STANDARD_LANES;
            int       count         = Random.Range(2, 3);
            List<int> selectedLanes = new List<int>();

            while(selectedLanes.Count < count)
            {
                int lane = Random.Range(0, lanes.Length);
                if(!selectedLanes.Contains(lane)) selectedLanes.Add(lane);
            }

            foreach(int idx in selectedLanes)
            {
                wave.obstacles.Add(new ObstacleSetup
                                   {
                                       xPosition       = lanes[idx],
                                       health          = Random.Range(2, 5) * 10,
                                       obstacleColor   = RandomColor(),
                                       groupId         = -1,
                                       movementPattern = MovementPatternType.SingleFalling
                                   });
            }

            LevelData.SanitizeWaveOverlaps(wave, _laneSpacing);
            levelData.waves.Add(wave);
            EditorUtility.SetDirty(levelData);
        }

        private void GenerateCircularWave(LevelData levelData)
        {
            WaveData wave = new WaveData
                            {
                                delayBeforeWave = 2.5f,
                                obstacles       = new List<ObstacleSetup>()
                            };

            int   groupId = levelData.waves.Count * 10;
            int   count   = 4;
            float centerX = Random.value > 0.5f ? -2.0f : 2.0f;

            for(int i = 0; i < count; i++)
            {
                float angle = (360f / count) * i * Mathf.Deg2Rad;
                wave.obstacles.Add(new ObstacleSetup
                                   {
                                       xPosition       = centerX + Mathf.Cos(angle) * 2.0f,
                                       health          = 30,
                                       obstacleColor   = RandomColor(),
                                       groupId         = groupId,
                                       movementPattern = MovementPatternType.CircularRotating,
                                       orbitRadius     = 2.0f
                                   });
            }

            LevelData.SanitizeWaveOverlaps(wave, _laneSpacing);
            levelData.waves.Add(wave);
            EditorUtility.SetDirty(levelData);
        }

        private void GeneratePingPongWave(LevelData levelData)
        {
            WaveData wave = new WaveData
                            {
                                delayBeforeWave = 2.0f,
                                obstacles       = new List<ObstacleSetup>()
                            };

            int groupId = levelData.waves.Count * 10;

            wave.obstacles.Add(new ObstacleSetup
                               {
                                   xPosition        = 0f,
                                   health           = Random.Range(3, 6) * 10,
                                   obstacleColor    = RandomColor(),
                                   groupId          = groupId,
                                   movementPattern  = MovementPatternType.PingPongHorizontal,
                                   pingPongSpeed    = 3.0f,
                                   pingPongRange    = 2.0f,
                                   xpRewardPerLayer = 30
                               });

            LevelData.SanitizeWaveOverlaps(wave, _laneSpacing);
            levelData.waves.Add(wave);
            EditorUtility.SetDirty(levelData);
        }

        private void GenerateBossWave(LevelData levelData)
        {
            WaveData bossWave = new WaveData
                                {
                                    delayBeforeWave = 3f,
                                    obstacles       = new List<ObstacleSetup>()
                                };

            int groupId = 999;

            bossWave.obstacles.Add(new ObstacleSetup
                                   {
                                       xPosition        = 0f,
                                       health           = 150,
                                       obstacleColor    = new Color(0.9f, 0.2f, 0.2f),
                                       groupId          = groupId,
                                       movementPattern  = MovementPatternType.BossOrbit,
                                       orbitRadius      = 2.5f,
                                       isBossUnit       = true,
                                       xpRewardPerLayer = 25
                                   });

            for(int i = 0; i < 4; i++)
            {
                bossWave.obstacles.Add(new ObstacleSetup
                                       {
                                           xPosition        = 0f,
                                           health           = 30,
                                           obstacleColor    = new Color(0.6f, 0.15f, 0.15f),
                                           groupId          = groupId,
                                           movementPattern  = MovementPatternType.BossOrbit,
                                           orbitRadius      = 2.5f,
                                           isBossUnit       = false,
                                           xpRewardPerLayer = 10
                                       });
            }

            LevelData.SanitizeWaveOverlaps(bossWave, _laneSpacing);
            levelData.waves.Add(bossWave);
            EditorUtility.SetDirty(levelData);
        }

        private static Color RandomColor() => new Color(Random.value, Random.value, Random.value);

        private void DrawDefaultInspectorWithoutScriptField()
        {
            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();
            if(prop.NextVisible(true))
            {
                do
                {
                    if(prop.name is "m_Script" or "levelIndex" or "obstacleFallSpeed" or "xpToLevelUp") continue;
                    EditorGUILayout.PropertyField(prop, true);
                } while(prop.NextVisible(false));
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
