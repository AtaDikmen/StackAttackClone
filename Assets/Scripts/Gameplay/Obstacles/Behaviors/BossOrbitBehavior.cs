using System.Collections.Generic;
using UnityEngine;
using Gameplay.Levels;

namespace Gameplay.Obstacles.Behaviors
{
    /// <summary>
    /// Boss formation: The FIRST member (index 0) sits at the pivot (0,0).
    /// Minions follow a precise SQUARE / RECTANGULAR perimeter orbit with crisp 90-degree corner breaks.
    /// Preserves uniform spacing and initial offsets even when minions are defeated.
    /// </summary>
    public class BossOrbitBehavior : IMovementBehavior
    {
        private ObstacleGroup _group;
        private readonly float _halfWidth;
        private readonly float _halfDepth;
        private readonly float _totalPerimeter;
        private readonly float _linearSpeed;
        private float _currentDistance;

        private const float BossSlowdownFactor = 0.35f;

        public bool IsAnchored { get; set; } = false;

        private readonly Dictionary<Obstacle, float> _orbiterOffsets = new Dictionary<Obstacle, float>();

        public BossOrbitBehavior(ObstacleSetup config)
        {
            _halfWidth = Mathf.Max(1.5f, config.orbitRadius > 0 ? config.orbitRadius : 2.2f);
            _halfDepth = _halfWidth;
            _totalPerimeter = 4f * (_halfWidth + _halfDepth);
            _linearSpeed = _totalPerimeter / 6f; 
        }

        public BossOrbitBehavior(float halfWidth, float halfDepth, float revolutionSeconds = 6f)
        {
            _halfWidth = Mathf.Max(1.5f, halfWidth);
            _halfDepth = Mathf.Max(1.5f, halfDepth);
            _totalPerimeter = 4f * (_halfWidth + _halfDepth);
            _linearSpeed = _totalPerimeter / Mathf.Max(1f, revolutionSeconds);
        }

        public void Initialize(ObstacleGroup group)
        {
            _group = group;
            _orbiterOffsets.Clear();

            var members = _group.Members;
            if (members.Count == 0) return;

            if (members[0] != null)
                members[0].transform.localPosition = Vector3.zero;

            int orbiterCount = members.Count - 1;
            if (orbiterCount > 0)
            {
                float step = _totalPerimeter / orbiterCount;
                for (int i = 1; i < members.Count; i++)
                {
                    if (members[i] != null)
                        _orbiterOffsets[members[i]] = step * (i - 1);
                }
            }

            RepositionOrbiters();
        }

        public void Tick(float deltaTime)
        {
            if (_group == null) return;

            if (!IsAnchored)
            {
                _group.transform.Translate(
                    Vector3.back * (_group.FallSpeed * BossSlowdownFactor * deltaTime),
                    Space.World
                );
            }

            _currentDistance = (_currentDistance + _linearSpeed * deltaTime) % _totalPerimeter;
            RepositionOrbiters();
        }

        private void RepositionOrbiters()
        {
            if (_group == null) return;

            var members = _group.Members;
            for (int i = 0; i < members.Count; i++)
            {
                Obstacle obstacle = members[i];
                if (obstacle == null || obstacle.IsBossUnit) continue;

                if (_orbiterOffsets.TryGetValue(obstacle, out float initialOffset))
                {
                    float d = (_currentDistance + initialOffset) % _totalPerimeter;
                    obstacle.transform.localPosition = EvaluateSquarePosition(d);
                }
            }
        }

        /// <summary>
        /// Calculates position along square perimeter:
        /// Top (-W, H -> W, H) -> Right (W, H -> W, -H) -> Bottom (W, -H -> -W, -H) -> Left (-W, -H -> -W, H)
        /// Creating crisp 90-degree corner breaks.
        /// </summary>
        private Vector3 EvaluateSquarePosition(float distance)
        {
            float d = distance % _totalPerimeter;
            if (d < 0f) d += _totalPerimeter;

            float topLen = 2f * _halfWidth;
            float rightLen = 2f * _halfDepth;
            float bottomLen = 2f * _halfWidth;

            // Top segment: Moving Right from (-halfWidth, halfDepth) to (+halfWidth, halfDepth)
            if (d < topLen)
            {
                float x = -_halfWidth + d;
                return new Vector3(x, 0f, _halfDepth);
            }
            d -= topLen;

            // Right segment: Moving Down from (+halfWidth, halfDepth) to (+halfWidth, -halfDepth)
            if (d < rightLen)
            {
                float z = _halfDepth - d;
                return new Vector3(_halfWidth, 0f, z);
            }
            d -= rightLen;

            // Bottom segment: Moving Left from (+halfWidth, -halfDepth) to (-halfWidth, -halfDepth)
            if (d < bottomLen)
            {
                float x = _halfWidth - d;
                return new Vector3(x, 0f, -_halfDepth);
            }
            d -= bottomLen;

            // Left segment: Moving Up from (-halfWidth, -halfDepth) to (-halfWidth, +halfDepth)
            float zUp = -_halfDepth + d;
            return new Vector3(-_halfWidth, 0f, zUp);
        }
    }

    /// <summary>
    /// Explicit alias for square orbit behavior matching requirements.
    /// </summary>
    public class SquareOrbitBehavior : BossOrbitBehavior
    {
        public SquareOrbitBehavior(ObstacleSetup config) : base(config) { }
        public SquareOrbitBehavior(float halfWidth, float halfDepth, float revolutionSeconds = 6f) 
            : base(halfWidth, halfDepth, revolutionSeconds) { }
    }
}