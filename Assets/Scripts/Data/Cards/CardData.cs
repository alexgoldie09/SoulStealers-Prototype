using System.Collections.Generic;
using UnityEngine;
using SSR.Logic;

namespace SSR.Data
{
    [CreateAssetMenu(fileName = "NewCard", menuName = "SSR/Card Data")]
    public class CardData : ScriptableObject
    {
        public string cardDataID;
        public string cardName;
        public CardType cardType;
        public CardSuperType cardSuperType;
        public int spiritRank;

        [SerializeReference]
        public List<EffectData> effects = new List<EffectData>();
    }
}