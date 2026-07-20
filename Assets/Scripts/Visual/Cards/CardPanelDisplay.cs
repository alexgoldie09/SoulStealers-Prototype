using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SSR.Logic;

namespace SSR.Visual
{
    /// <summary>
    /// Controls a single card panel in the UI.
    /// Attach to the CardPanel prefab root.
    /// </summary>
    public class CardPanelDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _effectsText;
        [SerializeField] private TextMeshProUGUI _typeText;
        [SerializeField] private TextMeshProUGUI _spiritRankText;
        [SerializeField] private Image _panelBackground;
        [SerializeField] private Button _button;

        private static readonly Color ColorSpell = new Color(0.4f, 0.6f, 1f);
        private static readonly Color ColorSecret = new Color(0.5f, 0.3f, 0.7f);
        private static readonly Color ColorRitual = new Color(0.3f, 0.7f, 0.4f);
        private static readonly Color ColorCurse = new Color(0.7f, 0.2f, 0.2f);
        private static readonly Color ColorPrayer = new Color(0.9f, 0.8f, 0.3f);
        private static readonly Color ColorSpirit = new Color(0.9f, 0.5f, 0.1f);
        private static readonly Color ColorFaceDown = new Color(0.25f, 0.25f, 0.3f);
        private static readonly Color ColorHighlight = new Color(1f, 1f, 0.5f);

        private Color _baseColor;
        private System.Action _clickAction;

        public void Setup(RuntimeCard card, bool faceDown)
        {
            if (faceDown)
            {
                _nameText.text = "?";
                _typeText.text = "?";
                _effectsText.text = "";
                if (_spiritRankText != null)
                    _spiritRankText.text = "";
                _baseColor = ColorFaceDown;
            }
            else
            {
                _nameText.text = card.CurrentName;
                _effectsText.text = SummariseEffects(card);
                _typeText.text = card.CurrentType.ToString();
                _baseColor = GetColorForType(card.CurrentType);
                if (_spiritRankText != null)
                    _spiritRankText.text = card.CurrentType == CardType.Spirit ? $"Rank {card.SpiritRank}" : "";
            }
            _panelBackground.color = _baseColor;
            _button.onClick.RemoveAllListeners();
        }

        public void SetClickAction(System.Action action)
        {
            _clickAction = action;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _clickAction?.Invoke());
        }

        public void SetHighlight(bool highlighted)
        {
            _panelBackground.color = highlighted ? ColorHighlight : _baseColor;
        }

        private static Color GetColorForType(CardType type)
        {
            switch (type)
            {
                case CardType.Spell:   return ColorSpell;
                case CardType.Secret:  return ColorSecret;
                case CardType.Ritual:  return ColorRitual;
                case CardType.Curse:   return ColorCurse;
                case CardType.Prayer:  return ColorPrayer;
                case CardType.Spirit:  return ColorSpirit;
                default:               return Color.white;
            }
        }
        
        private string SummariseEffects(RuntimeCard card)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in card.Effects)
            {
                switch (e)
                {
                    case StealEffectData s:
                        sb.AppendLine($"Steal {s.BaseValue}");
                        break;
                    case BanishEffectData b:
                        sb.AppendLine($"Banish {b.BaseValue}");
                        break;
                    case DefenseEffectData d:
                        sb.AppendLine($"Defense {d.BaseValue}");
                        break;
                    case GiveSoulsEffectData g:
                        sb.AppendLine(g.IsImposed
                            ? $"Give {g.BaseValue} Souls"
                            : $"Gain {g.BaseValue} Souls");
                        break;
                    case SilenceEffectData:
                        sb.AppendLine("Silence");
                        break;
                    case NegateEffectData:
                        sb.AppendLine("Negate");
                        break;
                    case ModifierEffectData m:
                        {
                            var targets = new System.Text.StringBuilder();
                            foreach (var et in m.ModifiedEffectTypes)
                            {
                                if (targets.Length > 0) targets.Append("/");
                                targets.Append(et.ToString());
                            }
                            string targetStr = targets.Length > 0 ? targets.ToString() : "effects";
                            sb.AppendLine($"{(m.IsPositive ? "+" : "-")}{m.BaseValue} to {targetStr}");
                            break;
                        }
                    case TriggerEffectData t:
                        sb.AppendLine($"BoT Trigger →[{t.PayloadEffectIndex}]");
                        break;
                    case MergeEffectData:
                        sb.AppendLine("Merge");
                        break;
                    case ConspiracyEffectData:
                        sb.AppendLine("Conspiracy");
                        break;
                }
            }
            return sb.ToString().TrimEnd();
        }
    }
}
