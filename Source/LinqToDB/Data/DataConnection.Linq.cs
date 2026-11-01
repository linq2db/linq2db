using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Data
{
	public partial class DataConnection
	{
		protected virtual SqlStatement ProcessQuery(SqlStatement statement, EvaluationContext context)
		{
			CheckAndThrowOnDisposed();

			return statement;
		}

		static readonly ConcurrentDictionary<Type,bool> _processQueryOverrides = new();

		/// <summary>
		/// Whether this instance's type overrides <see cref="ProcessQuery"/>.
		/// </summary>
		/// <remarks>
		/// <see cref="ProcessQuery"/> is reached from one place only - the query runner's <c>GetCommand</c> - and the
		/// combined eager-loading path does not go through it: it renders the main query's statement and each combinable
		/// child's straight from the query. A subclass rewriting statements there would keep being called for the
		/// non-combinable children and silently lose the rest, so that path asks this and falls back to sequential
		/// execution rather than apply the rewrite to only part of the load.
		/// <para>
		/// Keyed on the type, not on <c>GetType() != typeof(DataConnection)</c>: the latter would disable combining for
		/// every typed subclass, most of which never touch this method. A missing method reads as overridden, which is
		/// the safe direction.
		/// </para>
		/// </remarks>
		internal bool IsProcessQueryOverridden
		{
			get
			{
				var type = GetType();

				// The common case, and free: an unsubclassed DataConnection cannot override anything.
				if (type == typeof(DataConnection))
					return false;

				return _processQueryOverrides.GetOrAdd(
					type,
					static t => t.GetMethod(
							nameof(ProcessQuery),
							BindingFlags.Instance | BindingFlags.NonPublic,
							null,
							[typeof(SqlStatement), typeof(EvaluationContext)],
							null)
						?.DeclaringType != typeof(DataConnection));
			}
		}

		#region IDataContext Members

		SqlProviderFlags IDataContext.SqlProviderFlags      => DataProvider.SqlProviderFlags;
		TableOptions     IDataContext.SupportedTableOptions => DataProvider.SupportedTableOptions;
		Type             IDataContext.DataReaderType        => DataProvider.DataReaderType;

		bool             IDataContext.CloseAfterUse    { get; set; }

		Expression IDataContext.GetReaderExpression(DbDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			CheckAndThrowOnDisposed();

			return DataProvider.GetReaderExpression(reader, idx, readerExpression, toType);
		}

		bool? IDataContext.IsDBNullAllowed(DbDataReader reader, int idx)
		{
			return DataProvider.IsDBNullAllowed(Options, reader, idx);
		}

		string IDataContext.ContextName => DataProvider.Name;

		Func<ISqlBuilder> IDataContext.CreateSqlBuilder => () => DataProvider.CreateSqlBuilder(MappingSchema, Options);

		Func<DataOptions,ISqlOptimizer> IDataContext.GetSqlOptimizer => DataProvider.GetSqlOptimizer;

		#endregion
	}
}
