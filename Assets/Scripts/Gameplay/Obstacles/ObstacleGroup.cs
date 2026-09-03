using System.Collections.Generic;
using UnityEngine;
using Gameplay.Levels;
using Gameplay.Obstacles.Behaviors;

namespace Gameplay.Obstacles
{
    public class ObstacleGroup : MonoBehaviour
    {
        private readonly List<Obstacle>    _members = new List<Obstacle>();
        private          IMovementBehavior _behavior;

        public float                   FallSpeed { get; private set; }
        public IReadOnlyList<Obstacle> Members   => _members;
        public IMovementBehavior       Behavior  => _behavior;

        public void SetFallSpeed(float speed)
        {
            FallSpeed = speed;
        }

        public void Initialize(float fallSpeed, MovementPatternType pattern, ObstacleSetup config)
        {
            FallSpeed = fallSpeed;

            _behavior = pattern switch
                        {
                            MovementPatternType.SingleFalling      => new SingleFallingBehavior(),
                            MovementPatternType.CircularRotating   => new CircularRotatingBehavior(config),
                            MovementPatternType.PingPongHorizontal => new PingPongHorizontalBehavior(config),
                            MovementPatternType.BossOrbit          => new BossOrbitBehavior(config),
                            _                                      => new SingleFallingBehavior()
                        };

            _behavior.Initialize(this);
        }

        public void AddMember(Obstacle obstacle)
        {
            if(obstacle == null || _members.Contains(obstacle)) return;

            _members.Add(obstacle);
            obstacle.transform.SetParent(transform, worldPositionStays: true);
            obstacle.OnDefeated += () => HandleMemberDefeated(obstacle);
        }

        private void Update()
        {
            _behavior?.Tick(Time.deltaTime);

            if(transform.position.z < -10f)
                DespawnGroup();
        }

        private void HandleMemberDefeated(Obstacle obstacle)
        {
            _members.Remove(obstacle);

            if(_members.Count == 0)
                Destroy(gameObject);
        }

        private void DespawnGroup()
        {
            for(int i = _members.Count - 1; i >= 0; i--)
            {
                if(_members[i] != null)
                    Destroy(_members[i].gameObject);
            }
            _members.Clear();
            Destroy(gameObject);
        }
    }
}
