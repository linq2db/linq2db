using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2017MemberTranslator : SqlServer2012MemberTranslator
	{
		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new SqlServer2017StringMemberTranslator();
		}

		protected class SqlServer2017StringMemberTranslator : SqlServerStringMemberTranslator
		{
			protected virtual bool IsDistinctSupportedInStringAgg => false;

			public override ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
			{
				/*
				 * REPLICATE(paddingSymbol, padding - LEN(value)) + value
				 */
				var factory         = translationContext.ExpressionFactory;
				var valueTypeString = factory.GetDbDataType(value);
				var valueTypeInt    = factory.GetDbDataType(typeof(int));

				var lengthValue = TranslateLength(translationContext, translationFlags, value);
				if (lengthValue == null)
					return null;

				var symbolsToAdd = factory.Sub(valueTypeInt, padding, lengthValue);
				var stringToAdd  = factory.Function(valueTypeString, "REPLICATE", paddingChar, symbolsToAdd);

				return factory.Add(valueTypeString, stringToAdd, value);
			}

			protected override Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool ignoreNulls)
			{
				var builder = new AggregateFunctionBuilder()
				.ConfigureAggregate(c => c
					.AllowOrderBy()
					.AllowFilter()
					.AllowDistinct(IsDistinctSupportedInStringAgg)
					.AllowNotNullCheck(true)
					.TranslateArguments(0)
					.OnBuildFunction(composer =>
					{
						var info = composer.BuildInfo;
						if (info.Value == null || info.Argument(0) == null)
						{
							return;
						}

						var factory   = info.Factory;
						var separator = info.Argument(0)!;
						var valueType = factory.GetDbDataType(info.Value);

						var value     = info.Value;
						if (!info.IsNullFiltered)
							value = factory.Coalesce(value, factory.Value(valueType, string.Empty));

						if (info.FilterCondition != null && !info.FilterCondition.IsTrue())
						{
							value = factory.Condition(info.FilterCondition, value, factory.Null(valueType));
						}

						var aggregateModifier = info.IsDistinct ? Sql.AggregateModifier.Distinct : Sql.AggregateModifier.None;

						var withinGroup = info.OrderBySql.Length > 0 ? info.OrderBySql.Select(o => new SqlWindowOrderItem(o.expr, o.desc, o.nulls)) : null;

						var fn = factory.Function(valueType, "STRING_AGG",
							[new SqlFunctionArgument(value, modifier: aggregateModifier), new SqlFunctionArgument(separator)],
							[true, true],
							isAggregate: true, 
							withinGroup: withinGroup,
							canBeAffectedByOrderBy: true);

						composer.SetResult(factory.Coalesce(fn, factory.Value(valueType, string.Empty)));
					}))
				.ConfigurePlain(c => c
					.TranslateArguments(0)
					.AllowFilter()
					.AllowNotNullCheck(true)
					.OnBuildFunction(composer =>
					{
						var info = composer.BuildInfo;
						if (info.Values.Length == 0 || info.Argument(0) == null)
						{
							composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
							return;
						}

						var factory   = info.Factory;
						var separator = info.Argument(0)!;
						var dataType  = factory.GetDbDataType(info.Values[0]);

						if (!composer.GetFilteredToNullValues(out IEnumerable<ISqlExpression>? values, out var error))
						{
							composer.SetError(error);
							return;
						}

						var items = info.IsNullFiltered
							? values
							: values.Select(i => factory.Coalesce(i, factory.Value(factory.GetDbDataType(i), ""))).ToArray();

						var function  = factory.Function(dataType, "CONCAT_WS",
							parametersNullability: ParametersNullabilityType.IfAllParametersNullable,
							[separator, ..items]);

						composer.SetResult(function);
					}));

				return builder.Build(translationContext, methodCall.Arguments[1], methodCall);
			}
		}
	}
}
