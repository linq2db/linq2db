using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class MemberTranslatorBase : IMemberTranslator
	{
		protected TranslationRegistration  Registration             = new();
		protected CombinedMemberTranslator CombinedMemberTranslator = new();

		/// <summary>
		/// Called before every translation attempt.
		/// </summary>
		/// <param name="translationContext"></param>
		/// <param name="memberExpression"></param>
		/// <param name="translationFlags"></param>
		/// <returns></returns>
		protected virtual Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		/// <summary>
		/// Returns null if the expression is not required for translation
		/// </summary>
		/// <param name="translationContext"></param>
		/// <param name="objExpression"></param>
		/// <param name="translationFlags"></param>
		/// <returns></returns>
		protected SqlPlaceholderExpression? TranslateNoRequiredExpression(ITranslationContext translationContext, Expression? objExpression, TranslationFlags translationFlags, bool skipIfParameter = true)
		{
			if (objExpression == null)
				return null;

			var obj = translationContext.Translate(objExpression, translationFlags);

			if (obj is not SqlPlaceholderExpression objPlaceholder)
				return null;

			if (skipIfParameter && objPlaceholder.Sql is SqlParameter)
				return null;

			return objPlaceholder;
		}

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is (MethodCallExpression or MemberExpression or NewExpression))
			{
				var memberInfoWithType = MemberHelper.GetMemberInfoWithType(memberExpression);
				var translationFunc    = Registration.GetTranslation(memberInfoWithType, out var isOptional);

				// An optional registration declines when the caller prefers client calculation, so the expression
				// falls through to client-side evaluation instead of becoming an SQL column.
				if (isOptional && translationFlags.HasFlag(TranslationFlags.SkipOptional))
					return null;

				if (translationFunc != null)
					return translationFunc(translationContext, memberExpression, translationFlags);
			}
			// Every binary, not only one carrying an operator method: a comparison between numbers has none, and a
			// translator can still have something to say about it - a duration's total compared against a bound is a
			// comparison of doubles. Widening this means every binary node reaches the translator chain now,
			// primitives included, so an IMemberTranslator sees shapes it did not before. The unary arm below keeps
			// its Method guard, which is why the two read differently.
			else if (memberExpression is BinaryExpression binaryExpression)
			{
				// Operand-typed lookup, distinct from the MemberInfo registry. Avoids collision
				// with `string.Concat(string, string)` which is registered as a *method* translator
				// (PreserveNull = false, C# semantics) — `a + b` on strings dispatches here with
				// PreserveNull = true (SQL null-propagation).
				var translationFunc = Registration.GetBinaryTranslation(binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type);
				if (translationFunc != null)
					return translationFunc(translationContext, binaryExpression, translationFlags);
			}
			else if (memberExpression is UnaryExpression { Method: not null } unaryExpression)
			{
				var translationFunc = Registration.GetUnaryTranslation(unaryExpression.NodeType, unaryExpression.Operand.Type);
				if (translationFunc != null)
					return translationFunc(translationContext, unaryExpression, translationFlags);
			}

			var translated = CombinedMemberTranslator.Translate(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			translated = Registration.ProvideReplacement(memberExpression);
			if (translated != null)
			{
				// This recursion bypasses ITranslationContext.Translate, so it carries no InsideTranslation flag.
				// Strip SkipOptional instead: a mandatory replacement has already been admitted, and its expansion
				// must translate rather than decline on an optional registration underneath it.
				return Translate(translationContext, translated, translationFlags & ~TranslationFlags.SkipOptional);
			}

			translated = TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			return null;
		}
	}
}
