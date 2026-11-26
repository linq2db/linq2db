using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class InternalsTests : TestBase
	{
		[Table]
		sealed class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test]
		public void ExtractingDataContext([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();
			var extracted = Internals.GetDataContext(table);
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(table.Where(t => t.Id == 1));
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(db.GetTable<SampleClass>());
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(db.GetTable<SampleClass>());
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(table.Set(t => t.Value, 1));
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(table.Value(t => t.Value, 1));
			Assert.That(extracted, Is.EqualTo(db));

			extracted = Internals.GetDataContext(table.Into(db.GetTable<SampleClass>()).Value(t => t.Value, () => 1));
			Assert.That(extracted, Is.EqualTo(db));
		}

		[Test]
		public void CreatingQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<SampleClass>();
			var queryable = table.Where(t => t.Id == 1);
			var newQueryable = Internals.CreateExpressionQueryInstance<SampleClass>(db, queryable.Expression);

			newQueryable.ToArray();
		}

		// not mixing props and fields because GetMembers()
		// will return all props (derived first) and then all fields (derived first)
		public class MemberOrderingBase
		{
			public int Member1;
			public int Member2;
			public int Member3;
		}

		public class MemberOrderingDerived : MemberOrderingBase
		{
			public int MemberDerived1;
			public new int Member2;
			public int MemberDerived3;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3959")]
		public void PublicInstanceValueMembersOrderBaseThenDerived()
		{
			var type = typeof(MemberOrderingDerived);

			var members = type.GetPublicInstanceValueMembers();

			//Console.WriteLine("Members: {0}", string.Join(", ", members.Select(m => m.Name)));

			// Test returned array
			Assert.That(members, Is.Not.Null);
			Assert.That(members, Has.Length.EqualTo(5), $"Expected 5 returned members, found {members.Length}.");

			var expected = String.Join(", ", new []
			{ 
				$"{nameof(MemberOrderingBase)}.{nameof(MemberOrderingBase.Member1)}",
				$"{nameof(MemberOrderingBase)}.{nameof(MemberOrderingBase.Member3)}",
				$"{nameof(MemberOrderingDerived)}.{nameof(MemberOrderingDerived.MemberDerived1)}",
				$"{nameof(MemberOrderingDerived)}.{nameof(MemberOrderingDerived.Member2)}",
				$"{nameof(MemberOrderingDerived)}.{nameof(MemberOrderingDerived.MemberDerived3)}",
			});

			var actual = String.Join(", ", members.Select(m => $"{m.DeclaringType!.Name}.{m.Name}"));

			Assert.That(actual, Is.EqualTo(expected));
		}
	}
}
