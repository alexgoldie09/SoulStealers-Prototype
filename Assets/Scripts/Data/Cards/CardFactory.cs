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

            int opponentID = ownerID == 1 ? 2 : 1;

            foreach (var asset in data.effects)
            {
                if (asset == null) continue;
                var effectData = asset.GetEffectData();
                effectData.SourceCardID = card.ID;
                effectData.ControllerID = ownerID;

                // Auto-populate opponent target for soul effects
                if (effectData is StealEffectData
                    || effectData is BanishEffectData
                    || effectData is GiveSoulsEffectData)
                {
                    if (effectData.TargetIDs.Count == 0)
                        effectData.TargetIDs.Add(opponentID);
                }

                card.Effects.Add(effectData);
            }

            return card;
        }
    }
}