using System;
using System.Threading;
using UnityEngine;
using VContainer;
using Core;
using Systems;
using Gameplay.Obstacles;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float dragSensitivity = 2.5f;
        [SerializeField] private float xBoundary = 3.2f;
        [SerializeField] private float lerpSpeed = 25f;

        [Header("Rotation / Tilt")]
        [SerializeField] private float maxTiltAngle = 15f;
        [SerializeField] private float rotationSpeed = 12f;

        [Header("Shooting & Recoil")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Atış anında uygulanacak ölçek çarpanı (X, Z genişler, Y basıklaşır)")]
        [SerializeField] private Vector3 recoilScaleMultiplier = new Vector3(1.25f, 0.75f, 1.25f);
        [Tooltip("Geri tepme sonrası orijinal boyuta dönme hızı")]
        [SerializeField] private float recoilReturnSpeed = 20f;

        private GameManager             _gameManager;
        private WeaponSystem            _weaponSystem;
        private HealthSystem            _healthSystem;
        private CancellationTokenSource _cts;

        private float   _currentX;
        private Vector3 _lastMousePos;
        private Vector3 _initialScale;
        private float   _invulnerabilityTimer;

        [Inject]
        public void Construct(GameManager gameManager, WeaponSystem weaponSystem, HealthSystem healthSystem)
        {
            _gameManager  = gameManager;
            _weaponSystem = weaponSystem;
            _healthSystem = healthSystem;
        }

        private void Start()
        {
            _currentX     = transform.position.x;
            _initialScale = transform.localScale;
            _cts          = new CancellationTokenSource();

            ShootRoutine(_cts.Token).Forget();
        }

        private void Update()
        {
            if(_gameManager is not { CurrentState: GameState.Playing }) return;

            HandleMovement();
            HandleRecoilReturn();
        }

        private void HandleMovement()
        {
            if(Input.GetMouseButtonDown(0))
            {
                _lastMousePos = Input.mousePosition;
            }
            else if(Input.GetMouseButton(0))
            {
                float deltaX = (Input.mousePosition.x - _lastMousePos.x) * dragSensitivity * 0.01f;
                _lastMousePos = Input.mousePosition;

                _currentX += deltaX;
                _currentX =  Mathf.Clamp(_currentX, -xBoundary, xBoundary);
            }

            Vector3 targetPos = new Vector3(_currentX, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPos, lerpSpeed * Time.deltaTime);

            HandleRotation(targetPos.x);
        }

        private void HandleRotation(float targetX)
        {
            float moveDelta    = targetX - transform.position.x;
            float targetYAngle = Mathf.Clamp(moveDelta * 5f, -1f, 1f) * maxTiltAngle;

            Quaternion targetRotation = Quaternion.Euler(0f, targetYAngle, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        private void HandleRecoilReturn()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _initialScale, recoilReturnSpeed * Time.deltaTime);
        }

        private void ApplyRecoilImpulse()
        {
            transform.localScale = Vector3.Scale(_initialScale, recoilScaleMultiplier);
        }

        private void OnTriggerEnter(Collider other)
        {
            if(_gameManager == null || _gameManager.CurrentState != GameState.Playing) return;
            if(Time.time < _invulnerabilityTimer) return;

            if(other.TryGetComponent<ObstacleLayer>(out var layer))
            {
                _invulnerabilityTimer = Time.time + 0.5f;
                _healthSystem?.TakeDamage(1);
                layer.TakeHit(1000);
            }
        }

        private async UniTaskVoid ShootRoutine(CancellationToken token)
        {
            while(!token.IsCancellationRequested)
            {
                if(_gameManager != null && _gameManager.CurrentState == GameState.Playing && Input.GetMouseButton(0))
                {
                    var spawnPoint = firePoint != null ? firePoint : transform;
                    _weaponSystem?.Fire(spawnPoint);

                    // Atış gerçekleştiğinde geri tepme efektini tetikle
                    ApplyRecoilImpulse();

                    float interval = _weaponSystem?.FireInterval ?? 0.2f;
                    await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                }
                else
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
