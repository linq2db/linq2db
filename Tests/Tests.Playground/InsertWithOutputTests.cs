using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;
using Tests.Tools;

namespace Tests.Playground
{
	[TestFixture]
	public class InsertWithOutputTests : TestBase
	{
		[Table]
		class TableWithData
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
			[Column(Length = 50)] public string ValueStr { get; set; }
		}

		[Table]
		class DestinationTable
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
			[Column(Length = 50)] public string ValueStr { get; set; }
		}

		static TableWithData[] GetSourceData()
		{
			return Enumerable.Range(1, 10).Select(i =>
					new TableWithData { Id = i, Value = -i, ValueStr = "Str" + i.ToString() })
				.ToArray();
		}

		[Test, Combinatorial]
		public void InsertWithOutputProjectionFromQueryTest([IncludeDataSources(ProviderName.SqlServer)] string context)
		{
			var sourceData = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable<DestinationTable>())
			{
				var output = source
					.Where(s => s.Id > 3)
					.InsertWithOutput(
						target,
						s => new DestinationTable
						{
							Id = s.Id + 100,
							Value = s.Value + 100,
							ValueStr = s.ValueStr + 100
						},
						inserted => new
						{
							Id = Sql.AsSql(inserted.Id + 1),
							ValueStr = Sql.AsSql(inserted.ValueStr + 1),
						}).ToArray();

				var zz = target.ToArray();

				AreEqual(target.Select(t => new
					{
						Id = t.Id + 1,
						ValueStr = t.ValueStr + 1,
					}),
					output);
			}
		}

		[Test, Combinatorial]
		public void InsertWithOutputFromQueryTest([IncludeDataSources(ProviderName.SqlServer)] string context)
		{
			var sourceData = GetSourceData();
			using (var db = GetDataContext(context))
			using (var source = db.CreateLocalTable(sourceData))
			using (var target = db.CreateLocalTable<DestinationTable>())
			{
				var output = source
					.Where(s => s.Id > 3)
					.InsertWithOutput(
						target,
						s => new DestinationTable
						{
							Id = s.Id + 100,
							Value = s.Value + 100,
							ValueStr = s.ValueStr + 100
						})
					.ToArray();

				AreEqual(source.Where(s => s.Id > 3).Select(s => new DestinationTable
					{
						Id = s.Id + 100,
						Value = s.Value + 100,
						ValueStr = s.ValueStr + 100,
					}),
					output, ComparerBuilder<DestinationTable>.GetEqualityComparer());
			}
		}

		[Test, Combinatorial]
		public void InsertWithOutputTest3([IncludeDataSources(ProviderName.SqlServer)] string context)
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
								ID = inserted.ChildID + inserted.ParentID
							})
						.ToArray();

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new
						{
							ID = c.ChildID + c.ParentID
						}),
						output);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test, Combinatorial]
		public void InsertWithOutputTest4([IncludeDataSources(ProviderName.SqlServer)] string context)
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
							inserted => Sql.AsSql(inserted.ChildID + inserted.ParentID))
						.ToArray();

					AreEqual(
						db.Child.Where(c => c.ChildID > idsLimit)
							.Select(c => c.ChildID + c.ParentID),
						output);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test, Combinatorial]
		public void InsertWithOutputObjTest1([IncludeDataSources(ProviderName.SqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				const int idsLimit = 1000;

				try
				{
					var id = idsLimit + 1;

					var child = new Child
					{
						ParentID = 1001,
						ChildID  = id
					};

					var output = db.Child.InsertWithOutput(child);

					Assert.AreEqual(db.Child.Single(c => c.ChildID > idsLimit), output);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test, Combinatorial]
		public void InsertWithOutputIntoTest1([IncludeDataSources(ProviderName.SqlServer)] string context)
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
										ChildID = id
									},
									t.Table,
									inserted =>
										new Child
										{
											ChildID = inserted.ChildID,
											ParentID = inserted.ParentID + 1
										}
								);

						Assert.AreEqual(1, output);

						AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
							{
								ParentID = c.ParentID,
								ChildID = c.ChildID
							}),
							t.Table.Select(c => new Child
								{
									ParentID = c.ParentID - 1,
									ChildID = c.ChildID
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

		[Test, Combinatorial]
		public void InsertWithOutputIntoTest2([IncludeDataSources(ProviderName.SqlServer)] string context)
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
										ChildID = id
									},
									t.Table);

						Assert.AreEqual(1, output);

						AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
							{
								ParentID = c.ParentID,
								ChildID = c.ChildID
							}),
							t.Table.Select(c => new Child
								{
									ParentID = c.ParentID,
									ChildID = c.ChildID
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


	}
}
