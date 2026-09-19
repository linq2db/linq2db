using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Sybase
{
	public class SybaseSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public SybaseSqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		// could be enabled if we add SP03 version support (also IsDistinctFromSupported should be enabled)
		//protected override bool SupportsDistinctAsExistsIntersect => true;

		/// <inheritdoc />
		public override bool CanLowerIntervalDifference => true;

		/// <summary>
		/// <c>DATEDIFF</c> counts milliseconds, finer than the three-and-a-third that an ASE <c>datetime</c>
		/// stores, so the remainder that completes an elapsed count is exact.
		/// </summary>
		protected override SqlIntervalUnit? FinestDateUnit => SqlIntervalUnit.Millisecond;

		/// <summary>
		/// The measurement stops there as well, so a component asked for below the millisecond is identically zero
		/// rather than merely imprecise - the count it is taken from was rounded to a coarser unit first.
		/// </summary>
		/// <remarks>
		/// Declared for the reason SQLite declares the same limit: declining while the expression is still being
		/// built leaves the member to .NET, which holds both dates and answers exactly. The three-and-a-third
		/// millisecond step above is what makes a stored difference carry a sub-millisecond part at all, so
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
		/// Boundary counting through <c>DATEDIFF</c>, which the tick decomposition turns into elapsed time.
		/// </summary>
		/// <remarks>
		/// The count is a 32-bit value, so asking it for milliseconds across more than about twenty-four days
		/// overflows. Nothing does: the whole part is counted in days and the millisecond count only ever spans the
		/// remainder of one, which is 86,400,000 at most.
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

		private static readonly string[] SybaseCharactersToEscape = {"_", "%", "[", "]", "^"};

		public override string[] LikeCharactersToEscape => SybaseCharactersToEscape;

		#endregion

		protected internal override IQueryElement VisitExistsPredicate(SqlPredicate.Exists predicate)
		{
			var result = base.VisitExistsPredicate(predicate);

			if (result is SqlPredicate.Exists { SubQuery: { HasSetOperators: true } selectQuery } exists)
			{
				var query = new SelectQuery() {DoNotRemove = true};
				query.From.Table(selectQuery);

				result = new SqlPredicate.Exists(exists.IsNot, query);
			}

			return result;
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func switch
			{
				{ Name: PseudoFunctions.REPLACE } => func.WithName("Str_Replace"),
				{ Name: PseudoFunctions.LENGTH } => func.WithName("CHAR_LENGTH"),

				{
					Name: "CharIndex",
					Parameters: [var p0, var p1, var p2],
					Type: var type,
				} => Add<int>(
						new SqlFunction(
							type,
							"CharIndex",
							p0,
							new SqlFunction(
								MappingSchema.GetDbDataType(typeof(string)),
								"Substring",
								p1,
								p2,
								new SqlFunction(MappingSchema.GetDbDataType(typeof(int)),
								"Len",
								p1)
							)
						),
						Sub(p2, 1)
					),

				{
					Name: "Stuff",
					Parameters:
					[
						var p0, var p1, _,
						SqlValue { Value: string @string, ValueType: var valueType }
					],
					Type: var type,
				} when string.IsNullOrEmpty(@string) =>
					new SqlFunction(
						type,
						"Stuff",
						ParametersNullabilityType.SameAsFirstParameter,
						p0,
						p1,
						p1,
						new SqlValue(valueType, null)
					),

				_ => base.ConvertSqlFunction(func),
			};
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
			else if (expr is SqlParameter param)
			{
				var paramType = param.Type.SystemType.UnwrapNullableType();

				var wrap = paramType == typeof(uint)
						|| paramType == typeof(long)
						|| paramType == typeof(ulong)
						|| paramType == typeof(float)
						|| paramType == typeof(double)
						|| paramType == typeof(decimal);

				if (wrap && param.IsQueryParameter)
				{
					if (!EvaluationContext.IsParametersInitialized)
					{
						wrap = false;
					}
					else
					{
						var paramValue = param.GetParameterValue(EvaluationContext.ParameterValues);
						wrap = paramValue.ProviderValue == null;
					}
				}

				if (wrap)
					expr = new SqlCastExpression(expr, param.Type, null, isMandatory: true);
			}

			return base.WrapColumnExpression(expr);
		}
	}
}
