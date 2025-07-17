using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class InterfaceTests : TestBase
	{
		[Test]
		public void Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent2
					group p by p.ParentID into gr
					select new
					{
						Count = gr.Count()
					};

				var _ = q.ToList();
			}
		}

		#region Issue 4031
		[Table("Person")]
		public class Issue4031BaseInternal
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseImplicit : IIssue4031
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseExplicit : IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseImplicitBad : IIssue4031
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		[Table("Person")]
		public class Issue4031BaseExplicitBad : IIssue4031
		{
			[Column("UNKNOWN")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case01 : Issue4031BaseExternal, IIssue4031
		{
		}

		public class Issue4031Case02 : Issue4031BaseInternal, IIssue4031
		{
		}

		public class Issue4031Case03 : Issue4031BaseImplicit
		{
		}

		public class Issue4031Case04 : Issue4031BaseImplicit
		{
			[Column("UNKNOWN")] public new int Id { get; set; }
		}

		public class Issue4031Case05 : Issue4031BaseExplicit
		{
		}

		public class Issue4031Case06 : Issue4031BaseExplicit
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		public class Issue4031Case07 : Issue4031BaseImplicit, IIssue4031
		{
		}

		public class Issue4031Case08 : Issue4031BaseImplicit, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case09 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case10 : Issue4031BaseExplicit, IIssue4031
		{
		}

		public class Issue4031Case11 : Issue4031BaseExplicit, IIssue4031
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		public class Issue4031Case12 : Issue4031BaseExplicit, IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case13 : Issue4031BaseImplicit, IIssue4031
		{
		}

		public class Issue4031Case14 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class Issue4031Case15 : Issue4031BaseImplicitBad, IIssue4031
		{
			[Column("PersonID")] int IIssue4031.Id { get; set; }
		}

		public class Issue4031Case16 : Issue4031BaseExternal, IIssue4031<int>
		{
		}

		[Test]
		public void Issue4031_Case01([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case01>(context);
		}

		[Test]
		public void Issue4031_Case02([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case02>(context);
		}

		[Test]
		public void Issue4031_Case03([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case03>(context);
		}

		// unsuported case:
		// we prefer member declared with "new" over interface implementation member for backward compatibility
		// (see https://github.com/linq2db/linq2db/issues/4113)
		[Test, ActiveIssue]
		public void Issue4031_Case04([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case04>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id });
			query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case05([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case05>(context);
		}

		[Test]
		public void Issue4031_Case06([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case06>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id });

			var sql = query.ToSqlQuery().Sql;

			BaselinesManager.LogQuery(sql);

			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case07([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case07>(context);
		}

		[Test]
		public void Issue4031_Case08([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case08>(context);
		}

		[Test]
		public void Issue4031_Case09([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case09>(context);
		}

		[Test]
		public void Issue4031_Case10([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case10>(context);
		}

		[Test]
		public void Issue4031_Case11([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case11>(context);
		}

		[Test]
		public void Issue4031_Case12([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case12>(context);
		}

		[Test]
		public void Issue4031_Case13([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case13>(context);
		}

		[Test]
		public void Issue4031_Case14([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute<Issue4031Case14>(context);
		}

		[Test]
		public void Issue4031_Case15([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_Execute_TwoFields<Issue4031Case15>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<Issue4031Case15>().Where(c => c.Id == -1).Select(c => new { c.Id });

			var sql = query.ToSqlQuery().Sql;

			BaselinesManager.LogQuery(sql);

			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test]
		public void Issue4031_Case16([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			Issue4031_ExecuteT<Issue4031Case16>(context);
		}

		void Issue4031_ExecuteT<T>(string context) where T : class, IIssue4031<int>
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void Issue4031_Execute<T>(string context) where T : class, IIssue4031
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void Issue4031_Execute_TwoFields<T>(string context) where T : class, IIssue4031
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(2));
			var columnNames = ed.Columns.Select(c => c.ColumnName).ToArray();
			Assert.That(columnNames, Does.Contain("PersonID"));
			Assert.That(columnNames, Does.Contain("UNKNOWN"));
		}
		#endregion

		#region Issue 3034
		interface IA
		{
			int Id { get; set; }
		}

		interface IB : IA
		{
			string Name { get; set; }
		}

		sealed class MyTable
		{
			public int     Id   { get; set; }
			public string? Name { get; set; }
		}

		[Test]
		public void Issue3034([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<MyTable>(new[]{ new MyTable() { Id = 1, Name = "old_name" }, new MyTable() { Id = 2, Name = "old_name" } });

			db.GetTable<IB>().TableName("MyTable")
				.Where(x => x.Id == 1)
				.Set(x => x.Name, x => "new_name")
				.Update();

			var results = t.OrderBy(r => r.Id).ToArray();

			Assert.That(results, Has.Length.EqualTo(2));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(results[0].Id, Is.EqualTo(1));
				Assert.That(results[0].Name, Is.EqualTo("new_name"));
				Assert.That(results[1].Id, Is.EqualTo(2));
				Assert.That(results[1].Name, Is.EqualTo("old_name"));
			}
		}
		#endregion

		#region Issue 4082
		public interface IIdentifiable
		{
			int Id { get; }
		}

		[Table]
		public class UserAccount : IIdentifiable
		{
			[PrimaryKey] public int     Id   { get; set; }
			[Column    ] public string? Name { get; set; }
		}

		[Test]
		public void Issue4082([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<UserAccount>(new[]{ new UserAccount() { Id = 1, Name = "old_name" }, new UserAccount() { Id = 2, Name = "old_name" } });

			var results = ((IQueryable<IIdentifiable>)db.GetTable<UserAccount>())
				.Where(x => x.Id == 1)
				.ToArray();

			Assert.That(results, Has.Length.EqualTo(1));
			Assert.That(results[0].Id, Is.EqualTo(1));
		}
		#endregion

		#region Issue 4607
		interface IHasDeleted
		{
			bool Interface { get; set; }
		}

		class SomeTable : IHasDeleted
		{
			public bool ClassProp { get; set; }
			public bool Interface { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4607")]
		public void Issue4607Test([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SomeTable>();

			var expr = Expression.MemberInit(
				Expression.New(typeof(SomeTable)),
				Expression.Bind(
					typeof(SomeTable).GetProperty(nameof(SomeTable.ClassProp))!,
					Expression.Constant(true)),
				Expression.Bind(
					typeof(IHasDeleted).GetProperty(nameof(IHasDeleted.Interface))!,
					Expression.Constant(false)));

			tb.Insert(Expression.Lambda<Func<SomeTable>>(expr));

			var res = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(res.ClassProp, Is.True);
				Assert.That(res.Interface, Is.False);
			}
		}
		#endregion

		#region Issue 4715
		interface IExplicitInterface<T> where T: IExplicitInterface<T>
		{
			int ExplicitPropertyRW { get; set; }
			int ExplicitPropertyRO { get; }
		}

		interface IImplicitInterface<T> where T : IImplicitInterface<T>
		{
			int ImplicitPropertyRW { get; set; }
			int ImplicitPropertyRO { get; }
		}

		class Issue4715Table : IExplicitInterface<Issue4715Table>, IImplicitInterface<Issue4715Table>
		{
			public int Id { get; set; }
			public int ImplicitPropertyRW { get; set; }
			public int ImplicitPropertyRO => 11;
			int IExplicitInterface<Issue4715Table>.ExplicitPropertyRW { get; set; }
			int IExplicitInterface<Issue4715Table>.ExplicitPropertyRO => 22;
		}

		private static MappingSchema ConfigureIssue4715Mapping()
		{
			return new FluentMappingBuilder()
				.Entity<Issue4715Table>()
				// when fixed - we should have better API to configure those two columns
				.HasAttribute(typeof(Issue4715Table).GetProperty("Tests.Linq.InterfaceTests.IExplicitInterface<Tests.Linq.InterfaceTests.Issue4715Table>.ExplicitPropertyRW", BindingFlags.NonPublic | BindingFlags.Instance)!, new ColumnAttribute("Prop3"))
				.HasAttribute(typeof(Issue4715Table).GetProperty("Tests.Linq.InterfaceTests.IExplicitInterface<Tests.Linq.InterfaceTests.Issue4715Table>.ExplicitPropertyRO", BindingFlags.NonPublic | BindingFlags.Instance)!, new ColumnAttribute("Prop4"))
				.Property(x => x.ImplicitPropertyRW).HasColumnName("Prop1")
				.Property(x => x.ImplicitPropertyRO).HasColumnName("Prop2")
				.Build()
				.MappingSchema;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4715")]
		public void Issue4715TestDescriptor()
		{
			var ed = ConfigureIssue4715Mapping().GetEntityDescriptor(typeof(Issue4715Table));

			Assert.That(ed.Columns, Has.Count.EqualTo(5));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(ed.Columns.Any(c => c.ColumnName == "Id"), Is.True);
				Assert.That(ed.Columns.Any(c => c.ColumnName == "Prop1"), Is.True);
				Assert.That(ed.Columns.Any(c => c.ColumnName == "Prop2"), Is.True);
				Assert.That(ed.Columns.Any(c => c.ColumnName == "Prop3"), Is.True);
				Assert.That(ed.Columns.Any(c => c.ColumnName == "Prop4"), Is.True);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4715")]
		public void Issue4715TestMapping([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureIssue4715Mapping());
			using var tb = db.CreateLocalTable<Issue4715Table>();

			var record = new Issue4715Table()
			{
				Id = 1,
				ImplicitPropertyRW = 2
			};

			((IExplicitInterface<Issue4715Table>)record).ExplicitPropertyRW = 3;

			db.Insert(record);

			var result = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.ImplicitPropertyRW, Is.EqualTo(2));
				Assert.That(((IExplicitInterface<Issue4715Table>)result).ExplicitPropertyRW, Is.EqualTo(3));
			}
		}
		#endregion

		#region v6p2 regression
		class ExtensionTable1 : IEntity
		{
			public int ID { get; set; }
			public int? FK { get; set; }

			[Association(ThisKey = nameof(FK), OtherKey = nameof(ExtensionTable2.ID))]
			public ExtensionTable2? Child => throw new InvalidOperationException();
		}

		class ExtensionTable2 : IEntity
		{
			public int ID { get; set; }
		}

		interface IEntity
		{
			int ID { get; set; }
		}

		[Test(Description = "6.0.0-preview.2 regression")]
		public void ExtensionRegression([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable<ExtensionTable1>();
			using var t2 = db.CreateLocalTable<ExtensionTable2>();

			Execute(t1.Select(r => r.Child!));

			void Execute<T>(IQueryable<T> query)
				where T : IEntity
			{
				query
					.Where(e => Sql.ToNullable(e.ID) != null)
					.Select(e => e.ID)
					.Distinct()
					.ToArray();
			}
		}

		#endregion

		#region Interface with Expression
		interface IParameters
		{
			Attributes Attributes { get; }
		}

		class Attributes
		{
			public int? UserId { get; set; }
		}

		interface IUserOwned
		{
			int UserId { get; }

			int UserIdMethod();
		}

		[Table]
		class TransactionLine : IUserOwned
		{
			[Column]
			public int Id { get; set; }

			[ExpressionMethod(nameof(UserIdExpression))]
			public int UserId => throw new InvalidOperationException();

			[ExpressionMethod(nameof(UserIdExpression))]
			public int UserIdMethod() => throw new InvalidOperationException();

			private static Expression<Func<TransactionLine, int>> UserIdExpression()
				=> x => x.Id;

			public static readonly TransactionLine[] Data = new[]
			{
				new TransactionLine() { Id = 1 },
				new TransactionLine() { Id = 2 },
			};
		}

		private static IQueryable<T> Filter1<T>(IQueryable<T> q, IParameters dbCtx)
				where T : IUserOwned
				=> dbCtx.Attributes.UserId is null
					? q
					: q.Where(x => x.UserId == dbCtx.Attributes.UserId.Value);

		private static IQueryable<T> Filter2<T>(IQueryable<T> q, IParameters dbCtx)
			where T : IUserOwned
			=> dbCtx.Attributes.UserId is null
				? q
				: q.Where(x => x.UserIdMethod() == dbCtx.Attributes.UserId.Value);

		private static IQueryable<T> Filter1Expr<T>(IQueryable<T> q, IParameters dbCtx)
				where T : IUserOwned
		{
			if (dbCtx.Attributes.UserId is null)
				return q;

			var parameter = Expression.Parameter(typeof(T));
			var property = Expression.Property(parameter, nameof(IUserOwned.UserId));
			var userIdValue = Expression.Constant(dbCtx.Attributes.UserId.Value);
			var body = Expression.Equal(property, userIdValue);
			var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

			return q.Where(lambda);
		}

		private static IQueryable<T> Filter2Expr<T>(IQueryable<T> q, IParameters dbCtx)
			where T : IUserOwned
		{
			if (dbCtx.Attributes.UserId is null)
				return q;

			var parameter = Expression.Parameter(typeof(T));
			var call = Expression.Call(parameter, nameof(IUserOwned.UserIdMethod), null);
			var userIdValue = Expression.Constant(dbCtx.Attributes.UserId.Value);
			var body = Expression.Equal(call, userIdValue);
			var lambda = Expression.Lambda<Func<T, bool>>(body, parameter);

			return q.Where(lambda);
		}

		class Parameters : IParameters
		{
			Attributes IParameters.Attributes { get; } = new Attributes() { UserId = 2 };
		}

		[Test]
		public void InterfaceFilterRegression([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(TransactionLine.Data);

			var cn = new Parameters();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(Filter1Expr(tb, cn).Single().Id, Is.EqualTo(2));
				Assert.That(Filter2Expr(tb, cn).Single().Id, Is.EqualTo(2));

				Assert.That(Filter1(tb, cn).Single().Id, Is.EqualTo(2));
				Assert.That(Filter2(tb, cn).Single().Id, Is.EqualTo(2));
			}
		}
		#endregion
	}
}
