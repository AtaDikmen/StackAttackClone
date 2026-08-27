using UnityEngine;

namespace Gameplay
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed    = 20f;
        [SerializeField] private float lifeTime = 3f;

        private void Start()
        {
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            transform.Translate(Vector3.forward * (speed * Time.deltaTime));
        }
    }
}
