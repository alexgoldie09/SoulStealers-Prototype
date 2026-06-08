using System;

namespace SSR.Logic
{
    /// <summary>
    /// Intermediate base for effects that carry a numeric value:
    /// Steal, Banish, GiveSouls, Defense, Modifier, Counter.
    ///
    /// Numeric values come in three forms (rule 104):
    /// Symbolic (1, 2, 3…) — can be modified by Modifier effects.
    /// WordForm (one, two…) — invariable, cannot be modified.
    /// X — declared by the controller; stored in XValue.
    ///
    /// Modifiers are applied by the EffectResolver.
    /// </summary>
    [Serializable]
    public abstract class NumericEffectData : EffectData
    {
        // The number printed on the card.
        // For X effects, BaseValue stays 0 until X is declared.
        public int BaseValue;

        // How the number is represented on the card. Rule 104.
        public NumericValueType ValueType = NumericValueType.Symbolic;

        // For X effects: the value declared by the controller.
        // Only meaningful when ValueType == NumericValueType.X.
        public int XValue;

        /// <summary>
        /// The raw value before Modifier effects are applied.
        /// For X effects this is XValue; for all others it is BaseValue.
        /// The resolver applies Modifiers on top of this. Rule 803.
        /// </summary>
        public int EffectiveBaseValue =>
            ValueType == NumericValueType.X ? XValue : BaseValue;
    }
}