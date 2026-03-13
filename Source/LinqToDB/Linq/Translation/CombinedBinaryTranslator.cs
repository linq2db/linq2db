using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	public sealed class CombinedBinaryTranslator : IBinaryTranslator
	{
		public CombinedBinaryTranslator()
		{
			Translators = new();
		}

		public CombinedBinaryTranslator(IEnumerable<IBinaryTranslator> translators)
		{
			Translators = translators.ToList();
		}

		public List<IBinaryTranslator> Translators { get; set; }

		public Expression? Translate(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			foreach (var translator in Translators)
			{
				var result = translator.Translate(translationContext, binaryExpression, translationFlags);
				if (result != null)
					return result;
			}

			return null;
		}

		public void Add(IBinaryTranslator binaryTranslator)
		{
			Translators.Add(binaryTranslator);
		}
	}
}
