using System;
using System.Collections.Generic;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.DataProvider
{
	public class WrapParametersVisitor : SqlQueryVisitor
	{
		bool      _needCast;
		bool      _inModifier;
		bool      _inInsert;
		bool      _inInsertOrUpdate;
		WrapFlags _wrapFlags;

		// One cast copy per source parameter, for the lifetime of a single traversal (every caller
		// constructs a fresh visitor). Without it a parameter shared across several MERGE clauses
		// would get a copy per occurrence, and since parameter naming dedupes by object identity
		// each copy would render its own DECLARE/SET of the same value.
		Dictionary<object, SqlParameter>? _castParameters;

		[Flags]
		public enum WrapFlags
		{
			None                 = 0,
			InSelect             = 1 << 0,
			InUpdateSet          = 1 << 1,
			InInsertValue        = 1 << 2,
			InInsertOrUpdate     = 1 << 4,
			InOutput             = 1 << 5,
			InMerge              = 1 << 6,
			InBinary             = 1 << 7,
			InFunctionParameters = 1 << 8,
			CastBoolean 		 = 1 << 9,
		}
	
		public WrapParametersVisitor(VisitMode visitMode) : base(visitMode, null)
		{
		}

		public IQueryElement WrapParameters(IQueryElement element, WrapFlags wrapFlags)
		{
			_wrapFlags = wrapFlags;
			var result = ProcessElement(element);

			return result;
		}

		readonly struct NeedCastScope : IDisposable
		{
			readonly WrapParametersVisitor _visitor;
			readonly bool                  _saveValue;

			public NeedCastScope(WrapParametersVisitor visitor, bool needCast)
			{
				_visitor = visitor;
				_saveValue = visitor._needCast;
				visitor._needCast = needCast;
			}

			public void Dispose()
			{
				_visitor._needCast = _saveValue;
			}
		}

		NeedCastScope NeedCast(bool needCast)
		{
			return new NeedCastScope(this, needCast);
		}

		// TAKE/SKIP are modifiers, not value positions: no cast belongs there, and the suppression has to
		// cover the whole subtree (VisitSqlBinaryExpression and friends read _inModifier). Hooking the two
		// modifiers keeps the clause traversal, and with it the visit-mode handling, in the base - this
		// visitor never writes into a select clause it may not own.
		protected override ISqlExpression? VisitTake(SqlSelectClause selectClause, ISqlExpression? takeValue)
		{
			return VisitModifier(takeValue);
		}

		protected override ISqlExpression? VisitSkip(SqlSelectClause selectClause, ISqlExpression? skipValue)
		{
			return VisitModifier(skipValue);
		}

		ISqlExpression? VisitModifier(ISqlExpression? expression)
		{
			var save = _inModifier;

			using var scope = NeedCast(false);

			_inModifier = true;
			var result = (ISqlExpression?)Visit(expression);
			_inModifier = save;

			return result;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			using var scope = NeedCast(_wrapFlags.HasFlag(WrapFlags.InSelect));
			return base.VisitSqlColumnExpression(column, expression);
		}

		protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			using var scope = NeedCast(false);
			return base.VisitSqlQuery(selectQuery);
		}

		protected internal override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			using var scope = NeedCast(false);
			return base.VisitSqlCastExpression(element);
		}

		protected internal override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
		{
			var saveNeedCast = _needCast;

			if (!_needCast)
			{
				if (_inInsertOrUpdate)
				{
					if (_wrapFlags.HasFlag(WrapFlags.InInsertOrUpdate))
						_needCast = true;
				}
				else if (_inInsert)
				{
					if (_wrapFlags.HasFlag(WrapFlags.InInsertValue))
						_needCast = true;
				}
				else if (_wrapFlags.HasFlag(WrapFlags.InUpdateSet))
				{
					_needCast = true;
				}
			}

			var result = base.VisitSqlSetExpression(element);

			_needCast = saveNeedCast;

			return result;
		}

		protected internal override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			using var scope = NeedCast(!_inModifier && _wrapFlags.HasFlag(WrapFlags.InBinary));
			return base.VisitSqlBinaryExpression(element);
		}

		protected internal override IQueryElement VisitSqlCoalesceExpression(SqlCoalesceExpression element)
		{
			using var scope = NeedCast(!_inModifier && _wrapFlags.HasFlag(WrapFlags.InFunctionParameters));
			return base.VisitSqlCoalesceExpression(element);
		}

		protected internal override IQueryElement VisitSqlConcatExpression(SqlConcatExpression element)
		{
			// Emitted as `+` / `||` chain or `CONCAT(...)` per provider's ConcatBuildStyle.
			// `||` and `+` chains are binary-shaped at the SQL level — gate consistently with
			// SqlBinaryExpression so operand parameter-wrap behaviour matches.
			using var scope = NeedCast(!_inModifier && _wrapFlags.HasFlag(WrapFlags.InBinary));
			return base.VisitSqlConcatExpression(element);
		}

		protected internal override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			using var scope = NeedCast(!_inModifier && _wrapFlags.HasFlag(WrapFlags.InFunctionParameters));
			return base.VisitSqlFunction(element);
		}

		protected internal override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
		{
			var needsCast =
				_needCast ||
				sqlParameter.Type.SystemType == typeof(bool) && _wrapFlags.HasFlag(WrapFlags.CastBoolean);

			if (needsCast && !sqlParameter.NeedsCast)
			{
				// The flag goes onto a copy, which the parent picks up from the return value. A caller
				// running inside a Transform-mode convert does not own the parameter - it still belongs to
				// the cached statement - and even a caller that owns the statement shares one instance
				// across every usage of that parameter, so setting the flag on it would cast references
				// this wrap was never asked about.
				_castParameters ??= new();

				// AccessorId identifies the accessor a query parameter's value comes from; ParametersContext
				// keeps one instance per id, so it is 1:1 with the parameter and additionally unifies copies
				// an earlier stage may have made. Parameters without an accessor (take/skip, dynamic) carry
				// their own value, so there the instance itself is the only sound key.
				var key = sqlParameter.AccessorId is int accessorId ? accessorId : (object)sqlParameter;

				if (!_castParameters.TryGetValue(key, out var newParameter))
				{
					newParameter = new SqlParameter(sqlParameter.Type, sqlParameter.Name, sqlParameter.Value)
					{
						IsQueryParameter = sqlParameter.IsQueryParameter,
						AccessorId       = sqlParameter.AccessorId,
						ValueConverter   = sqlParameter.ValueConverter,
						NeedsCast        = true,
					};

					_castParameters.Add(key, newParameter);
				}

				return newParameter;
			}

			return base.VisitSqlParameter(sqlParameter);
		}

		protected internal override IQueryElement VisitSqlInsertClause(SqlInsertClause element)
		{
			var save = _inInsert;
			_inInsert = true;
			var result = base.VisitSqlInsertClause(element);
			_inInsert = save;

			return result;
		}

		protected internal override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			using var scope = NeedCast(_wrapFlags.HasFlag(WrapFlags.InOutput));
			return base.VisitSqlOutputClause(element);
		}

		protected internal override IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
		{
			using var scope = NeedCast(_wrapFlags.HasFlag(WrapFlags.InMerge));
			return base.VisitSqlMergeOperationClause(element);
		}

		protected internal override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			var saveInInsertOrUpdate = _inInsertOrUpdate;
			_inInsertOrUpdate = true;
			var result = base.VisitSqlInsertOrUpdateStatement(element);
			_inInsertOrUpdate = saveInInsertOrUpdate;

			return result;
		}
	}
}
