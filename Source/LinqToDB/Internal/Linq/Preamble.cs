using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

#pragma warning disable MA0048 // IStepMaterializer grouped with the Preamble base it complements

namespace LinqToDB.Internal.Linq
{
	abstract class Preamble
	{
		public abstract object       Execute(IDataContext      dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);

		public abstract void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values);

		/// <summary>
		/// When <see langword="true"/>, this preamble does not execute a separate query and should not
		/// trigger an implicit transaction. Used by CteUnion single-query mode where the
		/// preamble is a placeholder that resolves data from the main query's result set.
		/// </summary>
		public virtual bool IsInlined => false;
	}

	/// <summary>
	/// Implemented only by combinable preambles (Default strategy, <c>Preamble&lt;TKey,T&gt;</c>): they render a single
	/// command and materialize from a reader positioned at their own result set, so sibling combinable preambles may be
	/// merged into one multi-result-set command (combined eager loading). Non-combinable preambles do not implement this
	/// and run sequentially via <see cref="Preamble.Execute"/>.
	/// </summary>
	interface IStepMaterializer
	{
		/// <summary>
		/// When <see langword="true"/>, this preamble can be merged into a combined multi-result-set command.
		/// </summary>
		bool CanCombine { get; }

		/// <summary>
		/// Returns this preamble's single statement for merging, or <see langword="null"/> if it cannot be combined (e.g. it
		/// renders more than one command). The statement is dialect-converted by the combined-command builder through a
		/// shared parameter normalizer; its parameter values come from <see cref="AddCombinableParameterValues"/>.
		/// </summary>
		SqlStatement? GetCombinableStatement();

		/// <summary>
		/// Adds this preamble query's parameter values (keyed by their <see cref="SqlParameter"/> AST nodes) to the shared
		/// <paramref name="values"/> used to bind the combined command.
		/// </summary>
		void AddCombinableParameterValues(SqlParameterValues values, IQueryExpressions expressions, IDataContext dataContext, object?[]? parameters);

		/// <summary>
		/// Materializes this preamble's result from a reader already positioned at its result set; the caller advances the
		/// reader with <c>NextResult</c> afterwards.
		/// </summary>
		object MaterializeFromReader(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles, DbDataReader dataReader);

		/// <summary>
		/// Asynchronous sibling of <see cref="MaterializeFromReader"/>.
		/// </summary>
		Task<object> MaterializeFromReaderAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles, DbDataReader dataReader, CancellationToken cancellationToken);
	}

	/// <summary>
	/// Unified per-step result producer for the shared scenario interpreter — the seam that will replace the split between
	/// <see cref="Preamble.Execute"/> (self-executing, recursive) and <see cref="IStepMaterializer.MaterializeFromReader"/>
	/// (reads a shared combined reader). The interpreter calls <see cref="Harvest"/> for a <c>Reader</c> step and stores the
	/// returned value in that step's <see cref="SqlCommandExecutionContext"/> slot.
	/// </summary>
	interface IStepHarvester
	{
		/// <summary>
		/// Produces this step's result. When <paramref name="reader"/> is non-null the step reads its own result set from the
		/// shared combined reader (the interpreter advances <c>NextResult</c> afterwards); when <see langword="null"/> the step
		/// runs its own query (self-executing, may recurse into nested eager loading). <paramref name="context"/> gives read
		/// access to earlier steps' results.
		/// </summary>
		object Harvest(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext context, int stepIndex, DbDataReader? reader);

		/// <summary>
		/// Asynchronous sibling of <see cref="Harvest"/>.
		/// </summary>
		Task<object> HarvestAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, SqlCommandExecutionContext context, int stepIndex, DbDataReader? reader, CancellationToken cancellationToken);
	}
}
