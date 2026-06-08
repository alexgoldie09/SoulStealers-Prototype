using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Abstract base for all effect data objects.
    /// Carries the properties common to every effect regardless of type.
    /// The EffectResolver reads these objects and modifies
    /// GameState accordingly. EffectData itself never touches GameState.
    /// </summary>
    [Serializable]
    public abstract class EffectData
    {
        // ── Type ──────────────────────────────────────────────────
        public abstract EffectType EffectType { get; }

        // ── Source ────────────────────────────────────────────────
        // The card this effect came from. Used by the resolver to
        // check card-dependent effects and ownership rules.
        public int SourceCardID;

        // The player who controls this effect.
        // "You" and "your" in effect text refer to this player. Rule 105.
        public int ControllerID;

        // ── Effect Chain ──────────────────────────────────────────
        // Index of this effect in the source card's printed effect list
        // (0-based). Used to resolve Linked dependency chains. Rule 701.
        public int PrintedEffectIndex;

        // ── Dependency ────────────────────────────────────────────
        // Rule 701: Unlinked, Linked, CardIndependent, CardDependent.
        public EffectDependency Dependency;

        // For Linked effects only: the PrintedEffectIndex of the
        // preceding effect this depends on. -1 = no dependency.
        // Rule 701 — Linked effects begin with "If you do,".
        public int LinkedToPrecedingEffectIndex = -1;

        // ── Status ────────────────────────────────────────────────
        // Active, Standby, or Silenced. Rule 106.
        public EffectStatus Status = EffectStatus.Active;

        // ── Optional ─────────────────────────────────────────────
        // True if the effect text contains "you may" or "up to X".
        // Optional effects may be declined by the controller. Rule 707.3.
        public bool IsOptional;

        // ── Targets ───────────────────────────────────────────────
        // Declared immediately when the effect enters the pile.
        // Rule 707 Step 1. Typed subclasses expose named accessors.
        public List<int> TargetIDs = new List<int>();

        // ── OR Choice ─────────────────────────────────────────────
        // Cards with multiple options separated by "or" create one
        // EffectData per option. All options share a ChoiceGroupIndex.
        // The controller declares their choice when the card is placed
        // on the pile (Step 2 of card resolution). Rule 700, 707.
        public bool IsChoiceEffect;
        public int ChoiceGroupIndex;
        public int ChoiceIndex;

        // ── Reveal Effect ─────────────────────────────────────────
        // True if this effect only fires when a face-down Spell is
        // turned face-up (Revealed). Ignored when the Spell is played
        // face-up normally. These are Replacement effects. Rule 304.8.
        public bool IsRevealEffect;
    }
}
