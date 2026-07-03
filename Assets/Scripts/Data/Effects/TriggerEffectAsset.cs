using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(menuName = "SSR/Effects/Trigger")]
    public class TriggerEffectAsset : EffectDataAsset
    {
        public TriggerTiming timing;
        public bool onlyOnOwnerTurn = true;
        // Index into CardData.effects list that holds the payload.
        // -1 = stub trigger (fires but does nothing).
        public int payloadEffectIndex = -1;

        public override EffectData GetEffectData() => new TriggerEffectData
        {
            Timing = timing,
            OnlyOnOwnerTurn = onlyOnOwnerTurn,
            PayloadEffectIndex = payloadEffectIndex
        };
    }
}