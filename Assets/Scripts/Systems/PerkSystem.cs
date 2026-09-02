using System.Collections.Generic;
using Data;
using Gameplay;
using UnityEngine;

namespace Systems
{
    public class PerkSystem
    {
        private readonly List<PerkDefinition> _masterPerkPool = new List<PerkDefinition>();
        private readonly List<PerkDefinition> _appliedPerks   = new List<PerkDefinition>();
        private readonly WeaponSystem         _weaponSystem;

        public IReadOnlyList<PerkDefinition> AppliedPerks => _appliedPerks;

        public PerkSystem(WeaponSystem weaponSystem, List<PerkDefinition> perkPool = null)
        {
            _weaponSystem = weaponSystem;
            if(perkPool != null)
                _masterPerkPool.AddRange(perkPool);
        }

        public void SetPerkPool(IEnumerable<PerkDefinition> perks)
        {
            _masterPerkPool.Clear();
            if(perks != null)
                _masterPerkPool.AddRange(perks);
        }

        public void ResetPerks()
        {
            _appliedPerks.Clear();
            _weaponSystem?.ResetWeaponStats();
        }

        public List<PerkDefinition> GetRandomPerks(int count = 3)
        {
            var available = GetFilteredAvailablePerks();
            var result    = new List<PerkDefinition>();

            while(result.Count < count && available.Count > 0)
            {
                int randomIndex = Random.Range(0, available.Count);
                result.Add(available[randomIndex]);
                available.RemoveAt(randomIndex);
            }

            return result;
        }

        /// <summary>
        /// Master Perk Pool içinden şartlara uymayanları eler:
        /// - Açılmış silahların "Unlock" kartlarını tekrar göstermez.
        /// - Henüz açılmamış alt silahların upgrade kartlarını göstermez.
        /// </summary>
        private List<PerkDefinition> GetFilteredAvailablePerks()
        {
            List<PerkDefinition> filtered = new List<PerkDefinition>();

            bool isBoomerangUnlocked = _weaponSystem != null && _weaponSystem.IsWeaponUnlocked(WeaponType.Boomerang);
            bool isRocketUnlocked    = _weaponSystem != null && _weaponSystem.IsWeaponUnlocked(WeaponType.Rocket);

            foreach(var perk in _masterPerkPool)
            {
                if(perk == null || perk.modifier == null) continue;

                var mod = perk.modifier;

                if(mod.statType == StatType.UnlockWeapon)
                {
                    var targetUnlock = mod.weaponOverride != WeaponType.Standard ? mod.weaponOverride : mod.targetWeapon;

                    if(targetUnlock == WeaponType.Boomerang && isBoomerangUnlocked) continue;
                    if(targetUnlock == WeaponType.Rocket && isRocketUnlocked) continue;

                    filtered.Add(perk);
                    continue;
                }

                if(mod.targetWeapon == WeaponType.Boomerang && !isBoomerangUnlocked) continue;
                if(mod.targetWeapon == WeaponType.Rocket && !isRocketUnlocked) continue;

                filtered.Add(perk);
            }

            return filtered;
        }

        public void ApplyPerk(PerkDefinition perk)
        {
            if(perk == null) return;

            _appliedPerks.Add(perk);
            if(perk.modifier != null && _weaponSystem != null)
                _weaponSystem.ApplyModifier(perk.modifier);
        }
    }
}
