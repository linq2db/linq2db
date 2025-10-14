using System.Linq;

using LinqToDB;
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
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
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
		}

		[Test]
		public void CreatingQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var queryable = table.Where(t => t.Id == 1);
				var newQueryable = Internals.CreateExpressionQueryInstance<SampleClass>(db, queryable.Expression);

				newQueryable.ToArray();
			}
		}

	}
}
