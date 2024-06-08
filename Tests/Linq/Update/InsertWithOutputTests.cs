using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using LinqToDB.Tools.Comparers;
using LinqToDB.Common;

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public class InsertWithOutputTests : TestBase
	{
		private const string FeatureInsertOutputSingle     = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputMultiple   = $"{TestProvName.AllSqlServer},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputWithSchema = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird},{TestProvName.AllMariaDB},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputInto       = $"{TestProvName.AllSqlServer}";

		[Table]
		sealed class TableWithData
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		sealed class TableWithDataAndSchema
		{
			[Column]              public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		sealed class DestinationTable
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
		public void InsertIValueInsertableWithOutputObjTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
		{
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable<TableWithData>())
			{
				var data = new TableWithData
				{
					Value    = value * 100,
					Id       = value,
					ValueStr = "SomeStr" + value
				};

				var output = source
					.Value(a => a.Value, value * 100)
					.Value(a => a.Id, value)
					.Value(a => a.ValueStr, "SomeStr" + value)
					.InsertWithOutput();

				Assert.AreEqual(data.Id, output.Id);
				Assert.AreEqual(data.Value, output.Value);
				Assert.AreEqual(data.ValueStr, output.ValueStr);
			}
		}

		[Test]
		public async Task InsertIValueInsertableWithOutputObjAsyncTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
		{
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable<TableWithData>())
			{
				var data = new TableWithData
				{
					Value    = value * 100,
					Id       = value,
					ValueStr = "SomeStr" + value
				};

				var output = await source
					.Value(a => a.Value, value * 100)
					.Value(a => a.Id, value)
					.Value(a => a.ValueStr, "SomeStr" + value)
					.InsertWithOutputAsync();

				Assert.AreEqual(data.Id, output.Id);
				Assert.AreEqual(data.Value, output.Value);
				Assert.AreEqual(data.ValueStr, output.ValueStr);
			}
		}

		[Test]
		public void InsertIValueInsertableWithOutputObjWithSetterTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
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

				var output = source
					.Value(a => a.Value, () => value * 100)
					.Value(a => a.Id, () => value)
					.Value(a => a.ValueStr, () => "SomeStr" + value)
					.InsertWithOutput();
				var data   = dataFunc.CompileExpression()();

				Assert.AreEqual(data.Id, output.Id);
				Assert.AreEqual(data.Value, output.Value);
				Assert.AreEqual(data.ValueStr, output.ValueStr);
			}
		}

		[Test]
		public async Task InsertIValueInsertableWithOutputObjWithSetterAsyncTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
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

				var output = await source
					.Value(a => a.Value, () => value * 100)
					.Value(a => a.Id, () => value)
					.Value(a => a.ValueStr, () => "SomeStr" + value)
					.InsertWithOutputAsync();
				var data = dataFunc.CompileExpression()();

				Assert.AreEqual(data.Id, output.Id);
				Assert.AreEqual(data.Value, output.Value);
				Assert.AreEqual(data.ValueStr, output.ValueStr);
			}
		}

		[Test]
		public void InsertIValueInsertableWithOutputDynamicWithSetterTest([IncludeDataSources(true, FeatureInsertOutputSingle)] string context, [Values(1, 2)] int value)
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

				var output = source
					.Value(a => a.Value, () => value * 100)
					.Value(a => a.Id, () => value)
					.Value(a => a.ValueStr, () => "SomeStr" + value)
					.InsertWithOutput(inserted => new { inserted.Id, inserted.Value, inserted.ValueStr });
				var data = dataFunc.CompileExpression()();

				Assert.AreEqual(data.Id, output.Id);
				Assert.AreEqual(data.Value, output.Value);
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
		public void InsertWithOutputIntoTempTable([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using (var db = GetDataContext(context))
			{
				const int idsLimit = 1000;

				try
				{
					var id = idsLimit + 1;

					db.Child.Delete(c => c.ChildID > idsLimit);

					var param = 10050;
					using var t = db.CreateLocalTable<Child>("TInserted", tableOptions: TableOptions.IsTemporary);
					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(
								db.Child,
								c => new Child()
								{
									ParentID = c.ParentID,
									ChildID  = id + Sql.AsSql(param)
								},
								t);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit)
						.Select(
						c => new Child()
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Select(c => new Child()
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}
						)
					);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test]
		public void InsertWithSetterWithOutputIntoTempTableByTableName([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using (var db = GetDataContext(context))
			{
				const int idsLimit = 1000;

				try
				{
					var id = idsLimit + 1;

					db.Child.Delete(c => c.ChildID > idsLimit);

					var param = 10050;
					using var t = db.CreateTempTable<Child>("TInserted");
					var tRef = db.GetTable<Child>()
						.TableOptions(TableOptions.IsTemporary)
						.TableName(t.TableName);
					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutputInto(
								db.Child,
								c => new Child()
								{
									ParentID = c.ParentID,
									ChildID  = id + Sql.AsSql(param)
								},
								tRef);

					Assert.AreEqual(1, output);

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit)
						.Select(
						c => new Child()
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}),
						t.Select(c => new Child()
						{
							ParentID = c.ParentID,
							ChildID  = c.ChildID
						}
						)
					);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}
		
		[Test]
		public async Task InsertWithSetterWithOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db = GetDataContext(context);
			const int idsLimit = 1000;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				var param = 10050;
				using var t = db.CreateTempTable<Child>("TInserted");
				var tRef = db.GetTable<Child>()
					.TableOptions(TableOptions.IsTemporary)
					.TableName(t.TableName);
				var output = await
					db.Child
						.Where(c => c.ChildID == 11)
						.InsertWithOutputIntoAsync(
							db.Child,
							c => new Child()
							{
								ParentID = c.ParentID,
								ChildID  = id + Sql.AsSql(param)
							},
							tRef);

				Assert.AreEqual(1, output);

				AreEqual(db.Child.Where(c => c.ChildID > idsLimit)
					.Select(
					c => new Child()
					{
						ParentID = c.ParentID,
						ChildID  = c.ChildID
					}),
					t.Select(c => new Child()
					{
						ParentID = c.ParentID,
						ChildID  = c.ChildID
					}
					)
				);
			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}

		[Test]
		public void InsertSingleRowOutputIntoTempTableByTableName([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable<TableWithData>("TableWithData_source");
			using var output = db.CreateTempTable<TableWithData>("TableWithData_output");
			var outputRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			Expression<Func<TableWithData>> setter = () => new TableWithData
			{
				Value    = 42123,
				Id       = 42,
				ValueStr = "SomeStr"
			};

			var rowCount = source.InsertWithOutputInto(setter, outputRef);
			Assert.AreEqual(1, rowCount);

			var sourceData = source.ToArray();
			Assert.AreEqual(1, sourceData.Length);
			Assert.AreEqual(42, sourceData[0].Id);
			Assert.AreEqual(42123, sourceData[0].Value);
			Assert.AreEqual("SomeStr", sourceData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(42, outputData[0].Id);
			Assert.AreEqual(42123, outputData[0].Value);
			Assert.AreEqual("SomeStr", outputData[0].ValueStr);
		}
		
		[Test]
		public async Task InsertSingleRowOutputIntoTempTableByTableNameAsync([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable<TableWithData>("TableWithData_source");
			using var output = db.CreateTempTable<TableWithData>("TableWithData_output");
			var outputRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			Expression<Func<TableWithData>> setter = () => new TableWithData
			{
				Value    = 42123,
				Id       = 42,
				ValueStr = "SomeStr"
			};

			var rowCount = await source.InsertWithOutputIntoAsync(setter, outputRef);
			Assert.AreEqual(1, rowCount);

			var sourceData = source.ToArray();
			Assert.AreEqual(1, sourceData.Length);
			Assert.AreEqual(42, sourceData[0].Id);
			Assert.AreEqual(42123, sourceData[0].Value);
			Assert.AreEqual("SomeStr", sourceData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(42, outputData[0].Id);
			Assert.AreEqual(42123, outputData[0].Value);
			Assert.AreEqual("SomeStr", outputData[0].ValueStr);
		}

		[Test]
		public void InsertSingleRowOutputIntoProjTempTableByTableName([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable<TableWithData>("TableWithData_source");
			using var output = db.CreateTempTable<DestinationTable>("DestinationTable_output");
			var outputRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			Expression<Func<TableWithData>> setter = () => new TableWithData
			{
				Value    = 42123,
				Id       = 42,
				ValueStr = "SomeStr"
			};

			var rowCount = source.InsertWithOutputInto(setter, outputRef, v => new DestinationTable
			{
				Value = v.Value * 2,
				Id = v.Id + 1,
				ValueStr = "Foo" + v.ValueStr
			});
			Assert.AreEqual(1, rowCount);

			var sourceData = source.ToArray();
			Assert.AreEqual(1, sourceData.Length);
			Assert.AreEqual(42, sourceData[0].Id);
			Assert.AreEqual(42123, sourceData[0].Value);
			Assert.AreEqual("SomeStr", sourceData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(43, outputData[0].Id);
			Assert.AreEqual(84246, outputData[0].Value);
			Assert.AreEqual("FooSomeStr", outputData[0].ValueStr);
		}

		[Test]
		public async Task InsertSingleRowOutputIntoProjTempTableByTableNameAsync([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var source = db.CreateLocalTable<TableWithData>("TableWithData_source");
			using var output = db.CreateTempTable<DestinationTable>("DestinationTable_output");
			var outputRef = db.GetTable<DestinationTable>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			Expression<Func<TableWithData>> setter = () => new TableWithData
			{
				Value    = 42123,
				Id       = 42,
				ValueStr = "SomeStr"
			};

			var rowCount = await source.InsertWithOutputIntoAsync(setter, outputRef, v => new DestinationTable
			{
				Value = v.Value * 2,
				Id = v.Id + 1,
				ValueStr = "Foo" + v.ValueStr
			});
			Assert.AreEqual(1, rowCount);

			var sourceData = source.ToArray();
			Assert.AreEqual(1, sourceData.Length);
			Assert.AreEqual(42, sourceData[0].Id);
			Assert.AreEqual(42123, sourceData[0].Value);
			Assert.AreEqual("SomeStr", sourceData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(43, outputData[0].Id);
			Assert.AreEqual(84246, outputData[0].Value);
			Assert.AreEqual("FooSomeStr", outputData[0].ValueStr);
		}

		[Test]
		public void InsertWithSetterWithOutputIntoProjTempTableByTableName([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db = GetDataContext(context);
			const int idsLimit = 1000;
			const int param = 4242;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using var t = db.CreateTempTable<Child>("TInserted");
				var tRef = db.GetTable<Child>()
					.TableOptions(TableOptions.IsTemporary)
					.TableName(t.TableName);

				var output =
					db.Child
						.Where(c => c.ChildID == 11)
						.InsertWithOutputInto(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							},
							tRef,
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
						ChildID = c.ChildID
					}),
					t.Select(c => new Child
					{
						ParentID = c.ParentID - param,
						ChildID = c.ChildID
					}
					)
				);

			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}

		[Test]
		public async Task InsertWithSetterWithOutputIntoProjTempTableByTableNameAsync([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db = GetDataContext(context);
			const int idsLimit = 1000;
			const int param = 4242;

			try
			{
				var id = idsLimit + 1;

				db.Child.Delete(c => c.ChildID > idsLimit);

				using var t = db.CreateTempTable<Child>("TInserted");
				var tRef = db.GetTable<Child>()
					.TableOptions(TableOptions.IsTemporary)
					.TableName(t.TableName);

				var output = await
					db.Child
						.Where(c => c.ChildID == 11)
						.InsertWithOutputIntoAsync(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							},
							tRef,
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
						ChildID = c.ChildID
					}),
					t.Select(c => new Child
					{
						ParentID = c.ParentID - param,
						ChildID = c.ChildID
					}
					)
				);

			}
			finally
			{
				db.Child.Delete(c => c.ChildID > idsLimit);
			}
		}

		[Test]
		public void InsertWithConfiguredQueryIntoTempTableByTableName([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateTempTable<TableWithData>("TableWithData_target");
			var targetRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);
			using var output = db.CreateTempTable<TableWithData>("TableWithData_output");
			var outputRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			var rowCount = db.Person
				.Where(p => p.Gender == Gender.Female)
				.Select(p => new TableWithData
				{
					Id = p.ID,
					Value = p.ID * 10,
					ValueStr = p.FirstName + " " + p.LastName
				})
				.Into(targetRef)
				.InsertWithOutputInto(outputRef);

			Assert.AreEqual(1, rowCount);

			var targetData = target.ToArray();
			Assert.AreEqual(1, targetData.Length);
			Assert.AreEqual(3, targetData[0].Id);
			Assert.AreEqual(30, targetData[0].Value);
			Assert.AreEqual("Jane Doe", targetData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(3, outputData[0].Id);
			Assert.AreEqual(30, outputData[0].Value);
			Assert.AreEqual("Jane Doe", outputData[0].ValueStr);
		}

		[Test]
		public async Task InsertWithConfiguredQueryIntoTempTableByTableNameAsync([IncludeDataSources(FeatureInsertOutputInto)] string context)
		{
			using var db     = GetDataContext(context);
			using var target = db.CreateTempTable<TableWithData>("TableWithData_target");
			var targetRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(target.TableName);
			using var output = db.CreateTempTable<TableWithData>("TableWithData_output");
			var outputRef = db.GetTable<TableWithData>()
				.TableOptions(TableOptions.IsTemporary)
				.TableName(output.TableName);

			var rowCount = await db.Person
				.Where(p => p.Gender == Gender.Female)
				.Select(p => new TableWithData
				{
					Id = p.ID,
					Value = p.ID * 10,
					ValueStr = p.FirstName + " " + p.LastName
				})
				.Into(targetRef)
				.InsertWithOutputIntoAsync(outputRef);

			Assert.AreEqual(1, rowCount);

			var targetData = target.ToArray();
			Assert.AreEqual(1, targetData.Length);
			Assert.AreEqual(3, targetData[0].Id);
			Assert.AreEqual(30, targetData[0].Value);
			Assert.AreEqual("Jane Doe", targetData[0].ValueStr);

			var outputData = output.ToArray();
			Assert.AreEqual(1, outputData.Length);
			Assert.AreEqual(3, outputData[0].Id);
			Assert.AreEqual(30, outputData[0].Value);
			Assert.AreEqual("Jane Doe", outputData[0].ValueStr);
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

		[Table]
		public partial class Issue3834Table
		{
			[Column("Id"     , IsPrimaryKey = true )] public int       Id         { get; set; }
			[Column("Nesto"  , CanBeNull    = false)] public string    Nesto      { get; set; } = null!;
			[Column("Nest"   , CanBeNull    = false)] public string    Nest       { get; set; } = null!;
			[Column("WhatSov", CanBeNull    = false)] public string    Whatsov    { get; set; } = null!;
			[Column("Co2grund")                     ] public string?   Co2Grund   { get; set; }
			[Column("Co2aend")                      ] public string?   Co2Aend    { get; set; }
		}

		[Test]
		public void Issue3834([IncludeDataSources(true, FeatureInsertOutputSingle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3834Table>();

			var what = new Issue3834Table()
			{
				Id       = 123,
				Co2Aend  = "What",
				Nest     = "Nessss",
				Co2Grund = "xxx",
				Nesto    = "Nesto",
				Whatsov  = "Whatsov"
			};

			var x = table.InsertWithOutput(what);

			Assert.AreEqual(what.Id      , x.Id      );
			Assert.AreEqual(what.Co2Aend , x.Co2Aend );
			Assert.AreEqual(what.Nest    , x.Nest    );
			Assert.AreEqual(what.Co2Grund, x.Co2Grund);
			Assert.AreEqual(what.Nesto   , x.Nesto   );
			Assert.AreEqual(what.Whatsov , x.Whatsov );
		}
	}
}
