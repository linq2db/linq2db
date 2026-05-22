using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// File-level code unit.
	/// </summary>
	public sealed class CodeFile : CodeElementList<ITopLevelElement>, ICodeElement
	{
		private string _name;
		private readonly List<CodeComment> _header;
		private readonly List<CodeImport>  _imports;

		public CodeFile(string fileName, IEnumerable<CodeComment>? header, IEnumerable<CodeImport>? imports, IEnumerable<ITopLevelElement>? items)
			: base(items)
		{
			FileName = fileName;
			_header  = [.. header  ?? []];
			_imports = [.. imports ?? []];
		}

		public CodeFile(string fileName)
			: this(fileName, null, null, null)
		{
		}

		/// <summary>
		/// File name. Assigned value ignored if <see cref="NameSource"/> set.
		/// </summary>
		public string FileName
		{
			get => NameSource?.Name ?? _name;
			[MemberNotNull(nameof(_name))]
			set => _name = NormalizeFileName(value);
		}

		/// <summary>
		/// File header coomment(s).
		/// </summary>
		public IReadOnlyList<CodeComment> Header => _header;
		/// <summary>
		/// File imports.
		/// </summary>
		public IReadOnlyList<CodeImport> Imports => _imports;

		/// <summary>
		/// Get or set optional reference to identifier used for name generation.
		/// </summary>
		public CodeIdentifier?           NameSource { get; set; }

		CodeElementType ICodeElement.ElementType => CodeElementType.File;

		internal void AddHeader(CodeComment comment)
		{
			_header.Add(comment);
		}

		internal void AddImport(CodeImport import)
		{
			_imports.Add(import);
		}

		private static string NormalizeFileName(string name)
		{
			// for file name normalization we use simple and strict logic where we just filter out
			// all non-digit and non-letter characters plus allow underscore and dot characters
			if (name.Length == 0)
				return "_";

			var sb = new StringBuilder();
			foreach (var (chr, cat) in name.EnumerateCharacters())
			{
				if (string.Equals(chr, ".", System.StringComparison.Ordinal))
				{
					sb.Append(chr);
					continue;
				}

				switch (cat)
				{
					// lazy validation for now
					case UnicodeCategory.UppercaseLetter   :
					case UnicodeCategory.TitlecaseLetter   :
					case UnicodeCategory.LowercaseLetter   :
					case UnicodeCategory.DecimalDigitNumber:
						sb.Append(chr);
						break;
					default                                :
						sb.Append('_');
						break;
				}
			}

			return sb.ToString();
		}
	}
}
