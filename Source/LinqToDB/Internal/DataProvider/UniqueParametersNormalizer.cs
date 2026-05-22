using System;
using System.Collections.Generic;
using System.Globalization;

namespace LinqToDB.Internal.DataProvider
{
	/// <summary>
	/// Parameter name rules, implemented by this policy:
	/// <list type="bullet">
	/// <item>duplicate name check is case-insensitive</item>
	/// <item>max name length: 50 characters</item>
	/// <item>allowed characters: ASCII digits, ASCII letters, _ (underscore).</item>
	/// <item>allowed first character: ASCII letter.</item>
	/// <item>default name if name missing/invalid: "p"</item>
	/// <item>duplicates resolved by adding "_counter" suffix</item>
	/// </list>
	/// </summary>
	public class UniqueParametersNormalizer : IQueryParametersNormalizer
	{
		private HashSet<string>? _usedParameterNames;

		protected virtual  StringComparer Comparer         => StringComparer.OrdinalIgnoreCase;
		protected virtual  string         DefaultName      => "p";
		protected virtual  string         CounterSeparator => "_";
		protected virtual  int            MaxLength        => 50;

		/// <summary>
		/// Method should validate name characters and remove or replace invalid characters.
		/// Default implementation removes all characters except ASCII letters/digits and underscore.
		/// </summary>
		protected virtual string MakeValidName(string name)
		{
			var badIdx = -1;

			for (var i = 0; i < name.Length; i++)
			{
				if (!(i == 0 ? IsValidFirstCharacter(name[i]) : IsValidCharacter(name[i])))
				{
					badIdx = i;
					break;
				}
			}

			if (badIdx != -1)
			{
#if SUPPORTS_SPAN
				// allocate memory on the stack if possible, and prepopulate it with the original string
				Span<char> newName = name.Length < 500 ? stackalloc char[name.Length] : name.ToCharArray();
				if (name.Length < 500)
				{
					// prepopulate the stack with the original string
					name.AsSpan().CopyTo(newName);
				}
#else
				var newName = name.ToCharArray();
#endif
				var newNameLength = badIdx;

				for (var i = badIdx; i < name.Length; i++)
				{
					var chr = name[i];

					// add only allowed characters
					if (newNameLength == 0 ? IsValidFirstCharacter(chr) : IsValidCharacter(chr))
						newName[newNameLength++] = chr;
				}

				if (newNameLength > 0)
				{
#if SUPPORTS_SPAN
					name = new string(newName.Slice(0, newNameLength));
#else
					name = new string(newName, 0, newNameLength);
#endif
				}
				else
					name = DefaultName;
			}

			return name;
		}

		protected virtual bool IsValidFirstCharacter(char chr)
		{
			return chr.IsAsciiLetter();
		}

		protected virtual bool IsValidCharacter(char chr)
		{
			return chr == '_' || chr.IsAsciiLetterOrDigit();
		}

		protected virtual bool IsReserved(string name) => false;

		public string? Normalize(string? originalName)
		{
			if (string.IsNullOrEmpty(originalName))
				originalName = DefaultName;

			if (originalName!.Length > MaxLength)
				originalName = originalName.Substring(0, MaxLength);

			originalName = MakeValidName(originalName!);

			string name;

			while (true)
			{
				name = originalName;

				// if the name is reserved or already in use, generate a unique name for the parameter
				var cnt = 0;
				while (IsReserved(name) || _usedParameterNames?.Contains(name) == true)
					name = string.Create(CultureInfo.InvariantCulture, $"{originalName}{CounterSeparator}{++cnt}");

				// if name is not too long, return it
				if (name.Length <= MaxLength)
					break;

				// if the original name is already reduced to a single character, being the first character
				// of the default name, throw an exception, so as to prevent an infinite loop
				if (originalName.Length == 1)
				{
					originalName = originalName[0] != DefaultName[0]
						? DefaultName
						: throw new InvalidOperationException("Cannot sufficiently shorten original name");
				}
				else
				{
					// otherwise, shorten original name by one character and retry
					originalName = originalName.Substring(0, originalName.Length - 1);
				}
			}

			(_usedParameterNames ??= new(Comparer)).Add(name);

			return name;
		}
	}
}
