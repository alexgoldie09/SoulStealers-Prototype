using NUnit.Framework;
using SSR.Logic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace SSR.Tests
{
    /// <summary>
    /// Verifies that all EffectData subclasses construct correctly,
    /// expose the right EffectType, and that shared base properties
    /// behave as expected. No resolver logic — data only.
    /// </summary>
    public class EffectDataTests
    {
        private static readonly string LogPath = Path.Combine(
            Application.dataPath, "Debug", "EffectDataTests.txt");

        private StringBuilder _log;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath,
                $"=== EffectDataTests Run: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}{Environment.NewLine}");
        }

        [SetUp]
        public void SetUp()
        {
            _log = new StringBuilder();
            _log.AppendLine($"--- {TestContext.CurrentContext.Test.Name} ---");
        }

        [TearDown]
        public void TearDown()
        {
            _log.AppendLine();
            File.AppendAllText(LogPath, _log.ToString());
        }

        private void Log(string msg)
        {
            Debug.Log(msg);
            _log.AppendLine(msg);
        }

        // ── Soul Effects ──────────────────────────────────────────

        [Test]
        public void StealEffectData_HasCorrectType()
        {
            var effect = new StealEffectData
            {
                BaseValue = 3,
                ValueType = NumericValueType.Symbolic,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            Log($"  StealEffectData — type={effect.EffectType}  " +
                $"value={effect.BaseValue}  " +
                $"effectiveBase={effect.EffectiveBaseValue}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(EffectType.Steal, effect.EffectType);
            Assert.AreEqual(3, effect.EffectiveBaseValue);
            Assert.AreEqual(2, effect.TargetPlayerID);
        }

        [Test]
        public void BanishEffectData_HasCorrectType()
        {
            var effect = new BanishEffectData
            {
                BaseValue = 2,
                ValueType = NumericValueType.Symbolic,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            Log($"  BanishEffectData — type={effect.EffectType}  " +
                $"value={effect.BaseValue}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(EffectType.Banish, effect.EffectType);
            Assert.AreEqual(2, effect.BaseValue);
            Assert.AreEqual(2, effect.TargetPlayerID);
        }

        [Test]
        public void GiveSoulsEffectData_Imposed_HasCorrectType()
        {
            var effect = new GiveSoulsEffectData
            {
                BaseValue = 2,
                IsImposed = true,
                ControllerID = 1
            };
            effect.TargetIDs.Add(2);

            Log($"  GiveSoulsEffectData — type={effect.EffectType}  " +
                $"imposed={effect.IsImposed}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(EffectType.GiveSouls, effect.EffectType);
            Assert.IsTrue(effect.IsImposed);
            Assert.AreEqual(2, effect.TargetPlayerID);
        }

        // ── Numeric Value Types ───────────────────────────────────

        [Test]
        public void NumericEffect_XValue_UsedWhenValueTypeIsX()
        {
            var effect = new StealEffectData
            {
                BaseValue = 0,
                ValueType = NumericValueType.X,
                XValue = 5
            };

            Log($"  X-value effect — baseValue={effect.BaseValue}  " +
                $"xValue={effect.XValue}  " +
                $"effectiveBase={effect.EffectiveBaseValue}");

            Assert.AreEqual(5, effect.EffectiveBaseValue);
        }

        [Test]
        public void NumericEffect_SymbolicValue_UsesBaseValue()
        {
            var effect = new BanishEffectData
            {
                BaseValue = 4,
                ValueType = NumericValueType.Symbolic,
                XValue = 0
            };

            Log($"  Symbolic effect — baseValue={effect.BaseValue}  " +
                $"effectiveBase={effect.EffectiveBaseValue}");

            Assert.AreEqual(4, effect.EffectiveBaseValue);
        }

        [Test]
        public void NumericEffect_WordFormValue_UsesBaseValue()
        {
            var effect = new BanishEffectData
            {
                BaseValue = 2,
                ValueType = NumericValueType.WordForm
            };

            Log($"  WordForm effect — baseValue={effect.BaseValue}  " +
                $"effectiveBase={effect.EffectiveBaseValue}");

            Assert.AreEqual(2, effect.EffectiveBaseValue);
        }

        // ── Static Effects ────────────────────────────────────────

        [Test]
        public void DefenseEffectData_HasCorrectType()
        {
            var effect = new DefenseEffectData { BaseValue = 2 };

            Log($"  DefenseEffectData — type={effect.EffectType}  value={effect.BaseValue}");

            Assert.AreEqual(EffectType.Defense, effect.EffectType);
            Assert.AreEqual(2, effect.BaseValue);
        }

        [Test]
        public void IndestructibleEffectData_DefaultIsPermanent()
        {
            var effect = new IndestructibleEffectData();

            Log($"  IndestructibleEffectData — type={effect.EffectType}  " +
                $"duration={effect.Duration}  " +
                $"targetCard={effect.TargetCardID}");

            Assert.AreEqual(EffectType.Indestructible, effect.EffectType);
            Assert.AreEqual(EffectDurationTiming.Permanent, effect.Duration);
            Assert.AreEqual(-1, effect.TargetCardID);
        }

        [Test]
        public void IgnoreEffectData_HasCorrectType()
        {
            var effect = new IgnoreEffectData
            {
                IgnoresDefense = true,
                IgnoresSecretResponses = true
            };
            effect.IgnoredCardTypes.Add(CardType.Secret);

            Log($"  IgnoreEffectData — type={effect.EffectType}  " +
                $"ignoresDefense={effect.IgnoresDefense}  " +
                $"ignoresSecrets={effect.IgnoresSecretResponses}  " +
                $"ignoredTypes={effect.IgnoredCardTypes.Count}");

            Assert.AreEqual(EffectType.Ignore, effect.EffectType);
            Assert.IsTrue(effect.IgnoresDefense);
            Assert.AreEqual(1, effect.IgnoredCardTypes.Count);
        }

        // ── Field Effects ─────────────────────────────────────────

        [Test]
        public void SilenceEffectData_DefaultDurationIsUntilNextTurn()
        {
            var effect = new SilenceEffectData();
            effect.TargetIDs.Add(42);

            Log($"  SilenceEffectData — type={effect.EffectType}  " +
                $"duration={effect.Duration}  " +
                $"targetCard={effect.TargetCardID}  " +
                $"maxTargets={effect.MaxTargets}");

            Assert.AreEqual(EffectType.Silence, effect.EffectType);
            Assert.AreEqual(EffectDurationTiming.UntilNextTurn, effect.Duration);
            Assert.AreEqual(42, effect.TargetCardID);
        }

        [Test]
        public void CounterEffectData_DefaultIsAddition()
        {
            var effect = new CounterEffectData { BaseValue = 2 };
            effect.TargetIDs.Add(10);

            Log($"  CounterEffectData — type={effect.EffectType}  " +
                $"count={effect.BaseValue}  " +
                $"isAddition={effect.IsAddition}  " +
                $"targetCard={effect.TargetCardID}");

            Assert.AreEqual(EffectType.Counter, effect.EffectType);
            Assert.IsTrue(effect.IsAddition);
            Assert.AreEqual(10, effect.TargetCardID);
        }

        [Test]
        public void MergeEffectData_HasCorrectType()
        {
            var effect = new MergeEffectData
            {
                SourceCardType = CardType.Ritual
            };
            effect.TargetIDs.Add(55);

            Log($"  MergeEffectData — type={effect.EffectType}  " +
                $"sourceType={effect.SourceCardType}  " +
                $"targetIncantation={effect.TargetIncantationID}");

            Assert.AreEqual(EffectType.Merge, effect.EffectType);
            Assert.AreEqual(CardType.Ritual, effect.SourceCardType);
            Assert.AreEqual(55, effect.TargetIncantationID);
        }

        // ── Pile Effects ──────────────────────────────────────────

        [Test]
        public void NegateEffectData_DefaultTargetsCard()
        {
            var effect = new NegateEffectData();
            effect.TargetIDs.Add(99);

            Log($"  NegateEffectData — type={effect.EffectType}  " +
                $"targetsCard={effect.TargetsCard}  " +
                $"typeRestriction={effect.TypeRestriction?.ToString() ?? "none"}  " +
                $"targetOnPile={effect.TargetOnPileID}");

            Assert.AreEqual(EffectType.Negate, effect.EffectType);
            Assert.IsTrue(effect.TargetsCard);
            Assert.AreEqual(99, effect.TargetOnPileID);
        }

        [Test]
        public void ConspiracyEffectData_HasCorrectType()
        {
            var effect = new ConspiracyEffectData();

            Log($"  ConspiracyEffectData — type={effect.EffectType}");

            Assert.AreEqual(EffectType.Conspiracy, effect.EffectType);
        }

        // ── Card Movement ─────────────────────────────────────────

        [Test]
        public void DestroyEffectData_HasCorrectType()
        {
            var effect = new DestroyEffectData
            {
                TypeRestriction = CardType.Ritual
            };
            effect.TargetIDs.Add(77);

            Log($"  DestroyEffectData — type={effect.EffectType}  " +
                $"typeRestriction={effect.TypeRestriction}  " +
                $"targetCard={effect.TargetCardID}");

            Assert.AreEqual(EffectType.Destroy, effect.EffectType);
            Assert.AreEqual(77, effect.TargetCardID);
        }

        [Test]
        public void DiscardEffectData_DefaultSourceZoneIsHand()
        {
            var effect = new DiscardEffectData();

            Log($"  DiscardEffectData — type={effect.EffectType}  " +
                $"sourceZone={effect.SourceZone}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(EffectType.Discard, effect.EffectType);
            Assert.AreEqual(ZoneType.Hand, effect.SourceZone);
        }

        [Test]
        public void RecallEffectData_HasCorrectType()
        {
            var effect = new RecallEffectData
            {
                Count = 2,
                TakesFromTop = true,
                SourceZone = ZoneType.DiscardPile
            };
            effect.TargetIDs.Add(1);

            Log($"  RecallEffectData — type={effect.EffectType}  " +
                $"count={effect.Count}  " +
                $"fromTop={effect.TakesFromTop}  " +
                $"sourceZone={effect.SourceZone}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(EffectType.Recall, effect.EffectType);
            Assert.AreEqual(2, effect.Count);
            Assert.IsTrue(effect.TakesFromTop);
        }

        [Test]
        public void CopyEffectData_DefaultIsPermanent()
        {
            var effect = new CopyEffectData();
            effect.TargetIDs.Add(33);

            Log($"  CopyEffectData — type={effect.EffectType}  " +
                $"duration={effect.Duration}  " +
                $"targetCard={effect.TargetCardID}");

            Assert.AreEqual(EffectType.Copy, effect.EffectType);
            Assert.AreEqual(EffectDurationTiming.Permanent, effect.Duration);
        }

        // ── Action Effects ────────────────────────────────────────

        [Test]
        public void SpecialPlayEffectData_HasCorrectType()
        {
            var effect = new SpecialPlayEffectData
            {
                AllowedCardType = CardType.Spell,
                CanPlayOutsideActionPhase = true
            };

            Log($"  SpecialPlayEffectData — type={effect.EffectType}  " +
                $"allowedType={effect.AllowedCardType}  " +
                $"outsideActionPhase={effect.CanPlayOutsideActionPhase}");

            Assert.AreEqual(EffectType.SpecialPlay, effect.EffectType);
            Assert.AreEqual(CardType.Spell, effect.AllowedCardType);
        }

        [Test]
        public void PactEffectData_HasCorrectType()
        {
            var effect = new PactEffectData { RequiredSpiritName = "Liria" };

            Log($"  PactEffectData — type={effect.EffectType}  " +
                $"requiredSpirit={effect.RequiredSpiritName}");

            Assert.AreEqual(EffectType.Pact, effect.EffectType);
            Assert.AreEqual("Liria", effect.RequiredSpiritName);
        }

        // ── Modifier ──────────────────────────────────────────────

        [Test]
        public void ModifierEffectData_Positive_HasCorrectType()
        {
            var effect = new ModifierEffectData
            {
                BaseValue = 2,
                IsPositive = true,
                ControllerOnly = true
            };
            effect.ModifiedEffectTypes.Add(EffectType.Banish);

            Log($"  ModifierEffectData — type={effect.EffectType}  " +
                $"value=+{effect.BaseValue}  " +
                $"targets={effect.ModifiedEffectTypes[0]}  " +
                $"controllerOnly={effect.ControllerOnly}");

            Assert.AreEqual(EffectType.Modifier, effect.EffectType);
            Assert.IsTrue(effect.IsPositive);
            Assert.AreEqual(1, effect.ModifiedEffectTypes.Count);
        }

        [Test]
        public void ModifierEffectData_Global_AppliestoAll()
        {
            var effect = new ModifierEffectData
            {
                BaseValue = 2,
                IsPositive = false,
                ControllerOnly = false
            };
            effect.ModifiedEffectTypes.Add(EffectType.Banish);
            effect.ModifiedEffectTypes.Add(EffectType.Steal);

            Log($"  Uzilda-style modifier — type={effect.EffectType}  " +
                $"value=-{effect.BaseValue}  " +
                $"global={!effect.ControllerOnly}  " +
                $"targetEffects={effect.ModifiedEffectTypes.Count}");

            Assert.IsFalse(effect.ControllerOnly);
            Assert.AreEqual(2, effect.ModifiedEffectTypes.Count);
        }

        // ── Base Class Properties ─────────────────────────────────

        [Test]
        public void EffectData_LinkedChain_DefaultIsMinusOne()
        {
            var effect = new StealEffectData();

            Log($"  Linked chain default — " +
                $"linkedToPreceding={effect.LinkedToPrecedingEffectIndex}  " +
                $"dependency={effect.Dependency}");

            Assert.AreEqual(-1, effect.LinkedToPrecedingEffectIndex);
        }

        [Test]
        public void EffectData_LinkedChain_SetCorrectly()
        {
            var effect = new BanishEffectData
            {
                PrintedEffectIndex = 1,
                Dependency = EffectDependency.Linked,
                LinkedToPrecedingEffectIndex = 0
            };

            Log($"  Linked effect — printedIndex={effect.PrintedEffectIndex}  " +
                $"linkedTo={effect.LinkedToPrecedingEffectIndex}  " +
                $"dependency={effect.Dependency}");

            Assert.AreEqual(0, effect.LinkedToPrecedingEffectIndex);
            Assert.AreEqual(EffectDependency.Linked, effect.Dependency);
        }

        [Test]
        public void EffectData_RevealEffect_FlaggedCorrectly()
        {
            var effect = new StealEffectData
            {
                IsRevealEffect = true,
                PrintedEffectIndex = 0
            };

            Log($"  Reveal effect — isReveal={effect.IsRevealEffect}  " +
                $"printedIndex={effect.PrintedEffectIndex}");

            Assert.IsTrue(effect.IsRevealEffect);
        }

        [Test]
        public void EffectData_ORChoice_GroupedCorrectly()
        {
            var optionA = new BanishEffectData
            {
                IsChoiceEffect = true,
                ChoiceGroupIndex = 0,
                ChoiceIndex = 0,
                BaseValue = 2
            };
            var optionB = new StealEffectData
            {
                IsChoiceEffect = true,
                ChoiceGroupIndex = 0,
                ChoiceIndex = 1,
                BaseValue = 1
            };

            Log($"  OR choice group 0 —");
            Log($"    Option A: {optionA.EffectType} value={optionA.BaseValue}  " +
                $"choiceIndex={optionA.ChoiceIndex}");
            Log($"    Option B: {optionB.EffectType} value={optionB.BaseValue}  " +
                $"choiceIndex={optionB.ChoiceIndex}");

            Assert.AreEqual(optionA.ChoiceGroupIndex, optionB.ChoiceGroupIndex);
            Assert.AreNotEqual(optionA.ChoiceIndex, optionB.ChoiceIndex);
        }

        [Test]
        public void EffectData_StatusDefaultsToActive()
        {
            var effect = new DestroyEffectData();

            Log($"  Default status — status={effect.Status}");

            Assert.AreEqual(EffectStatus.Active, effect.Status);
        }

        [Test]
        public void EffectData_TargetIDs_EmptyByDefault()
        {
            var effect = new StealEffectData();

            Log($"  Default targets — count={effect.TargetIDs.Count}  " +
                $"targetPlayer={effect.TargetPlayerID}");

            Assert.AreEqual(0, effect.TargetIDs.Count);
            Assert.AreEqual(-1, effect.TargetPlayerID);
        }
    }
}
