using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Steal")]
    public class StealEffectAsset : EffectDataAsset
    {
        public int baseValue;
        public NumericValueType valueType = NumericValueType.Symbolic;

        public override EffectData GetEffectData() => new StealEffectData
        {
            BaseValue = baseValue,
            ValueType = valueType
        };
    }
}