using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Merge")]
    public class MergeEffectAsset : EffectDataAsset
    {
        public CardType sourceCardType = CardType.Ritual;

        public override EffectData GetEffectData() => new MergeEffectData
        {
            SourceCardType = sourceCardType
        };
    }
}