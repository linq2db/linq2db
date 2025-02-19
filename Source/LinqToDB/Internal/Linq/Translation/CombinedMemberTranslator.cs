using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Linq.Translation
{
	public sealed class CombinedMemberTranslator : IMemberTranslator
	{
		public CombinedMemberTranslator()
		{
			Translators = new ();
		}

		public CombinedMemberTranslator(IEnumerable<IMemberTranslator> translators)
		{
			Translators = translators.ToList();
		}

		public List<IMemberTranslator> Translators { get; set; }

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			foreach (var translator in Translators)
			{
				var result = translator.Translate(translationContext, memberExpression, translationFlags);
				if (result != null)
					return result;
			}

			return null;
		}

		public void Add(IMemberTranslator memberTranslator)
		{
			Translators.Add(memberTranslator);
		}
	}
}
