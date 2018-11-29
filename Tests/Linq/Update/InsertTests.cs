using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	using Model;
	using System.Collections.Generic;

	[TestFixture]
	public class InsertTests : TestBase
	{
		[Test, DataContextSource(ProviderName.DB2, ProviderName.Informix,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DistinctInsert1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.BeginTransaction();

				try
				{
					db.Types.Delete(c => c.ID > 1000);

					Assert.AreEqual(
						Types.Select(_ => _.ID / 3).Distinct().Count(),
						db
							.Types
							.Select(_ => Math.Floor(_.ID / 3.0))
							.Distinct()
							.Insert(db.Types, _ => new LinqDataTypes
							{
								ID        = (int)(_ + 1001),
								GuidValue = Sql.NewGuid(),
								BoolValue = true
							}));
				}
				finally
				{
					db.Types.Delete(c => c.ID > 1000);
				}
			}
		}

		[Test, DataContextSource(ProviderName.DB2, ProviderName.Informix,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DistinctInsert2(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Types.Delete(c => c.ID > 1000);

					Assert.AreEqual(
						Types.Select(_ => _.ID / 3).Distinct().Count(),
						db.Types
							.Select(_ => Math.Floor(_.ID / 3.0))
							.Distinct()
							.Into(db.Types)
								.Value(t => t.ID,        t => (int)(t + 1001))
								.Value(t => t.GuidValue, t => Sql.NewGuid())
								.Value(t => t.BoolValue, t => true)
							.Insert());
				}
				finally
				{
					db.Types.Delete(c => c.ID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert1(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db.Child
						.Insert(() => new Child
						{
							ParentID = 1,
							ChildID  = id
						}));

					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert2(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db
							.Into(db.Child)
								.Value(c => c.ParentID, () => 1)
								.Value(c => c.ChildID,  () => id)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public async Task Insert2Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						await db
							.Into(db.Child)
								.Value(c => c.ParentID, () => 1)
								.Value(c => c.ChildID,  () => id)
							.InsertAsync());
					Assert.AreEqual(1, await db.Child.CountAsync(c => c.ChildID == id));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert3(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == 11)
							.Insert(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							}));
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public async Task Insert3Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						await db.Child
							.Where(c => c.ChildID == 11)
							.InsertAsync(db.Child, c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							}));
					Assert.AreEqual(1, await db.Child.CountAsync(c => c.ChildID == id));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert31(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == 11)
							.Select(c => new Child
							{
								ParentID = c.ParentID,
								ChildID  = id
							})
							.Insert(db.Child, c => c));
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert4(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c  => c.ParentID)
								.Value(c => c.ChildID,  () => id)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public async Task Insert4Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					await db.Child.DeleteAsync(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						await db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c  => c.ParentID)
								.Value(c => c.ChildID,  () => id)
							.InsertAsync());
					Assert.AreEqual(1, await db.Child.CountAsync(c => c.ChildID == id));
				}
				finally
				{
					await db.Child.DeleteAsync(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert5(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Child)
								.Value(c => c.ParentID, c => c.ParentID)
								.Value(c => c.ChildID,  id)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert6(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(p => p.Value1 == 11);

					Assert.AreEqual(1,
						db.Child
							.Where(c => c.ChildID == 11)
							.Into(db.Parent)
								.Value(p => p.ParentID, c => c.ParentID + 1000)
								.Value(p => p.Value1,   c => (int?)c.ChildID)
							.Insert());
					Assert.AreEqual(1, db.Parent.Count(p => p.Value1 == 11));
				}
				finally
				{
					db.Parent.Delete(p => p.Value1 == 11);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert7(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db
							.Child
								.Value(c => c.ChildID,  () => id)
								.Value(c => c.ParentID, 1)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert8(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child.Delete(c => c.ChildID > 1000);

					Assert.AreEqual(1,
						db
							.Child
								.Value(c => c.ParentID, 1)
								.Value(c => c.ChildID,  () => id)
							.Insert());
					Assert.AreEqual(1, db.Child.Count(c => c.ChildID == id));
				}
				finally
				{
					db.Child.Delete(c => c.ChildID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert9(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Child. Delete(c => c.ParentID > 1000);
					db.Parent.Delete(p => p.ParentID > 1000);

					db.Insert(new Parent { ParentID = id, Value1 = id });

					Assert.AreEqual(1,
						db.Parent
							.Where(p => p.ParentID == id)
							.Insert(db.Child, p => new Child
							{
								ParentID = p.ParentID,
								ChildID  = p.ParentID,
							}));
					Assert.AreEqual(1, db.Child.Count(c => c.ParentID == id));
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
			[Column] public byte[]    BinaryValue;
			[Column] public short     SmallIntValue;
		}

		[Test, DataContextSource]
		public void InsertArray1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);
					types.Insert(() => new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = null });

					Assert.IsNull(types.Single(t => t.ID == 1001).BinaryValue);
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertArray2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var types = db.GetTable<LinqDataTypesArrayTest>();

				try
				{
					types.Delete(t => t.ID > 1000);

					byte[] arr = null;

					types.Insert(() => new LinqDataTypesArrayTest { ID = 1001, BoolValue = true, BinaryValue = arr });

					var res = types.Single(t => t.ID == 1001).BinaryValue;

					Assert.IsNull(res);
				}
				finally
				{
					types.Delete(t => t.ID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertArray3(string context)
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

		[Test, DataContextSource]
		public void InsertArray4(string context)
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

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void InsertUnion1(string context)
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

					Assert.AreEqual(
						Child.     Select(c => new { ParentID = c.ParentID      }).Union(
						GrandChild.Select(c => new { ParentID = c.ParentID ?? 0 })).Count(),
						db.Parent.Count(c => c.ParentID > 1000));
				}
				finally
				{
					db.Parent.Delete(p => p.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertEnum1(string context)
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

					Assert.AreEqual(1,
						db.Parent4
						.Insert(() => new Parent4
						{
							ParentID = 1001,
							Value1   = p.Value1
						}));

					Assert.AreEqual(1, db.Parent4.Count(_ => _.ParentID == id && _.Value1 == p.Value1));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertEnum2(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(_ => _.ParentID > 1000);

					Assert.AreEqual(1,
						db.Parent4
							.Value(_ => _.ParentID, id)
							.Value(_ => _.Value1,   TypeValue.Value1)
						.Insert());

					Assert.AreEqual(1, db.Parent4.Count(_ => _.ParentID == id));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertEnum3(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					var id = 1001;

					db.Parent4.Delete(_ => _.ParentID > 1000);

					Assert.AreEqual(1,
						db.Parent4
							.Value(_ => _.ParentID, id)
							.Value(_ => _.Value1,   () => TypeValue.Value1)
						.Insert());

					Assert.AreEqual(1, db.Parent4.Count(_ => _.ParentID == id));
				}
				finally
				{
					db.Parent4.Delete(_ => _.ParentID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertNull(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Parent.Delete(p => p.ParentID == 1001);

					Assert.AreEqual(1,
						db
							.Into(db.Parent)
								.Value(p => p.ParentID, 1001)
								.Value(p => p.Value1,   (int?)null)
							.Insert());
					Assert.AreEqual(1, db.Parent.Count(p => p.ParentID == 1001));
				}
				finally
				{
					db.Parent.Delete(p => p.Value1 == 1001);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertWithIdentity1(string context)
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

				Assert.NotNull(id);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.NotNull (john);
				Assert.AreEqual(id, john.ID);
			}
		}

		[Test, DataContextSource]
		public async Task InsertWithIdentity1Async(string context)
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

				Assert.NotNull(id);

				var john = await db.Person.SingleAsync(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.NotNull (john);
				Assert.AreEqual(id, john.ID);
			}
		}

		[Test, DataContextSource]
		public void InsertWithIdentity2(string context)
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

				Assert.NotNull(id);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.NotNull (john);
				Assert.AreEqual(id, john.ID);
			}
		}

		[Test, DataContextSource]
		public async Task InsertWithIdentity2Async(string context)
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

				Assert.NotNull(id);

				var john = await db.Person.SingleAsync(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.NotNull (john);
				Assert.AreEqual(id, john.ID);
			}
		}

		[Test, DataContextSource]
		public void InsertWithIdentity3(string context)
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

				Assert.NotNull(id);

				var john = db.Person.Single(p => p.FirstName == "John" && p.LastName == "Shepard");

				Assert.NotNull (john);
				Assert.AreEqual(id, john.ID);
			}
		}

		[Test, DataContextSource]
		public void InsertWithIdentity4(string context)
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

					Assert.NotNull(id);

					var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.NotNull (john);
					Assert.AreEqual(id, john.ID);
				}
			}
		}

		[Test, DataContextSource]
		public async Task InsertWithIdentity4Async(string context)
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

					Assert.NotNull(id);

					var john = await db.Person.SingleAsync(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.NotNull (john);
					Assert.AreEqual(id, john.ID);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertWithIdentity5(string context)
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

					Assert.NotNull(id);

					var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

					Assert.NotNull (john);
					Assert.AreEqual(id, john.ID);
				}
			}
		}

		class GuidID
		{
			[Identity] public Guid ID;
					   public int  Field1;
		}

		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, TestProvName.SqlAzure)]
		public void InsertWithGuidIdentity(string context)
		{
			using (var db = new DataConnection(context))
			{
				var id = (Guid)db.InsertWithIdentity(new GuidID { Field1 = 1 });
				Assert.AreNotEqual(Guid.Empty, id);
			}
		}

		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, TestProvName.SqlAzure)]
		public void InsertWithGuidIdentityOutput(string context)
		{
			try
			{
				SqlServerConfiguration.GenerateScopeIdentity = false;
				using (var db = new DataConnection(context))
				{
					var id = (Guid) db.InsertWithIdentity(new GuidID {Field1 = 1});
					Assert.AreNotEqual(Guid.Empty, id);
				}
			}
			finally
			{
				SqlServerConfiguration.GenerateScopeIdentity = true;
			}
		}

		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, TestProvName.SqlAzure)]
		public void InsertWithIdentityOutput(string context)
		{
			using (var db = GetDataContext(context))
			using (new DeletePerson(db))
			{
				try
				{
					SqlServerConfiguration.GenerateScopeIdentity = false;
					for (var i = 0; i < 2; i++)
					{
						var person = new Person
						{
							FirstName = "John" + i,
							LastName  = "Shepard",
							Gender    = Gender.Male
						};

						var id = db.InsertWithIdentity(person);

						Assert.NotNull(id);

						var john = db.Person.Single(p => p.FirstName == "John" + i && p.LastName == "Shepard");

						Assert.NotNull(john);
						Assert.AreEqual(id, john.ID);
					}
				}
				finally
				{
					SqlServerConfiguration.GenerateScopeIdentity = true;
				}
			}
		}

		class GuidID2
		{
			[Identity] public Guid ID;
		}

		[Test, IncludeDataContextSource(
			ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, TestProvName.SqlAzure)]
		public void InsertWithGuidIdentity2(string context)
		{
			using (var db = new DataConnection(context))
			{
				var id = (Guid)db.InsertWithIdentity(new GuidID2 {});
			}
		}

		[Test, DataContextSource]
		public void InsertOrUpdate1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 0;

				try
				{
					id = Convert.ToInt32(db.Person.InsertWithIdentity(() => new Person
					{
						FirstName = "John",
						LastName  = "Shepard",
						Gender    = Gender.Male
					}));

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

					Assert.AreEqual("3", db.Patient.Single(p => p.PersonID == id).Diagnosis);
				}
				finally
				{
					db.Patient.Delete(p => p.PersonID == id);
					db.Person. Delete(p => p.ID       == id);
				}
			}
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void InsertOrUpdate2(string context)
		{
			using (var db = GetDataContext(context))
			{
				int id;
				using (new DisableLogging())
					id = Convert.ToInt32(db.Person.InsertWithIdentity(() => new Person
					{
						FirstName = "test",
						LastName  = "subject",
						Gender    = Gender.Unknown
					}));

				try
				{
					var records = db.Patient.InsertOrUpdate(
						() => new Patient
						{
							PersonID  = id,
							Diagnosis = "negative"
						},
						p => new Patient
						{
						});

					try
					{
						List<Patient> patients;

						using (new DisableLogging())
							patients = db.Patient.Where(p => p.PersonID == id).ToList();

						Assert.AreEqual(1, records);
						Assert.AreEqual(1, patients.Count);
						Assert.AreEqual(id, patients[0].PersonID);
						Assert.AreEqual("negative", patients[0].Diagnosis);

						records = db.Patient.InsertOrUpdate(
							() => new Patient
							{
								PersonID  = id,
								Diagnosis = "positive"
							},
							p => new Patient
							{
							});

						using (new DisableLogging())
							patients = db.Patient.Where(p => p.PersonID == id).ToList();

						Assert.LessOrEqual(records, 0);
						Assert.AreEqual(1, patients.Count);
						Assert.AreEqual(id, patients[0].PersonID);
						Assert.AreEqual("negative", patients[0].Diagnosis);
					}
					finally
					{
						using (new DisableLogging())
							db.Patient.Delete(p => p.PersonID == id);
					}
				}
				finally
				{
					using (new DisableLogging())
						db.Person.Delete(p => p.ID == id);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertOrReplace1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 0;

				try
				{
					id = Convert.ToInt32(db.Person.InsertWithIdentity(() => new Person
					{
						FirstName = "John",
						LastName  = "Shepard",
						Gender    = Gender.Male
					}));

					for (var i = 0; i < 3; i++)
					{
						db.InsertOrReplace(new Patient
						{
							PersonID  = id,
							Diagnosis = ("abc" + i).ToString(),
						});
					}

					Assert.AreEqual("abc2", db.Patient.Single(p => p.PersonID == id).Diagnosis);
				}
				finally
				{
					db.Patient.Delete(p => p.PersonID == id);
					db.Person. Delete(p => p.ID       == id);
				}
			}
		}

		[Test, DataContextSource]
		public async Task InsertOrReplace1Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 0;

				try
				{
					id = Convert.ToInt32(db.Person.InsertWithIdentity(() => new Person
					{
						FirstName = "John",
						LastName  = "Shepard",
						Gender    = Gender.Male
					}));

					for (var i = 0; i < 3; i++)
					{
						await db.InsertOrReplaceAsync(new Patient
						{
							PersonID  = id,
							Diagnosis = ("abc" + i).ToString(),
						});
					}

					Assert.AreEqual("abc2", (await db.Patient.SingleAsync(p => p.PersonID == id)).Diagnosis);
				}
				finally
				{
					await db.Patient.Where     (p => p.PersonID == id).DeleteAsync();
					await db.Person.DeleteAsync(p => p.ID       == id);
				}
			}
		}

		[Test]
		public void InsertOrReplaceWithIdentity()
		{
			Assert.Throws<LinqException>(() =>
			{
				using (var db = new TestDataConnection())
				{
					var p = new Person()
					{
						FirstName = Guid.NewGuid().ToString(),
						ID = 1000,
					};

					db.InsertOrReplace(p);
				}
			});
		}

		[Test, DataContextSource]
		public void InsertOrUpdate3(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 0;

				try
				{
					id = Convert.ToInt32(db.Person.InsertWithIdentity(() => new Person
					{
						FirstName = "John",
						LastName  = "Shepard",
						Gender    = Gender.Male
					}));

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
								PersonID  = id,
								//Diagnosis = diagnosis,
							});

						diagnosis = (diagnosis.Length + i).ToString();
					}

					Assert.AreEqual("3", db.Patient.Single(p => p.PersonID == id).Diagnosis);
				}
				finally
				{
					db.Patient.Delete(p => p.PersonID == id);
					db.Person. Delete(p => p.ID       == id);
				}
			}
		}

		[Test, DataContextSource]
		public async Task InsertOrUpdate3Async(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 0;

				try
				{
					id = await db.Person.InsertWithInt32IdentityAsync(() => new Person
					{
						FirstName = "John",
						LastName  = "Shepard",
						Gender    = Gender.Male
					});

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
								PersonID  = id,
								//Diagnosis = diagnosis,
							});

						diagnosis = (diagnosis.Length + i).ToString();
					}

					Assert.AreEqual("3", (await db.Patient.SingleAsync(p => p.PersonID == id)).Diagnosis);
				}
				finally
				{
					await db.Patient.DeleteAsync(p => p.PersonID == id);
					await db.Person. DeleteAsync(p => p.ID       == id);
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.OracleNative, ProviderName.OracleManaged)]
		public void InsertBatch1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				try
				{
					((DataConnection)db).BulkCopy(1, new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null    },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null    }
					});
				}
				finally
				{
					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void InsertBatch2(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Types2.Delete(_ => _.ID > 1000);

				try
				{
					((DataConnection)db).BulkCopy(100, new[]
					{
						new LinqDataTypes2 { ID = 1003, MoneyValue = 0m, DateTimeValue = null,         BoolValue = true,  GuidValue = new Guid("ef129165-6ffe-4df9-bb6b-bb16e413c883"), SmallIntValue =  null, IntValue = null    },
						new LinqDataTypes2 { ID = 1004, MoneyValue = 0m, DateTimeValue = DateTime.Now, BoolValue = false, GuidValue = null,                                             SmallIntValue =  2,    IntValue = 1532334 }
					});
				}
				finally
				{
					db.Types2.Delete(_ => _.ID > 1000);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert11(string context)
		{
			var p = new ComplexPerson { Name = new FullName { FirstName = "fn", LastName = "ln" }, Gender = Gender.Male };

			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
				{
					db.Insert(p);

					var inserted = db.GetTable<ComplexPerson>().Single(p2 => p2.ID > id);

					Assert.AreEqual(p.Name.FirstName, inserted.Name.FirstName);
					Assert.AreEqual(p.Name.LastName, inserted.Name.LastName);
					Assert.AreEqual(p.Gender, inserted.Gender);

				}
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert12(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
				{
					db
						.Into(db.GetTable<ComplexPerson>())
							.Value(_ => _.Name.FirstName, "FirstName")
							.Value(_ => _.Name.LastName,  () => "LastName")
							.Value(_ => _.Gender,         Gender.Female)
						.Insert();
				}
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test, DataContextSource]
		public void Insert13(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
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
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test, DataContextSource(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.SqlServer2000,
			ProviderName.SqlServer2005, ProviderName.Sybase, ProviderName.SybaseManaged)]
		public void Insert14(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Person.Delete(p => p.FirstName.StartsWith("Insert14"));

					Assert.AreEqual(1,
						db.Person
						.Insert(() => new Person
						{
							FirstName = "Insert14" + db.Person.Where(p => p.ID == 1).Select(p => p.FirstName).FirstOrDefault(),
							LastName  = "Shepard",
							Gender = Gender.Male
						}));

					Assert.AreEqual(1, db.Person.Count(p => p.FirstName.StartsWith("Insert14")));
				}
				finally
				{
					db.Person.Delete(p => p.FirstName.StartsWith("Insert14"));
				}
			}
		}

		[Test, DataContextSource]
		public void Insert15(string context)
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
					Assert.AreEqual(1, cnt);
				}
				finally
				{
					db.Person.Where(_ => _.FirstName.StartsWith("Insert15")).Delete();
				}
			}
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.SqlCe, ProviderName.SapHana)]
		public void InsertSingleIdentity(string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.TestIdentity.Delete();

					var id = db.TestIdentity.InsertWithIdentity(() => new TestIdentity {});

					Assert.NotNull(id);
				}
				finally
				{
					db.TestIdentity.Delete();
				}
			}
		}

		[Table("LinqDataTypes")]
		class TestConvertTable1
		{
			[PrimaryKey]                        public int      ID;
			[Column(DataType = DataType.Int64)] public TimeSpan BigIntValue;
		}

		[Test, DataContextSource]
		public void InsertConverted(string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<TestConvertTable1>();

				try
				{
					tbl.Delete(r => r.ID >= 1000);

					var tt = TimeSpan.FromMinutes(1);

					tbl.Insert(() => new TestConvertTable1 { ID = 1001, BigIntValue = tt });

					Assert.AreEqual(tt, tbl.First(t => t.ID == 1001).BigIntValue);
				}
				finally
				{
					tbl.Delete(r => r.ID >= 1000);
				}
			}
		}

		[Table("LinqDataTypes")]
		class TestConvertTable2
		{
			[PrimaryKey]                        public int       ID;
			[Column(DataType = DataType.Int64)] public TimeSpan? BigIntValue;
		}

		[Test, DataContextSource]
		public void InsertConvertedNullable(string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<TestConvertTable2>();

				try
				{
					tbl.Delete(r => r.ID >= 1000);

					var tt = TimeSpan.FromMinutes(1);

					tbl.Insert(() => new TestConvertTable2 { ID = 1001, BigIntValue = tt });

					Assert.AreEqual(tt, tbl.First(t => t.ID == 1001).BigIntValue);
				}
				finally
				{
					tbl.Delete(r => r.ID >= 1000);
				}
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008)]//, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void InsertWith(string context)
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

		[ActiveIssue(":NEW as parameter", Configuration = ProviderName.OracleNative)]
		[Test, DataContextSource]
		public void InsertByTableName(string context)
		{
			const string schemaName = null;
			const string tableName  = "xxPerson";

			using (var db = GetDataContext(context))
			{
				try
				{
					var table = db.CreateTable<Person>(tableName, schemaName: schemaName);

					Assert.AreEqual(tableName, table.TableName);
					Assert.AreEqual(schemaName, table.SchemaName);

					var person = new Person()
					{
						FirstName = "Steven",
						LastName = "King",
						Gender = Gender.Male,
					};

					// insert a row into the table
					db.Insert(person, tableName: tableName, schemaName: schemaName);
					var newId1 = db.InsertWithInt32Identity(person, tableName: tableName, schemaName: schemaName);
					var newId2 = db.InsertWithIdentity(person, tableName: tableName, schemaName: schemaName);

					var newCount = table.Count();
					Assert.AreEqual(3, newCount);

					Assert.AreNotEqual(newId1, newId2);

					var integritycount = table.Where(p => p.FirstName == "Steven" && p.LastName == "King" && p.Gender == Gender.Male).Count();
					Assert.AreEqual(3, integritycount);

					table.Drop();
				}
				finally
				{
					db.DropTable<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				}
			}
		}

		[ActiveIssue(":NEW as parameter", Configuration = ProviderName.OracleNative)]
		[Test, DataContextSource]
		public async Task InsertByTableNameAsync(string context)
		{
			const string schemaName = null;
			const string tableName  = "xxPerson";

			using (var db = GetDataContext(context))
			{
				try
				{
					var table = await db.CreateTableAsync<Person>(tableName, schemaName: schemaName);

					Assert.AreEqual(tableName, table.TableName);
					Assert.AreEqual(schemaName, table.SchemaName);

					var person = new Person()
					{
						FirstName = "Steven",
						LastName = "King",
						Gender = Gender.Male,
					};

					// insert a row into the table
					await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
					var newId1 = await db.InsertWithInt32IdentityAsync(person, tableName: tableName, schemaName: schemaName);
					var newId2 = await db.InsertWithIdentityAsync(person, tableName: tableName, schemaName: schemaName);

					var newCount = await table.CountAsync();
					Assert.AreEqual(3, newCount);

					Assert.AreNotEqual(newId1, newId2);

					var integritycount = await table.Where(p => p.FirstName == "Steven" && p.LastName == "King" && p.Gender == Gender.Male).CountAsync();
					Assert.AreEqual(3, integritycount);
					await table.DropAsync();
				}
				finally
				{
					await db.DropTableAsync<Person>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				}
			}
		}

		[Test, DataContextSource]
		public void InsertOrReplaceByTableName(string context)
		{
			const string schemaName = null;
			var tableName  = "xxPatient" + TestUtils.GetNext().ToString();

			using (var db = GetDataContext(context))
			{
				db.DropTable<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				var table = db.CreateTable<Patient>(tableName, schemaName: schemaName);

				try
				{
					Assert.AreEqual(tableName, table.TableName);
					Assert.AreEqual(schemaName, table.SchemaName);

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

					Assert.AreEqual(2, table.Count());

					db.InsertOrReplace(person1, tableName: tableName, schemaName: schemaName);
					db.InsertOrReplace(person2, tableName: tableName, schemaName: schemaName);

					Assert.AreEqual(2, table.Count());
				}
				finally
				{
					table.Drop(throwExceptionIfNotExists: false);
				}
			}
		}

		[Test, DataContextSource]
		public async Task InsertOrReplaceByTableNameAsync(string context)
		{
			const string schemaName = null;
			var tableName  = "xxPatient" + TestUtils.GetNext().ToString();

			using (var db = GetDataContext(context))
			{
				await db.DropTableAsync<Patient>(tableName, schemaName: schemaName, throwExceptionIfNotExists: false);
				var table = await db.CreateTableAsync<Patient>(tableName, schemaName: schemaName);
				try
				{
					Assert.AreEqual(tableName, table.TableName);
					Assert.AreEqual(schemaName, table.SchemaName);

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

					Assert.AreEqual(2, await table.CountAsync());

					await db.InsertOrReplaceAsync(person1, tableName: tableName, schemaName: schemaName);
					await db.InsertOrReplaceAsync(person2, tableName: tableName, schemaName: schemaName);

					Assert.AreEqual(2, await table.CountAsync());
				}
				finally
				{
					await table.DropAsync(throwExceptionIfNotExists: false);
				}
			}
		}
	}
}
