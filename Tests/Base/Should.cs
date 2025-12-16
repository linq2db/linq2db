using System;
using System.Globalization;
using System.Text;

using NUnit.Framework.Constraints;

namespace Tests
{
	public static class Should
	{
		public static StringConstraint Contain(params string[] expected)
		{
			return new SubstringsConstraint(expected);
		}

		class SubstringsConstraint : StringConstraint
		{
			string[]          _substrings;
			int               _matched;

			/// <summary>
			/// Initializes a new instance of the <see cref="SubstringConstraint"/> class.
			/// </summary>
			/// <param name="expected">The expected.</param>
			public SubstringsConstraint(params string[] expected)
				: base(string.Join(Environment.NewLine, expected))
			{
				_substrings = expected;
				descriptionText = "String containing";
			}

			/// <summary>
			/// The Description of what this constraint tests, for
			/// use in messages and in the ConstraintResult.
			/// </summary>
			public override string Description
			{
				get
				{
					var sb = new StringBuilder("String containing substrings");

					if (caseInsensitive)
						sb.Append(", ignoring case");

					sb.Append(" :");

					for (var i = 0; i < _substrings.Length; i++)
					{
						sb.AppendLine().Append(CultureInfo.InvariantCulture, $"\"{_substrings[i]}\"");

						if (i == _matched)
							sb.Append("    <-- not found");
					}

					return sb.ToString();
				}
			}

			/// <summary>
			/// Test whether the constraint is satisfied by a given value
			/// </summary>
			/// <param name="actual">The value to be tested</param>
			/// <returns>True for success, false for failure</returns>
			protected override bool Matches(string? actual)
			{
				if (actual == null)
					return false;

				var idx = 0;

				_matched = 0;

				foreach (var str in _substrings)
				{
					idx = actual.IndexOf(str, idx, StringComparison.Ordinal);

					if (idx < 0)
						return false;

					_matched++;
				}

				return true;
			}
		}
	}
}
