using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

#pragma warning disable MA0048 // IStepMaterializer grouped with the Harvester base it complements

namespace LinqToDB.Internal.Linq
{
	abstract class Harvester
	{
		public abstract object       Execute(IDataContext      dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, CancellationToken cancellationToken);

		public abstract void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values);

		/// <summary>
		/// When <see langword="true"/>, this harvester does not execute a separate query and should not
		/// trigger an implicit transaction. Used by CteUnion single-query mode where the
		/// harvester is a placeholder that resolves data from the main query's result set.
		/// </summary>
		public virtual bool IsInlined => false;

		/// <summary>
		/// Produces this harvester's result for the scenario interpreter: the self-executing path (<paramref name="reader"/>
		/// is <see langword="null"/>) runs <see cref="Execute"/> (its own query, may recurse into nested eager loading); the
		/// combined-reader path materializes this harvester's result set from the shared reader via
		/// <see cref="IStepMaterializer"/> — only combinable harvesters are ever invoked with a non-null reader.
		/// </summary>
		public object Harvest(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext context, DbDataReader? reader)
			=> reader is null
				? Execute(dataContext, expressions, parameters, context)
				: ((IStepMaterializer)this).MaterializeFromReader(dataContext, expressions, parameters, context, reader);

		/// <summary>Asynchronous sibling of <see cref="Harvest"/>.</summary>
		public Task<object> HarvestAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext context, DbDataReader? reader, CancellationToken cancellationToken)
			=> reader is null
				? ExecuteAsync(dataContext, expressions, parameters, context, cancellationToken)
				: ((IStepMaterializer)this).MaterializeFromReaderAsync(dataContext, expressions, parameters, context, reader, cancellationToken);
	}

	/// <summary>
	/// Implemented only by combinable harvesters (Default strategy, <c>Harvester&lt;TKey,T&gt;</c>): they render a single
	/// command and materialize from a reader positioned at their own result set, so sibling combinable harvesters may be
	/// merged into one multi-result-set command (combined eager loading). Non-combinable harvesters do not implement this
	/// and run sequentially via <see cref="Harvester.Execute"/>.
	/// </summary>
	interface IStepMaterializer
	{
		/// <summary>
		/// When <see langword="true"/>, this harvester can be merged into a combined multi-result-set command.
		/// </summary>
		bool CanCombine { get; }

		/// <summary>
		/// Returns this harvester's single statement for merging, or <see langword="null"/> if it cannot be combined (e.g. it
		/// renders more than one command). The statement is dialect-converted by the combined-command builder through a
		/// shared parameter normalizer; its parameter values come from <see cref="AddCombinableParameterValues"/>.
		/// </summary>
		SqlStatement? GetCombinableStatement();

		/// <summary>
		/// Adds this harvester query's parameter values (keyed by their <see cref="SqlParameter"/> AST nodes) to the shared
		/// <paramref name="values"/> used to bind the combined command.
		/// </summary>
		void AddCombinableParameterValues(SqlParameterValues values, IQueryExpressions expressions, IDataContext dataContext, object?[]? parameters);

		/// <summary>
		/// Materializes this harvester's result from a reader already positioned at its result set; the caller advances the
		/// reader with <c>NextResult</c> afterwards.
		/// </summary>
		object MaterializeFromReader(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, DbDataReader dataReader);

		/// <summary>
		/// Asynchronous sibling of <see cref="MaterializeFromReader"/>.
		/// </summary>
		Task<object> MaterializeFromReaderAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext? context, DbDataReader dataReader, CancellationToken cancellationToken);
	}
}
