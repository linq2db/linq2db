using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.DataProvider.Access
{
	using Common;
	using Extensions;
	using Linq;
	using Mapping;
	using Tools;
	using SqlProvider;
	using SqlQuery;

	class AccessSqlOptimizer : BasicSqlOptimizer
	{
		public AccessSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override bool CanCompareSearchConditions => true;

		protected static string[] AccessLikeCharactersToEscape = {"_", "?", "*", "%", "#", "-", "!"};

		public override bool   LikeIsEscapeSupported => false;

		public override string[] LikeCharactersToEscape => AccessLikeCharactersToEscape;

		public override ISqlPredicate ConvertLikePredicate(MappingSchema mappingSchema, SqlPredicate.Like predicate,
			EvaluationContext context)
		{
			if (predicate.Escape != null)
			{
				return new SqlPredicate.Like(predicate.Expr1, predicate.IsNot, predicate.Expr2, null);
			}

			return base.ConvertLikePredicate(mappingSchema, predicate, context);
		}

		public override ISqlExpression EscapeLikeCharacters(ISqlExpression expression, ref ISqlExpression? escape)
		{
			throw new LinqException("Access does not support `Replace` function which is required for such query.");
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			return statement.QueryType switch
			{
				QueryType.Delete => GetAlternativeDelete((SqlDeleteStatement)statement),
				QueryType.Update => CorrectAccessUpdate((SqlUpdateStatement)statement),
				_                => statement,
			};
		}

		private SqlUpdateStatement CorrectAccessUpdate(SqlUpdateStatement statement)
		{
			if (statement.SelectQuery.Select.HasModifier)
				throw new LinqToDBException("Access does not support update query limitation");

			statement = CorrectUpdateTable(statement);

			if (!statement.SelectQuery.OrderBy.IsEmpty)
				statement.SelectQuery.OrderBy.Items.Clear();

			return statement;
		}

		public override bool ConvertCountSubQuery(SelectQuery subQuery)
		{
			return !subQuery.Where.IsEmpty;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case "$ToLower$" : return new SqlFunction(func.SystemType, "LCase", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
				case "$ToUpper$" : return new SqlFunction(func.SystemType, "UCase", func.IsAggregate, func.IsPure, func.Precedence, func.Parameters);
			}
			return base.ConvertFunction(func);
		}

		static bool GenerateDateAdd(ISqlExpression expr1, ISqlExpression expr2, bool isSubstraction, EvaluationContext context,
			[MaybeNullWhen(false)] out ISqlExpression generated)
		{
			var dbType1 = expr1.GetExpressionType();
			var dbType2 = expr2.GetExpressionType();

			if (dbType1.SystemType.ToNullableUnderlying().In(typeof(DateTime), typeof(DateTimeOffset))
			    && dbType2.SystemType.ToNullableUnderlying() == typeof(TimeSpan)
			    && expr2.TryEvaluateExpression(context, out var value))
			{
				var ts = value as TimeSpan?;
				var interval = "d";
				long? increment;

				if (ts == null)
				{
					generated = new SqlValue(dbType1, null);
					return true;
				}

				if (ts.Value.Seconds > 0)
				{
					increment = (long)ts.Value.TotalSeconds;
					interval = "s";
				}
				else if (ts.Value.Minutes > 0)
				{
					increment = (long)ts.Value.TotalMinutes;
					interval = "n";
				}
				else if (ts.Value.Hours > 0)
				{
					increment = (long)ts.Value.TotalHours;
					interval = "h";
				}
				else
				{
					increment = (long)ts.Value.TotalDays;
				}

				if (isSubstraction)
					increment = -increment;

				generated = new SqlFunction(
						dbType1.SystemType!,
						"DateAdd",
						false,
						true,
						new SqlValue(interval),
						CreateSqlValue(increment, new DbDataType(typeof(long)), expr2),
						expr1)
					{ CanBeNull = expr1.CanBeNull || expr2.CanBeNull };
				return true;
			}


			generated = null;
			return false;
		}

		public override ISqlExpression ConvertExpressionImpl(ISqlExpression expr, ConvertVisitor visitor, EvaluationContext context)
		{
			expr = base.ConvertExpressionImpl(expr, visitor, context);

			switch (expr.ElementType)
			{
				case QueryElementType.SqlBinaryExpression:
				{
					var be = (SqlBinaryExpression)expr;

					switch (be.Operation)
					{
						case "+":
						{
							if (GenerateDateAdd(be.Expr1, be.Expr2, false, context, out var generated))
								return generated;

							if (GenerateDateAdd(be.Expr2, be.Expr1, false, context, out generated))
								return generated;

							break;
						}
						case "-":
						{
							if (GenerateDateAdd(be.Expr1, be.Expr2, true, context, out var generated))
								return generated;

							break;
						}
					}

					break;
				}

			}

			return expr;
		}
	}
}
