using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Extensions;
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
			using var db = GetDataContext(context);
			var q =
					from p in db.Parent2
					group p by p.ParentID into gr
					select new
					{
						Count = gr.Count()
					};

			var _ = q.ToList();
		}

		[Table("Person")]
		public class PersonBaseInternal
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class PersonBaseImplicit : IMappedId
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		[Table("Person")]
		public class PersonBaseExplicit : IMappedId
		{
			[Column("PersonID")] int IMappedId.Id { get; set; }
		}

		[Table("Person")]
		public class PersonBaseImplicitBadColumn : IMappedId
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		[Table("Person")]
		public class PersonBaseExplicitBadColumn : IMappedId
		{
			[Column("UNKNOWN")] int IMappedId.Id { get; set; }
		}

		public class PersonExternalBaseWithInterface : PersonBaseExternal, IMappedId;

		public class PersonInternalBaseWithInterface : PersonBaseInternal, IMappedId;

		public class PersonInheritedImplicit : PersonBaseImplicit;

		public class PersonInheritedImplicitHiddenColumn : PersonBaseImplicit
		{
			[Column("UNKNOWN")] public new int Id { get; set; }
		}

		public class PersonInheritedExplicit : PersonBaseExplicit;

		public class PersonInheritedExplicitShadowColumn : PersonBaseExplicit
		{
			[Column("UNKNOWN")] public int Id { get; set; }
		}

		public class PersonInheritedImplicitWithInterface : PersonBaseImplicit, IMappedId;

		public class PersonInheritedImplicitWithInterfaceHiddenColumn : PersonBaseImplicit, IMappedId
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class PersonBadImplicitWithInterfaceHiddenColumn : PersonBaseImplicitBadColumn, IMappedId
		{
			[Column("PersonID")] public new int Id { get; set; }
		}

		public class PersonInheritedExplicitWithInterface : PersonBaseExplicit, IMappedId;

		public class PersonInheritedExplicitWithInterfacePublicColumn : PersonBaseExplicit, IMappedId
		{
			[Column("PersonID")] public int Id { get; set; }
		}

		public class PersonInheritedExplicitWithInterfaceExplicitColumn : PersonBaseExplicit, IMappedId
		{
			[Column("PersonID")] int IMappedId.Id { get; set; }
		}

		public class PersonBadImplicitWithInterfaceExplicitColumn : PersonBaseImplicitBadColumn, IMappedId
		{
			[Column("PersonID")] int IMappedId.Id { get; set; }
		}

		public class PersonExternalBaseWithGenericInterface : PersonBaseExternal, IMappedId<int>;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_ExternalBaseWithInterface([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonExternalBaseWithInterface>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InternalBaseWithInterface([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInternalBaseWithInterface>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedImplicit([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedImplicit>(context);
		}

		// unsuported case:
		// we prefer member declared with "new" over interface implementation member for backward compatibility
		// (see https://github.com/linq2db/linq2db/issues/4113)
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031"), ActiveIssue(ErrorTypeName = "LinqToDB.LinqToDBException", ErrorMessage = "could not be converted to SQL",
			Details = "no-issue: an accepted limitation rather than a tracked defect - the comment above records the trade-off and cites #4113 as its justification, which is background for the choice and not a bug to fix.")]
		public void ColumnMapping_InheritedImplicitHiddenColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMappingTwoColumns<PersonInheritedImplicitHiddenColumn>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<PersonBadImplicitWithInterfaceExplicitColumn>().Where(c => c.Id == -1).Select(c => new { c.Id });
			query.ToArray();

			var sql = query.ToSqlQuery().Sql;
			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedExplicit([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedExplicit>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedExplicitShadowColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMappingTwoColumns<PersonInheritedExplicitShadowColumn>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<PersonBadImplicitWithInterfaceExplicitColumn>().Where(c => c.Id == -1).Select(c => new { c.Id });

			var sql = query.ToSqlQuery().Sql;

			BaselinesManager.LogQuery(sql);

			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedImplicitWithInterface([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedImplicitWithInterface>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedImplicitWithInterfaceHiddenColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedImplicitWithInterfaceHiddenColumn>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_BadImplicitWithInterfaceHiddenColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonBadImplicitWithInterfaceHiddenColumn>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedExplicitWithInterface([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedExplicitWithInterface>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedExplicitWithInterfacePublicColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedExplicitWithInterfacePublicColumn>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_InheritedExplicitWithInterfaceExplicitColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMapping<PersonInheritedExplicitWithInterfaceExplicitColumn>(context);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_BadImplicitWithInterfaceExplicitColumn([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMappingTwoColumns<PersonBadImplicitWithInterfaceExplicitColumn>(context);

			using var db = GetDataContext(context);

			var query = db.GetTable<PersonBadImplicitWithInterfaceExplicitColumn>().Where(c => c.Id == -1).Select(c => new { c.Id });

			var sql = query.ToSqlQuery().Sql;

			BaselinesManager.LogQuery(sql);

			Assert.That(sql, Is.Not.Contains("PersonID"));
			sql.ShouldContain("UNKNOWN", Exactly.Twice());
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4031")]
		public void ColumnMapping_ExternalBaseWithGenericInterface([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			AssertColumnMappingGenericInterface<PersonExternalBaseWithGenericInterface>(context);
		}

		void AssertColumnMappingGenericInterface<T>(string context) where T : class, IMappedId<int>
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void AssertColumnMapping<T>(string context) where T : class, IMappedId
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(1));
			Assert.That(ed.Columns[0].ColumnName, Is.EqualTo("PersonID"));
		}

		void AssertColumnMappingTwoColumns<T>(string context) where T : class, IMappedId
		{
			using var db = GetDataContext(context);
			db.GetTable<T>().Where(c => c.Id == -1).Select(c => new { c.Id }).ToList();

			var ed = db.MappingSchema.GetEntityDescriptor(typeof(T));
			Assert.That(ed.Columns, Has.Count.EqualTo(2));
			var columnNames = ed.Columns.Select(c => c.ColumnName).ToArray();
			Assert.That(columnNames, Does.Contain("PersonID"));
			Assert.That(columnNames, Does.Contain("UNKNOWN"));
		}

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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3034")]
		public void UpdateThroughInheritedInterfaceMembers([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4082")]
		public void FilterThroughInterfaceQueryableCast([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			using var db = GetDataContext(context);
			using var t = db.CreateLocalTable<UserAccount>(new[]{ new UserAccount() { Id = 1, Name = "old_name" }, new UserAccount() { Id = 2, Name = "old_name" } });

			var results = ((IQueryable<IIdentifiable>)db.GetTable<UserAccount>())
				.Where(x => x.Id == 1)
				.ToArray();

			Assert.That(results, Has.Length.EqualTo(1));
			Assert.That(results[0].Id, Is.EqualTo(1));
		}

		interface IHasDeleted
		{
			bool Interface { get; set; }
		}

		class SomeTable : IHasDeleted
		{
			[PrimaryKey] public int Id { get; set; }
			public bool ClassProp { get; set; }
			public bool Interface { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4607")]
		public void InsertMemberInitWithInterfaceMemberBinding([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<SomeTable>();

			var expr = Expression.MemberInit(
				Expression.New(typeof(SomeTable)),
				Expression.Bind(
					typeof(SomeTable).GetProperty(nameof(SomeTable.Id))!,
					Expression.Constant(1)),
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

		class ExplicitInterfaceTable : IExplicitInterface<ExplicitInterfaceTable>, IImplicitInterface<ExplicitInterfaceTable>
		{
			[PrimaryKey] public int Id { get; set; }
			public int ImplicitPropertyRW { get; set; }
			public int ImplicitPropertyRO => 11;
			int IExplicitInterface<ExplicitInterfaceTable>.ExplicitPropertyRW { get; set; }
			int IExplicitInterface<ExplicitInterfaceTable>.ExplicitPropertyRO => 22;
		}

		private static MappingSchema ConfigureExplicitInterfaceMapping()
		{
			return new FluentMappingBuilder()
				.Entity<ExplicitInterfaceTable>()
				// when fixed - we should have better API to configure those two columns
				.HasAttribute(typeof(ExplicitInterfaceTable).GetProperty("Tests.Linq.InterfaceTests.IExplicitInterface<Tests.Linq.InterfaceTests.ExplicitInterfaceTable>.ExplicitPropertyRW", BindingFlags.NonPublic | BindingFlags.Instance)!, new ColumnAttribute("Prop3"))
				.HasAttribute(typeof(ExplicitInterfaceTable).GetProperty("Tests.Linq.InterfaceTests.IExplicitInterface<Tests.Linq.InterfaceTests.ExplicitInterfaceTable>.ExplicitPropertyRO", BindingFlags.NonPublic | BindingFlags.Instance)!, new ColumnAttribute("Prop4"))
				.Property(x => x.ImplicitPropertyRW).HasColumnName("Prop1")
				.Property(x => x.ImplicitPropertyRO).HasColumnName("Prop2")
				.Build()
				.MappingSchema;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4715")]
		public void ExplicitInterfaceMembersEntityDescriptor()
		{
			var ed = ConfigureExplicitInterfaceMapping().GetEntityDescriptor(typeof(ExplicitInterfaceTable));

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
		public void ExplicitInterfaceMembersInsertAndRead([DataSources] string context)
		{
			using var db = GetDataContext(context, ConfigureExplicitInterfaceMapping());
			using var tb = db.CreateLocalTable<ExplicitInterfaceTable>();

			var record = new ExplicitInterfaceTable()
			{
				Id = 1,
				ImplicitPropertyRW = 2
			};

			((IExplicitInterface<ExplicitInterfaceTable>)record).ExplicitPropertyRW = 3;

			db.Insert(record);

			var result = tb.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.ImplicitPropertyRW, Is.EqualTo(2));
				Assert.That(((IExplicitInterface<ExplicitInterfaceTable>)result).ExplicitPropertyRW, Is.EqualTo(3));
			}
		}

		#region v6p2 regression
		class ExtensionTable1 : IEntity
		{
			[PrimaryKey] public int ID { get; set; }
			public int? FK { get; set; }

			[Association(ThisKey = nameof(FK), OtherKey = nameof(ExtensionTable2.ID))]
			public ExtensionTable2? Child => throw new InvalidOperationException();
		}

		class ExtensionTable2 : IEntity
		{
			[PrimaryKey] public int ID { get; set; }
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
			[PrimaryKey]
			public int Id { get; set; }

			[ExpressionMethod(nameof(UserIdExpression))]
			public int UserId => throw new ServerSideOnlyException(nameof(UserId));

			[ExpressionMethod(nameof(UserIdExpression))]
			public int UserIdMethod() => throw new ServerSideOnlyException(nameof(UserIdMethod));

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

		interface IGenericParentable
		{
			public int Value1 { get; set; }
		}

		[Table(Name="Parent", IsColumnAttributeRequired = false)]
		class GenericParent : IGenericParentable
		{
			public int ParentID { get; set; }
			public int Value1   { get; set; }
		}

		[Test]
		public void GenericInterfaceMemberTableContextTest()
		{
			using var db = GetDataConnection();

			var query = db.GetTable<GenericParent>()
				.Where (i => ((IGenericParentable)i).Value1 > 0)
				.Select(i => ((IGenericParentable)i).Value1);

			_ = query.ToList();
		}

		[Test]
		public void GenericInterfaceMemberExpressionTest()
		{
			using var db = GetDataConnection();

			static Expression<Func<TEntity,int>> GetProjection<TEntity>()
				where TEntity : IGenericParentable
			{
				return i => i.Value1;
			}

			var query = db.GetTable<GenericParent>()
				.Select(GetProjection<GenericParent>());

			_ = query.ToList();
		}

		[Test]
		public void GenericInterfaceMemberSelectContextTest()
		{
			using var db = GetDataConnection();

			var query = db.GetTable<GenericParent>()
				.Select(i => new GenericParent
				{
					ParentID = i.ParentID,
					Value1   = i.Value1
				})
				.Where (i => ((IGenericParentable)i).Value1 > 0)
				.Select(i => ((IGenericParentable)i).Value1);

			_ = query.ToList();
		}

		[Test]
		public void GenericInterfaceGroupingMaxProjectionTest()
		{
			using var db = GetDataConnection();

			Expression<Func<IGrouping<int,IGenericParentable>, int>> projection =
				group => group.Max(i => ((IGenericParentable)i).Value1);

			var query = db.GetTable<GenericParent>()
				.Where  (i => i.ParentID < 0)
				.GroupBy(i => i.ParentID)
				.Select (projection);

			_ = query.ToList();
		}

		[Test]
		public void GenericInterfaceConstraintGroupingMaxProjectionTest()
		{
			using var db = GetDataConnection();

			static Expression<Func<IGrouping<int,TEntity>,int>> GetGroupByProjection<TEntity>()
				where TEntity : IGenericParentable
			{
				return group => group.Max(i => i.Value1);
			}

			var query = db.GetTable<GenericParent>()
				.Where  (i => i.ParentID < 0)
				.GroupBy(i => i.ParentID)
				.Select (GetGroupByProjection<GenericParent>());

			_ = query.ToList();
		}

		#region Generic-interface filter through queryable shapes (#5547)

		// Regression matrix for projection-through-Select interacting with generic-interface
		// constrained filters. The hierarchy declares the navigation on the abstract base while
		// the FluentMappingBuilder registers the association on the concrete sealed type — this
		// produces the DeclaringType != ConcreteType member shape that IsAssociationInRealization
		// must resolve via the realized type. Mirrors the EF Core fixture
		// LinqToDB.EntityFrameworkCore.Tests.IssueTests.Issue5547_ContainsThroughQueryableShapes.
		public interface IHasMembershipProfile
		{
			MembershipProfile Profile   { get; set; }
			int               ProfileId { get; set; }
		}

		public class MembershipProfile
		{
			public int    Id      { get; set; }
			public string License { get; set; } = null!;
		}

		public abstract class MemberBase : IHasMembershipProfile
		{
			public int               Id        { get; set; }
			public int               ProfileId { get; set; }
			public MembershipProfile Profile   { get; set; } = null!;
		}

		public sealed class Member : MemberBase
		{
			public string Name { get; set; } = null!;
		}

		public sealed class MemberShare
		{
			public int    Id       { get; set; }
			public int    MemberId { get; set; }
			public Member Member   { get; set; } = null!;
		}

		public enum MemberQueryableShape
		{
			Direct,
			SelectProjection,
			WhereThenSelectProjection,
			SelectProjectionThenWhere,
			SelectProjectionDistinct,
		}

		static MappingSchema ConfigureMembershipMapping()
		{
			return new FluentMappingBuilder()
				.Entity<MembershipProfile>()
					.Property(e => e.Id).IsPrimaryKey()
					.Property(e => e.License)
				.Entity<Member>()
					.Property(e => e.Id).IsPrimaryKey()
					.Property(e => e.ProfileId)
					.Property(e => e.Name)
					.Association(e => e.Profile, e => e.ProfileId, p => p!.Id)
				.Entity<MemberShare>()
					.Property(e => e.Id).IsPrimaryKey()
					.Property(e => e.MemberId)
					.Association(e => e.Member, e => e.MemberId, m => m!.Id)
				.Build()
				.MappingSchema;
		}

		static IQueryable<T> FilterByLicense<T>(IQueryable<T> source, string[]? licenseFilter)
			where T : class, IHasMembershipProfile
		{
			if (licenseFilter != null)
				return source.Where(x => licenseFilter.Contains(x.Profile.License));

			return source;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5547")]
		public void GenericInterfaceFilterAcrossQueryableShapes(
			[IncludeDataSources(TestProvName.AllSQLite)] string context,
			[Values] MemberQueryableShape shape)
		{
			using var db = GetDataContext(context, ConfigureMembershipMapping());

			using var profiles = db.CreateLocalTable(new[]
			{
				new MembershipProfile { Id = 1, License = "12345" },
				new MembershipProfile { Id = 2, License = "67890" },
			});
			using var members  = db.CreateLocalTable(new Member[]
			{
				new() { Id = 1, ProfileId = 1, Name = "Alice Smith" },
				new() { Id = 2, ProfileId = 1, Name = "Bob Jones"   },
				new() { Id = 3, ProfileId = 2, Name = "Carol Davis" },
			});
			using var shares   = db.CreateLocalTable(new[]
			{
				new MemberShare { Id = 1, MemberId = 1 },
				new MemberShare { Id = 2, MemberId = 2 },
				new MemberShare { Id = 3, MemberId = 3 },
			});

			string[] licenseFilter = ["12345"];

			IQueryable<Member> source = shape switch
			{
				MemberQueryableShape.Direct                    => members,
				MemberQueryableShape.SelectProjection          => shares.Select(s => s.Member),
				MemberQueryableShape.WhereThenSelectProjection => shares.Where(s => s.Id > 0).Select(s => s.Member),
				MemberQueryableShape.SelectProjectionThenWhere => shares.Select(s => s.Member).Where(m => m.Id > 0),
				MemberQueryableShape.SelectProjectionDistinct  => shares.Select(s => s.Member).Distinct(),
				_                                              => throw new InvalidOperationException($"Unknown shape: {shape}"),
			};

			var result = FilterByLicense(source, licenseFilter)
				.OrderBy(m => m.Id)
				.Select(m => m.Id)
				.ToList();

			result.ShouldBe(new[] { 1, 2 });
		}

		#endregion

		#region Interface implemented through members of a base class that doesn't implement it

		public interface IChildWithParent
		{
			int     ParentID { get; set; }
			int     ChildID  { get; set; }
			Parent? Parent   { get; set; }
		}

		public class ChildWithParentBase
		{
			[Column] public int ParentID { get; set; }
			[Column] public int ChildID  { get; set; }

			[Association(ThisKey = nameof(ParentID), OtherKey = nameof(Model.Parent.ParentID))]
			public Parent? Parent { get; set; }
		}

		[Table("Child")]
		public class ChildImplementingInterfaceViaBase : ChildWithParentBase, IChildWithParent;

		static IQueryable<T> FilterByParentValue<T>(IQueryable<T> source, int value)
			where T : IChildWithParent
		{
			return source.Where(c => c.Parent!.Value1 == value);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_FilterThroughInterfaceCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var source = db.GetTable<ChildImplementingInterfaceViaBase>() as IQueryable<IChildWithParent>;

			var result = source
				.Where (c => c.Parent!.Value1 == 1)
				.Select(c => new { c.ParentID, c.ChildID })
				.ToList();

			var expected = Child
				.Where (c => c.Parent!.Value1 == 1)
				.Select(c => new { c.ParentID, c.ChildID });

			result.ShouldBe(expected, ignoreOrder: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_FilterThroughGenericConstraint([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var result = FilterByParentValue(db.GetTable<ChildImplementingInterfaceViaBase>(), 1)
				.Select(c => new { c.ParentID, c.ChildID })
				.ToList();

			var expected = Child
				.Where (c => c.Parent!.Value1 == 1)
				.Select(c => new { c.ParentID, c.ChildID });

			result.ShouldBe(expected, ignoreOrder: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_ProjectThroughInterfaceCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var source = db.GetTable<ChildImplementingInterfaceViaBase>() as IQueryable<IChildWithParent>;

			var result = source
				.Select(c => new { c.ParentID, c.ChildID, c.Parent })
				.ToList()
				.Select(r => (r.ParentID, r.ChildID, r.Parent?.Value1));

			var expected = Child
				.Select(c => (c.ParentID, c.ChildID, c.Parent!.Value1));

			result.ShouldBe(expected, ignoreOrder: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_LoadWithThroughInterfaceCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var source = db.GetTable<ChildImplementingInterfaceViaBase>() as IQueryable<IChildWithParent>;

			var result = source
				.LoadWith(c => c.Parent)
				.ToList()
				.Select(c => (c.ParentID, c.ChildID, c.Parent?.ParentID));

			var expected = Child
				.Select(c => (c.ParentID, c.ChildID, (int?)c.Parent!.ParentID));

			result.ShouldBe(expected, ignoreOrder: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_FilteredLoadWithThroughInterfaceCast([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var source = db.GetTable<ChildImplementingInterfaceViaBase>() as IQueryable<IChildWithParent>;

			var result = source
				.LoadWith(c => c.Parent, q => q.Where(p => p.Value1 == 1))
				.ToList()
				.Select(c => (c.ParentID, c.ChildID, c.Parent?.ParentID));

			var expected = Child
				.Select(c => (c.ParentID, c.ChildID, c.Parent!.Value1 == 1 ? c.Parent.ParentID : (int?)null));

			result.ShouldBe(expected, ignoreOrder: true);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4773")]
		public void AssociationViaBaseClass_FindAssociationDescriptorByInterfaceMember()
		{
			var descriptor = new MappingSchema().GetEntityDescriptor(typeof(ChildImplementingInterfaceViaBase));
			var member     = typeof(IChildWithParent).GetProperty(nameof(IChildWithParent.Parent))!;

			descriptor
				.FindAssociationDescriptor(member)
				.ShouldNotBeNull();
		}

		#endregion

		public class MappedBackgroundTask : IMappedBackgroundTask
		{
			public int? ID { get; set; }
			public int DurationID { get; set; }
			public int DurationInterval { get; set; }
			public int? PersonID { get; set; }
		}

		public interface IMappedBackgroundTask
		{
			[Column]
			int? ID { get; set; }
			[PrimaryKey]
			int DurationID { get; set; }
			[Column]
			int DurationInterval { get; set; }
			[Column]
			int? PersonID { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/1099")]
		public void WhereOnInterfaceTypedQueryable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using (db.CreateLocalTable<MappedBackgroundTask>())
			{
				var personId = 1;

				db.Insert(new MappedBackgroundTask
				{
					PersonID = 1,
					DurationID = 2,
					ID = 3,
					DurationInterval = 4
				});

				IQueryable<IMappedBackgroundTask> tasks = db.GetTable<MappedBackgroundTask>();

				var query = from task in tasks
							where task.PersonID == personId
							select task;

				var items = query.ToList();

				Assert.That(items, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(items[0].PersonID, Is.EqualTo(1));
					Assert.That(items[0].DurationID, Is.EqualTo(2));
					Assert.That(items[0].ID, Is.EqualTo(3));
					Assert.That(items[0].DurationInterval, Is.EqualTo(4));
				}
			}
		}

		interface IJoinedService
		{
			int  Id       { get; set; }
			int? IdClient { get; set; }
		}

		[Table]
		public partial class JoinedServiceAdsl : IJoinedService
		{
			[PrimaryKey] public int  Id       { get; set; } // int
			[Column    ] public int? IdClient { get; set; } // int
		}

		[Table]
		public partial class JoinedServiceClient
		{
			[PrimaryKey] public int Id { get; set; } // int
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2939")]
		public void JoinThroughInterfaceTypedQueryable([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var services = db.CreateLocalTable<JoinedServiceAdsl>();
			using var clients = db.CreateLocalTable<JoinedServiceClient>();
			IQueryable<JoinedServiceClient>   q_clients  = clients;
			IQueryable<IJoinedService> q_services = services;
			IQueryable<JoinedServiceAdsl>     q_adsl     = services;

			q_services = from adsl in q_adsl select adsl;

			var q_test = (
					from serv in q_services
					join client in q_clients on serv.IdClient equals client.Id
					select serv.Id
				);

			var res = q_test.ToList();
		}
	}
}
