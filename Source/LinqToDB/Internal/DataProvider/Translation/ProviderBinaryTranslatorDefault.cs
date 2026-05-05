using System.Linq.Expressions;

using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class ProviderBinaryTranslatorDefault : BinaryTranslatorBase
	{
		public ProviderBinaryTranslatorDefault()
		{
			// `a + b` on strings is emitted by the C# compiler as a BinaryExpression with
			// Method = string.Concat(string, string). Route it through SqlConcatExpression so
			// `a + b + c` flattens into a single AST node. The factory's Concat(...) sets
			// PreserveNull = true, matching SQL null-propagation semantics
			// (`'test' || NULL` → NULL, not `'test'`).
			Registration.RegisterBinary<string, string, string>((s1, s2) => s1 + s2, TranslateStringConcat);
		}

		static Expression? TranslateStringConcat(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(binaryExpression))
				return null;

			using var disposable = translationContext.UsingTypeFromExpression(binaryExpression.Left, binaryExpression.Right);

			if (!translationContext.TranslateToSqlExpression(binaryExpression.Left, out var left))
				return translationContext.CreateErrorExpression(binaryExpression.Left, type: binaryExpression.Type);

			if (!translationContext.TranslateToSqlExpression(binaryExpression.Right, out var right))
				return translationContext.CreateErrorExpression(binaryExpression.Right, type: binaryExpression.Type);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translationContext.ExpressionFactory.Concat(left, right), binaryExpression);
		}
	}
}
