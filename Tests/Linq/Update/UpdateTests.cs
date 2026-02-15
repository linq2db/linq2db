using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

#region ReSharper disable
// ReSharper disable ConvertToConstant.Local
#endregion

namespace Tests.xUpdate
{
	[TestFixture]
//	[Order(10000)]
	public class UpdateTests : TestBase
	{
		[Test]
		public void Update1([DataSources(ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Insert(parent);

				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID), Is.EqualTo(1));

				var cnt = db.Parent.Update(p => p.ParentID == parent.ParentID, p => new Parent { ParentID = p.ParentID + 1 });
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID + 1), Is.EqualTo(1));
			}
		}

		[Test]
		public async Task Update1Async([DataSources(ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				await db.InsertAsync(parent);

				Assert.That(await db.Parent.CountAsync(p => p.ParentID == parent.ParentID), Is.EqualTo(1));

				var cnt = await db.Parent.UpdateAsync(p => p.ParentID == parent.ParentID, p => new Parent { ParentID = p.ParentID + 1 });
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(await db.Parent.CountAsync(p => p.ParentID == parent.ParentID + 1), Is.EqualTo(1));
			}
		}

		[Test]
		public void Update2([DataSources(ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Insert(parent);

				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID), Is.EqualTo(1));

				var cnt = db.Parent.Where(p => p.ParentID == parent.ParentID).Update(p => new Parent { ParentID = p.ParentID + 1 });
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Parent.Count(p => p.ParentID == parent.ParentID + 1), Is.EqualTo(1));
			}
		}

		[Test]
		public async Task Update2Async([DataSources(ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				await db.InsertAsync(parent);

				Assert.That(await db.Parent.CountAsync(p => p.ParentID == parent.ParentID), Is.EqualTo(1));

				var cnt = await db.Parent.Where(p => p.ParentID == parent.ParentID).UpdateAsync(p => new Parent { ParentID = p.ParentID + 1 });
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(await db.Parent.CountAsync(p => p.ParentID == parent.ParentID + 1), Is.EqualTo(1));
			}
		}

		[Test]
		public void Update3([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(db.Child.Where(c => c.ChildID == id && c.Parent!.Value1 == 1).Update(c => new Child { ChildID = c.ChildID + 1 }), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update4([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(db.Child
							.Where(c => c.ChildID == id && c.Parent!.Value1 == 1)
								.Set(c => c.ChildID, c => c.ChildID + 1)
							.Update(), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update4String([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1001;

				var updatable =
					db.Child
						.Where(c => c.ChildID == id && c.Parent!.Value1 == 1)
						.Set(c => c.ChildID, c => c.ChildID + 1);

				updatable.Update();
			}
		}

		[Test]
		public async Task Update4Async([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				await db.Child.InsertAsync(() => new Child { ParentID = 1, ChildID = id });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(await db.Child.CountAsync(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(await db.Child
							.Where(c => c.ChildID == id && c.Parent!.Value1 == 1)
								.Set(c => c.ChildID, c => c.ChildID + 1)
							.UpdateAsync(), Is.EqualTo(1));
					Assert.That(await db.Child.CountAsync(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update5([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(db.Child
							.Where(c => c.ChildID == id && c.Parent!.Value1 == 1)
								.Set(c => c.ChildID, () => id + 1)
							.Update(), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update6([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Insert(new Parent4 { ParentID = id, Value1 = TypeValue.Value1 });

				Assert.That(db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value1), Is.EqualTo(1));

				var cnt = db.Parent4
						.Where(p => p.ParentID == id)
							.Set(p => p.Value1, () => TypeValue.Value2)
						.Update();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value2), Is.EqualTo(1));
			}
		}

		[Test]
		public void Update7([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Insert(new Parent4 { ParentID = id, Value1 = TypeValue.Value1 });

				Assert.That(db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value1), Is.EqualTo(1));
				var cnt = db.Parent4
						.Where(p => p.ParentID == id)
							.Set(p => p.Value1, TypeValue.Value2)
						.Update();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
				Assert.That(db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value2), Is.EqualTo(1));

				cnt = db.Parent4
						.Where(p => p.ParentID == id)
							.Set(p => p.Value1, TypeValue.Value3)
						.Update();
				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				Assert.That(db.Parent4.Count(p => p.ParentID == id && p.Value1 == TypeValue.Value3), Is.EqualTo(1));
			}
		}

		[Test]
		public void Update8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var parent = new Parent1 { ParentID = 1001, Value1 = 1001 };

				db.Insert(parent);

				parent.Value1++;

				db.Update(parent);

				Assert.That(db.Parent.Single(p => p.ParentID == parent.ParentID).Value1, Is.EqualTo(1002));
			}
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void Update9Old(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllClickHouse,
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				var q =
						from c in db.Child
						join p in db.Parent on c.ParentID equals p.ParentID
						where c.ChildID == id && c.Parent!.Value1 == 1
						select new { c, p };
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(q.Update(db.Child, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update9(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllClickHouse,
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				var q =
						from c in db.Child
						join p in db.Parent on c.ParentID equals p.ParentID
						where c.ChildID == id && c.Parent!.Value1 == 1
						select new { c, p };
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(q.Update(q => q.c, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void Update10Old(
			[DataSources(
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				var q =
						from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
						where c.ChildID == id && c.Parent!.Value1 == 1
						select new { c, p };
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(q.Update(db.Child, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void Update10(
			[DataSources(
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				ProviderName.SqlCe)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				var q =
						from p in db.Parent
						join c in db.Child on p.ParentID equals c.ParentID
						where c.ChildID == id && c.Parent!.Value1 == 1
						select new { c, p };
				using (Assert.EnterMultipleScope())
				{
					Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));
					Assert.That(q.Update(q => q.c, _ => new Child { ChildID = _.c.ChildID + 1, ParentID = _.p.ParentID }), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		//[Test]
		//public void Update11([DataSources] string context)
		//{
		//	using (var db = GetDataContext(context))
		//	{
		//		db.BeginTransaction();

		//		var q = db.GetTable<LinqDataTypes2>().Union(db.GetTable<LinqDataTypes2>());

		//		//db.GetTable<LinqDataTypes2>().Update(_ => q.Contains(_), _ => new LinqDataTypes2 { GuidValue = _.GuidValue });

		//		q.Update(_ => new LinqDataTypes2 { GuidValue = _.GuidValue });
		//	}
		//}

		[Test]
		public void Update12(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.Update(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });
			}
		}

		[Test]
		public async Task Update12Async(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				await (
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.UpdateAsync(q => q.p1, q => new Parent { ParentID = q.p2.ParentID });
			}
		}

		[Test]
		public void Update13(
			[DataSources(
				ProviderName.SqlCe,
				ProviderName.DB2,
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p1 in db.Parent
					join p2 in db.Parent on p1.ParentID equals p2.ParentID
					where p1.ParentID < 3
					select new { p1, p2 }
				)
				.Update(q => q.p2, q => new Parent { ParentID = q.p1.ParentID });
			}
		}

		[Test]
		public void Update14([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				db.Insert(new Person()
				{
					ID        = 100,
					FirstName = "Update14",
					LastName  = "whatever"
				});

				var name = "Update14";
				var idx = 4;

				db.Person
					.Where(_ => _.FirstName.StartsWith("Update14"))
					.Update(p => new Person()
					{
						LastName = (Sql.AsSql(name).Length + idx).ToString(),
					});

				var cnt = db.Person.Where(_ => _.FirstName.StartsWith("Update14")).Count();
				Assert.That(cnt, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestUpdateWithColumnFilter1([DataSources] string context, [Values] bool withMiddleName)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var newName = "UpdateColumnFilterUpdated";
				var p = new Person()
				{
					ID         = 100,
					FirstName  = newName,
					LastName   = "whatever",
					MiddleName = "som middle name",
					Gender     = Gender.Male
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
		}

		[Test]
		public void TestUpdateWithColumnFilter2([DataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var newName = "UpdateColumnFilterUpdated";
				var p = new Person()
				{
					ID        = 100,
					FirstName = "UpdateColumnFilter",
					LastName  = "whatever"
				};

				db.Insert(p);

				p = db.GetTable<Person>().Where(x => x.FirstName == p.FirstName).Single();

				p.FirstName = newName;
				p.LastName  = newName;

				var columsToUpdate = new HashSet<string> { nameof(p.FirstName) };

				db.Update(p, (a, b) => columsToUpdate.Contains(b.ColumnName));

				var updatedPerson = db.GetTable<Person>().Where(x => x.ID == p.ID).Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(updatedPerson.LastName, Is.EqualTo("whatever"));
					Assert.That(updatedPerson.FirstName, Is.EqualTo(newName));
				}

				// test for cached update query - must update both columns
				db.Update(p);
				updatedPerson = db.GetTable<Person>().Where(_ => _.ID == p.ID).Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(updatedPerson.LastName, Is.EqualTo(newName));
					Assert.That(updatedPerson.FirstName, Is.EqualTo(newName));
				}
			}
		}

		[Test]
		public void UpdateComplex1([DataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new ComplexPerson2
				{
					Name = new FullName
					{
						FirstName = "UpdateComplex",
						LastName  = "Empty"
					}
				};

				int id;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					person.ID = id = 100;
					db.Insert(person);
				}
				else
					id = db.InsertWithInt32Identity(person);

				var obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);
				obj.Name.LastName = obj.Name.FirstName;

				db.Update(obj);

				obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);

				Assert.That(obj.Name.LastName, Is.EqualTo(obj.Name.FirstName));
			}
		}

		[Test]
		public async Task UpdateComplex1Async([DataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new ComplexPerson2
				{
					Name = new FullName
					{
						FirstName = "UpdateComplex",
						LastName  = "Empty"
					}
				};

				int id;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					person.ID = id = 100;
					db.Insert(person);
				}
				else
					id = db.InsertWithInt32Identity(person);

				var obj = await db.GetTable<ComplexPerson2>().FirstAsync(_ => _.ID == id);
				obj.Name.LastName = obj.Name.FirstName;

				await db.UpdateAsync(obj);

				obj = await db.GetTable<ComplexPerson2>().FirstAsync(_ => _.ID == id);

				Assert.That(obj.Name.LastName, Is.EqualTo(obj.Name.FirstName));
			}
		}

		[Test]
		public void SetWithTernaryOperatorIssue([DataSources] string context)
		{
			Gender? nullableGender = Gender.Other;

			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new ComplexPerson2
				{
					Name = new FullName
					{
						FirstName = "UpdateComplex",
						LastName  = "Empty",
					},
					Gender = Gender.Male
				};

				int id;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					person.ID = id = 100;
					db.Insert(person);
				}
				else
					id = db.InsertWithInt32Identity(person);

				var cnt = db.GetTable<ComplexPerson2>()
						.Where(_ => _.Name.FirstName.StartsWith("UpdateComplex"))
						.Set(_ => _.Gender, _ => nullableGender.HasValue ? nullableGender.Value : _.Gender)
						.Update();

				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				var obj = db.GetTable<ComplexPerson2>()
						.First(_ => _.ID == id);

				Assert.That(obj.Gender, Is.EqualTo(Gender.Other));
			}
		}

		[Test]
		public void UpdateComplex2([DataSources] string context)
		{
			ResetPersonIdentity(context);

			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var person = new ComplexPerson2
				{
					Name = new FullName
					{
						FirstName = "UpdateComplex",
						LastName  = "Empty",
					}
				};

				int id;
				if (context.IsAnyOf(TestProvName.AllClickHouse))
				{
					person.ID = id = 100;
					db.Insert(person);
				}
				else
					id = db.InsertWithInt32Identity(person);

				var cnt = db.GetTable<ComplexPerson2>()
						.Where(_ => _.Name.FirstName.StartsWith("UpdateComplex"))
						.Set(_ => _.Name.LastName, _ => _.Name.FirstName)
						.Update();

				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));

				var obj = db.GetTable<ComplexPerson2>().First(_ => _.ID == id);

				Assert.That(obj.Name.LastName, Is.EqualTo(obj.Name.FirstName));
			}
		}

		[YdbMemberNotFound]
		[Obsolete("Remove test after API removed")]
		[Test]
		public void UpdateAssociation1Old([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child.Insert(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(parents.Update(db.Parent, x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[YdbMemberNotFound]
		[Obsolete("Remove test after API removed")]
		[Test]
		public async Task UpdateAssociation1AsyncOld([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				await db.Parent.InsertAsync(() => new Parent { ParentID = parentId, Value1 = parentId });
				await db.Child.InsertAsync(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(await parents.UpdateAsync(db.Parent, x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void UpdateAssociation1([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child .Insert(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(parents.Update(q => q, x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[YdbMemberNotFound]
		[Test]
		public async Task UpdateAssociation1Async([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				await db.Parent.InsertAsync(() => new Parent { ParentID = parentId, Value1 = parentId });
				await db.Child.InsertAsync(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(await parents.UpdateAsync(q => q, x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void UpdateAssociation2([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child.Insert(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(parents.Update(x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void UpdateAssociation3([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child.Insert(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(parents.Update(x => x.ParentID > 0, x => new Parent { Value1 = 5 }), Is.EqualTo(1));
			}
		}

		[Test]
		public void UpdateAssociation4([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child.Insert(() => new Child { ChildID = childId, ParentID = parentId });

				var parents =
						from child in db.Child
						where child.ChildID == childId
						select child.Parent;

				Assert.That(parents.Set(x => x.Value1, 5).Update(), Is.EqualTo(1));
			}
		}

		static readonly Func<TestDataConnection,int,string,int> _updateQuery =
			CompiledQuery.Compile<TestDataConnection,int,string,int>((ctx,key,value) =>
				ctx.Person
					.Where(_ => _.ID == key)
					.Set(_ => _.FirstName, value)
					.Update());

		[Test]
		public void CompiledUpdate()
		{
			using (var ctx = new TestDataConnection())
			{
				_updateQuery(ctx, 12345, "54321");
			}
		}

		[Table("LinqDataTypes")]
		sealed class Table1
		{
			[Column] public int  ID        { get; set; }
			[Column] public bool BoolValue { get; set; }

			[Association(ThisKey = "ID", OtherKey = "ParentID", CanBeNull = false)]
			public List<Table2> Tables2 = null!;
		}

		[Table("Parent")]
		sealed class Table2
		{
			[Column] public int  ParentID { get; set; }
			[Column] public int? Value1   { get; set; }

			[Association(ThisKey = "ParentID", OtherKey = "ID", CanBeNull = false)]
			public Table1 Table1 = null!;
		}

		[YdbUnexpectedSqlQuery]
		[Test]
		public void UpdateAssociation5(
			[DataSources(
				false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				ProviderName.DB2,
				TestProvName.AllInformix,
				ProviderName.SqlCe,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var ids = new[] { 10000, 20000 };

				db.GetTable<Table2>()
					.Where (x => ids.Contains(x.ParentID))
					.Select(x => x.Table1)
					.Distinct()
					.Set(y => y.BoolValue, y => y.Tables2.All(x => x.Value1 == 1))
					.Update();

				db.LastQuery!.ShouldContain("INNER JOIN");
				db.LastQuery!.ShouldContain("DISTINCT");
			}
		}

		[Test]
		public void UpdateSimilarNames([DataSources(TestProvName.AllClickHouse, TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				const int childId  = 10000;
				const int parentId = 20000;

				db.Parent.Insert(() => new Parent { ParentID = parentId, Value1 = parentId });
				db.Child.Insert(() => new Child { ChildID = childId, ParentID = parentId });

				// do not change names (!)
				// SQLite fails between aliases [child] and [Child]
				var parents =
						from child in db.Parent
						from parent in db.Child.InnerJoin(_ => _.ParentID == child.ParentID)
						where child.Value1 == parentId
						select parent;

				Assert.That(parents.Set(x => x.ParentID, parentId).Update(), Is.EqualTo(1));
			}
		}

		[Test]
		public void AsUpdatableTest([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Delete(c => c.ChildID > 1000);
				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));

				var q  = db.Child.Where(c => c.ChildID == id && c.Parent!.Value1 == 1);
				var uq = q.AsUpdatable();

				uq = uq.Set(c => c.ChildID, c => c.ChildID + 1);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(uq.Update(), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 1), Is.EqualTo(1));
				}
			}
		}

		[Test]
		public void AsUpdatableDuplicate([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 1001;

				db.Child.Delete(c => c.ChildID > 1000);
				db.Child.Insert(() => new Child { ParentID = 1, ChildID = id });

				Assert.That(db.Child.Count(c => c.ChildID == id), Is.EqualTo(1));

				var q  = db.Child.Where(c => c.ChildID == id && c.Parent!.Value1 == 1);
				var uq = q.AsUpdatable();

				uq = uq.Set(c => c.ChildID, c => c.ChildID + 1);
				uq = uq.Set(c => c.ChildID, c => c.ChildID + 2);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(uq.Update(), Is.EqualTo(1));
					Assert.That(db.Child.Count(c => c.ChildID == id + 2), Is.EqualTo(1));
				}
			}
		}

		[Table("GrandChild")]
		sealed class Table3
		{
			[PrimaryKey(1)] public int? ParentID;
			[PrimaryKey(2)] public int? ChildID;
			[Column]        public int? GrandChildID;
		}

		[Test]
		public void UpdateNullablePrimaryKey([DataSources(ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Update(new Table3 { ParentID = 10000, ChildID = null, GrandChildID = 1000 });

				if (db is DataConnection)
					Assert.That(((DataConnection)db).LastQuery!, Does.Contain("IS NULL"));

				db.Update(new Table3 { ParentID = 10000, ChildID = 111, GrandChildID = 1000 });

				if (db is DataConnection)
					Assert.That(((DataConnection)db).LastQuery!, Does.Not.Contain("IS NULL"));
			}
		}

		[Test]
		public void UpdateTop([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var rowsAffected = db.Parent
					.Where(p => p.ParentID >= 1000)
					.Take(5)
					.Set(p => p.Value1, 1)
					.Update();

				Assert.That(rowsAffected, Is.EqualTo(5));
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllSybase, ErrorMessage = ErrorHelper.Sybase.Error_UpdateWithTopOrderBy)]
		public void TestUpdateTakeOrdered([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					orderby x.ParentID descending
					select x;

				var rowsAffected = entities
					.Take(5)
					.Update(x => new Parent { Value1 = 1 });

				Assert.That(rowsAffected, Is.EqualTo(5));
				var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
				Assert.That(data, Is.EqualTo(new int[] { 1000, 1001, 1002, 1003, 1004, 1, 1, 1, 1, 1 }));
			}
		}

		[Test(Description = "Mainly to test ORDER BY generation for ORACLE 23c+")]
		public void TestUpdateOrdered(
			[DataSources(
			ProviderName.SqlCe,
			ProviderName.Ydb,
			TestProvName.AllInformix,
			TestProvName.AllClickHouse,
			TestProvName.AllDB2,
			TestProvName.AllSQLite,
			TestProvName.AllOracle21Minus,
			TestProvName.AllPostgreSQL,
			TestProvName.AllSapHana,
			TestProvName.AllSqlServer,
			TestProvName.AllSybase
			)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					orderby x.ParentID descending
					select x;

				var rowsAffected = entities
					.Update(x => new Parent { Value1 = 1 });

				Assert.That(rowsAffected, Is.EqualTo(9));
				var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
				Assert.That(data, Is.EqualTo(new int[] { 1000, 1, 1, 1, 1, 1, 1, 1, 1, 1 }));
			}
		}

		[Test]
		public void TestUpdateSkipTakeNotOrdered(
			[DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase,
			TestProvName.AllSqlServer2012Plus // needs fake order by for FETCH
			)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					select x;

				var rowsAffected = entities
					.Skip(6)
					.Take(5)
					.Update(x => new Parent { Value1 = 1 });

				Assert.That(rowsAffected, Is.EqualTo(3));
			}
		}

		[Test]
		public void TestUpdateSkipTakeOrdered(
			[DataSources(
			TestProvName.AllAccess,
			TestProvName.AllClickHouse,
			TestProvName.AllSybase
			)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					orderby x.ParentID descending
					select x;

				var rowsAffected = entities
					.Skip(2)
					.Take(5)
					.Update(x => new Parent { Value1 = 1 });

				Assert.That(rowsAffected, Is.EqualTo(5));

				var data = db.Parent.Where(p => p.ParentID >= 1000).OrderBy(p => p.ParentID).Select(r => r.Value1!.Value).ToArray();
				Assert.That(data, Is.EqualTo(new int[] { 1000, 1001, 1002, 1, 1, 1, 1, 1, 1008, 1009 }));
			}
		}

		[Test]
		public void TestUpdateTakeNotOrdered([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, ProviderName.Ydb)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				using (new DisableLogging())
				{
					for (var i = 0; i < 10; i++)
						db.Insert(new Parent { ParentID = 1000 + i, Value1 = 1000 + i });
				}

				var entities =
					from x in db.Parent
					where x.ParentID > 1000
					select x;

				var rowsAffected = entities
					.Take(5)
					.Update(x => new Parent { Value1 = 1 });

				Assert.That(rowsAffected, Is.EqualTo(5));
			}
		}

		[Test]
		public void UpdateSetSelect([DataSources(
			TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix, ProviderName.SqlCe)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Delete(_ => _.ParentID > 1000);

				var res =
				(
					from p in db.Parent
					join c in db.Child on p.ParentID equals c.ParentID
					where p.ParentID == 1
					select p
				)
				.Set(p => p.ParentID, p => db.Child.SingleOrDefault(c => c.ChildID == 11)!.ParentID + 1000)
				.Update();

				Assert.That(res, Is.EqualTo(1));

				res = db.Parent.Where(_ => _.ParentID == 1001).Set(_ => _.ParentID, 1).Update();
				Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public void UpdateIssue5340Regression([DataSources(TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllInformix, ProviderName.SqlCe)] string context)
		{
			using var db = GetDataConnection(context);
			using var _ = new RestoreBaseTables(db);

			// Test APPLY inside UPDATE subqueries.
			// This query is weird and doesn't make logical sense, but it demonstrates a compiler crash in 6.0.
			var res = db.Parent
				.Where(p => p.ParentID == 1)
				.Set(
					p => p.Value1,
					p => (
						from a in db.SelectQuery(() => 1)
						from b in db.Child
							.Where(c => p.ParentID == c.ParentID)
							.OrderBy(c => c.ChildID - p.ParentID)
							.Select(c => c.ChildID)
							.Take(1)
						select b
					).Single())
				.Update();

			Assert.That(res, Is.EqualTo(1));
			// Validate that the subquery contains a LATERAL JOIN or APPLY, because linq2db tries hard to avoid it and it is what causes the regression.
			db.LastQuery!.ShouldMatch("APPLY|LATERAL");
		}

		[Test]
		public void UpdateIssue319Regression(
			[DataSources(
				TestProvName.AllClickHouse,
				TestProvName.AllInformix,
				TestProvName.AllFirebird,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 100500;
				db.Insert(new Parent1()
				{
					ParentID = id
				});

				var query = db.GetTable<Parent1>()
						.Where(p => p.ParentID == id)
						.Select(p => new Parent1()
						{
							ParentID = p.ParentID
						});

				var queryResult = new Lazy<Parent1>(() => query.First());

				var cnt = db.GetTable<Parent1>()
						.Where(p => p.ParentID == id && query.Count() > 0)
						.Update(p => new Parent1()
						{
							Value1 = queryResult.Value.ParentID
						});

				if (context.SupportsRowcount())
					Assert.That(cnt, Is.EqualTo(1));
			}
		}

		// looks like managed provider handle null bit parameters as false, because it doesn't fail
		// maybe we need to do the same for unmanaged
		[ActiveIssue("AseException : Null value is not allowed in BIT TYPE", Configuration = ProviderName.Sybase)]
		[Test]
		public void UpdateIssue321Regression([DataSources(ProviderName.DB2, TestProvName.AllInformix, TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var id = 100500;

				var value1 = 3000m;
				var value2 = 13621m;
				var value3 = 60;

				db.Insert(new LinqDataTypes2()
				{
					ID = id,
					MoneyValue = value1,
					IntValue = value3
				});

				db.GetTable<LinqDataTypes2>()
					.Update(
						_ => _.ID == id,
						_ => new LinqDataTypes2
						{
							SmallIntValue = (short)(_.MoneyValue / (value2 / _.IntValue!))
						});

				var dbResult = db.GetTable<LinqDataTypes2>()
						.Where(_ => _.ID == id)
						.Select(_ => _.SmallIntValue).First();

				var expected = (short)(value1 / (value2 / value3));

				Assert.That(dbResult, Is.EqualTo(expected));
			}
		}

		[Test]
		public void UpdateMultipleColumns([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (new RestoreBaseTables(db))
			{
				var ldt = new LinqDataTypes
				{
					ID            = 1001,
					MoneyValue    = 1000,
					SmallIntValue = 100,
				};

				db.Types
					.Value(t => t.ID, ldt.ID)
					.Value(t => t.MoneyValue, () => ldt.MoneyValue)
					.Value(t => t.SmallIntValue, () => ldt.SmallIntValue)
					.Insert()
					;

				db.Types
					.Where(t => t.ID == ldt.ID)
					.Set(t => t.MoneyValue, () => 2000)
					.Set(t => t.SmallIntValue, () => 200)
					.Update()
					;

				var udt = db.Types.Single(t => t.ID == ldt.ID);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(udt.MoneyValue, Is.Not.EqualTo(ldt.MoneyValue));
					Assert.That(udt.SmallIntValue, Is.Not.EqualTo(ldt.SmallIntValue));
				}
			}
		}

		[Test]
		public void UpdateWithTypeConversion([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var _ = new RestoreBaseTables(db);

			var id = 1001;

			db.Types
				.Value(t => t.ID, id)
				.Value(t => t.MoneyValue, () => 100)
				.Value(t => t.SmallIntValue, () => 200)
				.Insert()
				;

			// use column with other type as value to have conversion in SQL
			// because constants/parameters already typed by target on query build
			db.Types
				.Where(t => t.ID == id)
				.Set(t => t.SmallIntValue, t => t.MoneyValue)
				.Set(t => t.MoneyValue, t => t.SmallIntValue)
				.Update()
				;
			db.Types
				.Where(t => t.ID == id)
				.Set(t => t.SmallIntValue, t => t.MoneyValue)
				.Set(t => t.MoneyValue, t => t.SmallIntValue)
				.Update()
				;

			var udt = db.Types.Single(t => t.ID == id);
			using (Assert.EnterMultipleScope())
			{
				// MySql doesn't know how update should work
				Assert.That(udt.MoneyValue, Is.EqualTo(100));
				Assert.That(udt.SmallIntValue, Is.EqualTo(context.IsAnyOf(TestProvName.AllMySql) ? 100 : 200));
			}
		}

		[Test]
		public void UpdateByTableName([DataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = "xxPerson";

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Person>(tableName, schemaName: schemaName);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(table.TableName, Is.EqualTo(tableName));
				Assert.That(table.SchemaName, Is.EqualTo(schemaName));
			}

			var person = new Person()
			{
				FirstName = "Steven",
				LastName  = "King",
				Gender    = Gender.Male,
			};

			// insert a row into the table
			db.Insert(person, tableName: tableName, schemaName: schemaName);
			var newCount  = table.Count();
			Assert.That(newCount, Is.EqualTo(1));

			var personForUpdate = table.Single();

			// update that row
			personForUpdate.MiddleName = "None";
			db.Update(personForUpdate, tableName: tableName, schemaName: schemaName);

			var updatedPerson = table.Single();
			Assert.That(updatedPerson.MiddleName, Is.EqualTo("None"));
		}

		[Test]
		public async Task UpdateByTableNameAsync([DataSources] string context)
		{
			const string? schemaName = null;
			var tableName  = "xxPerson";

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<Person>(tableName, schemaName: schemaName);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(table.TableName, Is.EqualTo(tableName));
				Assert.That(table.SchemaName, Is.EqualTo(schemaName));
			}

			var person = new Person()
			{
				FirstName = "Steven",
				LastName  = "King",
				Gender    = Gender.Male,
			};

			// insert a row into the table
			await db.InsertAsync(person, tableName: tableName, schemaName: schemaName);
			var newCount  = await table.CountAsync();
			Assert.That(newCount, Is.EqualTo(1));

			var personForUpdate = await table.SingleAsync();

			// update that row
			personForUpdate.MiddleName = "None";
			await db.UpdateAsync(personForUpdate, tableName: tableName, schemaName: schemaName);

			var updatedPerson = await table.SingleAsync();
			Assert.That(updatedPerson.MiddleName, Is.EqualTo("None"));
		}

		[Table("gt_s_one")]
		sealed class UpdateFromJoin
		{
			[PrimaryKey          ] public int     id   { get; set; }
			[Column(Length = 100)] public string? col1 { get; set; }
			[Column(Length = 100)] public string? col2 { get; set; }
			[Column(Length = 100)] public string? col3 { get; set; }
			[Column(Length = 100)] public string? col4 { get; set; }
			[Column(Length = 100)] public string? col5 { get; set; }
			[Column(Length = 100)] public string? col6 { get; set; }

			public static UpdateFromJoin[] Data = [];
		}

		[Table("access_mode")]
		sealed class AccessMode
		{
			[PrimaryKey]
			public int id { get; set; }

			[Column]
			public string? code { get; set; }

			public static AccessMode[] Data = [];
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void TestUpdateFromJoinOld([DataSources(
			TestProvName.AllAccess, // access doesn't have Replace mapping
			TestProvName.AllClickHouse,
			ProviderName.SqlCe,
			TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var gt_s_one = db.CreateLocalTable(UpdateFromJoin.Data))
			using (var access_mode = db.CreateLocalTable(AccessMode.Data))
			{
				gt_s_one
					.GroupJoin(
						access_mode,
						l => l.col3!.Replace("auth.", "").ToUpper(),
						am => am.code!.ToUpper(),
						(l, am) => new
						{
							l,
							am
						})
					.SelectMany(
						x => x.am.DefaultIfEmpty(),
						(x1, y1) => new
						{
							gt = x1.l,
							theAM = y1!.id
						})
					.Update(
						gt_s_one,
						s => new UpdateFromJoin()
						{
							col1 = s.gt.col1,
							col2 = s.gt.col2,
							col3 = s.gt.col3!.Replace("auth.", ""),
							col4 = s.gt.col4,
							col5 = s.gt.col3 == "empty" ? "1" : "0",
							col6 = s.gt.col3 == "empty" ? "" : s.theAM.ToString()
						});
			}
		}

		// https://stackoverflow.com/questions/57115728/
		[Test]
		public void TestUpdateFromJoin([DataSources(
			TestProvName.AllAccess, // access doesn't have Replace mapping
			TestProvName.AllClickHouse,
			ProviderName.SqlCe,
			TestProvName.AllInformix)] string context)
		{
			using (var db          = GetDataContext(context))
			using (var gt_s_one    = db.CreateLocalTable(UpdateFromJoin.Data))
			using (var access_mode = db.CreateLocalTable(AccessMode.Data))
			{
				gt_s_one
					.GroupJoin(
						access_mode,
						l => l.col3!.Replace("auth.", "").ToUpper(),
						am => am.code!.ToUpper(),
						(l, am) => new
						{
							l,
							am
						})
					.SelectMany(
						x => x.am.DefaultIfEmpty(),
						(x1, y1) => new
						{
							gt    = x1.l,
							theAM = y1!.id
						})
					.Update(
						q => q.gt,
						s => new UpdateFromJoin()
						{
							col1 = s.gt.col1,
							col2 = s.gt.col2,
							col3 = s.gt.col3!.Replace("auth.", ""),
							col4 = s.gt.col4,
							col5 = s.gt.col3 == "empty" ? "1" : "0",
							col6 = s.gt.col3 == "empty" ? "" : s.theAM.ToString()
						});
			}
		}

		[Obsolete("Remove test after API removed")]
		[Test]
		public void TestUpdateFromJoinDifferentTableOld([DataSources(
			TestProvName.AllAccess, // access doesn't have Replace mapping
			TestProvName.AllClickHouse,
			ProviderName.SqlCe,
			TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			using (var gt_s_one = db.CreateLocalTable(UpdateFromJoin.Data))
			using (var gt_s_one_target = db.CreateLocalTable(tableName: "gt_s_one_target", UpdateFromJoin.Data))
			using (var access_mode = db.CreateLocalTable(AccessMode.Data))
			{
#pragma warning disable CA1311 // Specify a culture or use an invariant version
				gt_s_one
					.GroupJoin(
						access_mode,
						l => l.col3!.Replace("auth.", "").ToUpper(),
						am => am.code!.ToUpper(),
						(l, am) => new
						{
							l,
							am
						})
					.SelectMany(
						x => x.am.DefaultIfEmpty(),
						(x1, y1) => new
						{
							gt = x1.l,
							theAM = y1!.id
						})
					.Update(
						gt_s_one.TableName("gt_s_one_target"),
						s => new UpdateFromJoin()
						{
							col1 = s.gt.col1,
							col2 = s.gt.col2,
							col3 = s.gt.col3!.Replace("auth.", ""),
							col4 = s.gt.col4,
							col5 = s.gt.col3 == "empty" ? "1" : "0",
							col6 = s.gt.col3 == "empty" ? "" : s.theAM.ToString()
						});
#pragma warning restore CA1311 // Specify a culture or use an invariant version
			}
		}

		[Test]
		public void TestUpdateFromJoinDifferentTable([DataSources(
			TestProvName.AllAccess, // access doesn't have Replace mapping
			TestProvName.AllClickHouse,
			ProviderName.SqlCe,
			TestProvName.AllInformix)] string context)
		{
			using (var db          = GetDataContext(context))
			using (var gt_s_one    = db.CreateLocalTable(UpdateFromJoin.Data))
			using (var gt_s_one_target = db.CreateLocalTable(tableName: "gt_s_one_target", UpdateFromJoin.Data))
			using (var access_mode = db.CreateLocalTable(AccessMode.Data))
			{
#pragma warning disable CA1311 // Specify a culture or use an invariant version
				gt_s_one.InnerJoin(gt_s_one_target, (t1, t2) => t1.id == t2.id, (t1, t2) => new { t1, t2 })
					.GroupJoin(
						access_mode,
						l => l.t1.col3!.Replace("auth.", "").ToUpper(),
						am => am.code!.ToUpper(),
						(l, am) => new
						{
							l,
							am
						})
					.SelectMany(
						x => x.am.DefaultIfEmpty(),
						(x1, y1) => new
						{
							gt    = x1.l,
							theAM = y1!.id
						})
					.Update(
						q => q.gt.t2,
						s => new UpdateFromJoin()
						{
							col1 = s.gt.t1.col1,
							col2 = s.gt.t1.col2,
							col3 = s.gt.t1.col3!.Replace("auth.", ""),
							col4 = s.gt.t1.col4,
							col5 = s.gt.t1.col3 == "empty" ? "1" : "0",
							col6 = s.gt.t1.col3 == "empty" ? "" : s.theAM.ToString()
						});
#pragma warning restore CA1311 // Specify a culture or use an invariant version
			}
		}

		enum UpdateSetEnum
		{
			Value1 = 6,
			Value2 = 7,
			Value3 = 8
		}

		[Table]
		sealed class UpdateSetTest
		{
			[PrimaryKey] public int            Id     { get; set; }
			[Column]     public Guid           Value1 { get; set; }
			[Column]     public int            Value2 { get; set; }
			[Column]     public UpdateSetEnum  Value3 { get; set; }
			[Column]     public Guid?          Value4 { get; set; }
			[Column]     public int?           Value5 { get; set; }
			[Column]     public UpdateSetEnum? Value6 { get; set; }

			public static UpdateSetTest[] Data = new UpdateSetTest[]
			{
				new UpdateSetTest() { Id = 1, Value1 = TestData.Guid3, Value2 = 10, Value3 = UpdateSetEnum.Value1 }
			};
		}

		[Test]
		public void TestSetValueCaching1(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = TestData.Guid1;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value1, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value1).Single(), Is.EqualTo(value));

				value = TestData.Guid2;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value1, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value1).Single(), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestSetValueCaching2(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = 11;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value2, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value2).Single(), Is.EqualTo(value));

				value = 12;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value2, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value2).Single(), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestSetValueCaching3(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = UpdateSetEnum.Value2;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value3, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value3).Single(), Is.EqualTo(value));

				value = UpdateSetEnum.Value3;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value3, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value3).Single(), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestSetValueCaching4(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = TestData.Guid1;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value4, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value4).Single(), Is.EqualTo(value));

				value = TestData.Guid2;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value4, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value4).Single(), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestSetValueCaching5(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = 11;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value5, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value5).Single(), Is.EqualTo(value));

				value = 12;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value5, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value5).Single(), Is.EqualTo(value));
			}
		}

		[Test]
		public void TestSetValueCaching6(
			[DataSources(
			TestProvName.AllSybase,
			TestProvName.AllMySql,
			TestProvName.AllFirebird,
			TestProvName.AllInformix,
			ProviderName.DB2,
			ProviderName.SqlCe)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(UpdateSetTest.Data))
			{
				var id = 1;
				var value = UpdateSetEnum.Value2;

				table.Where(_ => _.Id == id)
					.Set(_ => _.Value6, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value6).Single(), Is.EqualTo(value));

				value = UpdateSetEnum.Value3;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Value6, value)
					.Update();

				Assert.That(table.Where(_ => _.Id == id).Select(_ => _.Value6).Single(), Is.EqualTo(value));
			}
		}

		sealed class TextData
		{
			[Column]
			public int Id { get; set; }

			[Column(Length = int.MaxValue)]
			public string? Items1 { get; set; }

			[Column(Length = int.MaxValue)]
			public string? Items2 { get; set; }
		}

		[Test]
		public void TestSetValueExpr(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context, [Values("zz", "yy")] string str)
		{
			var data = new[]
			{
				new TextData { Id = 1, Items1 = "T1", Items2 = "Z1" },
				new TextData { Id = 2, Items1 = "T2", Items2 = "Z2" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var id = 1;

				table.Where(_ => _.Id >= id)
					.Set(x => $"{x.Items1} += {str}")
					.Set(x => $"{x.Items2} += {str}")
					//.Set(x => $"{x.Items}.WRITE({item}, {2}, {2})")
					.Update();

				var result = table.ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Items1, Is.EqualTo("T1" + str));
					Assert.That(result[0].Items2, Is.EqualTo("Z1" + str));

					Assert.That(result[1].Items1, Is.EqualTo("T2" + str));
					Assert.That(result[1].Items2, Is.EqualTo("Z2" + str));
				}

			}
		}

		[Test]
		public void TestSetValueExpr2(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context, [Values("zz", "yy")] string str)
		{
			var data = new[]
			{
				new TextData { Id = 1, Items1 = "T1", Items2 = "Z1" },
				new TextData { Id = 2, Items1 = "T2", Items2 = "Z2" },
			};

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var id = 1;

				table.Where(_ => _.Id >= id)
					.Set(x => x.Items1, x => $"{x.Items1}{str}")
					.Set(x => x.Items2, x => $"{x.Items2}{str}")
					.Update();

				var result = table.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(result[0].Items1, Is.EqualTo("T1" + str));
					Assert.That(result[0].Items2, Is.EqualTo("Z1" + str));

					Assert.That(result[1].Items1, Is.EqualTo("T2" + str));
					Assert.That(result[1].Items2, Is.EqualTo("Z2" + str));
				}

			}
		}

		[Table]
		sealed class MainTable
		{
			[PrimaryKey] public int Id;
			[Column] public string? Field;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(AssociatedTable.Id))]
			public AssociatedTable AssociatedOptional = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(AssociatedTable.Id), CanBeNull = false)]
			public AssociatedTable AssociatedRequired = null!;

			public static readonly MainTable[] Data = new []
			{
				new MainTable() { Id = 1, Field = "value 1" },
				new MainTable() { Id = 2, Field = "value 2" },
				new MainTable() { Id = 3, Field = "value 3" },
			};
		}

		[Table]
		sealed class AssociatedTable
		{
			[PrimaryKey] public int Id;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(MainTable.Id))]
			public MainTable MainOptional = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(MainTable.Id), CanBeNull = false)]
			public MainTable MainRequired = null!;

			public static readonly AssociatedTable[] Data = new []
			{
				new AssociatedTable() { Id = 1 },
				new AssociatedTable() { Id = 3 },
			};
		}

		[Test]
		public void UpdateByAssociationOptional([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db   = GetDataContext(context))
			using (var main = db.CreateLocalTable(MainTable.Data))
			using (db.CreateLocalTable(AssociatedTable.Data))
			{
				var id = 3;
					var cnt = main
						.Where(_ => _.Id == id)
						.Select(_ => _.AssociatedOptional!.MainOptional)
						.Update(p => new MainTable()
						{
							Field = "test"
						});

				var data = main.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					if (context.SupportsRowcount())
						Assert.That(cnt, Is.EqualTo(1));
					Assert.That(data[0].Field, Is.EqualTo("value 1"));
					Assert.That(data[1].Field, Is.EqualTo("value 2"));
					Assert.That(data[2].Field, Is.EqualTo("test"));
				}
			}
		}

		[Test]
		public void UpdateByAssociationRequired([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var main = db.CreateLocalTable(MainTable.Data))
			using (db.CreateLocalTable(AssociatedTable.Data))
			{
				var id = 3;
				var cnt = main
						.Where(_ => _.Id == id)
						.Select(_ => _.AssociatedRequired!.MainRequired)
						.Update(p => new MainTable()
						{
							Field = "test"
						});

				var data = main.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					if (context.SupportsRowcount())
						Assert.That(cnt, Is.EqualTo(1));
					Assert.That(data[0].Field, Is.EqualTo("value 1"));
					Assert.That(data[1].Field, Is.EqualTo("value 2"));
					Assert.That(data[2].Field, Is.EqualTo("test"));
				}
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void UpdateByAssociation2Optional([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db         = GetDataContext(context))
			using (var main       = db.CreateLocalTable(MainTable.Data))
			using (var associated = db.CreateLocalTable(AssociatedTable.Data))
			{
				var id = 3;
				var cnt = associated
					.Where(pat => pat.Id == id)
					.Select(p => p.MainOptional)
					.Update(p => new MainTable()
					{
						Field = "test"
					});

				var data = main.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					if (context.SupportsRowcount())
						Assert.That(cnt, Is.EqualTo(1));
					Assert.That(data[0].Field, Is.EqualTo("value 1"));
					Assert.That(data[1].Field, Is.EqualTo("value 2"));
					Assert.That(data[2].Field, Is.EqualTo("test"));
				}
			}
		}

		[YdbMemberNotFound]
		[Test]
		public void UpdateByAssociation2Required([DataSources(TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var main = db.CreateLocalTable(MainTable.Data))
			using (var associated = db.CreateLocalTable(AssociatedTable.Data))
			{
				var id = 3;
				var cnt = associated
					.Where(pat => pat.Id == id)
					.Select(p => p.MainRequired)
					.Update(p => new MainTable()
					{
						Field = "test"
					});

				var data = main.OrderBy(_ => _.Id).ToArray();
				using (Assert.EnterMultipleScope())
				{
					if (context.SupportsRowcount())
						Assert.That(cnt, Is.EqualTo(1));
					Assert.That(data[0].Field, Is.EqualTo("value 1"));
					Assert.That(data[1].Field, Is.EqualTo("value 2"));
					Assert.That(data[2].Field, Is.EqualTo("test"));
				}
			}
		}

		[Test]
		public void AsUpdatableEmptyTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var query = db.Person.AsUpdatable();

				var ex = Assert.Throws<LinqToDBException>(() => query.Update())!;
				Assert.That(ex.Message, Is.EqualTo("Update query has no setters defined."));
			}
		}

		[Test]
		public void UpdateWithNullableValue([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			decimal? value = 1.1M;

			db.Types
				.Where(r => r.ID == -1)
				.Set(r => r.MoneyValue, value)
				.Update();
		}

		[Test]
		public void Issue4136Test([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var someExternalDependency = 10;
			db.Types
				.Where(p => p.ID == -1)
				.Update(p => new LinqDataTypes { BoolValue = p.BoolValue || someExternalDependency > 0 });
		}
	}
}
