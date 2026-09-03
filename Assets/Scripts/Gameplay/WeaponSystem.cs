using System;
using UnityEngine;
using UnityEngine.Pool;
using Data;
using Audio;
using VContainer;
using Object = UnityEngine.Object;

namespace Gameplay
{
    [Serializable]
    public class WeaponStats
    {
        public WeaponType Type;
        public bool       IsUnlocked;
        public int        Damage;
        public float      FireRate;
        public float      Speed;
        public int        ProjectileCount;
        public int        PierceCount;
        public float      LastFireTime;

        public float FireInterval => Mathf.Max(0.05f, FireRate);

        public void SetDefaults(WeaponType type, bool unlocked, int damage, float fireRate, float speed, int count)
        {
            Type            = type;
            IsUnlocked      = unlocked;
            Damage          = damage;
            FireRate        = fireRate;
            Speed           = speed;
            ProjectileCount = count;
            PierceCount     = 0;
            LastFireTime    = -100f;
        }
    }

    public class WeaponSystem
    {
        private GameObject _standardPrefab;
        private GameObject _boomerangPrefab;
        private GameObject _rocketPrefab;

        private ObjectPool<Projectile> _standardPool;
        private ObjectPool<Projectile> _boomerangPool;
        private ObjectPool<Projectile> _rocketPool;

        private readonly AudioManager _audioManager;

        public WeaponStats StandardStats  { get; } = new WeaponStats();
        public WeaponStats BoomerangStats { get; } = new WeaponStats();
        public WeaponStats RocketStats    { get; } = new WeaponStats();

        public int        BaseDamage        => StandardStats.Damage;
        public float      BaseFireRate      => StandardStats.FireRate;
        public float      BaseSpeed         => StandardStats.Speed;
        public int        ProjectileCount   => StandardStats.ProjectileCount;
        public WeaponType CurrentWeaponType => WeaponType.Standard;
        public float      FireInterval      => StandardStats.FireInterval;

        [Inject]
        public WeaponSystem(AudioManager audioManager = null)
        {
            _audioManager = audioManager;
            ResetWeaponStats();
        }

        public void Initialize(GameObject standardPrefab, GameObject boomerangPrefab, GameObject rocketPrefab)
        {
            _standardPrefab  = standardPrefab;
            _boomerangPrefab = boomerangPrefab;
            _rocketPrefab    = rocketPrefab;

            _standardPool  = CreatePool(_standardPrefab);
            _boomerangPool = CreatePool(_boomerangPrefab != null ? _boomerangPrefab : _standardPrefab);
            _rocketPool    = CreatePool(_rocketPrefab != null ? _rocketPrefab : _standardPrefab);
        }

        private ObjectPool<Projectile> CreatePool(GameObject prefab)
        {
            return new ObjectPool<Projectile>(
                createFunc: () => CreateProjectileInstance(prefab),
                actionOnGet: null,
                actionOnRelease: p =>
                {
                    p.OnPoolRelease();
                    p.gameObject.SetActive(false);
                },
                actionOnDestroy: p =>
                {
                    if(p != null) Object.Destroy(p.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: 100,
                maxSize: 500
            );
        }

        public void ResetWeaponStats()
        {
            StandardStats.SetDefaults(WeaponType.Standard, unlocked: true, damage: 5, fireRate: 0.55f, speed: 18f, count: 1);
            BoomerangStats.SetDefaults(WeaponType.Boomerang, unlocked: false, damage: 9, fireRate: 2.00f, speed: 14f, count: 1);
            RocketStats.SetDefaults(WeaponType.Rocket, unlocked: false, damage: 15, fireRate: 2.80f, speed: 13f, count: 1);
        }

        public bool IsWeaponUnlocked(WeaponType weapon)
        {
            return weapon switch
                   {
                       WeaponType.Standard  => true,
                       WeaponType.Boomerang => BoomerangStats.IsUnlocked,
                       WeaponType.Rocket    => RocketStats.IsUnlocked,
                       _                    => false
                   };
        }

        public void UnlockWeapon(WeaponType weapon)
        {
            switch(weapon)
            {
                case WeaponType.Boomerang:
                    BoomerangStats.IsUnlocked   = true;
                    BoomerangStats.LastFireTime = Time.time;
                    break;
                case WeaponType.Rocket:
                    RocketStats.IsUnlocked   = true;
                    RocketStats.LastFireTime = Time.time;
                    break;
            }
        }

        public WeaponStats GetStats(WeaponType weapon)
        {
            return weapon switch
                   {
                       WeaponType.Boomerang => BoomerangStats,
                       WeaponType.Rocket    => RocketStats,
                       _                    => StandardStats
                   };
        }

        public void ApplyModifier(StatModifier modifier)
        {
            if(modifier == null) return;

            if(modifier.statType == StatType.UnlockWeapon)
            {
                var weaponToUnlock = modifier.weaponOverride != WeaponType.Standard
                    ? modifier.weaponOverride
                    : modifier.targetWeapon;

                UnlockWeapon(weaponToUnlock);
                return;
            }

            WeaponStats target = GetStats(modifier.targetWeapon);
            if(target == null) return;

            switch(modifier.statType)
            {
                case StatType.ProjectileCount:
                    int maxCount = target.Type == WeaponType.Standard ? 5 : 3;
                    if(modifier.mode == ModifierMode.Flat)
                        target.ProjectileCount += Mathf.RoundToInt(modifier.value);
                    else
                        target.ProjectileCount = Mathf.RoundToInt(target.ProjectileCount * (1f + modifier.value));
                    target.ProjectileCount = Mathf.Clamp(target.ProjectileCount, 1, maxCount);
                    break;

                case StatType.FireRate:
                    if(modifier.mode == ModifierMode.Percent)
                        target.FireRate *= (1f - modifier.value);
                    else
                        target.FireRate -= modifier.value;
                    target.FireRate = Mathf.Max(0.08f, target.FireRate);
                    break;

                case StatType.Damage:
                    if(modifier.mode == ModifierMode.Flat)
                        target.Damage += Mathf.RoundToInt(modifier.value);
                    else
                        target.Damage = Mathf.RoundToInt(target.Damage * (1f + modifier.value));
                    break;

                case StatType.PierceCount:
                    if(modifier.mode == ModifierMode.Flat)
                        target.PierceCount += Mathf.RoundToInt(modifier.value);
                    else
                        target.PierceCount = Mathf.RoundToInt(target.PierceCount * (1f + modifier.value));
                    target.PierceCount = Mathf.Max(0, target.PierceCount);
                    break;
            }
        }

        public void Fire(Transform firePoint)
        {
            if(firePoint == null) return;

            FirePrimary(firePoint);
            TickSubWeapons(firePoint);
        }

        private void FirePrimary(Transform firePoint)
        {
            float spacing = 0.35f;
            int   count   = StandardStats.ProjectileCount;
            float startX  = -((count - 1) * spacing) / 2f;

            Vector3 forwardOffset = firePoint.forward * 0.6f;

            for(int i = 0; i < count; i++)
            {
                Vector3    spawnPos   = firePoint.position + forwardOffset + new Vector3(startX + i * spacing, 0, 0);
                Projectile projectile = GetProjectile(spawnPos, firePoint.rotation, WeaponType.Standard);

                projectile.Initialize(
                    damage: StandardStats.Damage,
                    speed: StandardStats.Speed,
                    type: WeaponType.Standard,
                    pool: _standardPool,
                    audioManager: _audioManager,
                    pierceCount: StandardStats.PierceCount
                );
            }

            _audioManager?.PlayShoot();
        }

        public void TickSubWeapons(Transform firePoint)
        {
            if(firePoint == null) return;

            float now = Time.time;

            if(BoomerangStats.IsUnlocked && now >= BoomerangStats.LastFireTime + BoomerangStats.FireInterval)
            {
                BoomerangStats.LastFireTime = now;
                FireBoomerangs(firePoint);
            }

            if(RocketStats.IsUnlocked && now >= RocketStats.LastFireTime + RocketStats.FireInterval)
            {
                RocketStats.LastFireTime = now;
                FireRockets(firePoint);
            }
        }

        private void FireBoomerangs(Transform firePoint)
        {
            int   count   = BoomerangStats.ProjectileCount;
            float spacing = 0.45f;
            float startX  = -((count - 1) * spacing) / 2f;

            for(int i = 0; i < count; i++)
            {
                float curveDirection = (i % 2 == 0) ? 1f : -1f;

                var spawnPos = firePoint.position + new Vector3(startX + i * spacing, 0, 0.1f);
                var proj     = GetProjectile(spawnPos, firePoint.rotation, WeaponType.Boomerang);
                proj.Initialize(
                    damage: BoomerangStats.Damage,
                    speed: BoomerangStats.Speed,
                    type: WeaponType.Boomerang,
                    pool: _boomerangPool,
                    ownerTransform: firePoint,
                    curveDirection: curveDirection,
                    audioManager: _audioManager
                );
            }

            _audioManager?.PlayBoomerangFire();
        }

        private void FireRockets(Transform firePoint)
        {
            int count = RocketStats.ProjectileCount;

            for(int i = 0; i < count; i++)
            {
                float curveDir = (count == 1) ? 1f : (i % 2 == 0 ? 1f : -1f);
                float xOffset  = (i - (count - 1) / 2f) * 0.4f;

                var spawnPos = firePoint.position + new Vector3(xOffset, 0, 0.15f);
                var proj     = GetProjectile(spawnPos, firePoint.rotation, WeaponType.Rocket);
                proj.Initialize(
                    damage: RocketStats.Damage,
                    speed: RocketStats.Speed,
                    type: WeaponType.Rocket,
                    pool: _rocketPool,
                    curveDirection: curveDir,
                    explosionRadius: 2.5f,
                    audioManager: _audioManager
                );
            }

            _audioManager?.PlayRocketFire();
        }

        private Projectile GetProjectile(Vector3 position, Quaternion rotation, WeaponType type)
        {
            var targetPool = type switch
                             {
                                 WeaponType.Boomerang => _boomerangPool,
                                 WeaponType.Rocket    => _rocketPool,
                                 _                    => _standardPool
                             };

            Projectile proj = null;

            if(targetPool != null)
                proj = targetPool.Get();
            else
            {
                GameObject targetPrefab = type switch
                                          {
                                              WeaponType.Boomerang => _boomerangPrefab,
                                              WeaponType.Rocket    => _rocketPrefab,
                                              _                    => _standardPrefab
                                          };

                if(targetPrefab != null)
                {
                    var obj = Object.Instantiate(targetPrefab);
                    proj = obj.GetComponent<Projectile>() ?? obj.AddComponent<Projectile>();
                }
                else
                    proj = CreateFallbackProjectile();
            }

            proj.transform.SetPositionAndRotation(position, rotation);
            proj.gameObject.SetActive(true);

            return proj;
        }

        private Projectile CreateProjectileInstance(GameObject prefab)
        {
            if(prefab != null)
            {
                var obj = Object.Instantiate(prefab);
                obj.SetActive(false);
                return obj.GetComponent<Projectile>() ?? obj.AddComponent<Projectile>();
            }

            return CreateFallbackProjectile();
        }

        private Projectile CreateFallbackProjectile()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = Vector3.one * 0.3f;
            var col                       = sphere.GetComponent<SphereCollider>();
            if(col != null) col.isTrigger = true;

            var rb = sphere.GetComponent<Rigidbody>() ?? sphere.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            sphere.SetActive(false);

            return sphere.AddComponent<Projectile>();
        }
    }
}
