using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// Modifier: increases or decreases the numeric value of other effects.
    /// Modifiers only affect Symbolic numbers and other Modifiers.
    /// They never affect Word-form values. Rule 803.
    ///
    /// Examples from the Spirit cards:
    /// Heinrich: "Your banishing effects get +2." (positive, Banish only, controller)
    /// Uzilda: "All banishing and stealing effects get -2." (negative, global)
    /// Molok: "Your stealing spells get +1." (positive, Steal, Sorcery type only)
    ///
    /// Modified values that would go negative are treated as 0. Rule 104.3.
    /// Modifiers that modify other Modifiers may result in negative values. Rule 803.
    /// </summary>
    [Serializable]
    public class ModifierEffectData : NumericEffectData
    {
        public override EffectType EffectType => EffectType.Modifier;

        // Which effect types this Modifier applies to.
        // Can target multiple (e.g. both Steal and Banish for Uzilda).
        public List<EffectType> ModifiedEffectTypes = new List<EffectType>();

        // True = positive modifier (+X). False = negative modifier (-X).
        // The sign here determines direction; BaseValue holds the magnitude.
        public bool IsPositive = true;

        // True  = only applies to the controller's effects.
        // False = applies to all players' matching effects (global).
        // e.g. Uzilda's -2 applies globally. Rule 803.
        public bool ControllerOnly = true;

        // Optional: only applies to effects from cards of this type.
        // e.g. Molok: "stealing spells get +1" — restricted to Sorcery type.
        // Null = applies regardless of source card type.
        public CardType? SourceCardTypeRestriction;
    }
}