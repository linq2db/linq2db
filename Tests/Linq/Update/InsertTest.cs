﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.Update
{
	using Model;

	[TestFixture]
	public class InsertTest : TestBase
	{
		[Test]
		public void DistinctInsert1([DataContexts(ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access)] string context)
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

		[Test]
		public void DistinctInsert2([DataContexts(ProviderName.DB2, ProviderName.Informix, ProviderName.PostgreSQL, ProviderName.SQLite, ProviderName.Access)] string context)
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

		[Test]
		public void Insert1([DataContexts] string context)
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

		[Test]
		public void Insert2([DataContexts] string context)
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

		[Test]
		public void Insert3([DataContexts] string context)
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

		[Test]
		public void Insert31([DataContexts] string context)
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

		[Test]
		public void Insert4([DataContexts] string context)
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

		[Test]
		public void Insert5([DataContexts] string context)
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

		[Test]
		public void Insert6([DataContexts] string context)
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
								.Value(p => p.ParentID, c => c.ParentID)
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

		[Test]
		public void Insert7([DataContexts] string context)
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

		[Test]
		public void Insert8([DataContexts] string context)
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

		[Test]
		public void Insert9([DataContexts] string context)
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

		[Test]
		public void InsertArray1([DataContexts] string context)
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

		[Test]
		public void InsertArray2([DataContexts] string context)
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

		[Test]
		public void InsertArray3([DataContexts] string context)
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
		public void InsertArray4([DataContexts] string context)
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
		public void InsertUnion1([DataContexts] string context)
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

		[Test]
		public void InsertEnum1([DataContexts] string context)
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

		[Test]
		public void InsertEnum2([DataContexts] string context)
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

		[Test]
		public void InsertEnum3([DataContexts] string context)
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

		[Test]
		public void InsertNull([DataContexts] string context)
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

		[Test]
		public void InsertWithIdentity1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Person.Delete(p => p.ID > 2);

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
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			}
		}

		[Test]
		public void InsertWithIdentity2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Person.Delete(p => p.ID > 2);

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
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			}
		}

		[Test]
		public void InsertWithIdentity3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					db.Person.Delete(p => p.ID > 2);

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
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			}
		}

		[Test]
		public void InsertWithIdentity4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					for (var i = 0; i < 2; i++)
					{
						db.Person.Delete(p => p.ID > 2);

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
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			}
		}

		[Test]
		public void InsertWithIdentity5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				try
				{
					for (var i = 0; i < 2; i++)
					{
						db.Person.Delete(p => p.ID > 2);

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
				finally
				{
					db.Person.Delete(p => p.ID > 2);
				}
			}
		}

		[Test]
		public void InsertOrUpdate1([DataContexts] string context)
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

		[Test]
		public void InsertOrReplace1([DataContexts] string context)
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

		[Test]
		public void InsertOrUpdate3([DataContexts] string context)
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

		[Test]
		public void InsertBatch1([IncludeDataContexts(ProviderName.Oracle)] string context)
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

		[Test]
		public void InsertBatch2([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
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

		public class FullName
		{
			           public string FirstName     { get; set; }
			           public string LastName;
			[Nullable] public string MiddleName;
		}

		[Table("Person", IsColumnAttributeRequired=false)]
		[Column("FirstName",  "Name.FirstName")]
		[Column("LastName",   "Name.LastName")]
		[Column("MiddleName", "Name.MiddleName")]
		public class TestPerson1
		{
			[Identity]
			[SequenceName(ProviderName.Firebird, "PersonID")]
			[Column("PersonID", IsPrimaryKey=true)]
			public int ID;

			public string Gender;

			public FullName Name;
		}

		[Test]
		public void Insert11([DataContexts] string context)
		{
			var p = new TestPerson1 { Name = new FullName { FirstName = "fn", LastName = "ln" }, Gender = "M" };

			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
				{
					db.Insert(p);
				}
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test]
		public void Insert12([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
				{
					db
						.Into(db.GetTable<TestPerson1>())
							.Value(_ => _.Name.FirstName, "FirstName")
							.Value(_ => _.Name.LastName,  () => "LastName")
							.Value(_ => _.Gender,         "F")
						.Insert();
				}
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test]
		public void Insert13([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = db.Person.Max(t => t.ID);

				try
				{
					db
						.GetTable<TestPerson1>()
						.Insert(() => new TestPerson1
						{
							Name = new FullName
							{
								FirstName = "FirstName",
								LastName  = "LastName"
							},
							Gender = "M",
						});
				}
				finally
				{
					db.Person.Delete(t => t.ID > id);
				}
			}
		}

		[Test]
		public void Insert14([DataContexts(
			ProviderName.SqlCe, ProviderName.Access, ProviderName.SqlServer2000,
			ProviderName.SqlServer2005, ProviderName.Sybase)] string context)
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

		[Test]
		public void InsertSingleIdentity([DataContexts(ProviderName.Informix, ProviderName.SqlCe)] string context)
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
	}
}
