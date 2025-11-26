using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.Tools.Comparers;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	[TestFixture]
	public class InsertWithOutputTests : TestBase
	{
		private const string FeatureInsertOutputSingle                  = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputSingleWithExpressions   = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebirdLess5},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputMultipleWithExpressions = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputMultiple                = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird5Plus},{TestProvName.AllMariaDB},{TestProvName.AllPostgreSQL},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputWithSchema              = $"{TestProvName.AllSqlServer},{TestProvName.AllFirebird},{TestProvName.AllMariaDB},{TestProvName.AllSQLite}";
		private const string FeatureInsertOutputInto                    = $"{TestProvName.AllSqlServer}";

		[Table]
		sealed class TableWithData
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table(Schema = "TestSchema")]
		sealed class TableWithDataAndSchema
		{
			[PrimaryKey]          public int     Id       { get; set; }
			[Column]              public int     Value    { get; set; }
			[Column(Length = 50)] public string? ValueStr { get; set; }
		}

		[Table]
		sealed class DestinationTable
		{
			[PrimaryKey]          public int     Id       { get; set; }
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
		public void InsertWithOutputProjectionFromQueryTest([IncludeDataSources(true, FeatureInsertOutputMultipleWithExpressions)] string context, [Values(100, 200)] int param)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5192")]
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
				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id > 3)
						.InsertWithOutputAsync(
							target,
							s => new DestinationTable
							{
								Id       = s.Id       + param,
								Value    = s.Value    + param,
								ValueStr = s.ValueStr + param
							}));

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
				var output = await AsyncEnumerableToListAsync(
					source
						.Where(s => s.Id == 3)
						.InsertWithOutputAsync(
							target,
							s => new DestinationTable
							{
								Id       = s.Id       + param,
								Value    = s.Value    + param,
								ValueStr = s.ValueStr + param
							}));

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
		public void InsertWithOutputTest3([IncludeDataSources(true, FeatureInsertOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
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
		public void InsertWithOutputTest4([IncludeDataSources(true, FeatureInsertOutputSingleWithExpressions)] string context, [Values(100, 200)] int param)
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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

						Assert.That(output, Is.EqualTo(1));

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

						Assert.That(output, Is.EqualTo(1));

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

						Assert.That(output, Is.EqualTo(1));

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

						Assert.That(output, Is.EqualTo(1));

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

					Assert.That(output, Is.EqualTo(1));

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

					Assert.That(output, Is.EqualTo(1));

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

				Assert.That(output, Is.EqualTo(1));

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
			Assert.That(rowCount, Is.EqualTo(1));

			var sourceData = source.ToArray();
			Assert.That(sourceData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sourceData[0].Id, Is.EqualTo(42));
				Assert.That(sourceData[0].Value, Is.EqualTo(42123));
				Assert.That(sourceData[0].ValueStr, Is.EqualTo("SomeStr"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(42));
				Assert.That(outputData[0].Value, Is.EqualTo(42123));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("SomeStr"));
			}
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
			Assert.That(rowCount, Is.EqualTo(1));

			var sourceData = source.ToArray();
			Assert.That(sourceData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sourceData[0].Id, Is.EqualTo(42));
				Assert.That(sourceData[0].Value, Is.EqualTo(42123));
				Assert.That(sourceData[0].ValueStr, Is.EqualTo("SomeStr"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(42));
				Assert.That(outputData[0].Value, Is.EqualTo(42123));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("SomeStr"));
			}
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
			Assert.That(rowCount, Is.EqualTo(1));

			var sourceData = source.ToArray();
			Assert.That(sourceData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sourceData[0].Id, Is.EqualTo(42));
				Assert.That(sourceData[0].Value, Is.EqualTo(42123));
				Assert.That(sourceData[0].ValueStr, Is.EqualTo("SomeStr"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(43));
				Assert.That(outputData[0].Value, Is.EqualTo(84246));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("FooSomeStr"));
			}
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
			Assert.That(rowCount, Is.EqualTo(1));

			var sourceData = source.ToArray();
			Assert.That(sourceData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sourceData[0].Id, Is.EqualTo(42));
				Assert.That(sourceData[0].Value, Is.EqualTo(42123));
				Assert.That(sourceData[0].ValueStr, Is.EqualTo("SomeStr"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(43));
				Assert.That(outputData[0].Value, Is.EqualTo(84246));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("FooSomeStr"));
			}
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

				Assert.That(output, Is.EqualTo(1));

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

				Assert.That(output, Is.EqualTo(1));

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

			Assert.That(rowCount, Is.EqualTo(1));

			var targetData = target.ToArray();
			Assert.That(targetData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(targetData[0].Id, Is.EqualTo(3));
				Assert.That(targetData[0].Value, Is.EqualTo(30));
				Assert.That(targetData[0].ValueStr, Is.EqualTo("Jane Doe"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(3));
				Assert.That(outputData[0].Value, Is.EqualTo(30));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("Jane Doe"));
			}
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

			Assert.That(rowCount, Is.EqualTo(1));

			var targetData = target.ToArray();
			Assert.That(targetData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(targetData[0].Id, Is.EqualTo(3));
				Assert.That(targetData[0].Value, Is.EqualTo(30));
				Assert.That(targetData[0].ValueStr, Is.EqualTo("Jane Doe"));
			}

			var outputData = output.ToArray();
			Assert.That(outputData, Has.Length.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(outputData[0].Id, Is.EqualTo(3));
				Assert.That(outputData[0].Value, Is.EqualTo(30));
				Assert.That(outputData[0].ValueStr, Is.EqualTo("Jane Doe"));
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
				using (Assert.EnterMultipleScope())
				{
					Assert.That(output.Id, Is.EqualTo(data.Id));
					Assert.That(output.Value, Is.EqualTo(data.Value));
					Assert.That(output.ValueStr, Is.EqualTo(data.ValueStr));
				}
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
			using (Assert.EnterMultipleScope())
			{
				Assert.That(x.Id, Is.EqualTo(what.Id));
				Assert.That(x.Co2Aend, Is.EqualTo(what.Co2Aend));
				Assert.That(x.Nest, Is.EqualTo(what.Nest));
				Assert.That(x.Co2Grund, Is.EqualTo(what.Co2Grund));
				Assert.That(x.Nesto, Is.EqualTo(what.Nesto));
				Assert.That(x.Whatsov, Is.EqualTo(what.Whatsov));
			}
		}

		#region Issue 3581

		[Table]
		sealed class Issue3581Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column ]public string? Name { get; set; }

			[Column("ExternalId", ".Id")]
			[Column("Source", ".Source")]
			public ExternalId? ExternalId { get; set; }
		}

		sealed class ExternalId
		{
			public string? Id { get; set; }
			public string? Source { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3581")]
		public void Issue3581Test([IncludeDataSources(true, FeatureInsertOutputSingle)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<Issue3581Table>();

			var row = new Issue3581Table {Id = 1, Name = "John Doe", ExternalId = new ExternalId() { Id = "1", Source = "unknown" } };
			var created = table.InsertWithOutput(row);

			Assert.That(created, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(created.Id, Is.EqualTo(row.Id));
				Assert.That(created.Name, Is.EqualTo(row.Name));
				Assert.That(created.ExternalId, Is.Not.Null);
				Assert.That(created.ExternalId!.Id, Is.EqualTo(row.ExternalId.Id));
				Assert.That(created.ExternalId.Source, Is.EqualTo(row.ExternalId.Source));
			}
		}
		#endregion
	}
}
