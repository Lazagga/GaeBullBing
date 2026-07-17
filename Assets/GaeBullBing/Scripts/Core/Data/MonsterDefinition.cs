using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Monster", fileName = "MonsterDefinition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private MonsterTier tier;
        [SerializeField, Min(1)] private int maxHp = 1;
        [SerializeField, Min(1)] private int moveDistance = 1;

        public string Id => id;
        public string DisplayName => displayName;
        public MonsterTier Tier => tier;
        public int MaxHp => maxHp;
        public int MoveDistance => moveDistance;
    }
}
