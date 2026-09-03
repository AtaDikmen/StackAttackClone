using UnityEngine;
using Gameplay.Obstacles;

namespace Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class KillZone : MonoBehaviour
    {
        private void Awake()
        {
            var boxCol = GetComponent<BoxCollider>();
            if(boxCol != null)
                boxCol.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Obstacle obstacle = null;

            if(other.TryGetComponent<ObstacleLayer>(out var layer))
                obstacle = layer.ParentObstacle;
            else if(other.TryGetComponent<Obstacle>(out var obs))
                obstacle = obs;

            if(obstacle == null) return;

            obstacle.TakeDamage(99999, false);
        }

        private void OnDrawGizmos()
        {
            var col = GetComponent<BoxCollider>();
            if(col == null) return;

            Gizmos.color  = new Color(1f, 0.1f, 0.1f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
        }
    }
}
