using SSR.Logic;

namespace SSR.Data
{
    /// <summary>
    /// Creates RuntimeCards from CardData ScriptableObject assets.
    /// Lives in SSR.Data because it depends on CardData (UnityEngine).
    /// Returns SSR.Logic types so the logic layer never touches Unity.
    /// </summary>
    public static class CardFactory
    {
        public static RuntimeCard CreateRuntimeCard(CardData data, int ownerID)
        {
            var card = new RuntimeCard(
                cardDataID: data.cardDataID,
                ownerID: ownerID,
                type: data.cardType,
                superType: data.cardSuperType,
                cardName: data.cardName,
                spiritRank: data.spiritRank
            );

            foreach (var effect in data.effects)
            {
                if (effect != null)
                    card.Effects.Add(effect);
            }

            return card;
        }
    }
}