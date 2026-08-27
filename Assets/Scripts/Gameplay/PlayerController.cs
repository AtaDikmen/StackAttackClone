using UnityEngine;
using VContainer;
using Core;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace Gameplay
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float dragSensitivity = 2.5f; 
        [SerializeField] private float xBoundary = 4.5f;

        [Header("Shooting")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.2f;

        private GameManager _gameManager;
        private CancellationTokenSource _cts;
        
        private float _currentX;
        private Vector3 _lastMousePos;

        [Inject]
        public void Construct(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();
            _currentX = transform.position.x;

            ShootRoutine(_cts.Token).Forget();
        }

        private void Update()
        {
            if (_gameManager.CurrentState != GameState.Playing) return;
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0))
            {
                float deltaX = (Input.mousePosition.x - _lastMousePos.x) * dragSensitivity * 0.01f;
                _lastMousePos = Input.mousePosition;

                _currentX += deltaX;
                _currentX = Mathf.Clamp(_currentX, -xBoundary, xBoundary);
            }

            Vector3 targetPosition = new Vector3(_currentX, transform.position.y, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, 25f * Time.deltaTime);
        }

        private async UniTaskVoid ShootRoutine(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_gameManager.CurrentState == GameState.Playing && Input.GetMouseButton(0))
                {
                    Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                    await UniTask.Delay(System.TimeSpan.FromSeconds(fireRate), cancellationToken: token);
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