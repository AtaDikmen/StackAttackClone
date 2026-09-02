using System;

namespace Data
{
    public enum StatType
    {
        ProjectileCount,
        FireRate,
        Damage,
        UnlockWeapon,
        WeaponType = UnlockWeapon,
        PierceCount
    }

    public enum ModifierMode
    {
        Flat,
        Percent
    }

    public enum WeaponType
    {
        Standard  = 0,
        Normal    = 0,
        Boomerang = 1,
        Rocket    = 2
    }

    [Serializable]
    public class StatModifier
    {
        public StatType     statType;
        public ModifierMode mode;
        public float        value;

        public WeaponType targetWeapon   = WeaponType.Standard;
        public WeaponType weaponOverride = WeaponType.Standard;
    }
}
