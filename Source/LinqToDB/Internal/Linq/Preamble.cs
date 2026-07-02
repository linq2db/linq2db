using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlQuery;

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

		/// <summary>
		/// When <see langword="true"/>, this preamble can render a single command and materialize from a reader
		/// positioned at its own result set, so sibling combinable preambles may be merged into one
		/// multi-result-set command (combined eager loading). Default: <see langword="false"/>.
		/// </summary>
		public virtual bool CanCombine => false;

		/// <summary>
		/// Returns this preamble's single statement for merging into one multi-result-set command, or
		/// <see langword="null"/> if it cannot be combined (e.g. it renders more than one command). Only called when
		/// <see cref="CanCombine"/> is <see langword="true"/>. The statement is dialect-converted by the combined-command
		/// builder through a shared parameter normalizer; its parameter values come from <see cref="AddCombinableParameterValues"/>.
		/// </summary>
		public virtual SqlStatement? GetCombinableStatement() => null;

		/// <summary>
		/// Adds this preamble query's parameter values (keyed by their <see cref="SqlParameter"/> AST nodes) to the
		/// shared <paramref name="values"/> used to bind the combined command. Only called when <see cref="CanCombine"/>
		/// is <see langword="true"/>.
		/// </summary>
		public virtual void AddCombinableParameterValues(SqlParameterValues values, IQueryExpressions expressions, IDataContext dataContext, object?[]? parameters) { }

		/// <summary>
		/// Materializes this preamble's result from a reader already positioned at its result set; the caller advances
		/// the reader with <c>NextResult</c> afterwards. Only called when <see cref="CanCombine"/> is <see langword="true"/>.
		/// </summary>
		public virtual object MaterializeFromReader(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles, DbDataReader dataReader)
			=> throw new NotSupportedException();

		/// <summary>
		/// Asynchronous sibling of <see cref="MaterializeFromReader"/>.
		/// </summary>
		public virtual Task<object> MaterializeFromReaderAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object?[]? preambles, DbDataReader dataReader, CancellationToken cancellationToken)
			=> throw new NotSupportedException();
	}
}
