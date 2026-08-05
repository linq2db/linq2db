using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		/// <summary>
		/// Elapsed ticks between two date/time values, via <c>DATEDIFF_BIG</c> at nanosecond resolution.
		/// </summary>
		/// <remarks>
		/// Nanoseconds rather than a coarser unit because <c>datetime2</c> stores 100ns and anything coarser would
		/// silently drop the fraction. Every value SQL Server can store is a whole number of 100ns, so dividing by
		/// 100 is exact. The nanosecond form overflows <c>bigint</c> beyond roughly 292 years, and SQL Server
		/// raises an error there rather than returning a wrapped value - a loud failure, not a wrong duration.
		/// <para>
		/// <c>DATEDIFF_BIG</c> arrived in SQL Server 2016, so 2012 and earlier override this back to unsupported.
		/// </para>
		/// </remarks>
		/// <summary>
		/// <c>DATEDIFF_BIG</c> counts nanoseconds, and <c>datetime2</c> stores 100ns, so the leftover of a total
		/// is exact. It is only ever applied to a sub-unit window, well inside the roughly 292 years at which the
		/// nanosecond form would overflow.
		/// </summary>
		protected override SqlIntervalUnit? FinestDateUnit => SqlIntervalUnit.Nanosecond;

		/// <summary>
		/// A difference used on its own, with no member taken from it, still has to become a value. Ticks are that
		/// value - the read path turns them back into a <c>TimeSpan</c> - and nanoseconds divided by 100 are
		/// exact for every instant <c>datetime2</c> can hold.
		/// </summary>
		/// <remarks>
		/// This is the one place a whole-range fine difference is used, so it carries that form's roughly 292-year
		/// limit. Members of a difference do not go through here; they are counted in their own unit and never
		/// form a tick total.
		/// </remarks>
		protected override ISqlExpression? LowerIntervalDifference(SqlIntervalDifferenceExpression element)
		{
			var longType    = Factory.GetDbDataType(typeof(long));
			var nanoseconds = CountDateBoundaries(SqlIntervalUnit.Nanosecond, element.Start, element.End);

			return nanoseconds == null ? null : Factory.Div(longType, nanoseconds, Factory.Value(longType, 100L));
		}

		static string? DatePartName(SqlIntervalUnit unit)
		{
			return unit switch
			{
				SqlIntervalUnit.Nanosecond  => "nanosecond",
				SqlIntervalUnit.Day         => "day",
				SqlIntervalUnit.Hour        => "hour",
				SqlIntervalUnit.Minute      => "minute",
				SqlIntervalUnit.Second      => "second",
				SqlIntervalUnit.Millisecond => "millisecond",
				SqlIntervalUnit.Microsecond => "microsecond",
				_                           => null,
			};
		}

		protected override ISqlExpression? ShiftDate(SqlIntervalUnit unit, ISqlExpression amount, ISqlExpression date)
		{
			var part = DatePartName(unit);
			if (part == null)
				return null;

			return Factory.Function(Factory.GetDbDataType(date), "DateAdd",
				Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part), amount, date);
		}

		protected override ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			var part = DatePartName(unit);
			if (part == null)
				return null;

			// DateDiff_Big, not DateDiff: the 32-bit form overflows at about 24 days in milliseconds. Counting
			// whole units keeps the number small, but the caller may ask for a fine unit over a long range.
			return Factory.Function(Factory.GetDbDataType(typeof(long)), "DateDiff_Big",
				Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part), start, end);
		}

		readonly SqlServerVersion _sqlServerVersion;

		public SqlServerSqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify)
		{
			_sqlServerVersion = sqlServerVersion;
		}

		protected override bool SupportsDistinctAsExistsIntersect => _sqlServerVersion < SqlServerVersion.v2022;

		public override ISqlExpression ConvertConcat(SqlConcatExpression element)
		{
			// SQL Server's `+` (and 2025+ `||`) operator and `CONCAT(...)` function reject
			// `text`/`ntext` operands ("The data types nvarchar and ntext are incompatible
			// in the add operator"). These LOB types have been deprecated since 2005 and
			// cannot participate in string operations — cast them up to `[N]VARCHAR(MAX)`
			// before delegating to the base concat lowering.
			ISqlExpression[]? operands = null;

			for (var i = 0; i < element.Expressions.Length; i++)
			{
				var operand     = element.Expressions[i];
				var operandType = QueryHelper.GetDbDataType(operand, MappingSchema);

				var castTo = operandType.DataType switch
				{
					DataType.NText => new DbDataType(typeof(string), DataType.NVarChar),
					DataType.Text  => new DbDataType(typeof(string), DataType.VarChar),
					_              => default(DbDataType?),
				};

				if (castTo == null)
					continue;

				operands    ??= (ISqlExpression[])element.Expressions.Clone();
				operands[i] = PseudoFunctions.MakeCast(operand, castTo.Value);
			}

			if (operands != null)
				element = new SqlConcatExpression(element.PreserveNull, operands);

			return base.ConvertConcat(element);
		}

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var like = base.ConvertSearchStringPredicate(predicate);

			if (predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) == true)
			{
				SqlPredicate.ExprExpr? subStrPredicate = null;

				switch (predicate.Kind)
				{
					case SqlPredicate.SearchString.SearchKind.StartsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)), "LEFT", predicate.Expr1,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "LEN", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, new SqlFunction(
									MappingSchema.GetDbDataType(typeof(string)), "RIGHT", predicate.Expr1,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "LEN", predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);

						break;
					}
					case SqlPredicate.SearchString.SearchKind.Contains:
					{
						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "CHARINDEX",
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr2),
									new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
										predicate.Expr1)),
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

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var type1 = element.Expr1.SystemType!.ToUnderlying();

					if (type1 == typeof(double) || type1 == typeof(float))
					{
						return new SqlBinaryExpression(
							element.Expr2.SystemType!,
							new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "Convert", SqlDataType.Int32, element.Expr1),
							element.Operation,
							element.Expr2);
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			cast = FloorBeforeConvert(cast);

			if (cast.ToType.DataType == DataType.Decimal)
			{
				if (cast.ToType.Precision == null && cast.ToType.Scale == null)
				{
					cast = cast.WithToType(cast.ToType.WithPrecisionScale(38, 17));
				}
			}

			return base.ConvertConversion(cast);
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			switch (func.Name)
			{
				case PseudoFunctions.LENGTH:
				{
					/*
					 * LEN(value + ".") - 1
					 */

					var value     = func.Parameters[0];
					var valueType = Factory.GetDbDataType(value);
					var funcType  = Factory.GetDbDataType(typeof(int));

					var valueString = Factory.Concat(value, Factory.Value(valueType, "."));
					var valueLength = Factory.Function(funcType, "LEN", valueString);

					return Factory.Sub(func.Type, valueLength, Factory.Value(func.Type, 1));
	}
}

			return base.ConvertSqlFunction(func);
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			if (expr is SqlValue
				{
					Value: uint or long or ulong or float or double or decimal,
				} value)
			{
				expr = new SqlCastExpression(expr, value.ValueType, null, isMandatory: true);
			}

			if (expr is SqlParameter { IsQueryParameter: false } param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();
				if (paramType == typeof(uint)
					|| paramType == typeof(long)
					|| paramType == typeof(ulong)
					|| paramType == typeof(float)
					|| paramType == typeof(double)
					|| paramType == typeof(decimal))
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
