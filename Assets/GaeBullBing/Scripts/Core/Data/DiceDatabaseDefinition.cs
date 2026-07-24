using System;
using UnityEngine;

namespace GaeBullBing.Core.Data
{
    public sealed class DiceDatabaseDefinition : ScriptableObject
    {
        [SerializeField] private DiceDefinition[] dice = Array.Empty<DiceDefinition>();
        [SerializeField] private float[] gradeWeights = Array.Empty<float>();

        public DiceDefinition[] Dice => dice;
        public float GetGradeWeight(DiceGrade grade) =>
            gradeWeights != null && (int)grade >= 0 && (int)grade < gradeWeights.Length
                ? gradeWeights[(int)grade]
                : 0f;
    }
}
