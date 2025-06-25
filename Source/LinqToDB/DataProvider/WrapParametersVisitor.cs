using System;

using LinqToDB.SqlQuery;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.DataProvider
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
			None             = 0,
			InSelect         = 1 << 0,
			InUpdateSet      = 1 << 1,
			InInsertValue    = 1 << 2,
			InInsertOrUpdate = 1 << 4,
			InOutput         = 1 << 5,
			InMerge          = 1 << 6,
			InBinary         = 1 << 7,
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

		protected override IQueryElement VisitSqlSelectClause(SqlSelectClause element)
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

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			using var scope = NeedCast(false);
			return base.VisitSqlQuery(selectQuery);
		}

		protected override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			using var scope = NeedCast(false);
			return base.VisitSqlCastExpression(element);
		}

		protected override IQueryElement VisitSqlSetExpression(SqlSetExpression element)
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

		protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			using var scope = NeedCast(!_inModifier && _wrapFlags.HasFlag(WrapFlags.InBinary));
			return base.VisitSqlBinaryExpression(element);
		}

		protected override IQueryElement VisitSqlParameter(SqlParameter sqlParameter)
		{
			if (_needCast)
			{
				if (!sqlParameter.NeedsCast)
				{
					sqlParameter.NeedsCast = true;
				}
			}

			return base.VisitSqlParameter(sqlParameter);
		}

		protected override IQueryElement VisitSqlInsertClause(SqlInsertClause element)
		{
			var save = _inInsert;
			_inInsert = true;
			var result = base.VisitSqlInsertClause(element);
			_inInsert = save;

			return result;
		}

		protected override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			using var scope = NeedCast(_wrapFlags.HasFlag(WrapFlags.InOutput));
			return base.VisitSqlOutputClause(element);
		}

		protected override IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
		{
			using var scope = NeedCast(_wrapFlags.HasFlag(WrapFlags.InMerge));
			return base.VisitSqlMergeOperationClause(element);
		}

		protected override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			var saveInInsertOrUpdate = _inInsertOrUpdate;
			_inInsertOrUpdate = true;
			var result = base.VisitSqlInsertOrUpdateStatement(element);
			_inInsertOrUpdate = saveInInsertOrUpdate;

			return result;
		}
	}
}
