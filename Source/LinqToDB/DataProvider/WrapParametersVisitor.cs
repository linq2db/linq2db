using System;

namespace LinqToDB.DataProvider
{
	using SqlQuery;
	using SqlQuery.Visitors;

	class WrapParametersVisitor : SqlQueryVisitor
	{
		bool _needCast;
		bool _inModifier;

		public WrapParametersVisitor(VisitMode visitMode) : base(visitMode)
		{
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
			using var scope = NeedCast(true);
			return base.VisitSqlColumnExpression(column, expression);
		}

		protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
		{
			using var scope = NeedCast(false);
			return base.VisitSqlQuery(selectQuery);
		}

		protected override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			if (element.Name == PseudoFunctions.CONVERT)
			{
				var newElement = base.VisitSqlFunction(element);

				if (newElement is SqlFunction func)
				{
					var foundParam = false;
					foreach (var param in func.Parameters)
					{
						if (param is SqlParameter sqlParam)
						{
							foundParam = true;
							sqlParam.NeedsCast = false;
						}
					}

					if (foundParam)
						func.DoNotOptimize = true;
				}

				return newElement;
			}

			return base.VisitSqlFunction(element);
		}

		protected override IQueryElement VisitSqlUpdateStatement(SqlUpdateStatement element)
		{
			using var scope = NeedCast(true);
			return base.VisitSqlUpdateStatement(element);
		}

		protected override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			using var scope = NeedCast(!_inModifier);
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

		protected override IQueryElement VisitSqlInsertOrUpdateStatement(SqlInsertOrUpdateStatement element)
		{
			using var scope = NeedCast(true);
			return base.VisitSqlInsertOrUpdateStatement(element);
		}

		protected override IQueryElement VisitSqlOutputClause(SqlOutputClause element)
		{
			using var scope = NeedCast(true);
			return base.VisitSqlOutputClause(element);
		}

		protected override IQueryElement VisitSqlMergeOperationClause(SqlMergeOperationClause element)
		{
			using var scope = NeedCast(true);
			return base.VisitSqlMergeOperationClause(element);
		}
	}
}
