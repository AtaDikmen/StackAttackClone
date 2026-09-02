using UnityEngine;
using Gameplay.Levels;

namespace Gameplay.Obstacles.Behaviors
{
    /// <summary>
    /// The entire group ping-pongs horizontally (±pingPongRange on X) while continuously
    /// falling along -Z. Obstacles within the group keep their relative X spacing.
    /// </summary>
    public class PingPongHorizontalBehavior : IMovementBehavior
    {
        private          ObstacleGroup _group;
        private readonly float         _speed;
        private readonly float         _range;

        private float _baseX;
        private float _pingPongTimer;

        public PingPongHorizontalBehavior(ObstacleSetup config)
        {
            _speed = config.pingPongSpeed;
            _range = config.pingPongRange;
        }

        public void Initialize(ObstacleGroup group)
        {
            _group         = group;
            _baseX         = group.transform.position.x;
            _pingPongTimer = 0f;
        }

        public void Tick(float deltaTime)
        {
            _group.transform.Translate(Vector3.back * (_group.FallSpeed * deltaTime), Space.World);

            _pingPongTimer += deltaTime * _speed;
            float offsetX = Mathf.PingPong(_pingPongTimer, _range * 2f) - _range;

            var pos = _group.transform.position;
            _group.transform.position = new Vector3(_baseX + offsetX, pos.y, pos.z);
        }
    }
}
