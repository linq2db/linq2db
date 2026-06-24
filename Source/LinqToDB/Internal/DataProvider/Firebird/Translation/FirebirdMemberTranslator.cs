using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class FirebirdMemberTranslator : ProviderMemberTranslatorDefault
	{
		protected override IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslation();
		}

		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new FirebirdDateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new FirebirdStringMemberTranslator();
		}

		protected override IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslator();
		}

		protected class SqlTypesTranslation : SqlTypesTranslationDefault
		{
			protected override Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(18, 10));

			protected override Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Decimal).WithPrecisionScale(10, 4));

			protected override Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
				=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Timestamp));

			protected override Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
					return null;

				return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.VarChar));
			}
		}

		protected class FirebirdDateFunctionsTranslator : DateFunctionsTranslatorBase
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				var factory          = translationContext.ExpressionFactory;
				var intDataType      = factory.GetDbDataType(typeof(int));
				var shortIntDataType = factory.GetDbDataType(typeof(short));

				string partStr;

				switch (datepart)
				{
					case Sql.DateParts.Year: partStr = "year"; break;
					case Sql.DateParts.Quarter:
					{
						var result = factory.Function(shortIntDataType, "Extract", factory.Expression(shortIntDataType, "Month from {0}", dateTimeExpression));

						result = factory.Increment(factory.Div(shortIntDataType, factory.Decrement(result), 3));
						return result;
					}
					case Sql.DateParts.Month:       partStr = "month"; break;
					case Sql.DateParts.DayOfYear:   partStr = "yearday"; break;
					case Sql.DateParts.Day:         partStr = "day"; break;
					case Sql.DateParts.Week:        partStr = "week"; break;
					case Sql.DateParts.WeekDay:     partStr = "weekday"; break;
					case Sql.DateParts.Hour:        partStr = "hour"; break;
					case Sql.DateParts.Minute:      partStr = "minute"; break;
					case Sql.DateParts.Second:      partStr = "second"; break;
					case Sql.DateParts.Millisecond: partStr = "millisecond"; break;
					default:
						return null;
				}

				// Cast(Floor(Extract({part} from {date})) as int)

				var extractDbType = shortIntDataType;

				switch (datepart)
				{
					case Sql.DateParts.Second:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 4);
						break;
					case Sql.DateParts.Millisecond:
						extractDbType = factory.GetDbDataType(typeof(decimal)).WithPrecisionScale(9, 1);
						break;
				}

				var resultExpression =
					factory.Function(extractDbType, "Extract", factory.Expression(shortIntDataType, partStr + " from {0}", dateTimeExpression));

				switch (datepart)
				{
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					{
						resultExpression = factory.Increment(resultExpression);
						break;
					}
					case Sql.DateParts.Second:
					case Sql.DateParts.Millisecond:
					{
						resultExpression = factory.Cast(factory.Function(factory.GetDbDataType(typeof(long)), "Floor", resultExpression), intDataType);
						break;
					}
				}

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeOffsetDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				return TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment,
				Sql.DateParts                                                       datepart)
			{
				var factory = translationContext.ExpressionFactory;

				var number = increment;
				switch (datepart)
				{
					case Sql.DateParts.Quarter:
					{
						datepart = Sql.DateParts.Month;
						number   = factory.Multiply(number, 3);
						break;
					}
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
						return null;
					case Sql.DateParts.Week:
					{
						datepart = Sql.DateParts.Day;
						number   = factory.Multiply(number, 7);
						break;
					}
				}

				// Firebird does not support dynamic increment in DateAdd function
				number = QueryHelper.MarkAsNonQueryParameters(number);

				var partExpression   = factory.NotNullExpression(factory.GetDbDataType(typeof(string)), datepart.ToString());
				var resultExpression = factory.Function(factory.GetDbDataType(dateTimeExpression), "DateAdd", partExpression, number, dateTimeExpression);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateMakeDateTime(
				ITranslationContext translationContext,
				DbDataType          resulType,
				ISqlExpression      year,
				ISqlExpression      month,
				ISqlExpression      day,
				ISqlExpression?     hour,
				ISqlExpression?     minute,
				ISqlExpression?     second,
				ISqlExpression?     millisecond)
			{
				var factory        = translationContext.ExpressionFactory;
				var stringDataType = factory.GetDbDataType(typeof(string)).WithDataType(DataType.VarChar);
				var intDataType    = factory.GetDbDataType(typeof(int));

				ISqlExpression CastToLength(ISqlExpression expression, int stringLength)
				{
					return factory.Cast(expression, stringDataType.WithLength(stringLength));
				}

				ISqlExpression PartExpression(ISqlExpression expression, int padSize)
				{
					if (translationContext.TryEvaluate(expression, out var expressionValue) && expressionValue is int intValue)
					{
						return factory.Value(stringDataType, intValue.ToString(CultureInfo.InvariantCulture).PadLeft(padSize, '0'));
					}

					return factory.Function(stringDataType, "LPad",
						ParametersNullabilityType.SameAsFirstParameter,
						CastToLength(expression, padSize),
						factory.Value(intDataType, padSize),
						factory.Value(stringDataType, "0"));
				}

				var yearString  = PartExpression(year, 4);
				var monthString = PartExpression(month, 2);
				var dayString   = PartExpression(day, 2);

				var resultExpression = factory.Concat(
					yearString, factory.Value(stringDataType, "-"),
					monthString, factory.Value(stringDataType, "-"), dayString);

				if (hour != null || minute != null || second != null || millisecond != null)
				{
					hour        ??= factory.Value(intDataType, 0);
					minute      ??= factory.Value(intDataType, 0);
					second      ??= factory.Value(intDataType, 0);
					millisecond ??= factory.Value(intDataType, 0);

					resultExpression = factory.Concat(
						resultExpression,
						factory.Value(stringDataType, " "),
						PartExpression(hour, 2), factory.Value(stringDataType, ":"),
						PartExpression(minute, 2), factory.Value(stringDataType, ":"),
						PartExpression(second, 2), factory.Value(stringDataType, "."),
						PartExpression(millisecond, 3)
					);
				}

				resultExpression = factory.Cast(resultExpression, resulType);

				return resultExpression;
			}

			protected override ISqlExpression? TranslateDateTimeTruncationToDate(ITranslationContext translationContext, ISqlExpression dateExpression, TranslationFlags translationFlags)
			{
				var factory    = translationContext.ExpressionFactory;
				var sourceType = factory.GetDbDataType(dateExpression);
				var cast       = factory.Cast(dateExpression, new DbDataType(sourceType.SystemType, DataType.Date), true);

				return cast;
			}

			protected override ISqlExpression? TranslateNow(ITranslationContext translationContext, TranslationFlags translationFlags)
			{
				return null;
			}
		}

		protected class FirebirdStringMemberTranslator : StringMemberTranslatorBase
		{
			protected virtual bool IsWithinGroupSupported => false;
			protected virtual bool IsDistinctSupported    => false;

			// Firebird's TRIM(LEADING/TRAILING <chars> FROM <value>) treats <chars> as a
			// literal substring, not a set — does not match .NET's set semantics. Firebird
			// has no native regex replace either, so fall back to client-side eval when
			// chars are supplied.
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars != null)
					return null;

				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return factory.Expression(valueType, "TRIM(LEADING FROM {0})", value);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars != null)
					return null;

				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return factory.Expression(valueType, "TRIM(TRAILING FROM {0})", value);
			}

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString, bool isNullableResult, bool withoutSeparator)
			{
				var builder = new AggregateFunctionBuilder()
					.ConfigureAggregate(c =>
					{
						c.TransformValue(ConvertOperandToString);

						if (withoutSeparator)
							c.HasSequenceIndex(0);
						else
							c.HasSequenceIndex(1).TranslateArguments(0);

						c.AllowOrderBy(IsWithinGroupSupported)
							.AllowDistinct(IsDistinctSupported)
							.AllowFilter()
							.AllowNotNullCheck(true)
							.OnBuildFunction(composer =>
							{
								var info = composer.BuildInfo;
								if (info.Value == null || (!withoutSeparator && info.Argument(0) == null))
								{
									return;
								}

								var factory   = info.Factory;
								var valueType = factory.GetDbDataType(info.Value);
								var separator = withoutSeparator
									? factory.Value(valueType, string.Empty)
									: info.Argument(0)!;

								var value = info.Value;
								if (!info.IsNullFiltered && nullValuesAsEmptyString)
									value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

								if (info is { FilterCondition.IsTrue: false })
								{
									if (!info.IsGroupBy)
									{
										composer.SetFallback(f => f.AllowFilter(false));
										return;
									}

									value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
								}

								var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

								var withinGroup = info.OrderBySql.Length > 0 ? info.OrderBySql.Select(o => new SqlWindowOrderItem(o.expr, o.desc, o.nulls)) : null;

								var fn = factory.Function(valueType, "LIST",
									[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator)],
									[true, true],
									isAggregate : true,
									withinGroup : withinGroup,
									canBeAffectedByOrderBy : true);

								SetStringJoinResult(composer, fn, isNullableResult, valueType);
							});
					});

				if (withoutSeparator)
				{
					ConfigureConcat(builder, wrapByCoalesce: true);
				}
				else
				{
					ConfigureConcatWsEmulation(builder, nullValuesAsEmptyString, isNullableResult, (factory, valueType, separator, valuesExpr) =>
					{
						var intDbType = factory.GetDbDataType(typeof(int));
						var substring = factory.Function(valueType, "SUBSTRING",
							[new SqlFunctionArgument(valuesExpr, suffix: factory.Fragment("FROM {0}", factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1))))],
							[true]
						);

						return substring;
					}, withoutSeparator);
				}

				return builder.Build(translationContext, methodCall, isExpression: translationFlags.HasFlag(TranslationFlags.Expression));
			}

			// {value} IS NULL OR {value} NOT SIMILAR TO '%[^WHITESPACES]%'
			public override ISqlExpression? TranslateIsNullOrWhiteSpace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value)
			{
				var factory   = translationContext.ExpressionFactory;
				var pattern   = factory.Value(factory.GetDbDataType(typeof(string)), $"%[^{WHITESPACES}]%");
				var predicate = factory.LikePredicate(value, isNot: true, pattern, escape: null, functionName: "SIMILAR TO");

				return WrapIsNullOrWhiteSpaceResult(translationContext, value, predicate);
			}
		}

		protected override ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory  = translationContext.ExpressionFactory;
			var timePart = factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Gen_Uuid");

			return timePart;
		}

		/// <summary>
		/// Builds the Firebird <c>Cast(Lower(UUID_TO_CHAR({0})) as VarChar(36))</c> shape for a Guid → string conversion.
		/// Shared by the <c>Guid.ToString()</c> member translator and the <c>cast(guid as string)</c> path in
		/// <see cref="FirebirdSqlExpressionConvertVisitor"/> so both emit identical SQL.
		/// </summary>
		/// <remarks>
		/// Firebird's native <c>UUID_TO_CHAR</c> returns <c>CHAR(36)</c> — a fixed-width type that pads shorter
		/// values with trailing spaces. The inner function is typed as <c>CHAR(36)</c> to match that database
		/// semantic; the outer CAST then forces the result to <c>VARCHAR(36)</c> so a composing
		/// <c>COALESCE(..., '')</c> doesn't promote the whole expression to <c>CHAR(36)</c> and pad the
		/// empty-string branch to 36 spaces. Without the explicit CHAR typing on the inner function the optimizer
		/// (see <c>SqlExpressionOptimizerVisitor.VisitSqlCastExpression</c>) would treat the CAST as a no-op and
		/// elide it. Matches PostgreSQL / Sybase / SqlServer / MySql / SqlCe / SapHana Guid translators which all
		/// converge on a <c>VARCHAR(36)</c> result.
		/// </remarks>
		public static ISqlExpression TranslateGuidToString(ISqlExpression guidExpr, ISqlExpressionFactory factory)
		{
			var stringDataType = factory.GetDbDataType(typeof(string));
			var charType       = stringDataType.WithDataType(DataType.Char).WithLength(36);
			var varCharType    = stringDataType.WithDataType(DataType.VarChar).WithLength(36);

			var toChar  = factory.Function(charType, "UUID_TO_CHAR", guidExpr);
			var toLower = factory.ToLower(toChar);
			var cast    = factory.Cast(toLower, varCharType);

			return cast;
		}

		protected class GuidMemberTranslator : GuidMemberTranslatorBase
		{
			protected override ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
			{
				return TranslateGuidToString(guidExpr, translationContext.ExpressionFactory);
			}
		}

		protected class Firebird25WindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsWindowFunctionsSupported => false;
		}

		protected class FirebirdWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsPercentRankSupported    => false;
			protected override bool IsCumeDistSupported       => false;
			protected override bool IsNTileSupported          => false;
			// NTH_VALUE and NTH_VALUE FROM FIRST/LAST are supported from Firebird 3.
			// IGNORE/RESPECT NULLS is NOT supported (Firebird rejects the IGNORE token).
			protected override bool IsNthValueSupported       => true;
			protected override bool IsNthValueFromSupported   => true;
			protected override bool IsFrameRowsSupported      => false;
			protected override bool IsFrameRangeSupported     => false;
			protected override bool IsFrameGroupsSupported    => false;
			protected override bool IsFrameExclusionSupported => false;
			protected override bool IsPercentileContSupported => false;
			protected override bool IsPercentileDiscSupported => false;
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new FirebirdWindowFunctionsMemberTranslator();
		}

	}
}
