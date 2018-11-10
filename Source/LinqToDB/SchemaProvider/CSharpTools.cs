using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LinqToDB.SchemaProvider
{
	public static class CSharpTools
	{
		/// <summary>
		/// Reserved words taken from
		/// <see href="https://msdn.microsoft.com/en-us/library/x53a06bb%28v=vs.140%29.aspx"/>.
		/// List actual for C# 7.3.
		/// Doesn't contain "using static",
		/// </summary>
		private static readonly ISet<string> _reservedWords
			= new HashSet<string>()
		{
			"abstract",  "as",         "base",      "bool",     "break",    "byte",      "case",    "catch",
			"char",      "checked",    "class",     "const",    "continue", "decimal",   "default", "delegate",
			"do",        "double",     "else",      "enum",     "event",    "explicit",  "extern",  "false",
			"finally",   "fixed",      "float",     "for",      "foreach",  "goto",      "if",      "implicit",
			"in",        "int",        "interface", "internal", "is",       "lock",      "long",    "namespace",
			"new",       "null",       "object",    "operator", "out",      "override",  "params",  "private",
			"protected", "public",     "readonly",  "ref",      "return",   "sbyte",     "sealed",  "short",
			"sizeof",    "stackalloc", "static",    "string",   "struct",   "switch",    "this",    "throw",
			"true",      "try",        "typeof",    "uint",     "ulong",    "unchecked", "unsafe",  "ushort",
			"using",     "virtual",    "void",      "volatile", "while"
		};

		/// <summary>
		/// Contextual words taken from
		/// <see href="https://msdn.microsoft.com/en-us/library/x53a06bb%28v=vs.140%29.aspx"/>.
		/// List actual for C# 7.3.
		/// </summary>
		private static readonly ISet<string> _contextualWords
			= new HashSet<string>()
		{
			"add",    "alias", "ascending", "async",   "await",  "by",     "descending", "dynamic",
			"equals", "from",  "get",       "global",  "group",  "into",   "join",       "let",
			"nameof", "on",    "orderby",   "partial", "remove", "select", "set",        "unmanaged",
			"value",  "var",   "when",      "where",   "yield"
		};

		private static readonly ISet<UnicodeCategory> _otherCharsCategories;

		private static readonly ISet<UnicodeCategory> _startCharCategories;

		static CSharpTools()
		{
			_startCharCategories = new HashSet<UnicodeCategory>()
			{
				// Lu letter
				UnicodeCategory.UppercaseLetter,
				// Ll letter
				UnicodeCategory.LowercaseLetter,
				// Lt letter
				UnicodeCategory.TitlecaseLetter,
				// Lm letter
				UnicodeCategory.ModifierLetter,
				// Lo letter
				UnicodeCategory.OtherLetter,
				// Nl letter
				UnicodeCategory.LetterNumber
			};

			_otherCharsCategories = new HashSet<UnicodeCategory>(_startCharCategories)
			{
				// Mn
				UnicodeCategory.NonSpacingMark,
				// Mc
				UnicodeCategory.SpacingCombiningMark,
				// Nd
				UnicodeCategory.DecimalDigitNumber,
				// Pc
				UnicodeCategory.ConnectorPunctuation,
				// Cf
				UnicodeCategory.Format
			};
		}

		/// <summary>
		/// Converts <paramref name="name"/> to valid C# identifier.
		/// </summary>
		public static string ToValidIdentifier(string name)
		{
			if (name == null || name == string.Empty || name == "@")
			{
				return "_";
			}

			if (_reservedWords.Contains(name) || _contextualWords.Contains(name))
			{
				return "@" + name;
			}

			if (name.StartsWith("@"))
			{
				if (_reservedWords.Contains(name.Substring(1)) || _contextualWords.Contains(name.Substring(1)))
				{
					return name;
				}
				else
				{
					name = name.Substring(1);
				}
			}

			var sb = new StringBuilder();

			foreach (var chr in name)
			{
				var cat = CharUnicodeInfo.GetUnicodeCategory(chr);
				if (sb.Length == 0 && !_startCharCategories.Contains(cat) && chr != '_')
				{
					sb.Append("_");
				}

				if (sb.Length != 0 && !_otherCharsCategories.Contains(cat))
				{
					sb.Append("_");
				}
				else
				{
					sb.Append(chr);
				}
			}

			if (sb.Length >= 2 && sb[0] == '_' && sb[1] == '_' && (sb.Length == 2 || sb[2] != '_'))
			{
				sb.Insert(0, '_');
			}

			return sb.ToString();
		}

		public static string ToStringLiteral(string value)
		{
			if (value == null)
				return "null";

			var sb = new StringBuilder("\"");

			foreach (var chr in value)
			{
				switch (chr)
				{
					case '\t':     sb.Append("\\t");            break;
					case '\n':     sb.Append("\\n");            break;
					case '\r':     sb.Append("\\r");            break;
					case '\\':     sb.Append("\\\\");           break;
					case '"' :     sb.Append("\\\"");           break;
					case '\0':     sb.Append("\\0");            break;
					case '\u0085':
					case '\u2028':
					case '\u2029':
							 sb.Append($"\\u{(ushort)chr:X4}"); break;
					default: sb.Append(chr);                    break;
				}
			}

			sb.Append('"');

			return sb.ToString();
		}
	}
}
