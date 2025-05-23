using System;
using NUnit.Framework;
using UnityEssentials;

namespace UnityEssentialsTests
{
    [TestFixture]
    public class TickUpdateTests
    {
        [SetUp]
        public void SetUp()
        {
            // Ensure a clean state before each test
            TickUpdate.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            TickUpdate.Clear();
        }

        [Test]
        public void Register_ThrowsOnNullAction()
        {
            Assert.Throws<ArgumentNullException>(() => TickUpdate.Register(1, null));
        }

        [Test]
        public void Register_ThrowsOnNonPositiveTicksPerSecond()
        {
            Assert.Throws<ArgumentException>(() => TickUpdate.Register(0, () => { }));
            Assert.Throws<ArgumentException>(() => TickUpdate.Register(-5, () => { }));
        }

        [Test]
        public void Register_AddsActionAndDoesNotDuplicate()
        {
            int callCount = 0;
            Action action = () => callCount++;

            TickUpdate.Register(10, action);
            TickUpdate.Register(10, action); // Should not add duplicate

            // Simulate enough time to trigger at least one tick
            TickUpdate.Update(0.1f);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Unregister_RemovesAction()
        {
            int callCount = 0;
            Action action = () => callCount++;

            TickUpdate.Register(5, action);
            TickUpdate.Unregister(5, action);

            TickUpdate.Update(1.0f);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Unregister_DoesNothingIfActionNotFound()
        {
            // Should not throw
            TickUpdate.Unregister(5, () => { });
        }

        [Test]
        public void Update_ExecutesRegisteredActionsAtCorrectRate()
        {
            int callCount = 0;
            TickUpdate.Register(2, () => callCount++);

            // 0.5s per tick at 2 ticks/sec, so 1.0s = 2 ticks
            TickUpdate.Update(1.0f);

            Assert.AreEqual(2, callCount);
        }

        [Test]
        public void Update_DoesNotExecuteIfNoActions()
        {
            // Should not throw or do anything
            TickUpdate.Update(1.0f);
        }

        [Test]
        public void Update_RemovesEmptyGroups()
        {
            int callCount = 0;
            Action action = () => callCount++;

            TickUpdate.Register(3, action);
            TickUpdate.Unregister(3, action);

            // Should remove the group internally without error
            TickUpdate.Update(1.0f);

            // No exception, group is cleaned up
            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void Clear_ResetsAllState()
        {
            int callCount = 0;
            Action action = () => callCount++;

            TickUpdate.Register(1, action);
            TickUpdate.Clear();

            TickUpdate.Update(1.0f);

            Assert.AreEqual(0, callCount);
        }
    }
}