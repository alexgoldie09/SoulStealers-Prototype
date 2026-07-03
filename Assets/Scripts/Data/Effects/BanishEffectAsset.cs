using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Banish")]
    public class BanishEffectAsset : EffectDataAsset
    {
        public int baseValue;
        public NumericValueType valueType = NumericValueType.Symbolic;

        public override EffectData GetEffectData() => new BanishEffectData
        {
            BaseValue = baseValue,
            ValueType = valueType
        };
    }
}