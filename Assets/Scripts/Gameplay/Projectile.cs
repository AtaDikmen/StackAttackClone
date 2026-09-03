using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Data;
using Gameplay.Obstacles;
using Audio;

namespace Gameplay
{
    public class Projectile : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] private float defaultSpeed = 18f;
        [SerializeField] private int defaultDamage = 10;

        private float      _speed       = 18f;
        private int        _damage      = 10;
        private WeaponType _type        = WeaponType.Standard;
        private int        _pierceCount = 0;

        private       float _aliveTime;
        private const float MaxLifeTime = 6f;
        private       bool  _isRecycled;

        private Vector3   _startPosition;
        private Transform _ownerTransform;
        private float     _maxDistance = 15f;
        private float     _arcWidth    = 3.5f;
        private float     _forwardTravel;
        private bool      _isReturning;

        private float _curveDirection  = 1f;
        private float _explosionRadius = 2.5f;

        private IObjectPool<Projectile> _pool;
        private AudioManager            _audioManager;

        private readonly static Collider[]   OverlapBuffer       = new Collider[32];
        private readonly static HashSet<int> ExplodedObstacleIds = new HashSet<int>();
        private readonly        HashSet<int> _hitObstacleIds     = new HashSet<int>();

        public int        Damage => _damage;
        public WeaponType Type   => _type;

        public void Initialize(
            int                     damage,
            float                   speed,
            WeaponType              type,
            IObjectPool<Projectile> pool,
            Transform               ownerTransform  = null,
            float                   curveDirection  = 0f,
            float                   explosionRadius = 2.5f,
            int                     pierceCount     = 0,
            AudioManager            audioManager    = null)
        {
            _damage          = damage > 0 ? damage : defaultDamage;
            _speed           = speed > 0 ? speed : defaultSpeed;
            _type            = type;
            _pool            = pool;
            _ownerTransform  = ownerTransform;
            _curveDirection  = curveDirection != 0f ? curveDirection : 1f;
            _explosionRadius = explosionRadius > 0 ? explosionRadius : 2.5f;
            _pierceCount     = pierceCount;
            _audioManager    = audioManager;

            _aliveTime     = 0f;
            _forwardTravel = 0f;
            _startPosition = transform.position;
            _isReturning   = false;
            _isRecycled    = false;
            _hitObstacleIds.Clear();

            if(_type == WeaponType.Rocket)
            {
                float launchAngle = _curveDirection * 38f;
                transform.rotation = Quaternion.Euler(0f, launchAngle, 0f);
            }
        }

        private void Update()
        {
            if(_isRecycled) return;

            _aliveTime += Time.deltaTime;

            if(_aliveTime >= MaxLifeTime)
            {
                Recycle();
                return;
            }

            switch(_type)
            {
                case WeaponType.Standard:
                    transform.Translate(Vector3.forward * (_speed * Time.deltaTime), Space.World);
                    break;

                case WeaponType.Boomerang:
                    UpdateBoomerang();
                    break;

                case WeaponType.Rocket:
                    UpdateRocket();
                    break;
            }
        }

        private void UpdateBoomerang()
        {
            transform.Rotate(Vector3.up, 1080f * Time.deltaTime, Space.Self);

            Vector3 currentPos      = transform.position;
            Vector3 targetReturnPos = _ownerTransform != null ? _ownerTransform.position : _startPosition;

            if(!_isReturning)
            {
                _forwardTravel += _speed * Time.deltaTime;
                float progress = Mathf.Clamp01(_forwardTravel / _maxDistance);

                float sideDisplacement = Mathf.Sin(progress * Mathf.PI) * _arcWidth * _curveDirection;

                Vector3 targetPos = _startPosition + new Vector3(sideDisplacement, 0f, _forwardTravel);
                transform.position = targetPos;

                if(_forwardTravel >= _maxDistance)
                    _isReturning = true;
            }
            else
            {
                Vector3 returnDir = (targetReturnPos - currentPos);
                returnDir.y = 0f;

                if(returnDir.sqrMagnitude < 0.36f || currentPos.z <= targetReturnPos.z + 0.2f)
                {
                    Recycle();
                    return;
                }

                transform.position += returnDir.normalized * (_speed * 1.25f * Time.deltaTime);
            }
        }

        private void UpdateRocket()
        {
            float currentAngle                   = transform.eulerAngles.y;
            if(currentAngle > 180f) currentAngle -= 360f;

            float newAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * 4.5f);
            transform.rotation = Quaternion.Euler(0f, newAngle, 0f);

            transform.Translate(Vector3.forward * (_speed * 1.35f * Time.deltaTime), Space.Self);
        }

        public bool TryMarkHit(Obstacle obstacle)
        {
            if(_isRecycled || obstacle == null) return false;

            if(_type == WeaponType.Rocket)
            {
                ExplodeAOE();
                Recycle();
                return false;
            }

            int obstacleId = obstacle.GetInstanceID();
            if(!_hitObstacleIds.Add(obstacleId)) return false;

            if(_type == WeaponType.Standard)
            {
                if(_pierceCount > 0)
                {
                    _pierceCount--;
                    return true;
                }

                Recycle();
                return true;
            }

            if(_type == WeaponType.Boomerang)
            {
                _isReturning = true;
                return true;
            }

            return true;
        }

        private void ExplodeAOE()
        {
            _audioManager?.PlayRocketImpact();

            ExplodedObstacleIds.Clear();
            int hitCount = Physics.OverlapSphereNonAlloc(transform.position, _explosionRadius, OverlapBuffer);

            for(int i = 0; i < hitCount; i++)
            {
                var col = OverlapBuffer[i];
                if(col == null) continue;

                Obstacle obstacle = null;
                if(col.TryGetComponent<ObstacleLayer>(out var layer))
                    obstacle = layer.ParentObstacle;
                else if(col.TryGetComponent<Obstacle>(out var obs))
                    obstacle = obs;

                if(obstacle != null)
                {
                    int obsId = obstacle.GetInstanceID();
                    if(ExplodedObstacleIds.Add(obsId))
                        obstacle.TakeDamage(_damage);
                }
            }

            ExplodedObstacleIds.Clear();
        }

        public void Recycle()
        {
            if(_isRecycled) return;
            _isRecycled = true;

            if(_pool != null)
                _pool.Release(this);
            else
                Destroy(gameObject);
        }

        public void OnPoolRelease()
        {
            var trail = GetComponentInChildren<TrailRenderer>();
            if(trail != null)
                trail.Clear();

            _hitObstacleIds.Clear();
            _isReturning       = false;
            _pierceCount       = 0;
            _ownerTransform    = null;
            _audioManager      = null;
            transform.rotation = Quaternion.identity;
        }
    }
}
