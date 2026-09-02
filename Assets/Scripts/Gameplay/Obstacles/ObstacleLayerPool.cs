using UnityEngine;
using UnityEngine.Pool;

namespace Gameplay.Obstacles
{
    public class ObstacleLayerPool : MonoBehaviour
    {
        [SerializeField] private ObstacleLayer layerPrefab;
        [SerializeField] private int           defaultCapacity = 50;
        [SerializeField] private int           maxSize         = 200;

        private ObjectPool<ObstacleLayer> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<ObstacleLayer>(
                createFunc: CreateLayer,
                actionOnGet: OnGetLayer,
                actionOnRelease: OnReleaseLayer,
                actionOnDestroy: OnDestroyLayer,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public ObstacleLayer Get()                        => _pool.Get();
        public void          Release(ObstacleLayer layer) => _pool.Release(layer);


        private ObstacleLayer CreateLayer()
        {
            return Instantiate(layerPrefab, transform);
        }

        private static void OnGetLayer(ObstacleLayer layer)
        {
            layer.gameObject.SetActive(true);
        }

        private void OnReleaseLayer(ObstacleLayer layer)
        {
            layer.OnPoolRelease();
            layer.gameObject.SetActive(false);
            layer.transform.SetParent(transform);
        }

        private static void OnDestroyLayer(ObstacleLayer layer)
        {
            if(layer != null) Destroy(layer.gameObject);
        }
    }
}
