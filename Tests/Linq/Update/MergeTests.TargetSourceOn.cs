using System.Linq;
using LinqToDB.Linq;
using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB;
	using LinqToDB.Mapping;

	// tests for target/source/match condition configuration methods, not covered by other tests
	public partial class MergeTests
	{
		[Table("DoesntMatter")]
		public class TableWithoutKey
		{
			[Column]
			public int Id { get; set; }
		}

		[Test]
		public void OnTargetKeyWithoutKeyFields([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = db.GetTable<TableWithoutKey>();

				var exception = Assert.Catch(
					() => table
					.MergeInto(table)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge())!;

				Assert.That(exception, Is.InstanceOf<LinqToDBException>());
				Assert.That(exception.Message, Is.EqualTo("Method OnTargetKey() needs at least one primary key column"));
			}
		}

		[Test]
		public void MergeInto([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.MergeInto(table)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void InsertPartialSourceProjection_KnownFieldsInDefaultSetter([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = GetSource1(db)
					.Select(s => new TestMapping1()
					{
						Id = s.Id,
						Field1 = s.Field1,
						Field2 = s.Field2,
						Field3 = s.Field3,
						Field4 = s.Field4
					})
					.MergeInto(table)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(2, rows, context);

				Assert.That(result, Has.Count.EqualTo(6));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
				AssertRow(InitialSourceData[2], result[4], null, null);
				AssertRow(InitialSourceData[3], result[5], null, 216);
			}
		}

		[Test]
		public void UsingTarget([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.UsingTarget()
					.OnTargetKey()
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = t.Field1 + s.Field2
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(4, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				Assert.Multiple(() =>
				{
					Assert.That(result[0].Id, Is.EqualTo(InitialTargetData[0].Id));
					Assert.That(result[0].Field1, Is.EqualTo(InitialTargetData[0].Field1 + InitialTargetData[0].Field2));
					Assert.That(result[0].Field2, Is.EqualTo(InitialTargetData[0].Field2));
					Assert.That(result[0].Field3, Is.Null);
					Assert.That(result[0].Field4, Is.Null);
					Assert.That(result[0].Field5, Is.Null);

					Assert.That(result[1].Id, Is.EqualTo(InitialTargetData[1].Id));
					Assert.That(result[1].Field1, Is.EqualTo(InitialTargetData[1].Field1 + InitialTargetData[1].Field2));
					Assert.That(result[1].Field2, Is.EqualTo(InitialTargetData[1].Field2));
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.Null);

					Assert.That(result[2].Id, Is.EqualTo(InitialTargetData[2].Id));
					Assert.That(result[2].Field1, Is.EqualTo(InitialTargetData[2].Field1 + InitialTargetData[2].Field2));
					Assert.That(result[2].Field2, Is.EqualTo(InitialTargetData[2].Field2));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.EqualTo(203));
					Assert.That(result[2].Field5, Is.Null);

					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1 + InitialTargetData[3].Field2));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnKeysSingleField([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(s => s.Field1 != null).Select(s => new TestMapping1()
					{
						Id = s.Id,
						Field1 = s.Field1 - 5,
						Field2 = s.Field2,
						Field3 = s.Field3,
						Field4 = s.Field4,
						Field5 = s.Field5
					}))
					.On(t => t.Field1, s => s.Field1)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field2 = 123
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(123));
					Assert.That(result[3].Field3, Is.EqualTo(InitialTargetData[3].Field3));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnKeysPartialSourceProjection_KnownFieldInKeySelector([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(s => s.Field1 != null).Select(s => new TestMapping1()
					{
						Field1 = s.Field1 - 5,
						Field2 = s.Field2
					}))
					.On(t => t.Field1, s => s.Field1)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field2 = 123
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(123));
					Assert.That(result[3].Field3, Is.EqualTo(InitialTargetData[3].Field3));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnKeysMultipleFields([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(s => s.Field1 != null && s.Field2 != null).Select(s => new TestMapping1()
					{
						Id = s.Id,
						Field1 = s.Field1,
						Field2 = s.Field2 - 1,
						Field3 = s.Field3,
						Field4 = s.Field4,
						Field5 = s.Field5
					}))
					.On(t => new { t.Field1, t.Field2 }, s => new { s.Field1, s.Field2 })
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field3 = 123
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.EqualTo(123));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		sealed class Key
		{
			public int? fkey1;
			public int? fkey2;
		}

		[Test]
		public void OnKeysMemberInitFields([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(s => s.Field1 != null && s.Field2 != null).Select(s => new TestMapping1()
					{
						Id = s.Id,
						Field1 = s.Field1,
						Field2 = s.Field2 - 1,
						Field3 = s.Field3,
						Field4 = s.Field4,
						Field5 = s.Field5
					}))
					.On(t => new Key { fkey1 = t.Field1, fkey2 = t.Field2 }, s => new Key { fkey2 = s.Field2, fkey1 = s.Field1 })
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field3 = 123
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.EqualTo(123));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnKeysFieldAndConstant([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(s => s.Field1 != null && s.Field2 != null))
					.On(t => new { t.Field1, t.Field2 }, s => new { s.Field1, Field2 = (int?)6 })
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field3 = 321
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(1, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(InitialTargetData[3].Id));
					Assert.That(result[3].Field1, Is.EqualTo(InitialTargetData[3].Field1));
					Assert.That(result[3].Field2, Is.EqualTo(InitialTargetData[3].Field2));
					Assert.That(result[3].Field3, Is.EqualTo(321));
					Assert.That(result[3].Field4, Is.EqualTo(InitialTargetData[3].Field4));
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnKeysFieldAndConstantPartialSourceProjection_UnknownFieldInKey([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var exception = Assert.Catch(
					() => table
						.Merge()
						.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
						.On(t => new { t.Field1, t.Field2 }, s => new { Field1 = s.Field1, s.Field2 })
						.UpdateWhenMatched((t, s) => new TestMapping1()
						{
							Field3 = 321
						})
						.Merge())!;

				Assert.That(exception, Is.InstanceOf<LinqException>());

				Assert.That(exception.Message,  Does.EndWith("'s.Field2' could not be converted to SQL."));
			}
		}
	}
}
