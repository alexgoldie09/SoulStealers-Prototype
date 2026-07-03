using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Indestructible")]
    public class IndestructibleEffectAsset : EffectDataAsset
    {
        public EffectDurationTiming duration = EffectDurationTiming.Permanent;

        public override EffectData GetEffectData() => new IndestructibleEffectData
        {
            Duration = duration
        };
    }
}