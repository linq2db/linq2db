using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Playground
{
	[TestFixture]
	public class InsertWithOutputTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column] public int Value { get; set; }
		}

		[Test, Combinatorial]
		public void SampleSelectTest([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				var result = table.ToArray();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int idsLimit = 1000;

				try
				{
					var id = idsLimit + 1;

					db.Child.Delete(c => c.ChildID > idsLimit);

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
								inserted.ChildID,
								inserted.ParentID
							})
						.ToArray();

						AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new
							{
								c.ChildID,
								c.ParentID,
							}),
							output);
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				const int idsLimit = 1000;

				try
				{
					var id = idsLimit + 1;

					db.Child.Delete(c => c.ChildID > idsLimit);

					var output =
						db.Child
							.Where(c => c.ChildID == 11)
							.InsertWithOutput(db.Child, c => new Child
							{
								ChildID  = id,
								ParentID = c.ParentID
							}).ToArray();

					AreEqual(db.Child.Where(c => c.ChildID > idsLimit).Select(c => new Child
						{
							ChildID = c.ChildID,
							ParentID = c.ParentID,
						}),
						output);

				}
				finally
				{
					db.Child.Delete(c => c.ChildID > idsLimit);
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputTest3(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputTest4(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputObjTest1(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputIntoTest1(string context)
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

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void InsertWithOutputIntoTest2(string context)
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
