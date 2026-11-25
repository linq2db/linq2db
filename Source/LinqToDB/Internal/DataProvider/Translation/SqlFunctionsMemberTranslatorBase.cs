using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class SqlFunctionsMemberTranslatorBase : MemberTranslatorBase
	{
		public SqlFunctionsMemberTranslatorBase()
		{
			Registration.RegisterMethod<int, int, int?>((value,          compareTo) => Sql.NullIf(value, compareTo), TranslateNullifMethod, isGenericTypeMatch: true);
			Registration.RegisterMethod<int?, int?, int?>((value,        compareTo) => Sql.NullIf(value, compareTo), TranslateNullifMethod, isGenericTypeMatch: true);
			Registration.RegisterMethod<object, object, object?>((value, compareTo) => Sql.NullIf(value, compareTo), TranslateNullifMethod, isGenericTypeMatch: true);

			Registration.RegisterMethod<int, int?>(value => Sql.ToNullable(value), TranslateToNullableMethod, isGenericTypeMatch: true);
			Registration.RegisterMethod<int, int>(value  => Sql.AsNullable(value), TranslateAsNullableMethod, isGenericTypeMatch: true);

			Registration.RegisterMethod<int, int, int, bool>((value,    low, high) => Sql.Between(value, low, high),    TranslateBetweenMethod,    isGenericTypeMatch: true);
			Registration.RegisterMethod<int?, int?, int?, bool>((value, low, high) => Sql.Between(value, low, high),    TranslateBetweenMethod,    isGenericTypeMatch: true);
			Registration.RegisterMethod<int, int, int, bool>((value,    low, high) => Sql.NotBetween(value, low, high), TranslateNotBetweenMethod, isGenericTypeMatch: true);
			Registration.RegisterMethod<int?, int?, int?, bool>((value, low, high) => Sql.NotBetween(value, low, high), TranslateNotBetweenMethod, isGenericTypeMatch: true);
		}

		Expression? TranslateBetweenMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateBetween(translationContext, methodCall, isNot: false);
		}

		Expression? TranslateNotBetweenMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateBetween(translationContext, methodCall, isNot: true);
		}

		static Expression? TranslateBetween(ITranslationContext translationContext, MethodCallExpression methodCall, bool isNot)
		{
			// The operands are translated where the call stands, so a value the query has already read (an aggregate of a
			// grouping, a column of a subquery) is read from there. An operand without SQL form declines the call.
			// A column the call's result is assigned to or compared with types that boolean result, not the operands.
			using var disposable = translationContext.UsingColumnDescriptor(null);

			ISqlExpression? value, low, high;

			// The value is compared with each bound, so each pair is typed as the operands of that comparison, `>=` or `<=`, are.
			// The value is written once for both, so a pair without a converted column takes the other pair's.
			using (UsingComparisonDescriptor(translationContext, methodCall.Arguments[0], methodCall.Arguments[1], methodCall.Arguments[2]))
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out value)
					|| !translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out low))
					return null;
			}

			using (UsingComparisonDescriptor(translationContext, methodCall.Arguments[0], methodCall.Arguments[2], methodCall.Arguments[1]))
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[2], out high))
					return null;
			}

			var searchCondition = translationContext.ExpressionFactory.SearchCondition().Add(new SqlPredicate.Between(value, isNot, low, high));

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, searchCondition, methodCall);
		}

		static IDisposable? UsingComparisonDescriptor(ITranslationContext translationContext, Expression left, Expression right, Expression otherRight)
		{
			return translationContext is IComparedValueTranslationContext comparedValueContext
				? comparedValueContext.UsingComparisonDescriptor(left, right, otherRight)
				: null;
		}

		Expression? TranslateToNullableMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			// Sql.ToNullable<T>(T) is a pure nullability widener: reuse the argument's placeholder re-typed to T?
			// (MakeNullable) so a SQL NULL is preserved instead of collapsing to default(T). If the argument isn't SQL
			// (a client-side value), decline so it evaluates client-side.
			if (translationContext.Translate(methodCall.Arguments[0], translationFlags) is not SqlPlaceholderExpression placeholder)
				return null;

			return placeholder.MakeNullable();
		}

		Expression? TranslateAsNullableMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			// Sql.AsNullable<T>(T) annotates nullability but returns T (not T?), so emit the argument's placeholder
			// unchanged - re-typing it (MakeNullable) would change a wrapping (T?) cast's operand type and trip
			// ExpressionVisitor unary validation. NULL is preserved by the source (e.g. a LEFT JOIN) and the cast,
			// matching the original [Expression("{0}", CanBeNull = true)] which also emitted just the column.
			if (translationContext.Translate(methodCall.Arguments[0], translationFlags) is not SqlPlaceholderExpression placeholder)
				return null;

			return placeholder;
		}

		Expression? TranslateNullifMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var valueExpr, out var valueError))
				return valueError.WithType(methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var compareToExpr, out var compareToError))
				return compareToError.WithType(methodCall.Type);

			var result = TranslateNullifMethod(translationContext, methodCall, valueExpr, compareToExpr, translationFlags);
			if (result == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, result, methodCall);
		}

		protected virtual ISqlExpression? TranslateNullifMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression valueExpr, ISqlExpression compareToExpr, TranslationFlags translationFlags)
		{
			var factory   = translationContext.ExpressionFactory;
			var valueType = factory.GetDbDataType(valueExpr);
			
			var condition = factory.Condition(factory.Equal(valueExpr, compareToExpr, null), factory.NullValue(valueType), valueExpr);

			return condition;
		}
	}
}
