using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.xUpdate
{
	/// <summary>
	/// Infrastructure tests for the fluent <c>Upsert</c> API (issue #2558): null-argument
	/// validation on the entry points, direct-invocation guards on the marker chain
	/// methods, shared test model, and fake-table helpers. End-to-end scenarios that hit
	/// real databases live in <see cref="UpsertTests"/> partial <c>UpsertTests.cs</c>.
	/// </summary>
	[TestFixture]
	public partial class UpsertTests : TestBase
	{
		#region Test-only model

		[Table("UpsertTest")]
		public sealed class UpsertRow
		{
			[PrimaryKey]                     public int       Id         { get; set; }
			[Column]                         public string    Name       { get; set; } = null!;
			[Column]                         public int       Version    { get; set; }
			[Column]                         public DateTime? CreatedAt  { get; set; }
			[Column]                         public string?   CreatedBy  { get; set; }
			[Column]                         public DateTime? UpdatedAt  { get; set; }
			[Column]                         public string?   UpdatedBy  { get; set; }
		}

		#endregion

		#region Entry-method null-argument validation (no database needed)

		public static IEnumerable<TestCaseData> NullParameterCases
		{
			get
			{
				var table         = new FakeTable<UpsertRow>();
				var iqsource      = (IQueryable<UpsertRow>)table;
				var item          = new UpsertRow();
				var enumItems     = new[] { new UpsertRow() };
				Expression<Func<IUpsertable<UpsertRow, UpsertRow>, IUpsertable<UpsertRow, UpsertRow>>> cfgIdentity = u => u;
				Expression<Func<IUpsertable<UpsertRow, UpsertRow>, IUpsertable<UpsertRow, UpsertRow>>> cfgIdentityTS = u => u;

				var cases = new TestDelegate[]
				{
					// ------------ single entity ------------
					() => LinqExtensions.Upsert(null!, item),
					() => LinqExtensions.Upsert<UpsertRow>(table, null!),
					() => LinqExtensions.Upsert(null!, item, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, null!, cfgIdentity),
					() => LinqExtensions.Upsert(table, item, configure: null!),

					() => LinqExtensions.UpsertAsync(null!, item),
					() => LinqExtensions.UpsertAsync<UpsertRow>(table, null!),
					() => LinqExtensions.UpsertAsync(null!, item, cfgIdentity),
					() => LinqExtensions.UpsertAsync<UpsertRow>(table, null!, cfgIdentity),
					() => LinqExtensions.UpsertAsync(table, item, configure: null!),

					// ------------ IEnumerable<TSource> ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(null!, enumItems, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, enumItems, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(null!, enumItems, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, enumItems, configure: null!),

					// ------------ IQueryable<TSource> ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(null!, iqsource, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>(table, iqsource, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(null!, iqsource, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>(table, iqsource, configure: null!),

					// ------------ mirror (IQueryable receiver, ITable arg) ------------
					() => LinqExtensions.Upsert<UpsertRow, UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentityTS),
					() => LinqExtensions.Upsert(iqsource, target: null!, cfgIdentityTS),
					() => LinqExtensions.Upsert(iqsource, table, configure: null!),

					() => LinqExtensions.UpsertAsync<UpsertRow, UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync(iqsource, target: null!, cfgIdentityTS),
					() => LinqExtensions.UpsertAsync(iqsource, table, configure: null!),
				};

				return cases.Select((d, i) => new TestCaseData(d).SetName($"Upsert.API.NullParameter.{i:D2}"));
			}
		}

		[TestCaseSource(nameof(NullParameterCases))]
		public void UpsertApiNullParameter(TestDelegate action)
		{
			Action act = () => action();
			act.ShouldThrow<ArgumentNullException>();
		}

		#endregion

		#region Chain-method direct-invocation — must throw NotSupportedException

		[Test]
		public void Match_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.Match((t, s) => t.Id == s.Id);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void Set_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.Set(x => x.Version, () => 1);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void InsertBranch_Set_DirectInvocation_Throws()
		{
			var builder = default(IUpsertInsertBuilder<UpsertRow, UpsertRow>)!;
			Action act = () => builder.Set(x => x.Version, () => 1);
			act.ShouldThrow<NotSupportedException>();
		}

		[Test]
		public void SkipInsert_DirectInvocation_Throws()
		{
			var upsertable = default(IUpsertable<UpsertRow, UpsertRow>)!;
			Action act = () => upsertable.SkipInsert();
			act.ShouldThrow<NotSupportedException>();
		}

		#endregion

		#region Fakes (for null-arg validation only)

		private sealed class FakeQueryProvider : IQueryProvider
		{
			IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotImplementedException();
			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
			object IQueryProvider.Execute(Expression expression) => throw new NotImplementedException();
			TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotImplementedException();
		}

		private sealed class FakeTable<TEntity> : ITable<TEntity>
			where TEntity : notnull
		{
			IDataContext            IExpressionQuery.DataContext                                  => throw new NotImplementedException();
			Expression              IExpressionQuery.Expression                                   => throw new NotImplementedException();
			IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => throw new NotImplementedException();

			Type                    IQueryable.         ElementType                               => throw new NotImplementedException();
			Expression              IQueryable.         Expression                                => throw new NotImplementedException();
			IQueryProvider          IQueryable.         Provider                                  => new FakeQueryProvider();
			Expression              IQueryProviderAsync.Expression                                => throw new NotImplementedException();

			Expression IExpressionQuery<TEntity>.Expression => Expression.Constant((ITable<TEntity>)this);

			public QueryDebugView DebugView => throw new NotImplementedException();

			IQueryable IQueryProvider.CreateQuery(Expression expression) => throw new NotImplementedException();
			IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression) => throw new NotImplementedException();
			object IQueryProvider.Execute(Expression expression) => throw new NotImplementedException();
			TResult IQueryProvider.Execute<TResult>(Expression expression) => throw new NotImplementedException();

			Task<TResult> IQueryProviderAsync.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) => throw new NotImplementedException();
			Task<IAsyncEnumerable<TResult>> IQueryProviderAsync.ExecuteAsyncEnumerable<TResult>(Expression expression, CancellationToken cancellationToken) => throw new NotImplementedException();

			IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => throw new NotImplementedException();

			public string?      ServerName   { get; }
			public string?      DatabaseName { get; }
			public string?      SchemaName   { get; }
			public string       TableName    { get; } = null!;
			public TableOptions TableOptions { get; }
			public string?      TableID      { get; }

			public string GetTableName() => throw new NotImplementedException();
		}

		#endregion
	}
}
