using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	using SqlQuery;

	public class MathMemberTranslatorBase : MemberTranslatorBase
	{
		public MathMemberTranslatorBase()
		{
			RegisterMax();
			RegisterMin();
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

		protected virtual ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
		{
			var factory = translationContext.ExpressionFactory;

			var xType        = factory.GetDbDataType(xValue);
			var yType        = factory.GetDbDataType(yValue);
			var yValueResult = yValue;

			if (!xType.EqualsDbOnly(yType))
			{
				yValueResult = new SqlCastExpression(yValue, xType, null);
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
				yValueResult = new SqlCastExpression(yValue, xType, null);
			}

			var result = factory.Condition(factory.LessOrEqual(xValue, yValue), xValue, yValueResult);
			return result;
		}
	}
}
