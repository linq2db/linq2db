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
			if (memberExpression is (MethodCallExpression or MemberExpression or NewExpression or UnaryExpression { Method: not null } or BinaryExpression { Method: not null }))
			{
				var memberInfoWithType = MemberHelper.GetMemberInfoWithType(memberExpression);
				var translationFunc    = Registration.GetTranslation(memberInfoWithType);
				if (translationFunc != null)
					return translationFunc(translationContext, memberExpression, translationFlags);
			}

			var translated = CombinedMemberTranslator.Translate(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			translated = Registration.ProvideReplacement(memberExpression);
			if (translated != null)
			{
				return Translate(translationContext, translated, translationFlags); 
			}

			translated = TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
			if (translated != null)
				return translated;

			return null;
		}
	}
}
