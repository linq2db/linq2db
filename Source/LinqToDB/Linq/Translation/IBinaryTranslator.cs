using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	public interface IBinaryTranslator
	{
		Expression? Translate(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags);
	}
}
