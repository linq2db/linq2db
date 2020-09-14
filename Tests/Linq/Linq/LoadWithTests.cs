﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Transactions;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class LoadWithTests : TestBase
	{
		[Test]
		public void LoadWith1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.Child.LoadWith(p => p.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Parent);
			}
		}

		[Test]
		public void LoadWithAsTable1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.Child.LoadWithAsTable(p => p.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Parent);
			}
		}

		[Test]
		public void LoadWith2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GrandChild.LoadWith(p => p.Child!.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWithAsTable2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.GrandChild.LoadWithAsTable(p => p.Child!.Parent)
					select t;

				var ch = q.First();

				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWithAsTable4([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from t in db.Parent.LoadWithAsTable(p => p.Children.First().Parent)
					select t;

				var ch = q.FirstOrDefault();

				Assert.IsNotNull(ch.Children[0].Parent);
			}
		}

		[Test]
		public void LoadWith3([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetConvertExpression<IEnumerable<Child>,ImmutableList<Child>>(
					t => ImmutableList.Create(t.ToArray()));

				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}
		}

		[Test]
		public void LoadWithAsTable3([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				db.MappingSchema.SetConvertExpression<IEnumerable<Child>,ImmutableList<Child>>(
					t => ImmutableList.Create(t.ToArray()));

				var q =
					from p in db.Parent.LoadWithAsTable(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}
		}

		class EnumerableToImmutableListConvertProvider<T> : IGenericInfoProvider
		{
			public void SetInfo(MappingSchema mappingSchema)
			{
				mappingSchema.SetConvertExpression<IEnumerable<T>,ImmutableList<T>>(
					t => ImmutableList.Create(t.ToArray()));
			}
		}

		[Test]
		public void LoadWith4([DataSources] string context)
		{
			var ms = new MappingSchema();
			ms.SetGenericConvertProvider(typeof(EnumerableToImmutableListConvertProvider<>));

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context, ms))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children3)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children3).FirstOrDefault();

				Assert.IsNotNull(ch);
			}
		}

		[Test]
		public void LoadWith5([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children.First().GrandChildren[0].Child!.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.Children).SelectMany(p => p.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWith6([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren2[0].Child!.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.ToList().Select(t => t.p).SelectMany(p => p.GrandChildren2).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWith7([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren2[0].Child!.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				var ch = q.Select(t => t.p).SelectMany(p => p.GrandChildren2).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWith8([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GrandChild.LoadWith(p => p.Child!.GrandChildren[0].Child!.Parent)
					select p;

				var ch = q.SelectMany(p => p.Child!.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNotNull(ch.Child);
				Assert.IsNotNull(ch.Child!.Parent);
			}
		}

		[Test]
		public void LoadWith9([DataSources(TestProvName.AllAccess)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GrandChild.LoadWith(p => p.Child!.GrandChildren)
					select p;

				var ch = q.SelectMany(p => p.Child!.GrandChildren).FirstOrDefault();

				Assert.IsNotNull(ch);
				Assert.IsNull   (ch.Child);
			}
		}

		[Test]
//		[Timeout(15000)]
		public void LoadWith10([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children)
					where p.ParentID < 2
					select p;

				for (var i = 0; i < 100; i++)
				{
					var _ = q.ToList();
				}
			}
		}

		[Test]
		public void LoadWith11([DataSources(ProviderName.Access)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children).LoadWith(p => p.GrandChildren)
					where p.ParentID < 2
					select p;

				foreach (var parent in q)
				{
					Assert.IsNotNull (parent.Children);
					Assert.IsNotNull (parent.GrandChildren);
					Assert.IsNotEmpty(parent.Children);
					Assert.IsNotEmpty(parent.GrandChildren);
					Assert.IsNull    (parent.Children3);
				}
			}
		}

		[Test]
		public void LoadWith12([DataSources(TestProvName.AllAccess)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				var q1 =
					(from p in db.Parent.LoadWith(p => p.Children[0].Parent!.Children)
					where p.ParentID < 2
					select new
					{
						p,
					});

				var result = q1.FirstOrDefault();

				Assert.DoesNotThrow(() => result.p.Children.Single().Parent!.Children.Single());
			}
		}

		[Test]
		public void TransactionScope([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSQLite)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled))
			{
				var result = db.Parent
					.Where(x => x.ParentID == 1)
					.Select(p => new 
					{
						Id = p.ParentID,
						Children = p.Children.Select(c => new 
						{
							Id = c.ChildID,
						}).ToArray() 
					})
					.FirstOrDefault();

				transaction.Complete();
			}
		}

		[Test]
		public async Task TransactionScopeAsync([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllSQLite)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (var transaction = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }, TransactionScopeAsyncFlowOption.Enabled))
			{
				var result = await db.Parent
					.Where(x => x.ParentID == 1)
					.Select(p => new 
					{
						Id = p.ParentID,
						Children = p.Children.Select(c => new 
						{
							Id = c.ChildID,
						}).ToArray() 
					})
					.FirstOrDefaultAsync();

				transaction.Complete();
			}
		}

		class MainItem
		{
			[Column]
			public int Id { get; set; }
			[Column(Length = 50)]
			public string? Value { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubItem1.ParentId))]
			public SubItem1[] SubItems1 { get; set; } = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubItem2.ParentId))]
			public SubItem2[] SubItems2 { get; set; } = null!;
		}

		class MainItem2
		{
			[Column]
			public int Id { get; set; }
			[Column(Length = 50)]
			public string? Value { get; set; }

			[Column]
			public int? MainItemId { get; set; }

			[Association(ThisKey = nameof(MainItemId), OtherKey = nameof(LoadWithTests.MainItem.Id), CanBeNull = true)]
			public MainItem? MainItem { get;set; }
		}

		class SubItem1
		{
			[Column]
			public int Id { get; set; }
			[Column(Length = 50)]
			public string? Value { get; set; }
			[Column]
			public int? ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(MainItem.Id))]
			public MainItem? Parent { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(SubItem1_Sub.ParentId))]
			public SubItem1_Sub[] SubSubItems { get; set; } = null!;
		}

		class SubItem1_Sub
		{
			[Column]
			public int Id { get; set; }
			[Column(Length = 50)]
			public string? Value { get; set; }
			[Column]
			public int? ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(SubItem1.Id))]
			public SubItem1? ParentSubItem { get; set; }
		}


		class SubItem2
		{
			[Column]
			public int Id { get; set; }
			[Column(Length = 50)]
			public string? Value { get; set; }
			[Column]
			public int? ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(MainItem.Id))]
			public MainItem? Parent { get; set; }
		}

		Tuple<MainItem[], MainItem2[], SubItem1[], SubItem1_Sub[], SubItem2[]> GenerateTestData()
		{
			var mainItems = Enumerable.Range(0, 10).Select(i => new MainItem
			{
				Id = i,
				Value = "Main_" + i
			}).ToArray();

			var mainItems2 = Enumerable.Range(0, 5).Select(i => new MainItem2
			{
				Id = i * 2,
				Value = "Main2_" + i,
				MainItemId = i
			}).ToArray();


			var subItems1 = Enumerable.Range(0, 20).Select(i => new SubItem1
			{
				Id = i * 10,
				Value = "Sub1_" + i,
				ParentId = i % 2 == 0 ? (int?)i / 2 : null
			}).ToArray();

			var subSubItems1 = Enumerable.Range(0, 20).Select(i => new SubItem1_Sub()
			{
				Id = i * 100,
				Value = "SubSub1_" + i,
				ParentId = (i * 10) / 3
			}).ToArray();

			var subItems2 = Enumerable.Range(0, 20).Select(i => new SubItem2
			{
				Id = i * 10,
				Value = "Sub2_" + i,
				ParentId = i % 2 == 0 ? (int?)i / 2 : null
			}).ToArray();

			return Tuple.Create(mainItems, mainItems2, subItems1, subSubItems1, subItems2);
		}


		[Test]
		public void LoadWithAndFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			using (db.CreateLocalTable(testData.Item4))
			using (db.CreateLocalTable(testData.Item5))
			{
				var filterQuery = from m in db.GetTable<MainItem>()
					from m2 in db.GetTable<MainItem2>().InnerJoin(m2 => m2.Id == m.Id)
					from mm in db.GetTable<MainItem2>().InnerJoin(mm => mm.Id == m2.Id)
					where m.Id > 1
					select m;

				var query = filterQuery
					.LoadWith(m => m.SubItems1)
					.ThenLoad(c => c.SubSubItems)
					.ThenLoad(ss => ss.ParentSubItem)
					.LoadWith(m => m.SubItems2);
				
				var result = query.ToArray();

				Assert.That(result[0].SubItems1[0].SubSubItems[0].ParentSubItem, Is.Not.Null);

				var query2 = filterQuery
					.LoadWith(m => m.SubItems1, q => q.Where(e => e.Value == e.Value))
					.ThenLoad(c => c.SubSubItems, q => q.Where(e => e.Value == e.Value))
					.ThenLoad(ss => ss.ParentSubItem, q => q.Where(e => e!.Value == e.Value))
					.LoadWith(m => m.SubItems2, q => q.Where(e => e.Value == e.Value))
					.ThenLoad(e => e.Parent);
				
				var result2 = query2.ToArray();

				Assert.That(result2[0].SubItems1[0].SubSubItems[0].ParentSubItem, Is.Not.Null);
				Assert.That(result2[0].SubItems2[0].Parent, Is.Not.Null);

				var query3 = filterQuery
					.LoadWith(m => m.SubItems1)
					.ThenLoad(c => c.SubSubItems)
					.ThenLoad(ss => ss.ParentSubItem)
					.LoadWith(m => m.SubItems2)
					.ThenLoad(e => e.Parent);

				var result3 = query3.ToArray();

				Assert.That(result3[0].SubItems1[0].SubSubItems[0].ParentSubItem, Is.Not.Null);
				Assert.That(result3[0].SubItems2[0].Parent, Is.Not.Null);
			}
		}

		[Test]
		public void LoadWithAndFilteredProperty([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			using (db.CreateLocalTable(testData.Item4))
			using (db.CreateLocalTable(testData.Item5))
			{
				var filterQuery = from m in db.GetTable<MainItem>()
					where m.Id > 1
					select m;
				
				var query1 = filterQuery
					.LoadWith(m => m.SubItems1.Where(e => e.ParentId % 2 == 0).Take(2));
				
				var result1 = query1.ToArray();
				
				Assert.That(result1[0].SubItems1.Length, Is.GreaterThan(0));
				
				
				var query2 = filterQuery
					.LoadWith(m => m.SubItems1.Where(e => e.ParentId % 2 == 0).Take(2),
						e => e.Where(i => i.Value!.StartsWith("Sub1_")));
				
				var result2 = query2.ToArray();
				
				Assert.That(result2[0].SubItems1.Length, Is.GreaterThan(0));
				
				var query3 = filterQuery
					.LoadWith(m => m.SubItems1[0].Parent!.SubItems2.Where(e => e.ParentId % 2 == 0).Take(2),
						e => e.Where(i => i.Value!.StartsWith("Sub2_")));
				
				var result3 = query3.ToArray();
				
				Assert.That(result3[0].SubItems1[0].Parent!.SubItems2.Length, Is.GreaterThan(0));
				
				var query3_1 = filterQuery
					.LoadWith(m => m.SubItems1)
					.ThenLoad(s => s.Parent)
					.ThenLoad(p => p!.SubItems2.Where(e => e.ParentId % 2 == 0).Take(2), e => e.Where(i => i.Value!.StartsWith("Sub2_")));
				
				var result3_1 = query3_1.ToArray();
				
				Assert.That(result3_1[0].SubItems1[0].Parent!.SubItems2.Length, Is.GreaterThan(0));

				var query4 = filterQuery
					.LoadWith(m => m.SubItems1.Where(e => e.ParentId % 2 == 0),
						e => e.Where(i => i.Value!.StartsWith("Sub1_")));

				var result4 = query4.ToArray();

				Assert.That(result4[0].SubItems1.Length, Is.GreaterThan(0));

			}
		}

		[Test]
		public void LoadWithAndQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			using (db.CreateLocalTable(testData.Item4))
			using (db.CreateLocalTable(testData.Item5))
			{
				var filterQuery = from m in db.GetTable<MainItem>()
					where m.Id > 1
					select m;
				
				var query = filterQuery
					.LoadWith(m => m.SubItems1,
						q => q
							.Where(i => i.Id % 2 == 0)
							.Join(db.GetTable<MainItem2>(), qq => qq.Id / 10, mm => mm.Id, (qq, mm) => qq)
							.Select(qq => new SubItem1 { Id = qq.Id, Value = "QueryResult" + qq.Id })
					);
				
				var result = query.ToArray();
				
				var query2 = filterQuery
					.LoadWith(m => m.SubItems1)
					.ThenLoad(s => s.SubSubItems, q => q.Where(c => c.Id == 1).Take(2));
				
				var result2 = query2.ToArray();
				
				
				var mainQuery = from s in db.GetTable<SubItem1>()
					select s;

				var query3 = mainQuery
					.LoadWith(s => s.Parent!, q => q.Where(p => p.Id % 3 == 0));

				var result3 = query3.ToArray();
			}
		}

		[Test]
		public void LoadWithRecursive([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			using (db.CreateLocalTable(testData.Item4))
			using (db.CreateLocalTable(testData.Item5))
			{
				var filterQuery = from m in db.GetTable<MainItem>()
					from mm in db.GetTable<MainItem2>().InnerJoin(mm => mm.Id == m.Id)
					where m.Id > 1
					select m;

				var query = filterQuery
					.LoadWith(m => m.SubItems1,
						ms => ms.LoadWith(c => c.SubSubItems, cq => cq.LoadWith(ss => ss.ParentSubItem)))
					.LoadWith(m => m.SubItems2);

				var result = query.ToArray();

				var query2 = filterQuery
					.LoadWith(m => m.SubItems1)
					.ThenLoad(si => si.SubSubItems, qsi => qsi.LoadWith(_ => _.ParentSubItem));

				var result2 = query2.ToArray();

			}
		}

		[Test]
		public void LoadWithPlain([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var testData = GenerateTestData();

			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(testData.Item1))
			using (db.CreateLocalTable(testData.Item2))
			using (db.CreateLocalTable(testData.Item3))
			using (db.CreateLocalTable(testData.Item4))
			using (db.CreateLocalTable(testData.Item5))
			{
				var filterQuery = from m in db.GetTable<MainItem2>()
					where m.Id > 1
					select m;

				var query = filterQuery
					.LoadWith(m => m.MainItem)
					.ThenLoad(m => m!.SubItems2);

				var result = query.ToArray();

			}
		}

		[Table]
		class ParentRecord
		{
			[Column] public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildRecord.ParentId))]
			public List<ChildRecord> Children = null!;

			[Association(ThisKey = nameof(Id), OtherKey = nameof(ChildRecord.ParentId), ExpressionPredicate = nameof(ActiveChildrenPredicate))]
			public List<ChildRecord> ActiveChildren = null!;

			public static Expression<Func<ParentRecord, ChildRecord, bool>> ActiveChildrenPredicate => (p, c) => c.IsActive;

			public static readonly ParentRecord[] Items = new[]
			{
				new ParentRecord() { Id = 1 }
			};
		}

		[Table]
		class ChildRecord
		{
			[Column] public int Id        { get; set; }
			[Column] public int ParentId  { get; set; }
			[Column] public bool IsActive { get; set; }

			public static readonly ChildRecord[] Items = new[]
			{
				new ChildRecord() { Id = 11, ParentId = 1, IsActive = true  },
				new ChildRecord() { Id = 12, ParentId = 1, IsActive = false },
				new ChildRecord() { Id = 13, ParentId = 1, IsActive = true  },
			};
		}

		[Test]
		public void LoadWithAssociationPredicateExpression([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (new AllowMultipleQuery())
			using (var db      = GetDataContext(context))
			using (var parents = db.CreateLocalTable(ParentRecord.Items))
			using (              db.CreateLocalTable(ChildRecord.Items))
			{
				var result = parents
					.LoadWith(p => p.Children)
					.LoadWith(p => p.ActiveChildren)
					.ToArray();


				Assert.AreEqual(1, result.Length);
				Assert.AreEqual(3, result[0].Children.Count);
				Assert.AreEqual(2, result[0].ActiveChildren.Count);
			}
		}
	}
}
