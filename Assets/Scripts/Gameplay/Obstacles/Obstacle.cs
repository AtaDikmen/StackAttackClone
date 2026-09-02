using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Systems;

namespace Gameplay.Obstacles
{
    public class Obstacle : MonoBehaviour
    {
        [Header("Layer Config")]
        [SerializeField] private GameObject layerPrefab;
        [SerializeField] private float layerHeightOffset = 0.4f;
        [SerializeField] private int   hpPerLayer        = 10;

        private int  _maxHealth;
        private int  _currentHealth;
        private int  _xpRewardPerLayer = 10;
        private bool _isBossUnit;

        private readonly List<ObstacleLayer> _visualLayers = new List<ObstacleLayer>();
        private          ObstacleLayerPool   _layerPool;
        private          XPSystem            _xpSystem;

        public event Action           OnDefeated;
        public event Action<int, int> OnHealthChanged;

        public bool IsBossUnit    => _isBossUnit;
        public int  CurrentHealth => _currentHealth;
        public int  MaxHealth     => _maxHealth;

        [Inject]
        public void Construct(ObstacleLayerPool layerPool, XPSystem xpSystem)
        {
            _layerPool = layerPool;
            _xpSystem  = xpSystem;
        }

        public void Initialize(int totalHealth, Color color, int xpRewardPerLayer = 10, bool isBossUnit = false)
        {
            _maxHealth        = totalHealth;
            _currentHealth    = totalHealth;
            _xpRewardPerLayer = xpRewardPerLayer;
            _isBossUnit       = isBossUnit;

            int layerCount = Mathf.CeilToInt((float)totalHealth / hpPerLayer);

            float halfLayerHeight = layerHeightOffset * 0.5f;

            for(int i = 0; i < layerCount; i++)
            {
                ObstacleLayer layer = GetOrCreateLayer();
                layer.transform.SetParent(transform, false);

                float yPos = halfLayerHeight + (i * layerHeightOffset);
                layer.transform.localPosition = new Vector3(0, yPos, 0);
                layer.transform.localRotation = Quaternion.identity;

                layer.Initialize(this, color);
                if(_isBossUnit)
                {
                    layer.SetTextVisible(false);
                }
                _visualLayers.Add(layer);
            }

            UpdateAllLayerTexts();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        private ObstacleLayer GetOrCreateLayer()
        {
            if(_layerPool != null)
                return _layerPool.Get();

            if(layerPrefab != null)
            {
                var layerObj = Instantiate(layerPrefab);
                return layerObj.GetComponent<ObstacleLayer>() ?? layerObj.AddComponent<ObstacleLayer>();
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = new Vector3(1.2f, layerHeightOffset * 0.9f, 1.2f);
            var col                       = cube.GetComponent<BoxCollider>();
            if(col != null) col.isTrigger = true;

            var rb = cube.GetComponent<Rigidbody>() ?? cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            return cube.AddComponent<ObstacleLayer>();
        }

        public void TakeDamage(int damage)
        {
            if(_currentHealth <= 0) return;

            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            UpdateAllLayerTexts();
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            int expectedLayerCount = Mathf.CeilToInt((float)_currentHealth / hpPerLayer);

            while(_visualLayers.Count > expectedLayerCount && _visualLayers.Count > 0)
            {
                int           topIndex = _visualLayers.Count - 1;
                ObstacleLayer topLayer = _visualLayers[topIndex];
                _visualLayers.RemoveAt(topIndex);

                RecycleLayer(topLayer);
                _xpSystem?.AddXP(_xpRewardPerLayer);
            }

            if(_currentHealth <= 0)
                Die();
        }

        private void UpdateAllLayerTexts()
        {
            if(_isBossUnit)
            {
                foreach(var layer in _visualLayers)
                {
                    if(layer != null)
                        layer.SetTextVisible(false);
                }
                return;
            }

            int displayHp = Mathf.Max(0, _currentHealth);
            foreach(var layer in _visualLayers)
            {
                if(layer != null)
                    layer.UpdateHealthDisplay(displayHp);
            }
        }

        private void RecycleLayer(ObstacleLayer layer)
        {
            if(layer == null) return;

            if(_layerPool != null)
                _layerPool.Release(layer);
            else
                Destroy(layer.gameObject);
        }

        private void Die()
        {
            for(int i = _visualLayers.Count - 1; i >= 0; i--)
                RecycleLayer(_visualLayers[i]);
            _visualLayers.Clear();

            OnDefeated += null;
            OnDefeated?.Invoke();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            OnDefeated      = null;
            OnHealthChanged = null;
        }
    }
}
