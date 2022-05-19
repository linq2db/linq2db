using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using LinqToDB.Tools.Comparers;
using LinqToDB.Common;

namespace Tests.xUpdate;

using Model;

[TestFixture]
public class InsertWithOutputTests : TestBase
{
	private const string FeatureInsertOutputSingle     = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLiteClassic}";
	private const string FeatureInsertOutputMultiple   = $"{TestProvName.AllSqlServer},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLiteClassic}";
	private const string FeatureInsertOutputWithSchema = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird},{TestProvName.AllMariaDB},{TestProvName.AllSQLiteClassic}";
	private const string FeatureInsertOutputInto       = TestProvName.AllSqlServer;

	[Table]
	class TableWithData
	{
		[Column]              public int     Id       { get; set; }
		[Column]              public int     Value    { get; set; }
		[Column(Length = 50)] public string? ValueStr { get; set; }
	}

	[Table(Schema = "TestSchema")]
	class TableWithDataAndSchema
	{
		[Column]              public int     Id       { get; set; }
		[Column]              public int     Value    { get; set; }
		[Column(Length = 50)] public string? ValueStr { get; set; }
	}

	[Table]
	class DestinationTable
	{
		[Column]              public int     Id       { get; set; }
		[Column]              public int     Value    { get; set; }
		[Column(Length = 50)] public string? ValueStr { get; set; }
	}

	static TableWithData[] GetSourceData()
	{
		return Enumerable.Range(1, 10).Select(i =>
				new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
			.ToArray();
	}

	[Test]
	public void InsertWithOutputProjectionFromQueryTest([IncludeDataSources(true, FeatureInsertOutputMultiple)] string context, [Values(100, 200)] int param)
	{
		var sourceData    = GetSourceData();
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable(sourceData))
		using (var target = db.CreateLocalTable<DestinationTable>())
		{
			var output = source
				.Where(s => s.Id > 3)
				.InsertWithOutput(
					target,
					s => new DestinationTable
					{
						Id       = s.Id + 100 + param,
						Value    = s.Value + 100,
						ValueStr = s.ValueStr + 100
					},
					inserted => new
					{
						Id       = Sql.AsSql(inserted.Id + 1),
						ValueStr = Sql.AsSql(inserted.ValueStr + 1),
					}).ToArray();

			var zz = target.ToArray();

			AreEqual(target.Select(t => new
				{
					Id       = t.Id + 1,
					ValueStr = t.ValueStr + 1,
				}),
				output);
		}
	}

	[Test]
	public void InsertWithOutputFromQueryTest([IncludeDataSources(true, FeatureInsertOutputMultiple)] string context, [Values(100, 200)] int param)
	{
		var sourceData    = GetSourceData();
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable(sourceData))
		using (var target = db.CreateLocalTable<DestinationTable>())
		{
			var output = source
				.Where(s => s.Id > 3)
				.InsertWithOutput(
					target,
					s => new DestinationTable
					{
						Id       = s.Id + param,
						Value    = s.Value + param,
						ValueStr = s.ValueStr + param
					})
				.ToArray();

			AreEqual(source.Where(s => s.Id > 3).Select(s => new DestinationTable
				{
					Id       = s.Id + param,
					Value    = s.Value + param,
					ValueStr = s.ValueStr + param,
				}),
				output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}
	}

	[Test]
	public void InsertWithOutputFromQueryTestSingleRecord([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(100, 200)] int param)
	{
		var sourceData    = GetSourceData();
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable(sourceData))
		using (var target = db.CreateLocalTable<DestinationTable>())
		{
			var output = source
				.Where(s => s.Id == 3)
				.InsertWithOutput(
					target,
					s => new DestinationTable
					{
						Id       = s.Id + param,
						Value    = s.Value + param,
						ValueStr = s.ValueStr + param
					})
				.ToArray();

			AreEqual(source.Where(s => s.Id == 3).Select(s => new DestinationTable
				{
					Id       = s.Id + param,
					Value    = s.Value + param,
					ValueStr = s.ValueStr + param,
				}),
				output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}
	}

	[Test]
	public async Task InsertWithOutputFromQueryTestAsync([IncludeDataSources(true, FeatureInsertOutputMultiple)] string context, [Values(100, 200)] int param)
	{
		var sourceData    = GetSourceData();
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable(sourceData))
		using (var target = db.CreateLocalTable<DestinationTable>())
		{
			var output = await source
				.Where(s => s.Id > 3)
				.InsertWithOutputAsync(
					target,
					s => new DestinationTable
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param
					});

			AreEqual(source.Where(s => s.Id > 3).Select(s => new DestinationTable
			{
				Id       = s.Id       + param,
				Value    = s.Value    + param,
				ValueStr = s.ValueStr + param,
			}),
				output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}
	}

	[Test]
	public async Task InsertWithOutputFromQueryTestAsyncSingleRecord([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(100, 200)] int param)
	{
		var sourceData    = GetSourceData();
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable(sourceData))
		using (var target = db.CreateLocalTable<DestinationTable>())
		{
			var output = await source
				.Where(s => s.Id == 3)
				.InsertWithOutputAsync(
					target,
					s => new DestinationTable
					{
						Id       = s.Id       + param,
						Value    = s.Value    + param,
						ValueStr = s.ValueStr + param
					});

			AreEqual(source.Where(s => s.Id == 3).Select(s => new DestinationTable
			{
				Id       = s.Id       + param,
				Value    = s.Value    + param,
				ValueStr = s.ValueStr + param,
			}),
				output, ComparerBuilder.GetEqualityComparer<DestinationTable>());
		}
	}

	[Test]
	public void InsertWithOutputTest3([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				var output = db.Child
					.Where(c => c.ChildID == 11)
					.InsertWithOutput(
						db.Child,
						c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = id
						},
						inserted => new
						{
							ID = inserted.ChildID + inserted.ParentID + param
						})
					.ToArray();

				AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new
					{
						ID = c.ChildID + c.ParentID + param
					}),
					output);
			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputTest4([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				var output = db.Child
					.Where(c => c.ChildID == 11)
					.InsertWithOutput(
						db.Child,
						c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = id
						},
						inserted => Sql.AsSql(inserted.ChildID + inserted.ParentID + param))
					.ToArray();

				AreEqual(
					db.Child.Where(c => c.ChildID > idsLimit)
						.Select(c => c.ChildID + c.ParentID + param),
					output);
			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputObjTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
	{
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithData>())
		{
			var data = new TableWithData
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = source.InsertWithOutput(data);

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}

	[Test]
	public async Task InsertWithOutputObjAsyncTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
	{
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithData>())
		{
			var data = new TableWithData
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = await source.InsertWithOutputAsync(data);

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}

	[Test]
	public void InsertWithOutputObjWithSetterTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
	{
		using (var db = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithData>())
		{
			Expression<Func<TableWithData>> dataFunc = () => new TableWithData
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = source.InsertWithOutput(dataFunc);
			var data   = dataFunc.CompileExpression()();

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}

	[Test]
	public async Task InsertWithOutputObjWithSetterAsyncTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
	{
		using (var db = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithData>())
		{
			Expression<Func<TableWithData>> dataFunc = () => new TableWithData
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = await source.InsertWithOutputAsync(dataFunc);
			var data = dataFunc.CompileExpression()();

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}

	[Test]
	public void InsertWithOutputDynamicWithSetterTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
	{
		using (var db = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithData>())
		{
			Expression<Func<TableWithData>> dataFunc = () => new TableWithData
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = source.InsertWithOutput(dataFunc,
				inserted => new { inserted.Id, inserted.Value, inserted.ValueStr });
			var data = dataFunc.CompileExpression()();

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}

	[Test]
	public void InsertWithOutputIntoTest1([IncludeDataSources(false, FeatureInsertOutputInto)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using (var t = CreateTempTable<Child>(db, "TInserted", context))
				{
					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(db.Child, c => new Child
								{
									ParentID = c.ParentID,
									ChildID  = id
								},
								t.Table,
								inserted =>
									new Child
									{
										ChildID  = inserted.ChildID,
										ParentID = inserted.ParentID + param
									}
							);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Table.Select(c => new Child
							{
								ParentID = c.ParentID - param,
								ChildID  = c.ChildID
							}
						)
					);
				}

			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputIntoTest2([IncludeDataSources(false, FeatureInsertOutputInto)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using (var t = CreateTempTable<Child>(db, "TInserted", context))
				{

					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(db.Child, c => new Child
								{
									ParentID = c.ParentID,
									ChildID  = id + Sql.AsSql(param)
								},
								t.Table);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Table.Select(c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = c.ChildID
							}
						)
					);
				}
			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputIntoTest3([IncludeDataSources(true, FeatureInsertOutputInto)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using (var t = db.CreateLocalTable<Child>("TInserted"))
				{
					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(db.Child, c => new Child
								{
									ParentID = c.ParentID,
									ChildID  = id
								},
								t,
								inserted =>
									new Child
									{
										ChildID  = inserted.ChildID,
										ParentID = inserted.ParentID + param
									}
							);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Select(c => new Child
							{
								ParentID = c.ParentID - param,
								ChildID  = c.ChildID
							}
						)
					);
				}

			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputIntoTest4([IncludeDataSources(true, FeatureInsertOutputInto)] string context, [Values(100, 200)] int param)
	{
		using (var db = GetDataContext(context))
		{
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using (var t = db.CreateLocalTable<Child>("TInserted"))
				{

					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(db.Child, c => new Child
								{
									ParentID = c.ParentID,
									ChildID  = id + Sql.AsSql(param)
								},
								t);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Select(c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = c.ChildID
							}
						)
					);
				}
			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}
	}

	[Test]
	public void InsertWithOutputWithSchema([IncludeDataSources(true, FeatureInsertOutputWithSchema)] string context, [Values(1, 2)] int value)
	{
		using (var db     = GetDataContext(context))
		using (var source = db.CreateLocalTable<TableWithDataAndSchema>())
		{
			var data = new TableWithDataAndSchema()
			{
				Value    = value * 100,
				Id       = value,
				ValueStr = "SomeStr" + value
			};

			var output = source.InsertWithOutput(data);

			Assert.AreEqual(data.Id,       output.Id);
			Assert.AreEqual(data.Value,    output.Value);
			Assert.AreEqual(data.ValueStr, output.ValueStr);
		}
	}
}
