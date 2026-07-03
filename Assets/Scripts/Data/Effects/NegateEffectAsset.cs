using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Negate")]
    public class NegateEffectAsset : EffectDataAsset
    {
        public bool targetsCard = true;
        public bool useTypeRestriction;
        public CardType typeRestriction;

        public override EffectData GetEffectData() => new NegateEffectData
        {
            TargetsCard = targetsCard,
            TypeRestriction = useTypeRestriction ? typeRestriction : null
        };
    }
}