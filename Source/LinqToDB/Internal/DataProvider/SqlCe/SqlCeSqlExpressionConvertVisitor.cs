using System;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	public class SqlCeSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SqlCeSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullIf => false;

		/// <summary>
		/// The divisor carries an explicit <c>BIGINT</c> cast.
		/// </summary>
		/// <remarks>
		/// SQL CE types a literal past the <c>INT</c> range as <c>NUMERIC</c>, which would make the division
		/// numeric as well - and its <c>%</c> rejects that type outright: "Modulo is not supported on real, float,
		/// money, and numeric data types". Naming the type keeps the division integral.
		/// </remarks>
		protected override ISqlExpression TruncateDivide(ISqlExpression value, long divisor)
		{
			return Factory.Div(Factory.GetDbDataType(typeof(long)), value, TypedDivisor(divisor));
		}

		/// <summary>
		/// The same explicit <c>BIGINT</c> the division needs - SQL CE refuses a remainder on a numeric outright.
		/// </summary>
		protected override ISqlExpression TruncateRemainder(ISqlExpression value, long divisor)
		{
			return Factory.Mod(Factory.GetDbDataType(typeof(long)), value, TypedDivisor(divisor));
		}

		ISqlExpression TypedDivisor(long divisor)
		{
			var longType = Factory.GetDbDataType(typeof(long));

			return Factory.Cast(Factory.Value(longType, divisor), longType, true);
		}

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// <c>DATEDIFF</c> counts milliseconds, which is what a SQL CE <c>datetime</c> stores, so the leftover of a
		/// total is exact.
		/// </summary>
		protected override SqlIntervalUnit? FinestDateUnit => SqlIntervalUnit.Millisecond;

		/// <summary>
		/// The measurement stops at the millisecond as well, so a component asked for below one is identically zero
		/// rather than merely imprecise - the count it is taken from was rounded to a coarser unit first.
		/// </summary>
		/// <remarks>
		/// Declared for the reason SQLite declares the same limit: declining while the expression is still being
		/// built leaves the member to .NET, which holds both dates and answers exactly. A stored difference does
		/// carry a sub-millisecond part here - a <c>datetime</c> counts in three-and-a-third millisecond steps - so
		/// answering zero would be a wrong number rather than a coarse one.
		/// </remarks>
		public override SqlIntervalUnit IntervalResolution => SqlIntervalUnit.Millisecond;

		static string? DatePartName(SqlIntervalUnit unit)
		{
			return unit switch
			{
				SqlIntervalUnit.Day         => "day",
				SqlIntervalUnit.Hour        => "hour",
				SqlIntervalUnit.Minute      => "minute",
				SqlIntervalUnit.Second      => "second",
				SqlIntervalUnit.Millisecond => "millisecond",
				_                           => null,
			};
		}

		protected override ISqlExpression? ShiftDate(SqlIntervalUnit unit, ISqlExpression amount, ISqlExpression date)
		{
			var part = DatePartName(unit);

			return part == null
				? null
				: Factory.Function(Factory.GetDbDataType(date), "DateAdd",
					Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part), amount, date);
		}

		/// <summary>
		/// Boundary counting through <c>DATEDIFF</c>, which the anchor correction turns into elapsed units.
		/// </summary>
		/// <remarks>
		/// SQL CE has no wide form of <c>DATEDIFF</c>, so the count is a 32-bit integer and overflows about 24 days
		/// apart in milliseconds. Counting whole units keeps the number small for every unit a member asks for, and
		/// the fine count that fills in a fraction is only ever taken across a window shorter than one of those
		/// units. Only a total asked for in milliseconds spans the whole range in the fine unit, and there SQL CE
		/// raises an overflow rather than returning a wrapped value.
		/// </remarks>
		protected override ISqlExpression? CountDateBoundaries(SqlIntervalUnit unit, ISqlExpression start, ISqlExpression end)
		{
			var part = DatePartName(unit);

			return part == null
				? null
				: Factory.Cast(
					Factory.Function(Factory.GetDbDataType(typeof(int)), "DateDiff",
						Factory.NotNullExpression(Factory.GetDbDataType(typeof(string)), part), start, end),
					Factory.GetDbDataType(typeof(long)), true);
		}

		#region LIKE

		private static readonly string[] LikeSqlCeCharactersToEscape = { "_", "%" };

		public override string[] LikeCharactersToEscape => LikeSqlCeCharactersToEscape;

		#endregion

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			switch (element.Operation)
			{
				case "%":
				{
					var exprType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);

					if (!IsRemainderable(exprType))
					{
						return new SqlBinaryExpression(
							typeof(int),
							PseudoFunctions.MakeCast(element.Expr1, new DbDataType(typeof(int), DataType.Int32)),
							element.Operation,
							element.Expr2,
							element.Precedence);
					}

					break;
				}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		/// <summary>
		/// Whether SQL CE will take a remainder of the value as it stands - "Modulo is not supported on real, float,
		/// money, and numeric data types".
		/// </summary>
		/// <remarks>
		/// A column carrying a value converter is read as something its storage does not say - a duration read from a
		/// <c>BIGINT</c> - and the remainder is taken of what is stored, so the stored type answers here as well as the
		/// read one. Reading only the latter casts such a column to <c>INT</c>, which a duration in ticks overflows
		/// after a little over three minutes.
		/// </remarks>
		static bool IsRemainderable(DbDataType type)
		{
			return type.SystemType.IsIntegerType
				|| (type.DataType != DataType.Undefined && SqlDataType.GetDataType(type.DataType).Type.SystemType.IsIntegerType);
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
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "SUBSTRING",
										predicate.Expr1,
										new SqlValue(1),
										Factory.Length(predicate.Expr2))),
								SqlPredicate.Operator.Equal,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary, predicate.Expr2),
								null
							);
						break;
					}

					case SqlPredicate.SearchString.SearchKind.EndsWith:
					{
						var indexExpression = new SqlBinaryExpression(typeof(int),
							new SqlBinaryExpression(typeof(int),
								Factory.Length(predicate.Expr1),
								"-",
								Factory.Length(predicate.Expr2)),
							"+",
							new SqlValue(1));

						subStrPredicate =
							new SqlPredicate.ExprExpr(
								new SqlFunction(MappingSchema.GetDbDataType(typeof(byte[])), "Convert", SqlDataType.DbVarBinary,
									new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "SUBSTRING",
										predicate.Expr1,
										indexExpression,
										Factory.Length(predicate.Expr2))),
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
					var result = new SqlSearchCondition(predicate.IsNot, canBeUnknown: null, subStrPredicate.MakeNot(predicate.IsNot));

					return result;
				}
			}

			return like;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			var toType = cast.ToType;
			var argument = cast.Expression;

			switch (toType.DataType)
			{
				case DataType.UInt64:
				{
					var argumentType = QueryHelper.GetDbDataType(argument, MappingSchema);

					if (argumentType.SystemType.IsFloatType)
					{
						return PseudoFunctions.MakeCast(new SqlFunction(cast.Type, "Floor", argument), toType);
					}

					break;
				}

				case DataType.Time:
				case DataType.DateTime:
				{
					var type1 = argument.SystemType!.ToUnderlying();

					if (IsTimeDataType(toType))
					{
						if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
							return new SqlExpression(
								cast.Type, "Cast(Convert(NChar, {0}, 114) as DateTime)",
								Precedence.Primary, argument);

						if (argument.SystemType == typeof(string))
							return argument;

						return new SqlExpression(
							cast.Type, "Convert(NChar, {0}, 114)", Precedence.Primary,
							argument);
					}

					if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
					{
						if (IsDateDataType(toType, "Datetime"))
							return new SqlExpression(
								cast.Type, "Cast(Floor(Cast({0} as Float)) as DateTime)",
								Precedence.Primary, argument);
					}

					break;
				}

				case  DataType.Decimal:
				{
					if (cast.ToType.Precision == null && cast.ToType.Scale == null)
					{
						cast = cast.WithToType(cast.ToType.WithPrecisionScale(38, 17));
						return cast;
					}

					break;
				}
			}

			return base.ConvertConversion(cast);
		}
	}

}
