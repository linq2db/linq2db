using System.Linq.Expressions;

using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class UnaryTranslatorBase : IUnaryTranslator
	{
		protected UnaryTranslationRegistration Registration            = new();
		protected CombinedUnaryTranslator      CombinedUnaryTranslator = new();

		public Expression? Translate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
		{
			var translation = Registration.GetTranslation(unaryExpression.NodeType, unaryExpression.Operand.Type);
			if (translation != null)
				return translation(translationContext, unaryExpression, translationFlags);

			return CombinedUnaryTranslator.Translate(translationContext, unaryExpression, translationFlags);
		}
	}
}
