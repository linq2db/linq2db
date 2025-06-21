using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void SameSourceDeleteBySource([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySource()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(2));
				}

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);
			}
		}

		[Test]
		public void SameSourceDeleteBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 1)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void OtherSourceDeleteBySource([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id && t.Id == 3)
					.DeleteWhenNotMatchedBySource()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(3));

					Assert.That(result, Has.Count.EqualTo(1));
				}

				AssertRow(InitialTargetData[2], result[0], null, 203);
			}
		}

		[Test]
		public void OtherSourceDeleteBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => s.OtherId == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void AnonymousSourceDeleteBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						Key = _.OtherId
					}))
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void AnonymousListSourceDeleteBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						Key = _.OtherId
					}))
					.On((t, s) => s.Key == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void DeleteBySourceReservedAndCaseNames([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						select = _.OtherId,
						Select = _.OtherField1
					}))
					.On((t, s) => s.select == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void DeleteBySourceReservedAndCaseNamesFromList([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						INSERT = _.OtherId,
						insert = _.OtherField2
					}))
					.On((t, s) => s.INSERT == t.Id)
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 2)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}

		[Test]
		public void DeleteBySourceFromPartialSourceProjection([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new TestMapping1() { Id = _.Id, Field1 = _.Field1 }))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == 1)
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(3));
				}

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);
			}
		}
	}
}
