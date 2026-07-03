using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    /// <summary>
    /// Abstract base for all effect ScriptableObject assets.
    /// Each concrete subclass wraps one EffectData subtype and exposes
    /// its fields as plain serialized values.
    /// CardData holds List<EffectDataAsset> which Unity resolves as normal
    /// object references, sidestepping the cross-assembly subtype picker problem.
    /// </summary>
    public abstract class EffectDataAsset : ScriptableObject
    {
        /// <summary>
        /// Builds and returns a configured EffectData instance.
        /// Called by CardFactory when creating a RuntimeCard.
        /// Note: TargetIDs are not set here for soul effects (Steal/Banish/GiveSouls)
        /// or field-targeting effects (Destroy, Silence) — they are populated at runtime
        /// once player IDs or target selections are known.
        /// </summary>
        public abstract EffectData GetEffectData();
    }
}