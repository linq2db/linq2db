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

using NUnit.Framework;

using Shouldly;

namespace Tests.xUpdate
{
	/// <summary>
	/// Null-argument validation for every public <c>Upsert</c> / <c>UpsertAsync</c>
	/// overload — mirrors the <c>MergeTests</c> ApiParametersValidation suite.
	/// </summary>
	public partial class UpsertTests
	{
		public static IEnumerable<TestCaseData> NullParameterCases
		{
			get
			{
				var table         = new FakeTable<UpsertRow>();
				var iqsource      = (IQueryable<UpsertRow>)table;
				var item          = new UpsertRow();
				var enumItems     = new[] { new UpsertRow() };
				Expression<Func<IEntityUpsertBuilder<UpsertRow>, IEntityUpsertBuilder<UpsertRow>>> cfgIdentity = u => u;

				var cases = new TestDelegate[]
				{
					// ------------ single entity ------------
					() => LinqExtensions.Upsert(null!, item),
					() => LinqExtensions.Upsert<UpsertRow>(table, null!),
					() => LinqExtensions.Upsert(null!, item, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, (UpsertRow)null!, cfgIdentity),
					() => LinqExtensions.Upsert(table, item, configure: null!),

					() => { _ = LinqExtensions.UpsertAsync(null!, item); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, null!); },
					() => { _ = LinqExtensions.UpsertAsync(null!, item, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, (UpsertRow)null!, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync(table, item, configure: null!); },

					// ------------ IEnumerable<T> ------------
					() => LinqExtensions.Upsert<UpsertRow>(null!, enumItems, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, enumItems, configure: null!),

					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(null!, enumItems, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, (IEnumerable<UpsertRow>)null!, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, enumItems, configure: null!); },

					// ------------ IQueryable<T> ------------
					() => LinqExtensions.Upsert<UpsertRow>(null!, iqsource, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentity),
					() => LinqExtensions.Upsert<UpsertRow>(table, iqsource, configure: null!),

					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(null!, iqsource, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, (IQueryable<UpsertRow>)null!, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>(table, iqsource, configure: null!); },

					// ------------ mirror (IQueryable receiver, ITable arg) ------------
					() => LinqExtensions.Upsert<UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentity),
					() => LinqExtensions.Upsert(iqsource, target: null!, cfgIdentity),
					() => LinqExtensions.Upsert(iqsource, table, configure: null!),

					() => { _ = LinqExtensions.UpsertAsync<UpsertRow>((IQueryable<UpsertRow>)null!, table, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync(iqsource, target: null!, cfgIdentity); },
					() => { _ = LinqExtensions.UpsertAsync(iqsource, table, configure: null!); },
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
