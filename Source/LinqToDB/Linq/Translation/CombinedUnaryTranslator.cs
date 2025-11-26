using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	public sealed class CombinedUnaryTranslator : IUnaryTranslator
	{
		public CombinedUnaryTranslator()
		{
			Translators = new();
		}

		public CombinedUnaryTranslator(IEnumerable<IUnaryTranslator> translators)
		{
			Translators = translators.ToList();
		}

		public List<IUnaryTranslator> Translators { get; set; }

		public Expression? Translate(ITranslationContext translationContext, UnaryExpression unaryExpression, TranslationFlags translationFlags)
		{
			foreach (var translator in Translators)
			{
				var result = translator.Translate(translationContext, unaryExpression, translationFlags);
				if (result != null)
					return result;
			}

			return null;
		}

		public void Add(IUnaryTranslator unaryTranslator)
		{
			Translators.Add(unaryTranslator);
		}
	}
}
