using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests.Model;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class InsertTests : TestBase
	{
#if AZURE
		[ActiveIssue("Error from Azure runs (db encoding issue?): FbException : Malformed string", Configuration = TestProvName.AllFirebird)]
#endif
		[Test]
		public void DistinctInsert1(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllAccess)]
			string context)
		{
			using var _ = context.IsAnyOf(TestProvName.AllSapHana) ? new DisableBaseline("Client-side Guid generation") : null;
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				try
				{
					db.Types.Delete(c => c.ID > 1000);

					var cnt = db
						.Types
						.Select(_ => Math.Floor(_.ID / 3.0))
						.Distinct()
						.Insert(db.Types, _ => new LinqDataTypes
						{
							ID        = (int)(_ + 1001),
							GuidValue = Sql.NewGuid(),
							BoolValue = true
						});

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(
							cnt, Is.EqualTo(Types.Select(_ => _.ID / 3).Distinct().Count()));
				}
				finally
				{
					db.Types.Delete(c => c.ID > 1000);
				}
			}
		}

#if AZURE
		[ActiveIssue("Error from Azure runs (db encoding issue?): FbException : Malformed string", Configuration = TestProvName.AllFirebird)]
#endif
		[Test]
		public void DistinctInsert2(
			[DataSources(
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllAccess)]
			string context)
		{
			using var _ = context.IsAnyOf(TestProvName.AllSapHana) ? new DisableBaseline("Client-side Guid generation") : null;
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Types.Delete(c => c.ID > 1000);

					var cnt = db.Types
						.Select(_ => Math.Floor(_.ID / 3.0))
						.Distinct()
						.Into(db.Types)
							.Value(t => t.ID,        t => (int)(t + 1001))
							.Value(t => t.GuidValue, t => Sql.NewGuid())
							.Value(t => t.BoolValue, t => true)
						.Insert();

					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(
							cnt, Is.EqualTo(Types.Select(_ => _.ID / 3).Distinct().Count()));
				}
				finally
				{
					db.Types.Delete(c => c.ID > 1000);
				}
			}
		}

		[Test]
		public void Insert1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db.Child
						.Insert(() => new Child
						{
							ParentID = 1,
							ChildID  = id
						});
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db
							.Into(db.Child)
								.Value(c => c.ParentID, () => 1)
								.Value(c => c.ChildID,  () => id)
							.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public async Task Insert2Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					var cnt = await db
							.Into(db.Child)
								.Value(c => c.ParentID, () => 1)
								.Value(c => c.ChildID,  () => id)
							.InsertAsync();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(await db.Child.CountAsync(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db.Child
							.Where(c => c.ChildID == 11)
							.Insert(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							});
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public async Task Insert3Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					var cnt = await db.Child
							.Where(c => c.ChildID == 11)
							.InsertAsync(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							});
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(await db.Child.CountAsync(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert31([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db.Child
						.Where(c => c.ChildID == 11)
						.Select(c => new Child
						{
							ParentID = c.ParentID,
							ChildID  = id
						})
						.Insert(db.Child, c => c);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c  => c.ParentID)
								.Value(c => c.ChildID,  () => id)
							.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert4String([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;

				var insertable = db.Child
					.Where(c => c.ChildID == 111)
					.Into(db.Child)
					.Value(c => c.ParentID, c => c.ParentID)
					.Value(c => c.ChildID, () => id);

				var sql = insertable.Insert();
			}
		}

		[Test]
		public async Task Insert4Async([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					var cnt = await db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c  => c.ParentID)
								.Value(c => c.ChildID,  () => id)
							.InsertAsync();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(await db.Child.CountAsync(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c => c.ParentID)
								.Value(c => c.ChildID,  id)
							.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(p => p.Value1 == 11);

					var cnt = db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Parent)
								.Value(p => p.ParentID, c => c.ParentID + 1000)
								.Value(p => p.Value1,   c => (int?)c.ChildID)
							.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Parent.Count(p => p.Value1 == 11), Is.EqualTo(1));
				}
				finally
				{
					db.Parent.Delete(p => p.Value1 == 11);
				}
			}
		}

		sealed class InsertTable
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column]
			public DateTime? CreatedOn { get; set; }
			[Column]
			public DateTime? ModifiedOn { get; set; }
		}

		[Test]
		public void Insert6WithSameFields([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(new []
			{
				new InsertTable{Id = 1, CreatedOn = TestData.DateTime, ModifiedOn = TestData.DateTime},
				new InsertTable{Id = 2, CreatedOn = TestData.DateTime, ModifiedOn = TestData.DateTime},
			}))
			{
				var affected = table
					.Where(c => c.Id > 0)
					.Into(table)
					.Value(p => p.Id, c => c.Id + 10)
					.Value(p => p.CreatedOn,  c => Sql.CurrentTimestamp)
					.Value(p => p.ModifiedOn, c => Sql.CurrentTimestamp)
					.Insert();

				if (!context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(affected, Is.EqualTo(2));
			}
		}

		[Test]
		public void Insert7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db
						.Child
							.Value(c => c.ChildID,  () => id)
							.Value(c => c.ParentID, 1)
						.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					var cnt = db
						.Child
							.Value(c => c.ParentID, 1)
							.Value(c => c.ChildID,  () => id)
						.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test]
		public void Insert9([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child. Delete(c => c.ParentID > 1000);
					db.Parent.Delete(p => p.ParentID > 1000);

					db.Insert(new Parent { ParentID = id, Value1 = id });

					var cnt = db.Parent
						.Where(p => p.ParentID == id)
						.Insert(db.Child, p => new Child
						{
							ParentID = p.ParentID,
							ChildID  = p.ParentID,
						});
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Child.Count(c => c.ParentID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Child. Delete(c => c.ParentID > 1000);
					db.Parent.Delete(p => p.ParentID > 1000);
				}
			}
		}

		[Table("LinqDataTypes")]
		public class LinqDataTypesArrayTest
		{
			[Column] public int       ID;
			[Column] public decimal   MoneyValue;
			[Column] public DateTime? DateTimeValue;
			[Column] public bool      BoolValue;
			[Column] public Guid      GuidValue;
			[Column] public byte[]?   BinaryValue;
			[Column] public short     SmallIntValue;
		}

		[Test]
		public void InsertArray1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);
					types.Insert(() => new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = null });

					Assert.That(types.Single(t => t.ID == 1001).BinaryValue, Is.Null);
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test]
		public void InsertArray2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);

					byte[]? arr = null;

					types.Insert(() => new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = arr });

					var res = types.Single(t => t.ID == 1001).BinaryValue;

					Assert.That(res, Is.Null);
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test]
		public void InsertArray3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);

					var arr = new byte[] { 1, 2, 3, 4 };

					types.Insert(() => new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = arr });

					var res = types.Single(t => t.ID == 1001).BinaryValue;

					Assert.That(res, Is.EqualTo(arr));
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test]
		public void InsertArray4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);

					var arr = new byte[] { 1, 2, 3, 4 };

					db.Insert(new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = arr });

					var res = types.Single(t => t.ID == 1001).BinaryValue;

					Assert.That(res, Is.EqualTo(arr));
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test]
		public void InsertUnion1([DataSources] string context)
		{
			Child.Count();

			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(p => p.ParentID > 1000);

					var q =
						db.Child.     Select(c => new Parent { ParentID = c.ParentID,      Value1 = (int) Math.Floor(c.ChildID / 10.0) }).Union(
						db.GrandChild.Select(c => new Parent { ParentID = c.ParentID ?? 0, Value1 = (int?)Math.Floor((c.GrandChildID ?? 0) / 100.0) }));

					q.Insert(db.Parent, p => new Parent
					{
						ParentID = p.ParentID + 1000,
						Value1   = p.Value1
					});

					Assert.That(
						db.Parent.Count(c => c.ParentID > 1000), Is.EqualTo(Child.     Select(c => new { ParentID = c.ParentID      }).Union(
						GrandChild.Select(c => new { ParentID = c.ParentID ?? 0 })).Count()));
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID > 1000);
				}
			}
		}

		[Test]
		public void InsertEnum1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(_ => _.ParentID > 1000);

					var p = new Parent4
					{
						ParentID = id,
						Value1   = TypeValue.Value2
					};

					var cnt = db.Parent4
						.Insert(() => new Parent4
						{
							ParentID = 1001,
							Value1   = p.Value1
						});
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Parent4.Count(_ => _.ParentID == id && _.Value1 == p.Value1), Is.EqualTo(1));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test]
		public void InsertEnum2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(_ => _.ParentID > 1000);

					var cnt = db.Parent4
							.Value(_ => _.ParentID, id)
							.Value(_ => _.Value1,   TypeValue.Value1)
						.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Parent4.Count(_ => _.ParentID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test]
		public void InsertEnum3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(_ => _.ParentID > 1000);

					var cnt = db.Parent4
							.Value(_ => _.ParentID, id)
							.Value(_ => _.Value1,   () => TypeValue.Value1)
						.Insert();
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
						Assert.That(cnt, Is.EqualTo(1));

					Assert.That(db.Parent4.Count(_ => _.ParentID == id), Is.EqualTo(1));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test]
		public void InsertNull([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var cnt = db
					.Into(db.Parent)
						.Value(p => p.ParentID, 1001)
						.Value(p => p.Value1,   (int?)null)
					.Insert();
				if (!context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Parent.Count(p => p.ParentID == 1001), Is.EqualTo(1));
			}
		}

		[Test]
		public void InsertWithIdentity1([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				var id =
					db.Person
						.InsertWithIdentity(() => new Person
						{
							FirstName = "John",
							LastName  = "Shepard",
							Gender    = Gender.Male
						});

				Assert.That(id, Is.Not.Null);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.That(john, Is.Not.Null);
				Assert.That(john.ID, Is.EqualTo(id));
			}
		}

		[Test]
		public async Task InsertWithIdentity1Async([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				var id =
					await db.Person
						.InsertWithIdentityAsync(() => new Person
						{
							FirstName = "John",
							LastName  = "Shepard",
							Gender    = Gender.Male
						});

				Assert.That(id, Is.Not.Null);

				var john = await db.Person.SingleAsync(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.That(john, Is.Not.Null);
				Assert.That(john.ID, Is.EqualTo(id));
			}
		}

		[Test]
		public void InsertWithIdentity2([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				var id = db
					.Into(db.Person)
						.Value(p => p.FirstName, () => "John")
						.Value(p => p.LastName,  () => "Shepard")
						.Value(p => p.Gender,    () => Gender.Male)
					.InsertWithIdentity();

				Assert.That(id, Is.Not.Null);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.That(john, Is.Not.Null);
				Assert.That(john.ID, Is.EqualTo(id));
			}
		}

		[Test]
		public async Task InsertWithIdentity2Async([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				var id = await db
					.Into(db.Person)
						.Value(p => p.FirstName, () => "John")
						.Value(p => p.LastName,  () => "Shepard")
						.Value(p => p.Gender,    () => Gender.Male)
					.InsertWithIdentityAsync();

				Assert.That(id, Is.Not.Null);

				var john = await db.Person.SingleAsync(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.That(john, Is.Not.Null);
				Assert.That(john.ID, Is.EqualTo(id));
			}
		}

		[Test]
		public void InsertWithIdentity3([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				var id = db
					.Into(db.Person)
						.Value(p => p.FirstName, "John")
						.Value(p => p.LastName,  "Shepard")
						.Value(p => p.Gender,    Gender.Male)
					.InsertWithIdentity();

				Assert.That(id, Is.Not.Null);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.That(john, Is.Not.Null);
				Assert.That(john.ID, Is.EqualTo(id));
			}
		}

		[Test]
		public void InsertWithIdentity4([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				for (var i = 0; i < 2; i++)
				{
					var id = db.InsertWithIdentity(
						new Person
						{
							FirstName = "John" + i,
							LastName  = "Shepard",
							Gender    = Gender.Male
						});

					Assert.That(id, Is.Not.Null);

					var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.That(john, Is.Not.Null);
					Assert.That(john.ID, Is.EqualTo(id));
				}
			}
		}

		[Test]
		public async Task InsertWithIdentity4Async([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				for (var i = 0; i < 2; i++)
				{
					var id = await db.InsertWithIdentityAsync(
						new Person
						{
							FirstName = "John" + i,
							LastName  = "Shepard",
							Gender    = Gender.Male
						});

					Assert.That(id, Is.Not.Null);

					var john = await db.Person.SingleAsync(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.That(john, Is.Not.Null);
					Assert.That(john.ID, Is.EqualTo(id));
				}
			}
		}

		[Test]
		public void InsertWithIdentity5([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				for (var i = 0; i < 2; i++)
				{
					var person = new Person
					{
						FirstName = "John" + i,
						LastName  = "Shepard",
						Gender    = Gender.Male
					};

					var id = db.InsertWithIdentity(person);

					Assert.That(id, Is.Not.Null);

					var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.That(john, Is.Not.Null);
					Assert.That(john.ID, Is.EqualTo(id));
				}
			}
		}

		sealed class GuidID
		{
			[Identity] public Guid ID;
					   public int  Field1;
		}

		[Test]
		public void InsertWithGuidIdentity([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var id = (Guid)db.InsertWithIdentity(new GuidID { Field1 = 1 });
				Assert.That(id, Is.Not.EqualTo(Guid.Empty));
			}
		}

		[Test]
		public void InsertWithGuidIdentityOutput([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context, o => o.UseSqlServer(o => o with { GenerateScopeIdentity = false }));

			var id = (Guid) db.InsertWithIdentity(new GuidID {Field1 = 1});
			Assert.That(id, Is.Not.EqualTo(Guid.Empty));
		}

		[Test]
		public void InsertWithIdentityOutput([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context, o => o.UseSqlServer(o => o with { GenerateScopeIdentity = false }));
			using (new DeletePerson(db))
			{

				for (var i = 0; i < 2; i++)
				{
					var person = new Person
					{
						FirstName = "John" + i,
						LastName  = "Shepard",
						Gender    = Gender.Male
					};

					var id = db.InsertWithIdentity(person);

					Assert.That(id, Is.Not.Null);

					var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.That(john, Is.Not.Null);
					Assert.That(john.ID, Is.EqualTo(id));
				}
			}
		}

		sealed class GuidID2
		{
			[Identity] public Guid ID;
		}

		[Test]
		public void InsertWithGuidIdentity2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var id = (Guid)db.InsertWithIdentity(new GuidID2 {});
			}
		}

		[Test]
		public void InsertOrUpdate1([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				for (var i = 0; i < 3; i++)
				{
					db.Patient.InsertOrUpdate(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "abc",
						},
						p => new Patient
						{
							Diagnosis = (p.Diagnosis.Length + i).ToString(),
						});
				}

				Assert.That(db.Patient.Single(p => p.PersonID == id).Diagnosis, Is.EqualTo("3"));
			}
		}

		[Test]
		public void InsertOrUpdate2([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "test",
					LastName  = "subject",
					Gender    = Gender.Unknown
				};

				var id = db.InsertWithInt32Identity(person);

				var records = db.Patient.InsertOrUpdate(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "negative"
						},
						p => new Patient
						{
						});

				List<Patient> patients;

				using (new DisableLogging())
					patients = db.Patient.Where(p => p.PersonID == id).ToList();

				if (context.IsAnyOf(TestProvName.AllOracleNative))
					Assert.That(records, Is.EqualTo(-1));
				else
					Assert.That(records, Is.EqualTo(1));

				Assert.That(patients, Has.Count.EqualTo(1));
				Assert.Multiple(() =>
				{
					Assert.That(patients[0].PersonID, Is.EqualTo(id));
					Assert.That(patients[0].Diagnosis, Is.EqualTo("negative"));
				});

				records = db.Patient.InsertOrUpdate(
					() => new Patient
					{
						PersonID = id,
						Diagnosis = "positive"
					},
					p => new Patient
					{
					});

				using (new DisableLogging())
					patients = db.Patient.Where(p => p.PersonID == id).ToList();

				Assert.Multiple(() =>
				{
					Assert.That(records, Is.LessThanOrEqualTo(0));
					Assert.That(patients, Has.Count.EqualTo(1));
				});
				Assert.Multiple(() =>
				{
					Assert.That(patients[0].PersonID, Is.EqualTo(id));
					Assert.That(patients[0].Diagnosis, Is.EqualTo("negative"));
				});
			}
		}

		[Test]
		public void InsertOrReplace1([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				for (var i = 0; i < 3; i++)
				{
					db.InsertOrReplace(new Patient()
					{
						PersonID = id,
						Diagnosis = ("abc" + i).ToString(),
					});
				}

				Assert.That(db.Patient.Single(p => p.PersonID == id).Diagnosis, Is.EqualTo("abc2"));
			}
		}

		[Test]
		public async Task InsertOrReplace1Async([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				for (var i = 0; i < 3; i++)
				{
					await db.InsertOrReplaceAsync(new Patient
					{
						PersonID  = id,
						Diagnosis = ("abc" + i).ToString(),
					});
				}

				Assert.That((await db.Patient.SingleAsync(p => p.PersonID == id)).Diagnosis, Is.EqualTo("abc2"));
			}
		}

		[Test]
		public void InsertOrReplaceWithIdentity()
		{
			Assert.Throws<LinqToDBException>(() =>
			{
				using (var db = new DataConnection())
				{
					var p = new Person()
					{
						FirstName = TestData.Guid1.ToString(),
						ID = 1000,
					};

					db.InsertOrReplace(p);
				}
			});
		}

		[Test]
		public void InsertOrUpdate3([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				var diagnosis = "abc";

				for (var i = 0; i < 3; i++)
				{
					db.Patient.InsertOrUpdate(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "abc",
						},
						p => new Patient
						{
							Diagnosis = (p.Diagnosis.Length + i).ToString(),
						},
						() => new Patient
						{
							PersonID = id,
						});

					diagnosis = (diagnosis.Length + i).ToString();
				}

				Assert.That(db.Patient.Single(p => p.PersonID == id).Diagnosis, Is.EqualTo("3"));
			}
		}

		[Test]
		public async Task InsertOrUpdate3Async([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				var diagnosis = "abc";

				for (var i = 0; i < 3; i++)
				{
					await db.Patient.InsertOrUpdateAsync(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "abc",
						},
						p => new Patient
						{
							Diagnosis = (p.Diagnosis.Length + i).ToString(),
						},
						() => new Patient
						{
							PersonID = id,
						});

					diagnosis = (diagnosis.Length + i).ToString();
				}

				Assert.That((await db.Patient.SingleAsync(p => p.PersonID == id)).Diagnosis, Is.EqualTo("3"));
			}
		}

		[Test]
		public async Task InsertOrUpdate3xAsync([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				var diagnosis = "abc";

				var id2 = id;

				for (var i = 0; i < 3; i++)
				{
					await db.Patient.InsertOrUpdateAsync(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "abc",
						},
						p => new Patient
						{
							Diagnosis = (p.Diagnosis.Length + i).ToString(),
						},
						() => new Patient
						{
							PersonID = id2,
						});

					diagnosis = (diagnosis.Length + i).ToString();
				}

				Assert.That((await db.Patient.SingleAsync(p => p.PersonID == id)).Diagnosis, Is.EqualTo("3"));
			}
		}

		[Test]
		public void InsertOrUpdate4([InsertOrUpdateDataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new Person
				{
					FirstName = "John",
					LastName  = "Shepard",
					Gender    = Gender.Male
				};

				var id = db.InsertWithInt32Identity(person);

				for (var i = 0; i < 3; i++)
				{
					var diagnosis = "abc";
					db.Patient.InsertOrUpdate(
						() => new Patient
						{
							PersonID = id,
							Diagnosis = (Sql.AsSql(diagnosis).Length + i).ToString(),
						},
						p => new Patient
						{
							Diagnosis = (p.Diagnosis.Length + i).ToString(),
						});
				}

				Assert.That(db.Patient.Single(p => p.PersonID == id).Diagnosis, Is.EqualTo("3"));
			}
		}

		[Test]
		public void InsertBatch1([IncludeDataSources(TestProvName.AllOracle, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				try
				{
					var data = new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null, BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = null, BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null }
					};

					var options = new BulkCopyOptions { MaxBatchSize = 1 };

					if (context.IsAnyOf(ProviderName.ClickHouseClient))
						options = options with { WithoutSession = true };

					((DataConnection)db).BulkCopy(options, data);
				}
				finally
				{
					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		[Test]
		public void InsertBatch2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				try
				{
					var options = GetDefaultBulkCopyOptions(context) with { MaxBatchSize = 100 };

					((DataConnection)db).BulkCopy(options, new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,              BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null    },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = TestData.DateTime, BoolValue = false, GuidValue = null,                                             SmallIntValue =  2,    IntValue = 1532334 }
					});
				}
				finally
				{
					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		[Test]
		public void Insert11([DataSources] string context)
		{
			var p = new ComplexPerson { Name = new FullName { FirstName = "fn", LastName = "ln" }, Gender = Gender.Male };

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = db.Person.Max(t => t.ID);

				db.Insert(p);

				var inserted = db.GetTable<ComplexPerson>().Single(p2 => p2.ID > id || p2.ID == 0);

				Assert.Multiple(() =>
				{
					Assert.That(inserted.Name.FirstName, Is.EqualTo(p.Name.FirstName));
					Assert.That(inserted.Name.LastName, Is.EqualTo(p.Name.LastName));
					Assert.That(inserted.Gender, Is.EqualTo(p.Gender));
				});
			}
		}

		[Test]
		public void Insert12([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db
					.Into(db.GetTable<ComplexPerson>())
						.Value(_ => _.Name.FirstName, "FirstName")
						.Value(_ => _.Name.LastName,  () => "LastName")
						.Value(_ => _.Gender,         Gender.Female)
					.Insert();
			}
		}

		[Test]
		public void Insert13([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db
					.GetTable<ComplexPerson>()
					.Insert(() => new ComplexPerson
					{
						Name = new FullName
						{
							FirstName = "FirstName",
							LastName  = "LastName"
						},
						Gender = Gender.Male,
					});
			}
		}

		[Test]
		public void Insert14([DataSources(
			ProviderName.SqlCe,
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllSqlServer2005,
			TestProvName.AllSybase)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Person.Delete(p => p.FirstName.StartsWith("Insert14"));

					Assert.Multiple(() =>
					{
						Assert.That(db.Person
											.Insert(() => new Person
											{
							FirstName = "Insert14" + db.Person.Where(p => p.ID == 1).Select(p => p.FirstName).SingleOrDefault(),
												LastName = "Shepard",
												Gender = Gender.Male
											}), Is.EqualTo(1));

						Assert.That(db.Person.Count(p => p.FirstName.StartsWith("Insert14")), Is.EqualTo(1));
					});
				}
				finally
				{
					db.Person.Delete(p => p.FirstName.StartsWith("Insert14"));
				}
			}
		}

		[Test]
		public void Insert15([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => _.FirstName.StartsWith("Insert15")).Delete();

				try
				{
					db.Insert(new ComplexPerson
						{
							Name = new FullName
							{
								FirstName = "Insert15",
								LastName  = "Insert15"
							},
							Gender = Gender.Male,
						});

					var cnt = db.Person.Where(_ => _.FirstName.StartsWith("Insert15")).Count();
					Assert.That(cnt, Is.EqualTo(1));
				}
				finally
				{
					db.Person.Where(_ => _.FirstName.StartsWith("Insert15")).Delete();
				}
			}
		}

		[Test]
		public void Insert16([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Person.Where(_ => _.FirstName.StartsWith("Insert16")).Delete();

				try
				{
					var name = "Insert16";
					var idx = 4;

					db.Person.Insert(() => new Person()
					{
						FirstName = "Insert16",
						LastName  = (Sql.AsSql(name).Length + idx).ToString(),
						Gender    = Gender.Male,
					});

					var cnt = db.Person.Where(_ => _.FirstName.StartsWith("Insert16")).Count();
					Assert.That(cnt, Is.EqualTo(1));
				}
				finally
				{
					db.Person.Where(_ => _.FirstName.StartsWith("Insert16")).Delete();
				}
			}
		}

		// Access, SQLite, Firebird before v4, Informix and SAP Hana do not support DEFAULT in inserted values,
		// see https://github.com/linq2db/linq2db/pull/2954#issuecomment-821798021
		[Test]
		public void InsertDefault([DataSources(
			TestProvName.AllAccess,
			TestProvName.AllFirebirdLess4,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			try
			{
				db.Person.Insert(() => new Person
				{
					FirstName  = "InsertDefault",
					MiddleName = Sql.Default<string>(),
					LastName   = "InsertDefault",
					Gender     = Gender.Male,
				});
			}
			finally
			{
				db.Person.Delete(p => p.FirstName == "InsertDefault");
			}
		}

		[Test]
		public void InsertSingleIdentity([DataSources(
			TestProvName.AllInformix, ProviderName.SqlCe, TestProvName.AllSapHana, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.TestIdentity.Delete();

					var id = db.TestIdentity.InsertWithIdentity(() => new TestIdentity {});

					Assert.That(id, Is.Not.Null);
				}
				finally
				{
					db.TestIdentity.Delete();
				}
			}
		}

		[Table("LinqDataTypes")]
		sealed class TestConvertTable1
		{
			[PrimaryKey]                        public int      ID;
			[Column(DataType = DataType.Int64)] public TimeSpan BigIntValue;
		}

		[Test]
		public void InsertConverted([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<TestConvertTable1>();

				try
				{
					tbl.Delete(r => r.ID >= 1000);

					var tt = TimeSpan.FromMinutes(1);

					tbl.Insert(() => new TestConvertTable1 { ID = 1001, BigIntValue = tt });

					Assert.That(tbl.First(t => t.ID == 1001).BigIntValue, Is.EqualTo(tt));
				}
				finally
				{
					tbl.Delete(r => r.ID >= 1000);
				}
			}
		}

		[Table("LinqDataTypes")]
		sealed class TestConvertTable2
		{
			[PrimaryKey]                        public int       ID;
			[Column(DataType = DataType.Int64)] public TimeSpan? BigIntValue;
		}

		[Test]
		public void InsertConvertedNullable([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<TestConvertTable2>();

				try
				{
					tbl.Delete(r => r.ID >= 1000);

					var tt = TimeSpan.FromMinutes(1);

					tbl.Insert(() => new TestConvertTable2 { ID = 1001, BigIntValue = tt });

					Assert.That(tbl.First(t => t.ID == 1001).BigIntValue, Is.EqualTo(tt));
				}
				finally
				{
					tbl.Delete(r => r.ID >= 1000);
				}
			}
		}

		[Test]
		public void InsertWith([IncludeDataSources(TestProvName.AllSqlServer2008)]
			string context)
		{
			var m = null as int?;

			using (var db = GetDataContext(context))
			{
				(
					from c in db.Child.With("INDEX(IX_ChildIndex)")
					join id in db.GrandChild on c.ParentID equals id.ParentID
					where id.ChildID == m
					select c.ChildID
				)
				.Distinct()
				.Insert(db.Parent, t => new Parent { ParentID = t });
			}
		}

		[Test]
		public void InsertByTableName([DataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = TestUtils.GetTableName(context, "35");

			using (var db = GetDataContext(context))
			{
				try
				{
					db.DropTable<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);

					var table = db.CreateTable<Person>(tableName, schemaName: schemaName);

					Assert.Multiple(() =>
					{
						Assert.That(table.TableName, Is.EqualTo(tableName));
						Assert.That(table.SchemaName, Is.EqualTo(schemaName));
					});

					var person = new Person()
					{
						FirstName = "Steven",
						LastName = "King",
						Gender = Gender.Male,
					};

					// insert a row into the table
					db.Insert(person, tableName: tableName, schemaName: schemaName);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
					{
						var newId1 = db.InsertWithInt32Identity(person, tableName: tableName, schemaName: schemaName);
						var newId2 = db.InsertWithIdentity(person, tableName: tableName, schemaName: schemaName);

						var newCount = table.Count();
						Assert.Multiple(() =>
						{
							Assert.That(newCount, Is.EqualTo(3));

							Assert.That(newId2, Is.Not.EqualTo(newId1));
						});

						var integritycount = table.Where(p => p.FirstName == "Steven" && p.LastName == "King" && p.Gender == Gender.Male).Count();
						Assert.That(integritycount, Is.EqualTo(3));
					}

					table.Drop();
				}
				finally
				{
					db.DropTable<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public async Task InsertByTableNameAsync([DataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = TestUtils.GetTableName(context, "31");

			using (var db = GetDataContext(context))
			{
				try
				{
					var table = await db.CreateTableAsync<Person>(tableName, schemaName: schemaName);

					Assert.Multiple(() =>
					{
						Assert.That(table.TableName, Is.EqualTo(tableName));
						Assert.That(table.SchemaName, Is.EqualTo(schemaName));
					});

					var person = new Person()
					{
						FirstName = "Steven",
						LastName = "King",
						Gender = Gender.Male,
					};

					// insert a row into the table
					await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
					if (!context.IsAnyOf(TestProvName.AllClickHouse))
					{
						var newId1 = await db.InsertWithInt32IdentityAsync(person, tableName: tableName, schemaName: schemaName);
						var newId2 = await db.InsertWithIdentityAsync(person, tableName: tableName, schemaName: schemaName);

						var newCount = await table.CountAsync();
						Assert.Multiple(() =>
						{
							Assert.That(newCount, Is.EqualTo(3));

							Assert.That(newId2, Is.Not.EqualTo(newId1));
						});

						var integritycount = await table.Where(p => p.FirstName == "Steven" && p.LastName == "King" && p.Gender == Gender.Male).CountAsync();
						Assert.That(integritycount, Is.EqualTo(3));
					}

					await table.DropAsync();
				}
				finally
				{
					await db.DropTableAsync<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public void InsertOrReplaceByTableName([InsertOrUpdateDataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = "xxPatient" + (context.IsAnyOf(TestProvName.AllFirebird) ? TestUtils.GetNext().ToString() : string.Empty);

			using (var db = GetDataContext(context))
			{
				db.DropTable<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				var table = db.CreateTable<Patient>(tableName, schemaName: schemaName);

				try
				{
					Assert.Multiple(() =>
					{
						Assert.That(table.TableName, Is.EqualTo(tableName));
						Assert.That(table.SchemaName, Is.EqualTo(schemaName));
					});

					var person1 = new Patient()
					{
						PersonID = 1,
						Diagnosis = "ABC1",
					};

					var person2 = new Patient()
					{
						PersonID = 2,
						Diagnosis = "ABC2",
					};

					db.InsertOrReplace(person1, tableName: tableName, schemaName: schemaName);
					db.InsertOrReplace(person2, tableName: tableName, schemaName: schemaName);

					Assert.That(table.Count(), Is.EqualTo(2));

					db.InsertOrReplace(person1, tableName: tableName, schemaName: schemaName);
					db.InsertOrReplace(person2, tableName: tableName, schemaName: schemaName);

					Assert.That(table.Count(), Is.EqualTo(2));
				}
				finally
				{
					table.Drop(throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public async Task InsertOrReplaceByTableNameAsync([InsertOrUpdateDataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = "xxPatient" + (context.IsAnyOf(TestProvName.AllFirebird) ? TestUtils.GetNext().ToString() : string.Empty);

			using (var db = GetDataContext(context))
			{
				await db.DropTableAsync<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				var table = await db.CreateTableAsync<Patient>(tableName, schemaName: schemaName);
				try
				{
					Assert.Multiple(() =>
					{
						Assert.That(table.TableName, Is.EqualTo(tableName));
						Assert.That(table.SchemaName, Is.EqualTo(schemaName));
					});

					var person1 = new Patient()
					{
						PersonID = 1,
						Diagnosis = "ABC1",
					};

					var person2 = new Patient()
					{
						PersonID = 2,
						Diagnosis = "ABC2",
					};

					await db.InsertOrReplaceAsync(person1, tableName: tableName, schemaName: schemaName);
					await db.InsertOrReplaceAsync(person2, tableName: tableName, schemaName: schemaName);

					Assert.That(await table.CountAsync(), Is.EqualTo(2));

					await db.InsertOrReplaceAsync(person1, tableName: tableName, schemaName: schemaName);
					await db.InsertOrReplaceAsync(person2, tableName: tableName, schemaName: schemaName);

					Assert.That(await table.CountAsync(), Is.EqualTo(2));
				}
				finally
				{
					await table.DropAsync(throwExceptionIfNotExists: false);
				}
			}
		}

		[Test]
		public void TestInsertWithColumnFilter([DataSources] string context, [Values] bool withMiddleName)
		{
			using (var db = GetDataContext(context))
			{
				var newName = "InsertColumnFilter";
				try
				{
					var p = new Person()
					{
						FirstName  = newName,
						LastName   = "whatever",
						MiddleName = "som middle name",
						Gender     = Gender.Male
					};

					db.Insert(p, (a, b) => b.ColumnName != nameof(Model.Person.MiddleName) || withMiddleName);

					p = db.GetTable<Person>().Where(x => x.FirstName == p.FirstName).First();

					Assert.That(string.IsNullOrWhiteSpace(p.MiddleName), Is.EqualTo(!withMiddleName));
				}
				finally
				{
					db.Person.Where(x => x.FirstName == newName).Delete();
				}
			}
		}

		[Test]
		public void TestUpdateWithColumnFilter([DataSources] string context, [Values] bool withMiddleName)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			{
				var newName = "InsertColumnFilter";
				try
				{
					var p = new Person()
					{
						FirstName = newName,
						LastName = "whatever",
						MiddleName = "som middle name",
						Gender = Gender.Male
					};

					db.Insert(p);

					p = db.GetTable<Person>().Where(x => x.FirstName == p.FirstName).First();

					p.MiddleName = "updated name";

					db.Update(p, (a, b) => b.ColumnName != nameof(Model.Person.MiddleName) || withMiddleName);

					p = db.GetTable<Person>().Where(x => x.FirstName == p.FirstName).First();

					if (withMiddleName)
						Assert.That(p.MiddleName, Is.EqualTo("updated name"));
					else
						Assert.That(p.MiddleName, Is.Not.EqualTo("updated name"));
				}
				finally
				{
					db.Person.Where(x => x.FirstName == newName).Delete();
				}
			}
		}

		[Table]
		sealed class TestInsertOrReplaceTable
		{
			[PrimaryKey] public int     ID         { get; set; }
			[Column]     public string? FirstName  { get; set; }
			[Column]     public string? LastName   { get; set; }
			[Column]     public string? MiddleName { get; set; }
		}

		[Test]
		public void TestInsertOrReplaceWithColumnFilter([InsertOrUpdateDataSources] string context, [Values] bool withMiddleName, [Values] bool skipOnInsert)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceTable>())
			{
				var newName = "InsertOrReplaceColumnFilter";
				var p = new TestInsertOrReplaceTable()
				{
					FirstName = newName,
					LastName = "whatever",
					MiddleName = "som middle name",
				};

				db.InsertOrReplace(p, (a, b, isInsert) => b.ColumnName != nameof(TestInsertOrReplaceTable.MiddleName) || withMiddleName || !skipOnInsert);

				p = db.GetTable<TestInsertOrReplaceTable>().Where(x => x.FirstName == p.FirstName).First();

				Assert.That(string.IsNullOrWhiteSpace(p.MiddleName), Is.EqualTo(!withMiddleName && skipOnInsert));

				p.MiddleName = "updated name";
				db.InsertOrReplace(p, (a, b, isInsert) => b.ColumnName != nameof(TestInsertOrReplaceTable.MiddleName) || withMiddleName || skipOnInsert);

				p = db.GetTable<TestInsertOrReplaceTable>().Where(x => x.FirstName == p.FirstName).First();

				if (skipOnInsert || withMiddleName)
					Assert.That(p.MiddleName, Is.EqualTo("updated name"));
				else
					Assert.That(p.MiddleName, Is.Not.EqualTo("updated name"));
			}
		}

		[Test]
		public void AsValueInsertableTest([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceTable>())
			{
				var vi = table.AsValueInsertable();
				vi = vi.Value(x => x.ID, 123).Value(x => x.FirstName, "John");

				var cnt = vi.Insert();
				if (!context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(cnt, Is.EqualTo(1));
				Assert.That(table.Count(x => x.ID == 123 && x.FirstName == "John"), Is.EqualTo(1));
			}
		}

		[Test]
		public void AsValueInsertableEmptyTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var vi = db.Person.AsValueInsertable();

				var ex = Assert.Throws<LinqToDBException>(() => vi.Insert())!;
				Assert.That(ex.Message, Is.EqualTo("Insert query has no setters defined."));
			}
		}

		#region InsertIfNotExists (https://github.com/linq2db/linq2db/issues/3005)
		private int GetEmptyRowCount(string context)
		{
			var provider = GetProviderName(context, out _);

			// those providers generate IF (), which doesn't return rowcount if not entered
			// for some reason it doesn't affect managed sybase provider (provider bug?)
			// and oracle native provider always was "special"
			return provider.IsAnyOf(ProviderName.Sybase)
				|| provider.IsAnyOf(TestProvName.AllOracleNative)
				|| provider.IsAnyOf(TestProvName.AllSqlServer2005)
				? -1
				: 0;
		}

		private int GetNonEmptyRowCount(string context)
		{
			var provider = GetProviderName(context, out _);

			// oracle native provider and rowcount are not familiar with each other
			return provider.IsAnyOf(TestProvName.AllOracleNative)
				? -1
				: 1;
		}

		[Test]
		public void InsertIfNotExists_EmptyInit1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo() { });
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo() { });

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_EmptyInit2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo() { },
					() => new TestInsertOrReplaceInfo() { Id = 1 });
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo() { },
					() => new TestInsertOrReplaceInfo() { Id = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_EmptyNew1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo());
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo());

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_EmptyNew2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo(),
					() => new TestInsertOrReplaceInfo() { Id = 1 });
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => new TestInsertOrReplaceInfo(),
					() => new TestInsertOrReplaceInfo() { Id = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_NullExpr1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => null);
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => null);

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_NullExpr2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => null,
					() => new TestInsertOrReplaceInfo() { Id = 1 });
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					p => null,
					() => new TestInsertOrReplaceInfo() { Id = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_Null1([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					null);
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					null);

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}

		[Test]
		public void InsertIfNotExists_Null2([InsertOrUpdateDataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var cnt1 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					null,
					() => new TestInsertOrReplaceInfo() { Id = 1 });
				var cnt2 = table.InsertOrUpdate(
					() => new TestInsertOrReplaceInfo()
					{
						Id   = 1,
						Name = "test"
					},
					null,
					() => new TestInsertOrReplaceInfo() { Id = 1 });

				Assert.Multiple(() =>
				{
					Assert.That(cnt1, Is.EqualTo(GetNonEmptyRowCount(context)));
					Assert.That(cnt2, Is.EqualTo(GetEmptyRowCount(context)));
				});
			}
		}
		#endregion

		#region issue 2243
		[Table("test_insert_or_replace")]
		public partial class TestInsertOrReplaceInfo
		{
			[Column("id"), PrimaryKey, NotNull]                   public int       Id        { get; set; } // bigint
			[Column("name"), Nullable]                            public string?   Name      { get; set; } // character varying(100)
			[Column("created_by", SkipOnUpdate = true), Nullable] public string?   CreatedBy { get; set; } // character varying(100)
			[Column("updated_by", SkipOnInsert = true), Nullable] public string?   UpdatedBy { get; set; } // character varying(100)
		}

		[Test]
		public void Issue2243([InsertOrUpdateDataSources] string context, [Values(1, 2, 3)] int seed)
		{
			using (var db    = GetDataContext(context))
			using (var table = db.CreateLocalTable<TestInsertOrReplaceInfo>())
			{
				var user = $"TEST_USER{seed}";
				var item = new TestInsertOrReplaceInfo()
				{
					Id        = 1,
					Name      = "Test1",
					CreatedBy = user
				};

				db.InsertOrReplace(item);

				var res = table.Single();
				Assert.Multiple(() =>
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(res.Name, Is.EqualTo("Test1"));
					Assert.That(res.CreatedBy, Is.EqualTo(user));
					Assert.That(res.UpdatedBy, Is.Null);
				});

				item.Name      = "Test2";
				item.UpdatedBy = user;

				db.InsertOrReplace(item);

				res = table.Single();
				Assert.Multiple(() =>
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(res.Name, Is.EqualTo("Test2"));
					Assert.That(res.CreatedBy, Is.EqualTo(user));
					Assert.That(res.UpdatedBy, Is.EqualTo(user));
				});
			}
		}
		#endregion

		#region Issue 3927
		[Table]
		sealed class Issue3927Table
		{
			[Column(DataType = DataType.Char, Length = 11), PrimaryKey, NotNull] public string SerialNumber { get; set; } = null!;
			[Column(DataType = DataType.Int32)] public int PageNumber { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3927")]
		public void Issue3927Test1([DataSources(TestProvName.AllSybase, TestProvName.AllSapHana, TestProvName.AllMariaDB)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3927Table>();

			var serialNumber = "12345678901";
			var pageNumber = 9;
			tb
				.Where(display => display.SerialNumber == serialNumber)
				.Into(tb)
				.Value(display => display.PageNumber, pageNumber)
				.Insert();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3927")]
		public void Issue3927Test2([DataSources(TestProvName.AllSybase, TestProvName.AllSapHana, TestProvName.AllMariaDB)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue3927Table>();

			var serialNumber = "12345678901";
			var pageNumber = 9;
			tb
				.Where(display => display.SerialNumber == serialNumber)
				.Into(tb)
				.Value(display => display.PageNumber, r => pageNumber)
				.Insert();
		}
		#endregion

		#region Issue 4702
		[Table]
		public partial class Issue4702Table
		{
			//[SequenceName("Issue4702Table_Id_seq")]
			[PrimaryKey, Identity] public int Id { get; set; }
			[Column] public string? Text { get; set; }
		}

		[ActiveIssue(
			Details = "Update test to test different RetrieveIdentity modes for all providers with sequences",
			Configurations = [TestProvName.AllFirebird, TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllPostgreSQL, ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSapHana])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4702")]
		public void Issue4702Test([DataSources(false)] string context, [Values] bool useSequence)
		{
			using var db = GetDataConnection(context);
			using var tb = db.CreateLocalTable<Issue4702Table>();

			List<Issue4702Table> records = [
				new() { Text = "Text 1" },
				new() { Text = "Text 2" }
			];

			db.BulkCopy(new BulkCopyOptions { KeepIdentity = true }, records.RetrieveIdentity(db, useSequence));
			tb.Insert(() => new Issue4702Table() { Text = "Text 3" });
		}
		#endregion
	}
}
