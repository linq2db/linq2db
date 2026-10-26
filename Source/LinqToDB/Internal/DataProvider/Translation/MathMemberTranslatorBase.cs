using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
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
			RegisterTruncate();
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

		void RegisterTruncate()
		{
			Registration.RegisterMethod((decimal? v) => Sql.Truncate(v), TranslateTruncateMethod);
			Registration.RegisterMethod((double?  v) => Sql.Truncate(v), TranslateTruncateMethod);
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
			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			var routing = MidpointRounding.ToEven;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

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
			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

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

			var translated = TranslateSqlRoundToEven(translationContext, methodCall, translatedValue, precision);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && methodCall.Arguments.Skip(1).All(translationContext.CanBeEvaluatedOnClient))
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

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

			var translated = TranslateSqlRoundAwayFromZero(translationContext, methodCall, translatedValue, precision);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslatePow(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedX))
				return null;

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedY))
				return null;

			var translated = TranslatePow(translationContext, methodCall, translatedX, translatedY);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		Expression? TranslateTruncateMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			// Unconditional, unlike the Round handlers: Sql.Truncate's attributes did not prefer the server, so a call the client
			// can evaluate stayed on the client in every translation mode.
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedValue))
				return null;

			var translated = TranslateTruncate(translationContext, methodCall, translatedValue);

			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		/// <summary>
		/// Translates <c>Sql.RoundToEven</c>, which carries database rounding semantics. <c>Math.Round</c> goes through
		/// <see cref="TranslateRoundToEven"/> directly, so a provider overrides this one to change only the <c>Sql</c> form.
		/// </summary>
		protected virtual ISqlExpression? TranslateSqlRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
		{
			return TranslateRoundToEven(translationContext, methodCall, value, precision);
		}

		/// <summary>
		/// Translates <c>Sql.Round</c>, which carries database rounding semantics. <c>Math.Round</c> goes through
		/// <see cref="TranslateRoundAwayFromZero(ITranslationContext, MethodCallExpression, ISqlExpression, ISqlExpression?)"/>
		/// directly, so a provider overrides this one to change only the <c>Sql</c> form.
		/// </summary>
		protected virtual ISqlExpression? TranslateSqlRoundAwayFromZero(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
		{
			return TranslateRoundAwayFromZero(translationContext, methodCall, value, precision);
		}

		/// <summary>
		/// Translates <c>Sql.Truncate</c>.
		/// </summary>
		protected virtual ISqlExpression? TranslateTruncate(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value)
		{
			var factory = translationContext.ExpressionFactory;

			return factory.Function(factory.GetDbDataType(methodCall.Type), "Truncate", value);
		}

		/// <summary>
		/// Rounds half away from zero on the magnitude <c>m = abs(v) * power(10, p)</c>: an exact midpoint becomes
		/// <c>floor(m) + 1</c>, anything else the engine's own <c>round(m, 0)</c>. The result is
		/// <c>sign(v) * rounded / power(10, p)</c>. This is for an engine whose <c>ROUND</c> rounds a midpoint some other way.
		/// <paramref name="ten"/> must be typed so that <c>power</c> returns a floating-point value: an integer power
		/// overflows at ten digits and is zero for a negative precision.
		/// </summary>
		protected static ISqlExpression RoundAwayFromZeroByMagnitude(ISqlExpressionFactory factory, ISqlExpression value, ISqlExpression? precision, ISqlExpression ten)
		{
			var valueType = factory.GetDbDataType(value);

			var power     = precision == null ? null : factory.Function(valueType, "power", ten, precision);
			var magnitude = factory.Function(valueType, "abs", value);
			var scaled    = power == null ? magnitude : factory.Multiply(valueType, magnitude, power);
			var floor     = factory.Function(valueType, "floor", scaled);
			// floor(m + 0.5) is wrong for doubles: the addition itself rounds (0.49999999999999994 + 0.5 is 1.0, and above
			// 2^52 the sum lands on an even neighbour). The fraction m - floor(m) is exact, so only the midpoint test uses it.
			var rounded   = factory.Condition(
				factory.Equal(factory.Sub(valueType, scaled, floor), factory.Value(valueType, 0.5)),
				factory.Add(valueType, floor, factory.Value(valueType, 1.0)),
				factory.Function(valueType, "round", scaled, factory.Value(0)));
			var unscaled  = power == null ? rounded : factory.Div(valueType, rounded, power);

			return factory.Multiply(valueType, factory.Function(valueType, "sign", value), unscaled);
		}

		/// <summary>
		/// Applies <paramref name="round"/> to <paramref name="value"/> cast to decimal and casts the result back to the
		/// value's type, for an engine whose decimal rounding gives the <c>Sql</c> semantics its floating-point rounding lacks.
		/// </summary>
		protected static ISqlExpression? RoundThroughDecimal(ITranslationContext translationContext, ISqlExpression value, Func<ISqlExpression, ISqlExpression?> round)
		{
			var factory   = translationContext.ExpressionFactory;
			var valueType = factory.GetDbDataType(value);

			var result = round(factory.Cast(value, factory.GetDbDataType(typeof(decimal))));

			return result == null ? null : factory.Cast(result, valueType);
		}

		/// <summary>
		/// Translates <c>Sql.Truncate(v)</c> as <c>v &gt;= 0 ? Sql.Floor(v) : Sql.Ceiling(v)</c>, for a provider without a
		/// truncating function.
		/// </summary>
		protected static ISqlExpression? TranslateTruncateAsFloorOrCeiling(ITranslationContext translationContext, MethodCallExpression methodCall)
		{
			var value     = methodCall.Arguments[0];
			var isDecimal = value.Type == typeof(decimal?);

			var zero    = isDecimal ? Expression.Constant(0m, typeof(decimal?)) : Expression.Constant(0.0, typeof(double?));
			var floor   = isDecimal ? MemberHelper.MethodOf(() => Sql.Floor  ((decimal?)null)) : MemberHelper.MethodOf(() => Sql.Floor  ((double?)null));
			var ceiling = isDecimal ? MemberHelper.MethodOf(() => Sql.Ceiling((decimal?)null)) : MemberHelper.MethodOf(() => Sql.Ceiling((double?)null));

			var emulation = Expression.Condition(
				Expression.GreaterThanOrEqual(value, zero),
				Expression.Call(floor,   value),
				Expression.Call(ceiling, value));

			return translationContext.TranslateToSqlExpression(emulation, out var translated) ? translated : null;
		}

		protected virtual ISqlExpression? TranslateRoundToEven(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression value, ISqlExpression? precision)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType = factory.GetDbDataType(value);

			value = UncutDecimalLiteral(factory, value);

			ISqlExpression? result = null;

			if (precision is null or SqlValue { Value: 0 })
			{
				/*
					CASE
						WHEN value - FLOOR(value) = 0.5
						THEN ROUND(value * 0.5, 0) * 2
						ELSE ROUND(value, 0)
					END

					Half a midpoint is never itself a midpoint, so rounding it lands on the even neighbour whichever way the
					engine breaks ties, and for either sign. A multiplication keeps a decimal exact where a division may not
					(DuckDB divides a DECIMAL into a DOUBLE).
				 */

				var valueFloor = factory.Function(valueType, "FLOOR", value);
				var valueSub   = factory.Sub(valueType, value, valueFloor);

				var isMidpoint = factory.Equal(valueSub, factory.Value(valueType, 0.5));

				var evenNeighbour = factory.Multiply(
					valueType,
					factory.Function(valueType, "ROUND", ParametersNullabilityType.SameAsFirstParameter, factory.Multiply(valueType, value, factory.Value(valueType, 0.5)), factory.Value(0)),
					factory.Value(2));

				var falseValue = factory.Function(valueType, "ROUND", ParametersNullabilityType.SameAsFirstParameter, value, factory.Value(0));

				result = factory.Condition(isMidpoint, evenNeighbour, falseValue);
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

			value = UncutDecimalLiteral(factory, value);

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

			xValue = UncutDecimalLiteral(factory, xValue);

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

		// A decimal literal combined with a column takes the column's type, so the providers that render a literal's scale
		// (ClickHouse, YDB) cut it. The literal keeps its own scale here; the call keeps its declared type.
		internal static ISqlExpression UncutDecimalLiteral(ISqlExpressionFactory factory, ISqlExpression value)
		{
			// a decimal carries at most 28 places, so a larger scale cannot cut it
			if (value is SqlValue { Value: decimal literal } sqlValue
				&& sqlValue.ValueType.Scale is int scale and < 28)
			{
				var literalScale  = (decimal.GetBits(literal)[3] >> 16) & 0xFF;
				var integerDigits = Math.Abs(literal) < 1 ? 0 : decimal.Truncate(Math.Abs(literal)).ToString(CultureInfo.InvariantCulture).Length;

				// ClickHouse's Decimal32 / Decimal64 hold 9 / 18 digits in total: a literal with more scale than its column takes
				// integer digits from the column in the sum, and one with more digits cannot be read, so both take Decimal128
				int? familyDigits = sqlValue.ValueType.DataType switch
				{
					DataType.Decimal32 => 9,
					DataType.Decimal64 => 18,
					_                  => null,
				};

				if (decimal.Round(literal, scale) != literal)
				{
					var dataType = familyDigits == null ? sqlValue.ValueType.DataType : DataType.Decimal128;

					return factory.Value(sqlValue.ValueType.WithDataType(dataType).WithPrecisionScale(integerDigits + literalScale, literalScale), literal);
				}

				if (familyDigits is int digits && integerDigits + Math.Max(literalScale, scale) > digits)
					return factory.Value(sqlValue.ValueType.WithDataType(DataType.Decimal128), literal);
			}

			return value;
		}
	}
}
