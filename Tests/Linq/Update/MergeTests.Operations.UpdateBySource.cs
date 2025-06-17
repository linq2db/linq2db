using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void SameSourceUpdateBySource([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySource(t => new TestMapping1()
					{
						Id = t.Id * 12,
						Field1 = t.Field1 * 2,
						Field2 = t.Id * 13,
						Field3 = t.Field2 * 2,
						Field4 = t.Field4 * 2,
						Field5 = t.Field5 * 2
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[2].Id, Is.EqualTo(12));
					Assert.That(result[2].Field1, Is.Null);
					Assert.That(result[2].Field2, Is.EqualTo(13));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.Null);
					Assert.That(result[2].Field5, Is.Null);

					Assert.That(result[3].Id, Is.EqualTo(24));
					Assert.That(result[3].Field1, Is.EqualTo(4));
					Assert.That(result[3].Field2, Is.EqualTo(26));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void SameSourceUpdateBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == 1,
						t => new TestMapping1()
						{
							Id = 123,
							Field1 = t.Id * 11,
							Field2 = t.Field2 + t.Field4,
							Field3 = t.Field3 + t.Field3,
							Field4 = t.Field4 + t.Field2,
							Field5 = t.Field5 + t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[1], result[0], null, null);
				AssertRow(InitialTargetData[2], result[1], null, 203);
				AssertRow(InitialTargetData[3], result[2], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(123));
					Assert.That(result[3].Field1, Is.EqualTo(11));
					Assert.That(result[3].Field2, Is.Null);
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void OnConditionPartialSourceProjection_KnownFieldInCondition([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(s => new TestMapping2() { OtherId = s.OtherId }))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenNotMatchedBySource(t => new TestMapping1()
					{
						Id = t.Id + 10,
						Field1 = t.Field1 + t.Field2 + t.Field3,
						Field2 = t.Id * 10,
						Field3 = t.Field2 + t.Field1,
						Field4 = t.Field2,
						Field5 = t.Field1
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(2));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[2], result[0], null, 203);
				AssertRow(InitialTargetData[3], result[1], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[2].Id, Is.EqualTo(11));
					Assert.That(result[2].Field1, Is.Null);
					Assert.That(result[2].Field2, Is.EqualTo(10));
					Assert.That(result[2].Field3, Is.Null);
					Assert.That(result[2].Field4, Is.Null);
					Assert.That(result[2].Field5, Is.Null);

					Assert.That(result[3].Id, Is.EqualTo(12));
					Assert.That(result[3].Field1, Is.Null);
					Assert.That(result[3].Field2, Is.EqualTo(20));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.EqualTo(2));
				});
			}
		}

		[Test]
		public void OtherSourceUpdateBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db))
					.On((t, s) => t.Id == s.OtherId)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.Null);
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.EqualTo(2));
				});

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(4));
					Assert.That(result[3].Field1, Is.EqualTo(5));
					Assert.That(result[3].Field2, Is.EqualTo(6));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void AnonymousSourceUpdateBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.Key)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.Null);
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.EqualTo(2));
				});

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(4));
					Assert.That(result[3].Field1, Is.EqualTo(5));
					Assert.That(result[3].Field2, Is.EqualTo(6));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void AnonymousListSourceUpdateBySourceWithPredicate([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().Select(_ => new
					{
						Key = _.OtherId,
						Field01 = _.OtherField1,
						Field02 = _.OtherField2,
						Field03 = _.OtherField3,
						Field04 = _.OtherField4,
						Field05 = _.OtherField5,
					}))
					.On((t, s) => t.Id == s.Key)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.Null);
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.EqualTo(2));
				});

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(4));
					Assert.That(result[3].Field1, Is.EqualTo(5));
					Assert.That(result[3].Field2, Is.EqualTo(6));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void UpdateBySourceReservedAndCaseNames([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).Select(_ => new
					{
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}))
					.On((t, s) => t.Id == s.From)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.Null);
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.EqualTo(2));
				});

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(4));
					Assert.That(result[3].Field1, Is.EqualTo(5));
					Assert.That(result[3].Field2, Is.EqualTo(6));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}

		[Test]
		public void UpdateBySourceReservedAndCaseNamesFromList([MergeNotMatchedBySourceDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db).ToList().OrderBy(s => s.OtherId).Select(_ => new
					{
						From = _.OtherId,
						Order = _.OtherField1,
						Field = _.OtherField2,
						field = _.OtherField3,
						Select = _.OtherField4,
						Delete = _.OtherField5
					}))
					.On((t, s) => t.Id == s.From)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Field1 == 2,
						t => new TestMapping1()
						{
							Id = t.Id,
							Field1 = t.Field5,
							Field2 = t.Field4,
							Field3 = t.Field3,
							Field4 = t.Field2,
							Field5 = t.Field1
						})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(rows, Is.EqualTo(1));

					Assert.That(result, Has.Count.EqualTo(4));
				});

				AssertRow(InitialTargetData[0], result[0], null, null);

				Assert.Multiple(() =>
				{
					Assert.That(result[1].Id, Is.EqualTo(2));
					Assert.That(result[1].Field1, Is.Null);
					Assert.That(result[1].Field2, Is.Null);
					Assert.That(result[1].Field3, Is.Null);
					Assert.That(result[1].Field4, Is.Null);
					Assert.That(result[1].Field5, Is.EqualTo(2));
				});

				AssertRow(InitialTargetData[2], result[2], null, 203);

				Assert.Multiple(() =>
				{
					Assert.That(result[3].Id, Is.EqualTo(4));
					Assert.That(result[3].Field1, Is.EqualTo(5));
					Assert.That(result[3].Field2, Is.EqualTo(6));
					Assert.That(result[3].Field3, Is.Null);
					Assert.That(result[3].Field4, Is.Null);
					Assert.That(result[3].Field5, Is.Null);
				});
			}
		}
	}
}
