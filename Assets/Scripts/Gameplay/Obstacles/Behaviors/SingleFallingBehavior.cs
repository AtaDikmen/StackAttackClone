using UnityEngine;

namespace Gameplay.Obstacles.Behaviors
{
    public class SingleFallingBehavior : IMovementBehavior
    {
        private ObstacleGroup _group;

        public void Initialize(ObstacleGroup group)
        {
            _group = group;
        }

        public void Tick(float deltaTime)
        {
            if(_group == null) return;

            _group.transform.Translate(Vector3.back * (_group.FallSpeed * deltaTime), Space.World);

            if(_group.transform.position.z < -6f)
                Object.Destroy(_group.gameObject);
        }
    }
}
