using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using Data;
using Gameplay;
using Gameplay.Obstacles;
using Systems;
using UI;

namespace Core
{
    public class GameLifetimeScope : LifetimeScope
    {
        [Header("Gameplay Prefabs & Points")]
        [SerializeField] private PlayerController playerControllerPrefab;
        [SerializeField] private Transform playerSpawnPoint;

        [Header("Projectile Prefabs (Separate Weapons)")]
        [SerializeField] private GameObject standardProjectilePrefab;
        [SerializeField] private GameObject boomerangProjectilePrefab;
        [SerializeField] private GameObject rocketProjectilePrefab;

        [Header("Scene Components")]
        [SerializeField] private LevelSpawner levelSpawner;
        [SerializeField] private UIManager         uiManager;
        [SerializeField] private ObstacleLayerPool obstacleLayerPool;

        [Header("Perks Pool")]
        [SerializeField] private List<PerkDefinition> perkPool = new List<PerkDefinition>();

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<SaveManager>(Lifetime.Singleton);
            builder.Register<XPSystem>(Lifetime.Singleton);

            builder.Register<WeaponSystem>(Lifetime.Singleton).AsSelf();

            builder.Register<PerkSystem>(Lifetime.Singleton).WithParameter(perkPool);

            builder.Register<HealthSystem>(Lifetime.Singleton);

            if(levelSpawner != null) builder.RegisterComponent(levelSpawner);
            if(uiManager != null) builder.RegisterComponent(uiManager);
            if(obstacleLayerPool != null) builder.RegisterComponent(obstacleLayerPool);

            builder.RegisterEntryPoint<GameManager>(Lifetime.Singleton)
                   .WithParameter(playerControllerPrefab)
                   .WithParameter(playerSpawnPoint)
                   .WithParameter(levelSpawner)
                   .AsSelf();

            builder.RegisterBuildCallback(container =>
            {
                var weaponSystem = container.Resolve<WeaponSystem>();
                weaponSystem.Initialize(
                    standardProjectilePrefab,
                    boomerangProjectilePrefab,
                    rocketProjectilePrefab
                );
            });
        }
    }
}
