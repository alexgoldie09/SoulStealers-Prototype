using System.Collections.Generic;
using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Modifier")]
    public class ModifierEffectAsset : EffectDataAsset
    {
        public int baseValue;
        public bool isPositive = true;
        public bool controllerOnly = true;
        public List<EffectType> modifiedEffectTypes = new List<EffectType>();
        public bool useSourceCardTypeRestriction;
        public CardType sourceCardTypeRestriction;

        public override EffectData GetEffectData()
        {
            var e = new ModifierEffectData
            {
                BaseValue = baseValue,
                ValueType = NumericValueType.Symbolic,
                IsPositive = isPositive,
                ControllerOnly = controllerOnly,
                SourceCardTypeRestriction = useSourceCardTypeRestriction
                    ? sourceCardTypeRestriction
                    : null
            };
            foreach (var t in modifiedEffectTypes)
                e.ModifiedEffectTypes.Add(t);
            return e;
        }
    }
}