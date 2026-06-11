using System.Linq.Expressions;

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
		}

		Expression? TranslateToNullableMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			// If the argument can't be turned into SQL it's a client-side value — decline (return null) so it is
			// evaluated client-side, where Sql.ToNullable is a safe widen (there is no SQL NULL to lose).
			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var valueExpr))
				return null;

			// Sql.ToNullable<T>(T) is a pure nullability widener: identical SQL, result type T?. Building the
			// placeholder from the original call (type T?) makes the value read as nullable, so a SQL NULL is
			// preserved instead of collapsing to default(T) on the client.
			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, valueExpr, methodCall);
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
