using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Access.Translation
{
	public class AccessLibRedMemberTranslator : AccessMemberTranslator
	{
		protected override IMemberTranslator CreateDateMemberTranslator()
		{
			return new AccessLibRedDateFunctionsTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new AccessLibRedStringMemberTranslator();
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new AccessLibRedMathMemberTranslator();
		}

		// Power rather than ^, which LibRed's convert visitor maps to BXOR
		protected class AccessLibRedMathMemberTranslator : MathMemberTranslator
		{
			protected override ISqlExpression Power(ISqlExpressionFactory factory, DbDataType type, ISqlExpression x, ISqlExpression y)
			{
				return factory.Function(type, "Power", x, y);
			}
		}

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new AccessLibRedWindowFunctionsMemberTranslator();
		}

		protected class AccessLibRedDateFunctionsTranslator : DateFunctionsTranslator
		{
			protected override ISqlExpression? TranslateDateTimeDatePart(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, Sql.DateParts datepart)
			{
				if (datepart != Sql.DateParts.Millisecond)
					return base.TranslateDateTimeDatePart(translationContext, translationFlag, dateTimeExpression, datepart);

				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(typeof(int)), "DatePart", factory.Value("ms"), dateTimeExpression);
			}

			protected override ISqlExpression? TranslateDateTimeDateAdd(ITranslationContext translationContext, TranslationFlags translationFlag, ISqlExpression dateTimeExpression, ISqlExpression increment, Sql.DateParts datepart)
			{
				if (datepart != Sql.DateParts.Millisecond)
					return base.TranslateDateTimeDateAdd(translationContext, translationFlag, dateTimeExpression, increment, datepart);

				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(dateTimeExpression), "DateAdd", factory.Value("ms"), increment, dateTimeExpression);
			}
		}

		protected class AccessLibRedStringMemberTranslator : AccessStringMemberTranslator
		{
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimStart(translationContext, methodCall, translationFlags, value, trimChars);

				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(value), "LTrim", value, trimChars);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimEnd(translationContext, methodCall, translationFlags, value, trimChars);

				var factory = translationContext.ExpressionFactory;

				return factory.Function(factory.GetDbDataType(value), "RTrim", value, trimChars);
			}

			// aggregate form is STRING_AGG, as SqlServer2017StringMemberTranslator; the item form keeps the Access emulation
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

						c.AllowOrderBy()
							.AllowFilter()
							.AllowDistinct()
							.AllowNotNullCheck(true)
							.OnBuildFunction(composer =>
							{
								var info = composer.BuildInfo;
								if (info.Value == null || (!withoutSeparator && info.Argument(0) == null))
									return;

								var factory   = info.Factory;
								var valueType = factory.GetDbDataType(info.Value);

								ISqlExpression separator;
								if (withoutSeparator)
								{
									separator = factory.Value(valueType, string.Empty);
								}
								else
								{
									separator = info.Argument(0)!;
									separator = QueryHelper.MarkAsNonQueryParameters(separator);
								}

								var value = info.Value;
								if (!info.IsNullFiltered && nullValuesAsEmptyString)
									value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

								if (info is { FilterCondition.IsTrue: false })
								{
									value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));

									if (!info.IsGroupBy)
									{
										composer.SetFallback(f => f.AllowFilter(false));
										return;
									}
								}

								var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

								var withinGroup = info.OrderBySql.Length > 0 ? info.OrderBySql.Select(o => new SqlWindowOrderItem(o.expr, o.desc, o.nulls)) : null;

								var fn = factory.Function(valueType, "STRING_AGG",
									[new SqlFunctionArgument(value, modifier : aggregateModifier), new SqlFunctionArgument(separator)],
									[true, true],
									isAggregate : true,
									withinGroup : withinGroup,
									canBeAffectedByOrderBy : false);

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

						return factory.Function(valueType, "Mid",
							valuesExpr,
							factory.Add(intDbType, factory.Length(separator), factory.Value(intDbType, 1)));
					}, withoutSeparator);
				}

				return builder.Build(translationContext, methodCall, isExpression: translationFlags.HasFlag(TranslationFlags.Expression));
			}
		}

		protected class AccessLibRedWindowFunctionsMemberTranslator : WindowFunctionsMemberTranslator
		{
			protected override bool IsVarianceSupported             => true;
			protected override bool IsVarianceBareSupported         => true;
			protected override bool IsCorrelationSupported          => true;
			protected override bool IsLinearRegressionSupported     => true;
			protected override bool IsWindowFilterSupported         => true;
			protected override bool IsOrderedSetFilterSupported     => true;
			protected override bool IsLeadLagNullTreatmentSupported => true;
			protected override bool IsValueNullTreatmentSupported   => true;
			protected override bool IsNthValueFromSupported         => true;
			protected override bool IsAggregateDistinctSupported    => true;

			protected override string VarianceFunctionName => "Var";
		}
	}
}
