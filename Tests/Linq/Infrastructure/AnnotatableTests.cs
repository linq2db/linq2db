using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq.Builder;

using NUnit.Framework;

using Shouldly;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class AnnotatableTests : TestBase
	{
		sealed class ReadOnlyToggle : Annotatable
		{
			public bool ReadOnly { get; set; }

			public override bool IsReadOnly => ReadOnly;
		}

		[Test]
		public void Annotatable_IsReachable()
		{
			IReadOnlyAnnotatable a = new Annotatable();
			a.ShouldNotBeNull();
		}

		[Test]
		public void AddAnnotation_StoresValue()
		{
			var a = new Annotatable();

			var ann = a.AddAnnotation("k", 42);

			ann.Name.ShouldBe("k");
			ann.Value.ShouldBe(42);
			a.FindAnnotation("k")!.Value.ShouldBe(42);
			a["k"].ShouldBe(42);
		}

		[Test]
		public void AddAnnotation_Duplicate_Throws()
		{
			var a = new Annotatable();
			a.AddAnnotation("k", 1);

			Action act = () => a.AddAnnotation("k", 2);
			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void SetAnnotation_Overwrites()
		{
			var a = new Annotatable();
			a.AddAnnotation("k", 1);

			a.SetAnnotation("k", 2);

			a["k"].ShouldBe(2);
		}

		[Test]
		public void RemoveAnnotation_RemovesExisting()
		{
			var a = new Annotatable();
			a.AddAnnotation("k", 1);

			var removed = a.RemoveAnnotation("k");

			removed!.Value.ShouldBe(1);
			a.FindAnnotation("k").ShouldBeNull();
		}

		[Test]
		public void RemoveAnnotation_Missing_ReturnsNull()
		{
			var a = new Annotatable();

			a.RemoveAnnotation("k").ShouldBeNull();
		}

		[Test]
		public void Indexer_SetNull_Removes()
		{
			var a = new Annotatable();
			a.AddAnnotation("k", 1);

			a["k"] = null;

			a.FindAnnotation("k").ShouldBeNull();
		}

		[Test]
		public void GetAnnotations_OrderedByOrdinalName()
		{
			var a = new Annotatable();
			a.AddAnnotation("b", 2);
			a.AddAnnotation("a", 1);
			a.AddAnnotation("c", 3);

			a.GetAnnotations().Select(x => x.Name).ShouldBe(new[] { "a", "b", "c" });
		}

		[Test]
		public void FindAnnotation_EmptyName_Throws()
		{
			var a = new Annotatable();

			Action act = () => a.FindAnnotation("");
			act.ShouldThrow<ArgumentException>();
		}

		[Test]
		public void GetAnnotation_Missing_Throws()
		{
			var a = new Annotatable();

			Action act = () => a.GetAnnotation("k");
			act.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void GetAnnotation_Present_ReturnsAnnotation()
		{
			var a = new Annotatable();
			a.AddAnnotation("k", 42);

			a.GetAnnotation("k").Value.ShouldBe(42);
		}

		[Test]
		public void ReadOnly_WritesThrow()
		{
			var a = new ReadOnlyToggle();
			a.AddAnnotation("k", 1);
			a.ReadOnly = true;

			Action setA = () => a.SetAnnotation("k", 2);
			Action rmA  = () => a.RemoveAnnotation("k");
			Action addA = () => a.AddAnnotation("k2", 1);

			setA.ShouldThrow<InvalidOperationException>();
			rmA .ShouldThrow<InvalidOperationException>();
			addA.ShouldThrow<InvalidOperationException>();
		}

		[Test]
		public void AddAnnotations_FromEnumerable_Copies()
		{
			var a = new Annotatable();
			var source = new[]
			{
				new Annotation("a", 1),
				new Annotation("b", 2),
			};

			a.AddAnnotations(source);

			a.GetAnnotations().Select(x => (x.Name, x.Value)).ShouldBe(new[] { ("a", (object?)1), ("b", 2) });
		}

		[Test]
		public void AnnotationsToDebugString_Empty_ReturnsEmpty()
		{
			IReadOnlyAnnotatable a = new Annotatable();

			a.AnnotationsToDebugString().ShouldBe("");
		}

		[Test]
		public void AnnotationsToDebugString_NonEmpty_ContainsNameAndValue()
		{
			var ann = new Annotatable();
			ann.AddAnnotation("k", 42);

			var text = ((IReadOnlyAnnotatable)ann).AnnotationsToDebugString();

			text.ShouldContain("k");
			text.ShouldContain("42");
		}

		sealed class RecordingAnnotatable : AnnotatableBase
		{
			public readonly List<(string Name, object? New, object? Old)> Calls = new();

			protected override Annotation? OnAnnotationSet(string name, Annotation? annotation, Annotation? oldAnnotation)
			{
				Calls.Add((name, annotation?.Value, oldAnnotation?.Value));
				return base.OnAnnotationSet(name, annotation, oldAnnotation);
			}
		}

		[Test]
		public void OnAnnotationSet_InvokedForAddSetAndRemove()
		{
			var a = new RecordingAnnotatable();

			a.AddAnnotation("k", 1);
			a.SetAnnotation("k", 2);
			a.RemoveAnnotation("k");

			a.Calls.ShouldBe(new[]
			{
				("k", (object?)1,  (object?)null),
				("k", (object?)2,  (object?)1),
				("k", (object?)null, (object?)2),
			});
		}

		[Test]
		public void SetAnnotation_SameValue_DoesNotInvokeCallback()
		{
			var a = new RecordingAnnotatable();

			a.AddAnnotation("k", 1);
			a.SetAnnotation("k", 1);

			a.Calls.Count.ShouldBe(1); // only the initial Add fires; no-op Set is short-circuited
		}

		[Test]
		public void CteAnnotationsContainer_Snapshots_SourceBag()
		{
			// Guards MAJ001/MIN002: the container must not share its annotation storage with
			// the caller's bag, otherwise post-construction mutations would silently diverge
			// from the already-computed cache key.
			var source = new Annotatable();
			source.SetAnnotation("k", 1);

			var container = new CteAnnotationsContainer("n", source.GetAnnotations());
			var hash1     = container.GetHashCode();

			source.SetAnnotation("k", 2);
			source.SetAnnotation("added-later", true);

			container.Annotations.FindAnnotation("k")!.Value.ShouldBe(1);
			container.Annotations.FindAnnotation("added-later").ShouldBeNull();
			container.GetHashCode().ShouldBe(hash1);
		}

		[Test]
		public void CteAnnotationsContainer_Equality_ByNameAndAnnotationsValues()
		{
			var a1 = new Annotatable();
			a1.SetAnnotation("k", 1);

			var a2 = new Annotatable();
			a2.SetAnnotation("k", 1);

			var c1 = new CteAnnotationsContainer("n", a1.GetAnnotations());
			var c2 = new CteAnnotationsContainer("n", a2.GetAnnotations());

			c1.Equals(c2).ShouldBeTrue();
			c1.GetHashCode().ShouldBe(c2.GetHashCode());

			var c3 = new CteAnnotationsContainer("n", new[] { new Annotation("k", 2) });
			c1.Equals(c3).ShouldBeFalse();
		}

		[Test]
		public void CteAnnotationsContainer_NullAnnotations_Allowed()
		{
			var container = new CteAnnotationsContainer("n", annotations: null);

			container.Name.ShouldBe("n");
			container.Annotations.GetAnnotations().ShouldBeEmpty();
		}
	}
}
