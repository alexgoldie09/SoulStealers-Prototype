using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Conspiracy")]
    public class ConspiracyEffectAsset : EffectDataAsset
    {
        public override EffectData GetEffectData() => new ConspiracyEffectData();
    }
}