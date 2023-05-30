using System;
using System.Collections.Generic;

using LinqToDB.Common.Internal;

namespace LinqToDB.DataProvider
{
	public class UniqueParametersNormalizer : IQueryParametersNormalizer
	{
		private HashSet<string>? _usedParameterNames;

		protected virtual  StringComparer Comparer         => StringComparer.OrdinalIgnoreCase;
		protected virtual  string         DefaultName      => "p";
		protected virtual  string         CounterSeparator => "_";
		protected virtual  int            MaxLength        => 50;

		/// <summary>
		/// Method should validate name characters and remove or replace invalid characters.
		/// Implementation must not add additional characters, as it will lead to infinte loop from caller.
		/// Default implementation removes all characters except ASCII letters/digits and underscore.
		/// </summary>
		protected virtual string MakeValidName(string name)
		{
			var badIdx = -1;

			for (var i = 0; i < name.Length; i++)
			{
				if (!IsValidCharacter(name[i]))
				{
					badIdx = i;
					break;
				}
			}

			if (badIdx != -1)
			{
				using var sb = Pools.StringBuilder.Allocate();

				if (badIdx > 0)
					sb.Value.Append(name[0..badIdx]);

				for (var i = badIdx; i < name.Length; i++)
				{
					var chr = name[i];

					// add allowed character
					if (IsValidCharacter(chr))
						sb.Value.Append(chr);

				}

				if (sb.Value.Length > 0)
					name = sb.Value.ToString();
				else
					name = DefaultName;
			}

			return name;
		}
		protected virtual bool IsValidCharacter(char chr)
		{
#if NET7_0_OR_GREATER
			return chr == '_' || char.IsAsciiLetterOrDigit(chr);
#else
			return chr == '_'
				|| chr is >= 'a' and <= 'z'
				|| chr is >= 'A' and <= 'Z'
				|| chr is >= '0' and <= '9';
#endif
		}

		protected virtual bool IsReserved(string name) => false;

		public string? Normalize(string? originalName)
		{
			if (string.IsNullOrEmpty(originalName))
				originalName = DefaultName;

			if (originalName!.Length > MaxLength)
				originalName = originalName.Substring(0, MaxLength);

			var name = originalName;

			while (true)
			{
				originalName = MakeValidName(originalName!);

				name = originalName;

				var cnt = 0;
				while (IsReserved(name) || _usedParameterNames?.Contains(name) == true)
					name = $"{originalName}{CounterSeparator}{++cnt}";

				if (name.Length > MaxLength)
					originalName = originalName.Substring(0, name.Length - 1);
				else
					break;
			}

			(_usedParameterNames ??= new(Comparer)).Add(name);

			return name;
		}
	}
}
