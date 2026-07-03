using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Destroy")]
    public class DestroyEffectAsset : EffectDataAsset
    {
        public bool isOptional;
        public bool useTypeRestriction;
        public CardType typeRestriction;

        public override EffectData GetEffectData() => new DestroyEffectData
        {
            IsOptional = isOptional,
            TypeRestriction = useTypeRestriction ? typeRestriction : null
        };
    }
}