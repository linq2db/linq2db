using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq.Translation
{
	public interface IMemberTranslator
	{
		Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags);
	}
}
