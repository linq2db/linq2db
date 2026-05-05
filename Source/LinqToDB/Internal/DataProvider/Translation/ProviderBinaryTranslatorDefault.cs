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

			// If an operand can't be SQL-translated (e.g. let-bound non-translatable
			// expression), bail out so VisitBinary falls back to the regular binary
			// `+` handling which can partition the projection for client-side evaluation.
			if (!translationContext.TranslateToSqlExpression(binaryExpression.Left, out var left))
				return null;

			if (!translationContext.TranslateToSqlExpression(binaryExpression.Right, out var right))
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translationContext.ExpressionFactory.Concat(left, right), binaryExpression);
		}
	}
}
