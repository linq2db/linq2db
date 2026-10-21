using System.Linq;

using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

using Shouldly;

namespace Tests.Infrastructure
{
	/// <summary>
	/// The heartbeat's run total (<see cref="TestRunCounter"/>): the tests NUnit's filtered work-item tree will execute
	/// with the reporter's action, not the discovered test-case count.
	/// </summary>
	[TestFixture]
	public class TestRunCounterTests : TestBase
	{
		// A hand-built tree over methods without test attributes, so NUnit never discovers them as real tests:
		//
		//   Root
		//     Fixture        Runnable1, Runnable2, Ignored, Explicit
		//     IgnoredSuite   (Ignored)  InIgnored
		//     ExplicitSuite  (Explicit) InExplicit, ExplicitInExplicit (Explicit)
		static TestSuite BuildTree()
		{
			var root = new TestSuite("Root");

			var fixture = AddSuite(root, "Fixture", RunState.Runnable);
			AddTest(fixture, nameof(Sample.Runnable1), RunState.Runnable);
			AddTest(fixture, nameof(Sample.Runnable2), RunState.Runnable);
			AddTest(fixture, nameof(Sample.Ignored),   RunState.Ignored);
			AddTest(fixture, nameof(Sample.Explicit),  RunState.Explicit);

			var ignoredSuite = AddSuite(root, "IgnoredSuite", RunState.Ignored);
			AddTest(ignoredSuite, nameof(Sample.InIgnored), RunState.Runnable);

			var explicitSuite = AddSuite(root, "ExplicitSuite", RunState.Explicit);
			AddTest(explicitSuite, nameof(Sample.InExplicit),         RunState.Runnable);
			AddTest(explicitSuite, nameof(Sample.ExplicitInExplicit), RunState.Explicit);

			return root;

			static TestSuite AddSuite(TestSuite parent, string name, RunState runState)
			{
				var suite = new TestSuite(parent.FullName, name) { RunState = runState };
				parent.Add(suite);
				return suite;
			}

			static void AddTest(TestSuite parent, string method, RunState runState)
			{
				var test = new TestMethod(new MethodWrapper(typeof(Sample), method), parent) { RunState = runState };
				parent.Add(test);
			}
		}

		static long Count(TestSuite root, ITestFilter filter)
		{
			var item = WorkItemBuilder.CreateWorkItem(root, filter, recursive: true)!;

			return TestRunCounter.CountActionTests(item);
		}

		[Test]
		public void CountsOnlyTestsThatRunTheAction()
		{
			var root = BuildTree();

			// The discovered count includes every leaf.
			root.TestCaseCount
				.ShouldBe(7);

			Count(root, TestFilter.Empty)
				.ShouldBe(2);

			Count(root, FullName("Root.Fixture.Runnable1"))
				.ShouldBe(1);

			Count(root, FullName("Root.Fixture.Explicit"))
				.ShouldBe(1);

			// The suite is explicitly matched, its Explicit child is not.
			Count(root, FullName("Root.ExplicitSuite"))
				.ShouldBe(1);

			Count(root, FullName("Root.Fixture"))
				.ShouldBe(2);
		}

		// The XML form is what the adapter hands NUnit for a FullyQualifiedName filter.
		static TestFilter FullName(string fullName) => TestFilter.FromXml($"<filter><test>{fullName}</test></filter>");

		[Test]
		public void UnresolvableDispatcherYieldsNull()
		{
			var root = BuildTree();

			TestRunCounter
				.TryResolveRootWorkItem(new MainThreadWorkItemDispatcher(), root)
				.ShouldBeNull();

			TestRunCounter
				.TryResolveRootWorkItem(null, root)
				.ShouldBeNull();

			TestRunCounter
				.TryCount(null, root)
				.ShouldBeNull();
		}

		[Test]
		public void ResolvesRootWorkItemOnlyForItsOwnTest()
		{
			var root       = BuildTree();
			var other      = BuildTree();
			var item       = WorkItemBuilder.CreateWorkItem(root, TestFilter.Empty, recursive: true)!;
			var dispatcher = new SimpleWorkItemDispatcher();

			TestRunCounter
				.FindWorkItemFields(typeof(SimpleWorkItemDispatcher))
				.Single()
				.SetValue(dispatcher, item);

			TestRunCounter
				.TryResolveRootWorkItem(dispatcher, root)
				.ShouldBeSameAs(item);

			TestRunCounter
				.TryResolveRootWorkItem(dispatcher, other)
				.ShouldBeNull();

			TestRunCounter
				.TryCount(dispatcher, root)
				.ShouldBe(2);
		}

		// Guards the reflection hop: an NUnit upgrade that drops the dispatchers' top-level work item turns every run's
		// total into the discovered upper bound, which nothing else would report.
		[Test]
		public void NUnitDispatchersExposeTopLevelWorkItem()
		{
			TestRunCounter
				.FindWorkItemFields(typeof(SimpleWorkItemDispatcher))
				.ShouldNotBeEmpty();

			TestRunCounter
				.FindWorkItemFields(typeof(ParallelWorkItemDispatcher))
				.ShouldNotBeEmpty();
		}

		sealed class Sample
		{
			public void Runnable1() { }
			public void Runnable2() { }
			public void Ignored() { }
			public void Explicit() { }
			public void InIgnored() { }
			public void InExplicit() { }
			public void ExplicitInExplicit() { }
		}
	}
}
