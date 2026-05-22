using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.MySql.Translation
{
	public class MySql80MemberTranslator : MySqlMemberTranslator
	{
		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new MySql80StringMemberTranslator();
		}

		protected class MySql80StringMemberTranslator : MySqlStringMemberTranslator
		{
			// MySQL 8+ has REGEXP_REPLACE (ICU regex). Use it for set-semantics chars trim.
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimStart(translationContext, methodCall, translationFlags, value, trimChars);

				return TranslateRegexpTrim(translationContext, value, trimChars, atStart: true);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimEnd(translationContext, methodCall, translationFlags, value, trimChars);

				return TranslateRegexpTrim(translationContext, value, trimChars, atStart: false);
			}

			static ISqlExpression? TranslateRegexpTrim(ITranslationContext translationContext, ISqlExpression value, ISqlExpression trimChars, bool atStart)
			{
				if (trimChars is not SqlValue { Value: string chars } || chars.Length == 0)
					return null;

				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				var sb = new System.Text.StringBuilder(chars.Length * 2 + 4);
				if (atStart)
					sb.Append('^');
				sb.Append('[');
				foreach (var ch in chars)
				{
					if (ch is '\\' or ']' or '^' or '-' or '[')
						sb.Append('\\');
					sb.Append(ch);
				}

				sb.Append("]+");
				if (!atStart)
					sb.Append('$');

				// (?-i) forces case-sensitive matching regardless of column collation: .NET
				// `TrimStart('a')` removes only lowercase 'a', not 'A'. Both ICU (MySQL 8) and
				// PCRE (MariaDB) honour the inline flag.
				return factory.Expression(valueType, "REGEXP_REPLACE({0}, {1}, '')", value, factory.Value(valueType, "(?-i)" + sb.ToString()));
			}
		}
	}
}
