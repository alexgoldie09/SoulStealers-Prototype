using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Pact")]
    public class PactEffectAsset : EffectDataAsset
    {
        public string requiredSpiritName;

        public override EffectData GetEffectData() => new PactEffectData
        {
            RequiredSpiritName = requiredSpiritName
        };
    }
}