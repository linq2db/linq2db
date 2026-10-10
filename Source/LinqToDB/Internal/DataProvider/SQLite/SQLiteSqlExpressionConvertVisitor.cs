using System;
using System.Collections;
using System.Collections.Generic;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	public class SQLiteSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SQLiteSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool ConcatRequiresExplicitStringCast => false;

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <inheritdoc />
		public override bool CanLowerIntervalShift => true;

		/// <summary>
		/// Shifts by whole milliseconds using SQLite's date modifiers.
		/// </summary>
		/// <remarks>
		/// Durations are truncated toward zero to whole milliseconds. SQLite also normalizes the date
		/// operand to millisecond resolution, and strftime emits three fractional digits. Sub-millisecond
		/// date precision is not preserved, even when shifting by zero.
		/// </remarks>
		protected override ISqlExpression? LowerTemporalArithmetic(SqlTemporalArithmeticExpression element)
		{
			var longType   = Factory.GetDbDataType(typeof(long));
			var doubleType = Factory.GetDbDataType(typeof(double));
			var stringType = Factory.GetDbDataType(typeof(string));

			// Truncate before converting to double, and before negation so even Int64.MinValue is safe.
			var milliseconds = TruncateDivide(element.Interval, TimeSpan.TicksPerMillisecond);
			if (element.IsSubtract)
				milliseconds = Factory.Multiply(longType, milliseconds, -1L);

			var seconds  = Factory.Div(doubleType, Factory.Cast(milliseconds, doubleType), 1000);
			var modifier = Factory.Concat(Factory.Cast(seconds, stringType), " Second");
			return SQLiteDateTimeHelper.ShiftDate(Factory, element.Type, element.Temporal, modifier);
		}

		/// <summary>
		/// <c>julianday</c> returns a double, and a Julian day number today is around 2460000 - so one unit in the
		/// last place is about 47 microseconds. The millisecond is the finest quantum that survives that, whatever
		/// the column holds.
		/// </summary>
		public override SqlIntervalUnit IntervalResolution => SqlIntervalUnit.Millisecond;

		/// <summary>
		/// Elapsed ticks from the Julian day difference, resolved to the millisecond.
		/// </summary>
		/// <remarks>
		/// SQLite has no date type: values are text, and <c>julianday</c> is the only way to do arithmetic on them.
		/// It returns a double whose spacing at present-day dates is around fifty microseconds, so the millisecond
		/// it is rounded to is the finest unit that comes back exact - and it is also the resolution SQLite date
		/// arithmetic already works at, since <c>strftime</c>'s <c>%f</c> emits three fractional digits and
		/// <c>AddTicks</c> has always lost anything below that.
		/// <para>
		/// Rounding rather than truncating for the same reason: the true value is a whole number of milliseconds,
		/// and the error is far below half of one, so the nearest is the exact one.
		/// </para>
		/// </remarks>
		protected override ISqlExpression? ElapsedTicks(SqlIntervalDifferenceExpression element)
		{
			var doubleType = Factory.GetDbDataType(typeof(double));
			var longType   = Factory.GetDbDataType(typeof(long));

			var days         = Factory.Sub(doubleType, JulianDay(element.End), JulianDay(element.Start));
			var milliseconds = Factory.Function(doubleType, "Round", Factory.Multiply(doubleType, days, (double)(TimeSpan.TicksPerDay / TimeSpan.TicksPerMillisecond)));

			return Factory.Multiply(longType, Factory.Cast(milliseconds, longType, true), TimeSpan.TicksPerMillisecond);
		}

		ISqlExpression JulianDay(ISqlExpression date)
		{
			return Factory.Function(Factory.GetDbDataType(typeof(double)), "JulianDay", date);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				// (a + b) - (a & b) * 2
				"^" => Sub(
						Add(element.Expr1, element.Expr2, element.SystemType),
						Mul(new SqlBinaryExpression(element.SystemType, element.Expr1, "&", element.Expr2), 2),
						element.SystemType
					),

				_ => base.ConvertSqlBinaryExpression(element),
			};
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = ConvertSearchStringPredicateViaLike(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)),
									"Substr",
									predicate.Expr1,
									new SqlValue(1),
									Factory.Length(predicate.Expr2)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)),
									"Substr",
									predicate.Expr1,
									new SqlUnaryExpression(
										typeof(int),
										Factory.Length(predicate.Expr2), SqlUnaryOperation.Negation,
										Precedence.Unary
									)
								),
								SqlPredicate.Operator.Equal,
								predicate.Expr2, null);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "InStr", predicate.Expr1, predicate.Expr2),
								SqlPredicate.Operator.Greater,
								new SqlValue(0), null);

						break;
					}

				}

				if (subStrPredicate != null)
				{
					var result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null,
						like,
						subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		private static bool IsDateTime(DbDataType dbDataType)
		{
			if (dbDataType.DataType
					is DataType.Date
					or DataType.Time
					or DataType.DateTime
					or DataType.DateTime2
					or DataType.DateTimeOffset
					or DataType.SmallDateTime
					or DataType.Timestamp)
				return true;

			if (dbDataType.DataType != DataType.Undefined)
				return false;

			return IsDateTime(dbDataType.SystemType);
		}

		private static bool IsDateTime(Type type)
		{
			return    type    == typeof(DateTime)
			          || type == typeof(DateTimeOffset)
			          || type == typeof(DateTime?)
			          || type == typeof(DateTimeOffset?);
		}

		public override IQueryElement ConvertExprExprPredicate(SqlPredicate.ExprExpr predicate)
		{
			var leftType  = QueryHelper.GetDbDataType(predicate.Expr1, MappingSchema);
			var rightType = QueryHelper.GetDbDataType(predicate.Expr2, MappingSchema);

			if (IsDateTime(leftType) || IsDateTime(rightType))
			{
				var dateType = IsDateTime(leftType) ? leftType : rightType;
				var expr1 = GetActualExpr(predicate.Expr1);
				if (expr1 is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					var left = PseudoFunctions.MakeMandatoryCast(predicate.Expr1, dateType, null);
					predicate = new SqlPredicate.ExprExpr(left, predicate.Operator, predicate.Expr2, predicate.UnknownAsValue);
				}

				var expr2 = GetActualExpr(predicate.Expr2);
				if (expr2 is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					var right = PseudoFunctions.MakeMandatoryCast(predicate.Expr2, dateType, null);
					predicate = new SqlPredicate.ExprExpr(predicate.Expr1, predicate.Operator, right, predicate.UnknownAsValue);
				}
			}

			return base.ConvertExprExprPredicate(predicate);

			static ISqlExpression GetActualExpr(ISqlExpression expr)
			{
				expr = QueryHelper.UnwrapNullablity(expr);

				if (expr is SelectQuery selectQuery && selectQuery.Select.Columns.Count == 1)
				{
					expr = selectQuery.Select.Columns[0].Expression;
				}

				return expr;
			}
		}

		/// <inheritdoc />
		protected internal override IQueryElement VisitInListPredicate(SqlPredicate.InList predicate)
		{
			var element = base.VisitInListPredicate(predicate);
			if (element is not SqlPredicate.InList list)
				return element;

			var dateType = QueryHelper.GetDbDataType(list.Expr1, MappingSchema);
			if (!IsDateTime(dateType))
				return list;

			// Scalar collections normally expand in the SQL builder, after this visitor. Expand them
			// here so each item can be normalized, rather than wrapping the collection parameter.
			if (list.Values is [SqlParameter parameter]
				&& parameter.GetParameterValue(EvaluationContext.ParameterValues).ProviderValue is IEnumerable items and not string)
			{
				var expanded = new List<ISqlExpression>();
				foreach (var item in items)
					expanded.Add(new SqlValue(dateType, item));
				if (expanded.Count == 0)
					return SqlPredicate.MakeBool(list.IsNot);
				list = new SqlPredicate.InList(list.Expr1, list.WithNull, list.IsNot, expanded);
			}

			var expression = WrapDateTime(list.Expr1, dateType);
			var changed    = !ReferenceEquals(expression, list.Expr1);
			var values     = new ISqlExpression[list.Values.Count];

			for (var i = 0; i < values.Length; i++)
			{
				var value = list.Values[i];
				// Keep null elements visible to the SQL builder's nullable IN-list handling.
				values[i] = value.TryEvaluateExpression(EvaluationContext, out var evaluated) && evaluated is null
					? value
					: WrapDateTime(value, dateType);
				changed |= !ReferenceEquals(values[i], value);
			}

			return changed ? new SqlPredicate.InList(expression, list.WithNull, list.IsNot, values) : list;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var underlying = cast.SystemType.ToUnderlying();

			if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)
#if SUPPORTS_DATEONLY
											   || underlying == typeof(DateOnly)
#endif
			   )
			{
				if (!(cast.Expression.TryEvaluateExpression(EvaluationContext, out var value) && value is null))
				{
					var newExpr = WrapDateTime(cast.Expression, cast.ToType);

					if (!ReferenceEquals(cast.Expression, newExpr))
						return (ISqlExpression)Visit(newExpr);
				}
			}
			else if (underlying == typeof(Guid))
			{
				// as SQLite doesn't have types - type cast expressions could result in
				// wrong affinity inferred
				// https://www.sqlite.org/datatype3.html
				return (ISqlExpression)Visit(cast.Expression);
			}

			return base.ConvertConversion(cast);
		}

		ISqlExpression WrapDateTime(ISqlExpression expression, DbDataType dbDataType)
		{
			if (IsDateTime(dbDataType))
			{
				if (expression is not (SqlCastExpression or SqlFunction { DoNotOptimize: true }))
				{
					if (IsDateDataType(dbDataType, "Date"))
						return new SqlFunction(dbDataType, "Date", expression) { DoNotOptimize = true };

					// Timestamp formatting is already complete, including any date modifiers. Date-only
					// comparisons still need the Date() normalization above, regardless of the source type.
					if (expression is SqlFunction { Name: "strftime", Parameters: [SqlValue { Value: "%Y-%m-%d %H:%M:%f" }, _, ..] } function)
						return new SqlFunction(dbDataType, function.Name, function.Flags, function.NullabilityType,
							function.CanBeNullNullable, function.Parameters) { DoNotOptimize = true };

					return new SqlFunction(dbDataType, "strftime", ParametersNullabilityType.SameAsSecondParameter, new SqlValue("%Y-%m-%d %H:%M:%f"), expression) { DoNotOptimize = true };
				}
			}

			return expression;
		}
	}
}
