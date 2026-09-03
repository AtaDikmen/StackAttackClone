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
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            if(_renderer == null) _renderer   = GetComponent<MeshRenderer>();
            if(healthText == null) healthText = GetComponentInChildren<TextMeshPro>(true);
        }

        public void Initialize(Obstacle parent, Color color)
        {
            EnsureComponents();
            _parentObstacle = parent;

            // Havuzdan tekrar çekildiğinde metni varsayılan olarak görünür yap
            SetTextVisible(true);

            if(_renderer != null)
                _renderer.material.color = color;
        }

        public void SetTextVisible(bool isVisible)
        {
            EnsureComponents();
            if(healthText != null)
                healthText.gameObject.SetActive(isVisible);
        }

        public void UpdateHealthDisplay(int currentHealth)
        {
            EnsureComponents();
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
            SetTextVisible(true);
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
