using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Infrastructure
{
	/// <summary>
	/// The default comparer <c>AssertQuery</c> and <c>AreEqualWithComparer</c> fall back to when no comparer is passed.
	/// A failed comparison is observed through NUnit's <c>Assert.Throws</c>: it runs the delegate in an isolated
	/// context, so the recorded assertion failure does not fail the enclosing test. Shouldly's <c>Should.Throw</c> does not.
	/// </summary>
	[TestFixture]
	public class AssertQueryTests : TestBase
	{
		[Sql.Expression("{0} + 1", ServerSideOnly = true)]
		static int Shifted(int value) => value;

		[Test]
		public void ScalarQuery_WrongValues_Fails([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
				.Select(p => Shifted(p.ID));

			Assert.Throws<AssertionException>(() => AssertQuery(query));
		}

		[Test]
		public void ScalarUnion_KeepsDistinctRows([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query = db.Person
				.Select(p => p.ID)
				.Union(db.Person.Select(p => p.ID + 100));

			AssertQuery(query);
		}

		[Test]
		public void Scalars_DifferentValues_Fail()
		{
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { 1 }, new[] { 2 }));
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { Guid.NewGuid() }, new[] { Guid.NewGuid() }));
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { true }, new[] { false }));
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { Gender.Male }, new[] { Gender.Female }));
		}

		[Test]
		public void String_SameLengthDifferentValue_Fails()
		{
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { "ab" }, new[] { "cd" }));
		}

		[Test]
		public void NullableScalar_ComparesByValue()
		{
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new int?[] { 1, null }, new int?[] { 2, null }));

			AreEqualWithComparer(new int?[] { 1, null }, new int?[] { null, 1 });
		}

		[Test]
		public void Scalars_EqualValues_Pass()
		{
			AreEqualWithComparer(new[] { 1, 2 }, new[] { 2, 1 });
			AreEqualWithComparer(new[] { "a" }, new[] { "a" });
		}

		[Test]
		public void Decimal_ComparesByValue()
		{
			AreEqualWithComparer(new[] { 2.5m }, new[] { 2.50m });
			AreEqualWithComparer(new decimal?[] { 2.5m, null }, new decimal?[] { 2.50m, null });

			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { 2.5m }, new[] { 2.6m }));
		}

		[Test]
		public void Collections_CompareElementWise()
		{
			AreEqualWithComparer(new[] { new List<int> { 1, 2 } }, new[] { new List<int> { 1, 2 } });
			AreEqualWithComparer(new[] { new byte[] { 1, 2 } }, new[] { new byte[] { 1, 2 } });

			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { new byte[] { 1, 2 } }, new[] { new byte[] { 1, 3 } }));
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { new[] { 1, 2 } }, new[] { new[] { 2, 1 } }));
		}

		[Test]
		public void SequenceInterface_ComparesElementWise()
		{
			AreEqualWithComparer(new IEnumerable<int>[] { new[] { 1, 2 } }, new IEnumerable<int>[] { new List<int> { 1, 2 } });

			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new IEnumerable<int>[] { new[] { 1, 2 } }, new IEnumerable<int>[] { new[] { 1, 3 } }));
		}

		sealed class Node(params Node[] children) : IEnumerable<Node>
		{
			public int Size => children.Length;

			public IEnumerator<Node> GetEnumerator() => ((IEnumerable<Node>)children).GetEnumerator();

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
		}

		[Test]
		public void SelfReferentialSequence_ComparesElementWise()
		{
			AreEqualWithComparer(new[] { new Node(new Node()) }, new[] { new Node(new Node()) });

			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { new Node(new Node()) }, new[] { new Node(new Node(), new Node()) }));
		}

		[Test]
		public void Entity_ComparesMembers()
		{
			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { new Person(1, "John") }, new[] { new Person(1, "Jane") }));

			AreEqualWithComparer(new[] { new Person(1, "John") }, new[] { new Person(1, "John") });
		}

		[Test]
		public void DateTime_KeepsMemberComparison()
		{
			var utc   = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
			var local = DateTime.SpecifyKind(utc, DateTimeKind.Local);

			Assert.Throws<AssertionException>(() => AreEqualWithComparer(new[] { utc }, new[] { local }));

			AreEqualWithComparer(new DateTime?[] { utc }, new DateTime?[] { local });
		}
	}
}
