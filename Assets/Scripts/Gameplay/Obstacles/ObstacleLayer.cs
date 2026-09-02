using TMPro;
using UnityEngine;

namespace Gameplay.Obstacles
{
    public class ObstacleLayer : MonoBehaviour
    {
        [SerializeField] private TextMeshPro healthText;

        private Obstacle     _parentObstacle;
        private MeshRenderer _renderer;

        public Obstacle ParentObstacle => _parentObstacle;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            if(healthText == null) healthText = GetComponentInChildren<TextMeshPro>();
        }

        public void Initialize(Obstacle parent, Color color)
        {
            _parentObstacle = parent;
            if(_renderer == null) _renderer = GetComponent<MeshRenderer>();
            if(_renderer != null)
                _renderer.material.color = color;
        }

        public void SetTextVisible(bool isVisible)
        {
            if(healthText != null)
                healthText.gameObject.SetActive(isVisible);
        }

        public void UpdateHealthDisplay(int currentHealth)
        {
            if(healthText != null && healthText.gameObject.activeSelf)
                healthText.text = currentHealth > 0 ? currentHealth.ToString() : "";
        }

        public void TakeHit(int damage)
        {
            if(_parentObstacle != null)
                _parentObstacle.TakeDamage(damage);
        }

        public void OnPoolRelease()
        {
            _parentObstacle = null;
        }

        private void OnTriggerEnter(Collider other)
        {
            if(other.TryGetComponent<Projectile>(out var projectile))
            {
                if(ParentObstacle != null && projectile.TryMarkHit(ParentObstacle))
                    TakeHit(projectile.Damage);
            }
        }
    }
}
