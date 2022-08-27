﻿using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Expressions;
using LinqToDB.Linq;
using NUnit.Framework;
using Tests.Model;
using Tests.DataProvider;

namespace Tests.UserTests
{
	// test handling of DefaultExpression in queries
	// roslyn compiler replaces default with constant value, so we need to restore it from constant before execution
	[TestFixture]
	public class Issue3148Tests : TestBase
	{
		[Test]
		public void TestDefaultExpression([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Select(p => new Person
					{
						Patient = p.Patient != null ? p.Patient : default(Patient)
					});
				var query2 = db.Person
					.Select(p => new Person
					{
						Patient = p.Patient != null ? p.Patient : default(Patient)
					});

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_01([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => r.ID != default);
				var query2 = db.Person
					.Where(r => r.ID != default);

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_02([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => r.MiddleName != default);
				var query2 = db.Person
					.Where(r => r.MiddleName != default);

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_05([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children,
						GrandChildren = default!,
						ParentID      = default,
					});
				var query2 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children,
						GrandChildren = default!,
						ParentID      = default,
					});

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Parent>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Parent>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Parent>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Parent>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_06([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.ParentID != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});
				var query2 = db.Parent
					.Select(p => new Parent()
					 {
						 Children = p.Children.Where(c => c.ParentID != default).ToList(),
						 GrandChildren = default!,
						 ParentID = default,
					 });

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Parent>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Parent>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Parent>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Parent>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_07([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.Parent != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});
				var query2 = db.Parent
					.Select(p => new Parent()
					{
						Children      = p.Children.Where(c => c.Parent != default).ToList(),
						GrandChildren = default!,
						ParentID      = default,
					});

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Parent>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Parent>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Parent>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Parent>.CacheMissCount);
			}
		}

		// no idea wether it should work, but now it throws (due to bad expression rewrite?)
		// InvalidOperationException : variable 'x' of type 'Tests.Model.Child' referenced from scope '', but it is not defined
		[Test, ActiveIssue]
		public void TestDefaultExpression_08([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Child
					.Where(x => x.Parent != x
							.GrandChildren
							.Select(e => e.Child!.Parent)
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());
				var query2 = db.Child
					.Where(x => x.Parent != x
							.GrandChildren
							.Select(e => e.Child!.Parent)
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Child>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Child>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Child>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Child>.CacheMissCount);
			}
		}

		// some databases require EXISTS
		[Test, ActiveIssue(
			Configurations = new[]
			{
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase
			})]
		public void TestDefaultExpression_09([DataSources(TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
							.GrandChildren
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());
				var query2 = db.Child
					.Where(x => x.GrandChildren.FirstOrDefault() != x
							.GrandChildren
							.DefaultIfEmpty(default)
							.FirstOrDefault()
							&&
						x.ParentID != x
							.Parent!.Children
							.Select(p => p.ChildID)
							.DefaultIfEmpty(default)
							.FirstOrDefault());

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Child>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Child>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Child>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Child>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_10([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context, [Values] bool withDefault)
		{
			using (var db = new TestDataConnection(context))
			using (var tb = db.CreateLocalTable<TestTable>())
			{
				var provider = ((IQueryable)tb).Provider;
				Expression<Func<int>> query1 = () => tb.Truncate(default);
				Expression<Func<int>> query2 = () => tb.Drop(default);

				if (withDefault)
				{
					query1 = Restore(query1);
					query2 = Restore(query2);
				}

				provider.Execute<int>(query1.Body);
				Assert.True(db.LastQuery!.Contains("DELETE FROM"));
				provider.Execute<int>(query2.Body);
				Assert.True(db.LastQuery!.Contains("DROP TABLE IF EXISTS"));
			}
		}

		[Test]
		public void TestDefaultExpression_11([IncludeDataSources(TestProvName.AllSapHana)] string context, [Values] bool withDefault)
		{
			using (var db = new SapHanaTests.CalcViewInputParameters(context))
			{
				var provider = ((IQueryable)db.GetTable<TestTable>()).Provider;
				var var1     = "mandatory1";

				Expression<Func<IQueryable<SapHanaTests.FIT_CA_PARAM_TEST>>> query = () => db.CaParamTest(10, default, var1, default, null, default!);

				if (withDefault)
					query = Restore(query);

				try
				{
					provider.CreateQuery<SapHanaTests.FIT_CA_PARAM_TEST>(query.Body).ToArray();
					Assert.Fail();
				}
				catch (Exception e)
				{
					Assert.True(e.Message.Contains("invalid table name"));
				}
			}
		}

		[Test]
		public void TestDefaultExpression_12([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = new TestDataConnection(context))
			{
				var provider = ((IQueryable)db.GetTable<TestTable>()).Provider;

				int? var1 = 4;
				Expression<Func<IQueryable<TestTable>>> expr1 = () => db.FromSql<TestTable>("SELECT {0} as Id, {1} as Field1, {2} as Field2, {3} as Field3", default(int), default(int?), default(string), var1);
				Expression<Func<IQueryable<TestTable>>> expr2 = () => db.FromSql<TestTable>("SELECT {0} as Id, {1} as Field1, {2} as Field2, {3} as Field3", default(int), default(int?), default(string), var1);

				var body1 = expr1.Body;
				var body2 = expr2.Body;
				if (withDefault)
				{
					body1 = Restore(body1);
					body2 = Restore(body2);
				}

				var query1 = provider.CreateQuery<TestTable>(body1);
				var query2 = provider.CreateQuery<TestTable>(body2);

				query1.ToArray();
				var cacheMiss = Query<TestTable>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<TestTable>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_13([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => r.ID != default != default);
				var query2 = db.Person
					.Where(r => r.ID != default != default);

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_14([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => ((r.FirstName + "1") ?? default) == default);
				var query2 = db.Person
					.Where(r => ((r.FirstName + "1") ?? default) == default);

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_15([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => (((int?)r.ID + 1) ?? default) == default);
				var query2 = db.Person
					.Where(r => (((int?)r.ID + 1) ?? default) == default);

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_16([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.GroupBy(_ => new { _.LastName, f1 = default(int), f2 = default(Gender?), f3 = default(string) })
					.Select(g => new Person()
					{
						LastName   = g.Key.LastName,
						ID         = g.Key.f1,
						MiddleName = g.Key.f3,
						Gender     = g.Key.f2 ?? default
					});
				var query2 = db.Person
					.GroupBy(_ => new { _.LastName, f1 = default(int), f2 = default(Gender?), f3 = default(string) })
					.Select(g => new Person()
					{
						LastName   = g.Key.LastName,
						ID         = g.Key.f1,
						MiddleName = g.Key.f3,
						Gender     = g.Key.f2 ?? default
					});

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_17([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Child.Select(p => default(int));
				var query2 = db.Child.Select(p => default(int));

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<int>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<int>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<int>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<int>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_18([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Child.Select(p => default(int?));
				var query2 = db.Child.Select(p => default(int?));

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<int?>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<int?>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<int?>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<int?>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_19([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				db.Select(() => 1);
				var query1 = db.Child.Select(p => default(string));
				var query2 = db.Child.Select(p => default(string));

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<string>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<string>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<string>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<string>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_20([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = db.Person
					.Where(r => r.LastName.EndsWith("x", default));
				var query2 = db.Person
					.Where(r => r.LastName.EndsWith("x", default));

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Person>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Person>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Person>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Person>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_21([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = new TestDataConnection(context))
			{
				var provider = ((IQueryable)db.GetTable<TestTable>()).Provider;

				int? var1 = 4;
				Expression<Func<IQueryable<TestTable>>> expr1 = () => db.FromSql<TestTable>($"SELECT {default(int)} as Id, {default(int?)} as Field1, {default(string)} as Field2, {var1} as Field3");
				Expression<Func<IQueryable<TestTable>>> expr2 = () => db.FromSql<TestTable>($"SELECT {default(int)} as Id, {default(int?)} as Field1, {default(string)} as Field2, {var1} as Field3");

				var body1 = expr1.Body;
				var body2 = expr2.Body;
				if (withDefault)
				{
					body1 = Restore(body1);
					body2 = Restore(body2);
				}

				var query1 = provider.CreateQuery<TestTable>(body1);
				var query2 = provider.CreateQuery<TestTable>(body2);

				query1.ToArray();
				var cacheMiss = Query<TestTable>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<TestTable>.CacheMissCount);
			}
		}

		[Test]
		public void TestDefaultExpression_22([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool withDefault)
		{
			using (var db = GetDataContext(context))
			{
				var query1 = from person in db.Person
							 join doctor in db.Doctor.Select(d => new Doctor() { PersonID = default, Taxonomy = default! }) on
									new {person.ID, person.FirstName, person.LastName, Gender = (Gender?)person.Gender }
								equals
									new { ID = default(int), FirstName = default(string), LastName = doctor.Taxonomy, Gender = default(Gender?) } into j
							 from r in j.DefaultIfEmpty()
							 select r;
				var query2 = from person in db.Person
							 join doctor in db.Doctor.Select(d => new Doctor() { PersonID = default, Taxonomy = default! }) on
									new {person.ID, person.FirstName, person.LastName, Gender = (Gender?)person.Gender }
								equals
									new { ID = default(int), FirstName = default(string), LastName = doctor.Taxonomy, Gender = default(Gender?) } into j
							 from r in j.DefaultIfEmpty()
							 select r;

				if (withDefault)
				{
					query1 = query1.Provider.CreateQuery<Doctor>(Restore(query1.Expression));
					query2 = query2.Provider.CreateQuery<Doctor>(Restore(query2.Expression));
				}

				query1.ToArray();
				var cacheMiss = Query<Doctor>.CacheMissCount;
				query2.ToArray();
				Assert.AreEqual(cacheMiss, Query<Doctor>.CacheMissCount);
			}
		}

		// query shouldn't have explicit null/0 values, or they will be rewritten too
		private static Expression RestoreDefault(Expression e)
		{
			if (e is ConstantExpression c)
			{
				if (c.Value == null)
					return Expression.Default(e.Type);

				if (c.Type.IsValueType && Equals(c.Value, Activator.CreateInstance(c.Type)))
					return Expression.Default(e.Type);
			}

			return e;
		}

		private static T Restore<T>(T expr)
			where T: Expression
		{
			var restored = expr.Transform(RestoreDefault);

			Assert.AreNotEqual(expr, restored);
			Assert.IsNotNull(restored.Find<object?>(null, (_, e) => e.NodeType == ExpressionType.Default));

			return (T)restored;
		}

		[Table]
		public class TestTable
		{
			[PrimaryKey, Identity] public int     Id     { get; set; }
			[Column              ] public int?    Field1 { get; set; }
			[Column              ] public string? Field2 { get; set; }
			[Column              ] public int?    Field3 { get; set; }
		}
	}
}
