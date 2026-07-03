using System.Collections.Generic;
using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Ignore")]
    public class IgnoreEffectAsset : EffectDataAsset
    {
        public bool ignoresDefense;
        public bool ignoresSecretResponses;
        public List<CardType> ignoredCardTypes = new List<CardType>();

        public override EffectData GetEffectData()
        {
            var e = new IgnoreEffectData
            {
                IgnoresDefense = ignoresDefense,
                IgnoresSecretResponses = ignoresSecretResponses
            };
            foreach (var t in ignoredCardTypes)
                e.IgnoredCardTypes.Add(t);
            return e;
        }
    }
}