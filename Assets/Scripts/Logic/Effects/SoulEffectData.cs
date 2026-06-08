using System;

namespace SSR.Logic
{
    /// <summary>
    /// Steal X Souls: target opponent loses X souls,
    /// controller gains X souls. Affected by DEFENSE. Rule 800.
    /// </summary>
    [Serializable]
    public class StealEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.Steal;

        /// <summary>The target opponent's playerID. Declared at pile entry.</summary>
        public int TargetPlayerID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;
    }

    /// <summary>
    /// Banish X Souls: target opponent loses X souls.
    /// Controller gains nothing. Affected by DEFENSE. Rule 801.
    /// </summary>
    [Serializable]
    public class BanishEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.Banish;

        /// <summary>The target opponent's playerID. Declared at pile entry.</summary>
        public int TargetPlayerID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;
    }

    /// <summary>
    /// Give Souls effect. Two variants:
    ///
    /// Imposed ("Give X of your Souls"): controller loses X souls,
    /// target opponent gains them.
    ///
    /// Inflicted ("An opponent gives you X Souls"): target opponent
    /// loses X souls, controller gains them.
    ///
    /// Give Souls ignores DEFENSE and has no dedicated Modifiers. Rule 802.
    /// </summary>
    [Serializable]
    public class GiveSoulsEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.GiveSouls;

        // True = Imposed: controller loses souls ("Give X of your Souls").
        // False = Inflicted: target loses souls ("An opponent gives you X Souls").
        public bool IsImposed;

        /// <summary>The target player's playerID. Declared at pile entry.</summary>
        public int TargetPlayerID => TargetIDs.Count > 0 ? TargetIDs[0] : -1;
    }
}