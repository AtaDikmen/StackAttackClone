using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewPerk", menuName = "SO/Perk Definition")]
    public class PerkDefinition : ScriptableObject
    {
        public string perkName;

        [TextArea(2, 4)]
        public string description;

        public Sprite icon;

        public StatModifier modifier;
    }
}
