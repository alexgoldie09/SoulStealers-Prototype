using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Defense")]
    public class DefenseEffectAsset : EffectDataAsset
    {
        public int baseValue;

        public override EffectData GetEffectData() => new DefenseEffectData
        {
            BaseValue = baseValue,
            ValueType = NumericValueType.Symbolic
        };
    }
}