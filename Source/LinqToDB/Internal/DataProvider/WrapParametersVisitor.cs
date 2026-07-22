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
		bool      _noCast;
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

		// The values of an IN list are expanded from the parameter when the command is built. A cast around it
		// hides the parameter from that expansion, so the whole collection ends up bound as a single value -
		// which the ADO provider then fails to convert.
		protected internal override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var save = _noCast;
			_noCast = true;
			var result = base.VisitInListPredicate(predicate);
			_noCast = save;

			return result;
		}

		// The inlined-expression nodes keep their parameter in a SqlParameter-typed field, and the base visitor
		// casts the visited result straight back to it, so nothing may be wrapped around a parameter in that
		// position. The parameter is rendered inline there in any case, which is not a place a cast applies.
		protected internal override IQueryElement VisitSqlInlinedSqlExpression(SqlInlinedSqlExpression element)
		{
			var save = _noCast;
			_noCast = true;
			var result = base.VisitSqlInlinedSqlExpression(element);
			_noCast = save;

			return result;
		}

		protected internal override IQueryElement VisitSqlInlinedToSqlExpression(SqlInlinedToSqlExpression element)
		{
			var save = _noCast;
			_noCast = true;
			var result = base.VisitSqlInlinedToSqlExpression(element);
			_noCast = save;

			return result;
		}

		protected internal override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			// A cast already occupies this position, so nothing inside it needs another one.
			var needCast = _needCast;

			using var scope = NeedCast(false);

			var newElement = base.VisitSqlCastExpression(element);

			// When this position is one the visitor would have cast, the cast that is already here has to be
			// mandatory: a non-mandatory one is exactly what the optimizer may fold away, which would leave the
			// parameter bare in SQL that requires the cast. The operand is read from the visited node, since
			// visiting is what decides what the cast finally wraps.
			if (needCast && newElement is SqlCastExpression cast && cast.Expression.ElementType == QueryElementType.SqlParameter)
				return QueryHelper.EnsureMandatoryCast(cast, cast.ToType, GetVisitMode(cast) == VisitMode.Modify);

			return newElement;
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
			// A non-query parameter is rendered by inlining its value, never as a bound parameter, so it needs
			// no cast - and some of those literals sit where a cast is not even legal, such as the operand of
			// an Informix INTERVAL, which the translator marks non-query precisely because it must stay literal.
			var needsCast =
				!_noCast &&
				sqlParameter.IsQueryParameter &&
				(_needCast ||
					sqlParameter.Type.SystemType == typeof(bool) && _wrapFlags.HasFlag(WrapFlags.CastBoolean));

			if (needsCast)
			{
				// The cast belongs to this usage, not to the parameter: the instance is shared by every
				// reference to it, so marking the instance would cast positions this wrap was never asked
				// about - and, on a Transform pass, would write into the cached statement.
				return QueryHelper.EnsureMandatoryCast(sqlParameter, sqlParameter.Type, GetVisitMode(sqlParameter) == VisitMode.Modify);
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
