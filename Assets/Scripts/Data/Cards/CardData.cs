using System.Collections.Generic;
using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(fileName = "Card_", menuName = "SSR/Card Data")]
    public class CardData : ScriptableObject
    {
        public string cardDataID;
        public string cardName;
        public CardType cardType;
        public CardSuperType cardSuperType;
        public int spiritRank;
        
        public List<EffectDataAsset> effects = new List<EffectDataAsset>();
    }
}