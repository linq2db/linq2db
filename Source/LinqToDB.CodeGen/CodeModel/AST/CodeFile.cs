using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// File-level code unit.
	/// </summary>
	public class CodeFile : CodeElementList<ITopLevelElement>, ICodeElement
	{
		private string _name;

		public CodeFile(string fileName)
		{
			FileName = fileName;
		}

		/// <summary>
		/// File name.
		/// </summary>
		public string FileName
		{
			get => _name;
			[MemberNotNull(nameof(_name))]
			set => _name = NormalizeName(value);
		}

		/// <summary>
		/// File header coomment(s).
		/// </summary>
		public List<CodeComment> Header { get; } = new ();
		/// <summary>
		/// File imports.
		/// </summary>
		public List<CodeImport> Imports { get; } = new ();

		CodeElementType ICodeElement.ElementType => CodeElementType.File;

		private string NormalizeName(string name)
		{
			// for file name normalization we use simple and strict logic where we just filter out
			// all non-digit and non-letter characters plus allow underscore and dot characters
			if (name.Length == 0)
				return "_";

			var sb = new StringBuilder();
			foreach (var (chr, cat) in name.EnumerateCharacters())
			{
				if (chr == ".")
				{
					sb.Append(chr);
					continue;
				}

				switch (cat)
				{
					// lazy validation for now
					case UnicodeCategory.UppercaseLetter:
					case UnicodeCategory.TitlecaseLetter:
					case UnicodeCategory.LowercaseLetter:
					case UnicodeCategory.DecimalDigitNumber:
						sb.Append(chr);
						break;
					default:
						sb.Append('_');
						break;
				}
			}

			return sb.ToString();
		}
	}
}
