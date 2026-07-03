using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Silence")]
    public class SilenceEffectAsset : EffectDataAsset
    {
        public EffectDurationTiming duration = EffectDurationTiming.UntilNextTurn;
        public bool isOptional;

        public override EffectData GetEffectData() => new SilenceEffectData
        {
            Duration = duration,
            IsOptional = isOptional
        };
    }
}