using System.Collections.Generic;
using UnityEngine;
using Gameplay.Levels;

namespace Gameplay.Obstacles.Behaviors
{
    /// <summary>
    /// Members orbit the group pivot in the XY plane while the group falls along -Z.
    /// Preserves individual angle offsets when members die to prevent snap-repositioning.
    /// </summary>
    public class CircularRotatingBehavior : IMovementBehavior
    {
        private          ObstacleGroup _group;
        private readonly float         _orbitRadius;
        private readonly float         _rotateSpeed;
        private          float         _currentAngle;

        private readonly Dictionary<Obstacle, float> _memberAngles = new Dictionary<Obstacle, float>();

        public CircularRotatingBehavior(ObstacleSetup config)
        {
            _orbitRadius = config.orbitRadius;
            _rotateSpeed = 90f; // 1 full revolution per 4 s
        }

        public void Initialize(ObstacleGroup group)
        {
            _group = group;
            _memberAngles.Clear();

            var members = _group.Members;
            if(members.Count == 0) return;

            float angleStep = 360f / members.Count;
            for(int i = 0; i < members.Count; i++)
            {
                if(members[i] != null)
                    _memberAngles[members[i]] = angleStep * i;
            }

            RepositionMembers();
        }

        public void Tick(float deltaTime)
        {
            _group.transform.Translate(Vector3.back * (_group.FallSpeed * deltaTime), Space.World);

            _currentAngle += _rotateSpeed * deltaTime;
            RepositionMembers();
        }

        private void RepositionMembers()
        {
            var members = _group.Members;
            for(int i = 0; i < members.Count; i++)
            {
                Obstacle obstacle = members[i];
                if(obstacle != null && _memberAngles.TryGetValue(obstacle, out float initialAngleOffset))
                {
                    float rad = (_currentAngle + initialAngleOffset) * Mathf.Deg2Rad;
                    obstacle.transform.localPosition = new Vector3(Mathf.Cos(rad) * _orbitRadius, 0f, Mathf.Sin(rad) * _orbitRadius);
                }
            }
        }
    }
}
