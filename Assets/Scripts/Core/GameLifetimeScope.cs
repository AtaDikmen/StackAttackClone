using VContainer;
using VContainer.Unity;
using Data;
using Gameplay;
using UI;
using UnityEngine;

namespace Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Gameplay")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("UI")]
        [SerializeField] private UIManager uiManager;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveManager>(Lifetime.Singleton);

            builder.Register<GameManager>(Lifetime.Singleton).WithParameter(playerControllerPrefab).WithParameter(playerSpawnPoint);

            builder.RegisterComponent(uiManager);
        }
    }
}
