using System.Linq.Expressions;

using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class BinaryTranslatorBase : IBinaryTranslator
	{
		protected BinaryTranslationRegistration Registration             = new();
		protected CombinedBinaryTranslator      CombinedBinaryTranslator = new();

		public Expression? Translate(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			var translation = Registration.GetTranslation(binaryExpression.NodeType, binaryExpression.Left.Type, binaryExpression.Right.Type);
			if (translation != null)
				return translation(translationContext, binaryExpression, translationFlags);

			return CombinedBinaryTranslator.Translate(translationContext, binaryExpression, translationFlags);
		}
	}
}
