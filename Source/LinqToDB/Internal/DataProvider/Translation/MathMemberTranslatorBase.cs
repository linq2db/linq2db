using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class MathMemberTranslatorBase : MemberTranslatorBase
	{
		public MathMemberTranslatorBase()
		{
			RegisterMax();
			RegisterMin();
			RegisterAbs();
			RegisterRound();
			RgisterPow();
		}

		void RegisterMax()
		{
			Registration.RegisterMethod((byte    x, byte    y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((decimal x, decimal y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((double  x, double  y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((short   x, short   y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((int     x, int     y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((long    x, long    y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((sbyte   x, sbyte   y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((float   x, float   y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((ushort  x, ushort  y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((uint    x, uint    y) => Math.Max(x, y), TranslateMaxMethod);
			Registration.RegisterMethod((ulong   x, ulong   y) => Math.Max(x, y), TranslateMaxMethod);
		}

		void RegisterMin()
		{
			Registration.RegisterMethod((byte    x, byte    y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((decimal x, decimal y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((double  x, double  y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((short   x, short   y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((int     x, int     y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((long    x, long    y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((sbyte   x, sbyte   y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((float   x, float   y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((ushort  x, ushort  y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((uint    x, uint    y) => Math.Min(x, y), TranslateMinMethod);
			Registration.RegisterMethod((ulong   x, ulong   y) => Math.Min(x, y), TranslateMinMethod);
		}

		void RegisterAbs()
		{
			Registration.RegisterMethod((short   v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((int     v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((long    v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((sbyte   v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((float   v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((double  v) => Math.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((decimal v) => Math.Abs(v), TranslateAbsMethod);

			Registration.RegisterMethod((decimal? v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((double?  v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((short?   v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((int?     v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((long?    v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((sbyte?   v) => Sql.Abs(v), TranslateAbsMethod);
			Registration.RegisterMethod((float?   v) => Sql.Abs(v), TranslateAbsMethod);
		}

		void RegisterRound()
		{
			Registration.RegisterMethod((double v) => Math.Round(v)                            , TranslateMathRoundMethod);
			Registration.RegisterMethod((double v) => Math.Round(v, 0)                         , TranslateMathRoundMethod);
			Registration.RegisterMethod((double v) => Math.Round(v, MidpointRounding.ToEven)   , TranslateMathRoundMethod);
			Registration.RegisterMethod((double v) => Math.Round(v, 0, MidpointRounding.ToEven), TranslateMathRoundMethod);
			Registration.RegisterMethod((double v) => Sql.RoundToEven(v)                       , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((double v) => Sql.RoundToEven(v, 0)                    , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((double v) => Sql.Round(v)                             , TranslateRoundAwayFromZero);
			Registration.RegisterMethod((double v) => Sql.Round(v, 0)                          , TranslateRoundAwayFromZero);

			Registration.RegisterMethod((float v) => Math.Round(v)                            , TranslateMathRoundMethod);
			Registration.RegisterMethod((float v) => Math.Round(v, 0)                         , TranslateMathRoundMethod);
			Registration.RegisterMethod((float v) => Math.Round(v, MidpointRounding.ToEven)   , TranslateMathRoundMethod);
			Registration.RegisterMethod((float v) => Math.Round(v, 0, MidpointRounding.ToEven), TranslateMathRoundMethod);
			Registration.RegisterMethod((float v) => Sql.RoundToEven(v)                       , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((float v) => Sql.RoundToEven(v, 0)                    , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((float v) => Sql.Round(v)                             , TranslateRoundAwayFromZero);
			Registration.RegisterMethod((float v) => Sql.Round(v, 0)                          , TranslateRoundAwayFromZero);

			Registration.RegisterMethod((decimal v) => Math.Round(v)                            , TranslateMathRoundMethod);
			Registration.RegisterMethod((decimal v) => Math.Round(v, 0)                         , TranslateMathRoundMethod);
			Registration.RegisterMethod((decimal v) => Math.Round(v, MidpointRounding.ToEven)   , TranslateMathRoundMethod);
			Registration.RegisterMethod((decimal v) => Math.Round(v, 0, MidpointRounding.ToEven), TranslateMathRoundMethod);
			Registration.RegisterMethod((decimal v) => Sql.RoundToEven(v)                       , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((decimal v) => Sql.RoundToEven(v, 0)                    , TranslateRoundToEvenMethod);
			Registration.RegisterMethod((decimal v) => Sql.Round(v)                             , TranslateRoundAwayFromZero);
			Registration.RegisterMethod((decimal v) => Sql.Round(v, 0)                          , TranslateRoundAwayFromZero);
		}

		void RgisterPow()
		{
			Registration.RegisterMethod((double  x, double  y) => Math.Pow(x, y), TranslatePow);
			Registration.RegisterMethod((double  x, double  y) => Sql.Power(x, y), TranslatePow);
			Registration.RegisterMethod((decimal x, decimal y) => Sql.Power(x, y), TranslatePow);
		}

		Expression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedX))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedY))
				return null;

			var translated = TranslateMaxMethod(translationContext, methodCall, translatedX, translatedY);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedX))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedY))
				return null;

			var translated = TranslateMinMethod(translationContext, methodCall, translatedX, translatedY);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateAbsMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			var translated = TranslateAbsMethod(translationContext, methodCall, translatedValue);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		protected virtual ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
		{
			var factory = translationContext.ExpressionFactory;

			var xType        = factory.GetDbDataType(xValue);
			var yType        = factory.GetDbDataType(yValue);
			var yValueResult = yValue;

			if (!xType.EqualsDbOnly(yType))
			{
				yValueResult = factory.Cast(yValue, xType);
			}

			var result = factory.Condition(factory.GreaterOrEqual(xValue, yValue), xValue, yValueResult);
			return result;
		}

		protected virtual ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
		{
			var factory = translationContext.ExpressionFactory;

			var xType        = factory.GetDbDataType(xValue);
			var yType        = factory.GetDbDataType(yValue);
			var yValueResult = yValue;

			if (!xType.EqualsDbOnly(yType))
			{
				yValueResult = factory.Cast(yValue, xType);
			}

			var result = factory.Condition(factory.LessOrEqual(xValue, yValue), xValue, yValueResult);
			return result;
		}

		protected virtual ISqlExpression? TranslateAbsMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType = factory.GetDbDataType(value);

			var result = factory.Function(valueType, "Abs", value);
			return result;
		}

		Expression? TranslateMathRoundMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if ((translationFlags & TranslationFlags.Expression) != 0 && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			var routing = MidpointRounding.ToEven;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			ISqlExpression? precision = null;

			if (methodCall.Arguments.Count > 1)
			{
				if (methodCall.Arguments[1].Type == typeof(MidpointRounding))
				{
					if (!translationContext.TryEvaluate(methodCall.Arguments[1], out routing))
						return null;
					translationContext.MarkAsNonParameter(methodCall.Arguments[1], routing);
				}
				else if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out precision))
				{
					return null;
				}
				else if (methodCall.Arguments.Count > 2)
				{
					if (!translationContext.TryEvaluate(methodCall.Arguments[2], out routing))
						return null;
					translationContext.MarkAsNonParameter(methodCall.Arguments[2], routing);
				}
			}

			ISqlExpression? translated = null;

			switch (routing)
			{
				case MidpointRounding.ToEven:
					translated = TranslateRoundToEven(translationContext, methodCall, translatedValue, precision);
					break;
				case MidpointRounding.AwayFromZero:
					translated = TranslateRoundAwayFromZero(translationContext, methodCall, translatedValue, precision);
					break;
			}

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateRoundToEvenMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if ((translationFlags & TranslationFlags.Expression) != 0 && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			ISqlExpression? precision = null;

			if (methodCall.Arguments.Count > 1)
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out precision))
				{
					return null;
				}
			}

			if (precision is SqlValue sqlValue && sqlValue.Value is null or 0) 
				precision = null;

			var translated = TranslateRoundToEven(translationContext, methodCall, translatedValue, precision);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if ((translationFlags & TranslationFlags.Expression) != 0 && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			ISqlExpression? precision = null;

			if (methodCall.Arguments.Count > 1)
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out precision))
				{
					return null;
				}
			}

			if (precision is SqlValue sqlValue && sqlValue.Value is null or 0)
				precision = null;

			var translated = TranslateRoundAwayFromZero(translationContext, methodCall, translatedValue, precision);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslatePow(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if ((translationFlags & TranslationFlags.Expression) != 0 && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedX))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedY))
				return null;

			var translated = TranslatePow(translationContext, methodCall, translatedX, translatedY);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		protected virtual ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType = factory.GetDbDataType(value);

			ISqlExpression? result = null;

			if (precision is null or SqlValue { Value: 0 })
			{
				/*
					CASE
						WHEN ABS(value - FLOOR(value)) = 0.5
							AND FLOOR(value) % 2 != 0
						THEN FLOOR(value)
						ELSE ROUND(value, 0)
					END
				 */

				var valueFloor = factory.Function(valueType, "FLOOR", value);
				var valueSub   = factory.Sub(valueType, value, valueFloor);

				var condition = factory.SearchCondition(false)
					.Add(factory.Equal(valueSub, factory.Value(valueType, 0.5)))
					.Add(factory.Equal(factory.Mod(valueFloor, factory.Value(2)), factory.Value(0)));

				var trueValue = factory.Function(valueType, "FLOOR", value);

				var falseValue = factory.Function(valueType, "ROUND", ParametersNullabilityType.SameAsFirstParameter, value, factory.Value(0));

				result = factory.Condition(condition, trueValue, falseValue);
			}
			else
			{
				/*
				
				    CASE
				        WHEN value * 2 = ROUND(value * 2, precision) 
				             AND value <> ROUND(value, precision) 
				        THEN ROUND(value / 2, precision) * 2
				        ELSE ROUND(value, precision)
					END 
				 */

				var value2 = factory.Multiply(valueType, value, factory.Value(2));
				var roundValue2 = factory.Function(valueType, "ROUND", value2, precision);
				var roundValue3 = factory.Function(valueType, "ROUND", value, precision);

				var condition = factory.SearchCondition(false)
					.Add(factory.Equal(value2, roundValue2))
					.Add(factory.NotEqual(value, roundValue3));

				var trueValue = factory.Multiply(valueType, factory.Function(valueType, "ROUND", factory.Div(valueType, value, factory.Value(2)), precision), factory.Value(2));

				var falseValue = factory.Function(valueType, "ROUND", value, precision);

				result = factory.Condition(condition, trueValue, falseValue);
			}

			return result;
		}

		protected virtual ISqlExpression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType  = factory.GetDbDataType(value);
			var result = precision != null
				? factory.Function(valueType, "ROUND", value, precision)
				: factory.Function(valueType, "ROUND", value);

			return result;
		}

		protected virtual ISqlExpression? TranslatePow(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
		{
			var factory = translationContext.ExpressionFactory;

			var xType      = factory.GetDbDataType(xValue);
			var resultType = xType;

			if (xType.SystemType == typeof(decimal))
			{
				xType  = factory.GetDbDataType(typeof(double));
				xValue = factory.Cast(xValue, xType);
			}

			var yType        = factory.GetDbDataType(yValue);
			var yValueResult = yValue;

			if (!xType.EqualsDbOnly(yType))
			{
				yValueResult = factory.Cast(yValue, xType);
			}

			var result = factory.Function(xType, "Power", xValue, yValueResult);
			if (!resultType.EqualsDbOnly(xType))
			{
				result = factory.Cast(result, resultType);
			}

			return result;
		}

	}
}
