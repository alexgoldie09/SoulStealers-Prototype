using NUnit.Framework;
using SSR.Logic;
using System;

namespace SSR.Tests
{
    /// <summary>
    /// Tests for the various Zone classes to ensure they manage cards correctly according to game rules.
    /// </summary>
    public class ZoneTests
    {
        [Test]
        public void DiscardPile_PreservesInsertionOrder_TopIsLastAdded()
        {
            var pile = new DiscardPileZone();
            pile.AddToTop(1);
            pile.AddToTop(2);
            pile.AddToTop(3);

            Assert.AreEqual(3, pile.PeekTop());
            Assert.AreEqual(2, pile.PeekAt(1));
            Assert.AreEqual(1, pile.PeekAt(2));
        }

        [Test]
        public void DiscardPile_TakeFromTop_RemovesCorrectly()
        {
            var pile = new DiscardPileZone();
            pile.AddToTop(10);
            pile.AddToTop(20);

            int taken = pile.TakeFromTop();
            Assert.AreEqual(20, taken);
            Assert.AreEqual(10, pile.PeekTop());
        }

        [Test]
        public void IncantationZone_RejectsWhenAtCapacity()
        {
            var zone = new IncantationZone();
            zone.Add(1);
            zone.Add(2);
            zone.Add(3);

            Assert.IsTrue(zone.IsFull);
            Assert.Throws<InvalidOperationException>(() => zone.Add(4));
        }

        [Test]
        public void SorceryZone_RejectsWhenAtCapacity()
        {
            var zone = new SorceryZone();
            zone.Add(1);
            zone.Add(2);
            zone.Add(3);

            Assert.IsTrue(zone.IsFull);
            Assert.Throws<InvalidOperationException>(() => zone.Add(4));
        }

        [Test]
        public void SpiritZone_RejectsSecondSpirit()
        {
            var zone = new SpiritZone();
            zone.Add(1);

            Assert.IsTrue(zone.HasSpirit);
            Assert.Throws<InvalidOperationException>(() => zone.Add(2));
        }

        [Test]
        public void ResolutionPile_IsLIFO()
        {
            var pile = new ResolutionPileZone();
            pile.Push(1);
            pile.Push(2);
            pile.Push(3);

            Assert.AreEqual(3, pile.Pop());
            Assert.AreEqual(2, pile.Pop());
            Assert.AreEqual(1, pile.Pop());
            Assert.IsTrue(pile.IsEmpty);
        }

        [Test]
        public void ResolutionPile_Peek_DoesNotRemove()
        {
            var pile = new ResolutionPileZone();
            pile.Push(99);

            Assert.AreEqual(99, pile.Peek());
            Assert.AreEqual(99, pile.Peek());
            Assert.IsFalse(pile.IsEmpty);
        }

        [Test]
        public void RuntimeCard_OwnerAndController_DefaultToSamePlayer()
        {
            var card = new RuntimeCard("TestCard", 1, CardType.Spell,
                CardSuperType.Sorcery, "Test Spell");

            Assert.AreEqual(card.OwnerID, card.ControllerID);
        }

        [Test]
        public void RuntimeCard_ControllerCanDifferFromOwner()
        {
            var card = new RuntimeCard("CurseCard", 1, CardType.Curse,
                CardSuperType.Incantation, "Test Curse");

            card.ControllerID = 2;

            Assert.AreEqual(1, card.OwnerID);
            Assert.AreEqual(2, card.ControllerID);
        }

        [Test]
        public void RuntimeCard_CurseCannotBeSacrificed()
        {
            var curse = new RuntimeCard("Curse", 1, CardType.Curse,
                CardSuperType.Incantation, "Test Curse");
            var spell = new RuntimeCard("Spell", 1, CardType.Spell,
                CardSuperType.Sorcery, "Test Spell");

            Assert.IsFalse(curse.CanBeSacrificed);
            Assert.IsTrue(spell.CanBeSacrificed);
        }

        [Test]
        public void RuntimeCard_OnLeaveField_ClearsCounters()
        {
            var card = new RuntimeCard("Ritual", 1, CardType.Ritual,
                CardSuperType.Incantation, "Test Ritual");
            card.CounterCount = 3;
            card.HasEnteredField = true;

            card.OnLeaveField();

            Assert.AreEqual(0, card.CounterCount);
            Assert.IsFalse(card.HasEnteredField);
        }

        [Test]
        public void RuntimeCard_OnAttach_ClearsCountersAndSetsAttachedState()
        {
            var card = new RuntimeCard("Ritual", 1, CardType.Ritual,
                CardSuperType.Incantation, "Test Ritual");
            card.CounterCount = 2;

            card.OnAttach(hostID: 99);

            Assert.AreEqual(0, card.CounterCount);
            Assert.IsTrue(card.IsAttached);
            Assert.AreEqual(99, card.AttachedHostID);
            Assert.IsFalse(card.HasEnteredField);
        }

        [Test]
        public void SpiritPool_GetDealCount_AllThresholds()
        {
            var pool = new SpiritPoolState();

            for (int i = 0; i < 16; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(4, pool.GetDealCount());

            pool.AvailablePool.Clear();
            for (int i = 0; i < 6; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(3, pool.GetDealCount());

            pool.AvailablePool.Clear();
            for (int i = 0; i < 4; i++) pool.AvailablePool.Add(i);
            Assert.AreEqual(2, pool.GetDealCount());

            pool.AvailablePool.Clear();
            pool.AvailablePool.Add(0);
            pool.AvailablePool.Add(1);
            Assert.AreEqual(1, pool.GetDealCount());

            pool.AvailablePool.Clear();
            Assert.AreEqual(0, pool.GetDealCount());
        }

        [Test]
        public void SpiritPool_ResetCycle_MovesUnavailableToAvailable()
        {
            var pool = new SpiritPoolState();
            pool.UnavailablePool.AddRange(new[] { 1, 2, 3, 4 });

            pool.ResetCycle();

            Assert.AreEqual(4, pool.AvailablePool.Count);
            Assert.AreEqual(0, pool.UnavailablePool.Count);
            Assert.AreEqual(1, pool.CycleCount);
        }
    }
}
