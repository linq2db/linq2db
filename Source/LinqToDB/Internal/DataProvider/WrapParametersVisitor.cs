using System;

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

		protected internal override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
		{
			var save = _inModifier;

			using var scope = NeedCast(false);
			{
				_inModifier = true;

				element.TakeValue = (ISqlExpression?)Visit(element.TakeValue);
				element.SkipValue = (ISqlExpression?)Visit(element.SkipValue);

				_inModifier = save;
			}

			foreach (var column in element.Columns)
			{
				column.Expression = VisitSqlColumnExpression(column, column.Expression);
			}

			return element;
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
				// Callers that run this visitor over a statement they own (the provider
				// FinalizeStatement passes) construct it with VisitMode.Modify and keep the
				// in-place flip. A caller running inside a Transform-mode convert does not own
				// the parameter - it still belongs to the cached statement - so there the flag
				// has to go onto a copy, or it leaks a cast into every later render.
				if (GetVisitMode(sqlParameter) == VisitMode.Modify)
				{
					sqlParameter.NeedsCast = true;
				}
				else
				{
					var newParameter = new SqlParameter(sqlParameter.Type, sqlParameter.Name, sqlParameter.Value)
					{
						IsQueryParameter = sqlParameter.IsQueryParameter,
						AccessorId       = sqlParameter.AccessorId,
						ValueConverter   = sqlParameter.ValueConverter,
						NeedsCast        = true,
					};

					return NotifyReplaced(newParameter, sqlParameter);
				}
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
