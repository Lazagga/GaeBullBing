using UnityEngine;

namespace GaeBullBing.Core.Data
{
    [CreateAssetMenu(menuName = "GaeBullBing/Data/Monster", fileName = "MonsterDefinition")]
    public sealed class MonsterDefinition : ScriptableObject
    {
        [SerializeField] private string id = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField, Min(1)] private int maxHealth = 1;
        [SerializeField, Min(1)] private int moveDistance = 1;
        [SerializeField, Min(0)] private int reward;

        public string Id => id;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public int MoveDistance => moveDistance;
        public int Reward => reward;
    }
}
