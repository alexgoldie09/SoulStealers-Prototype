using System;
using System.Collections.Generic;

namespace SSR.Logic
{
    /// <summary>
    /// The live runtime state of a card during a game.
    /// Distinct from CardData (the static ScriptableObject asset in SSR.Data).
    /// The logic layer never references CardData directly — it works with
    /// this object and the cardDataID string to look up static data when needed.
    /// </summary>
    [Serializable]
    public class RuntimeCard
    {
        // ── Identity ──────────────────────────────────────────────
        public int ID;
        public string CardDataID;

        // ── Ownership & Control ───────────────────────────────────
        // Owner: the player who included this card in their deck.
        // Controller: the player who currently has it on the Field.
        // These can differ (e.g. a Curse played under an opponent's control).
        // Rule 105.
        public int OwnerID;
        public int ControllerID;

        // ── Card Type (runtime, can change via Copy or Merge) ─────
        public CardType CurrentType;
        public CardSuperType CurrentSuperType;
        public string CurrentName;
        public bool IsGold;
        public int SpiritRank;

        // ── Location & Field State ────────────────────────────────
        public CardLocation Location;
        public CardFaceState FaceState;

        // True from the moment the card reaches the Resolution Pile
        // until it leaves. Rule 601.6.
        public bool IsPlayed;

        // True only after the card has formally resolved and
        // entered a field subzone. Merged cards never Enter the Field.
        // Rule 604.5.
        public bool HasEnteredField;

        // ── Merge State ───────────────────────────────────────────
        // An attached card has not Entered the Field.
        // It loses its type and name; its effects are added to the host.
        // Rule 805.
        public bool IsAttached;
        public int AttachedHostID;

        // IDs of cards attached to this card as host. Max 2. Rule 805.3.
        public List<int> AttachedCardIDs = new List<int>();

        // ── Face-Down Spell Tracking ──────────────────────────────
        // A Spell played face-down is "treated as a Secret" on the field
        // but retains its underlying Spell type. Rule 304.5.
        // When this is true, CurrentType reflects the face-down treatment
        // but UnderlyingType holds the real type for rules that care.
        public bool IsFaceDownSpell;
        public CardType UnderlyingType;

        // ── Effect State ──────────────────────────────────────────
        // Per-effect status tracking (index maps to printed effect order).
        // Rule 106.
        public Dictionary<int, EffectStatus> EffectStatuses
            = new Dictionary<int, EffectStatus>();

        // True if a Silence effect is currently applied to this card.
        // Silenced cards lose all text and cannot trigger or activate. Rule 808.
        public bool IsSilenced;

        // True if this card currently has the Indestructible static effect.
        // Lost when the card is Silenced. Rule 810.
        public bool IsIndestructible;

        // Active duration-tracked effects applied to or by this card.
        public List<ActiveDuration> ActiveDurations = new List<ActiveDuration>();

        // ── Counters ──────────────────────────────────────────────
        // Counters persist while on Field. Cease to exist if the card
        // leaves the Field or is attached. Rule 107.
        public int CounterCount;

        // ── Prayer Tracking ───────────────────────────────────────
        // Prayers may only be activated once per Action Phase (pile must
        // be empty). Resets if the Prayer leaves and re-enters the Field
        // in the same Action Phase. Rule 308.
        public bool PrayerActivatedThisActionPhase;

        // ── Play History ─────────────────────────────────────────
        public PlayType LastPlayType;

        // ── Constructor ───────────────────────────────────────────
        public RuntimeCard(
            string cardDataID,
            int ownerID,
            CardType type,
            CardSuperType superType,
            string cardName,
            bool isGold = false,
            int spiritRank = 0)
        {
            ID = IDFactory.GetUniqueID();
            CardDataID = cardDataID;
            OwnerID = ownerID;
            ControllerID = ownerID;
            CurrentType = type;
            CurrentSuperType = superType;
            CurrentName = cardName;
            IsGold = isGold;
            SpiritRank = spiritRank;
            UnderlyingType = type;
            Location = CardLocation.MainDeck;
            FaceState = CardFaceState.FaceDown;
        }

        #region Helpers
        /// <summary>
        /// Returns true if this card is a face-down Sorcery in the Sorcery
        /// zone — either a true Secret or a Spell played face-down. Rule 304.5.
        /// </summary>
        public bool IsFaceDownSorcery =>
            Location == CardLocation.SorceryZone &&
            FaceState == CardFaceState.FaceDown;

        /// <summary>
        /// Returns the effective type for targeting purposes.
        /// A face-down Spell is treated as a Secret on the field but
        /// retains its Spell type for effects that care. Rule 304.5.
        /// </summary>
        public CardType EffectiveType =>
            IsFaceDownSpell ? CardType.Secret : CurrentType;

        /// <summary>
        /// Curses cannot be Sacrificed. Rule 307, 604.3.
        /// </summary>
        public bool CanBeSacrificed =>
            CurrentType != CardType.Curse && CurrentType != CardType.Spirit;

        /// <summary>
        /// Clears counters when the card leaves the Field. Rule 107.4.
        /// </summary>
        public void OnLeaveField()
        {
            CounterCount = 0;
            HasEnteredField = false;
        }

        /// <summary>
        /// Clears attached card counters when attaching via Merge.
        /// The attached card is no longer independently on the Field. Rule 107.5.
        /// </summary>
        public void OnAttach(int hostID)
        {
            CounterCount = 0;
            IsAttached = true;
            AttachedHostID = hostID;
            HasEnteredField = false;
        }

        /// <summary>
        /// Resets Prayer activation flag. Called when the Prayer re-enters
        /// the Field within the same Action Phase. Rule 308.
        /// </summary>
        public void ResetPrayerActivation()
        {
            PrayerActivatedThisActionPhase = false;
        }
        #endregion
    }
}
