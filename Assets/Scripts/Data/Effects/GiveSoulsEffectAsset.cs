using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/GiveSouls")]
    public class GiveSoulsEffectAsset : EffectDataAsset
    {
        public int baseValue;
        public NumericValueType valueType = NumericValueType.Symbolic;
        public bool isImposed;

        public override EffectData GetEffectData() => new GiveSoulsEffectData
        {
            BaseValue = baseValue,
            ValueType = valueType,
            IsImposed = isImposed
        };
    }
}