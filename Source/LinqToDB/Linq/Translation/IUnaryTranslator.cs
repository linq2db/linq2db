using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	public interface IUnaryTranslator
	{
		Expression? Translate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags);
	}
}
