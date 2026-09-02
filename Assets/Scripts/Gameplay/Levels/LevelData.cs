using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Levels
{
    public enum MovementPatternType
    {
        SingleFalling,
        CircularRotating,
        PingPongHorizontal,
        BossOrbit
    }

    [Serializable]
    public class ObstacleSetup
    {
        [Tooltip("Spawn X offset from spawnOrigin.")]
        public float xPosition;

        [Tooltip("Total HP.")]
        public int health = 30;

        public Color obstacleColor = Color.white;

        [Tooltip("Obstacles sharing the same groupId move together. -1 = independent.")]
        public int groupId = -1;

        public MovementPatternType movementPattern = MovementPatternType.SingleFalling;

        [Header("Circular / Boss Orbit")]
        public float orbitRadius = 2.0f;

        [Header("Ping-Pong")]
        public float pingPongSpeed = 3f;
        public float pingPongRange = 1.5f;

        [Header("Boss")]
        public bool isBossUnit = false;

        [Header("XP")]
        public int xpRewardPerLayer = 10;
    }

    [Serializable]
    public class WaveData
    {
        [Tooltip("Seconds to wait after the previous wave before spawning this one.")]
        public float delayBeforeWave = 2f;

        public List<ObstacleSetup> obstacles = new List<ObstacleSetup>();
    }

    [CreateAssetMenu(fileName = "NewLevel", menuName = "SO/Level Data")]
    public class LevelData : ScriptableObject
    {
        public int   levelIndex;
        public float obstacleFallSpeed = 5f;

        [Tooltip("XP required for perk selection.")]
        public int xpToLevelUp = 100;

        public List<WaveData> waves = new List<WaveData>();

        public const           float   MIN_OBSTACLE_SPACING = 2.0f;
        public readonly static float[] STANDARD_LANES       = { -3.0f, -1.0f, 1.0f, 3.0f };

#if UNITY_EDITOR
        [ContextMenu("Validate & Fix Overlaps")]
        public void SanitizeAllWaves()
        {
            foreach(var wave in waves)
            {
                SanitizeWaveOverlaps(wave, MIN_OBSTACLE_SPACING);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public static void SanitizeWaveOverlaps(WaveData wave, float minSpacing = MIN_OBSTACLE_SPACING)
        {
            if(wave == null || wave.obstacles == null || wave.obstacles.Count <= 1) return;

            wave.obstacles.Sort((a, b) => a.xPosition.CompareTo(b.xPosition));
            for(int i = 0; i < wave.obstacles.Count - 1; i++)
            {
                float currentX = wave.obstacles[i].xPosition;
                float nextX    = wave.obstacles[i + 1].xPosition;

                if(Mathf.Abs(nextX - currentX) < minSpacing)
                    wave.obstacles[i + 1].xPosition = currentX + minSpacing;
            }

            int circularCount = 0;
            foreach(var obs in wave.obstacles)
            {
                if(obs.movementPattern is MovementPatternType.CircularRotating or MovementPatternType.BossOrbit)
                    circularCount++;
            }

            if(circularCount > 1)
            {
                float requiredRadius = minSpacing / (2f * Mathf.Sin(Mathf.PI / circularCount));
                float safeRadius     = Mathf.Max(2.0f, requiredRadius);

                foreach(var obs in wave.obstacles)
                {
                    if(obs.movementPattern is MovementPatternType.CircularRotating or MovementPatternType.BossOrbit)
                        obs.orbitRadius = Mathf.Max(obs.orbitRadius, safeRadius);
                }
            }
        }
#endif
    }
}
