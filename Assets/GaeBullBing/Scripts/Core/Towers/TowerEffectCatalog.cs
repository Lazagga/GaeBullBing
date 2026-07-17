using System;
using System.Collections.Generic;

namespace GaeBullBing.Core.Towers
{
    // Upgrade JSON only selects an effect by ID. Actual behavior belongs in C# effect handlers.
    public static class TowerEffectCatalog
    {
        public const string Explode = "explode";
        public const string Burn = "burn";
        public const string Freeze = "freeze";
        public const string RangeAttack = "range_attack";
        public const string Knockback = "knockback";
        public const string Spread = "spread";
        public const string Shock = "shock";

        public static readonly IReadOnlyCollection<string> DeclaredEffectIds = Array.AsReadOnly(new[]
        {
            Explode, Burn, Freeze, RangeAttack, Knockback, Spread, Shock
        });

        // Add an ID here only after its runtime handler has been implemented and tested.
        public static readonly IReadOnlyCollection<string> ImplementedEffectIds = Array.AsReadOnly(Array.Empty<string>());

        public static bool IsDeclared(string id)
        {
            foreach (var declared in DeclaredEffectIds)
                if (string.Equals(declared, id, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
