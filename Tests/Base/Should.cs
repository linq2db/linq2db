using System;
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
	}

	class SubstringsConstraint : StringConstraint
	{
		StringComparison? _comparisonType;
		string[]          _substrings;
		int               _matched;

		/// <summary>
		/// Initializes a new instance of the <see cref="SubstringConstraint"/> class.
		/// </summary>
		/// <param name="expected">The expected.</param>
		public SubstringsConstraint(params string[] expected)
			: base(string.Join(Environment.NewLine, expected))
		{
			_substrings     = expected;
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
					sb.AppendLine().Append($"\"{_substrings[i]}\"");

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

			var actualComparison = _comparisonType ?? StringComparison.InvariantCulture;
			var idx              = 0;

			_matched = 0;

			foreach (var str in _substrings)
			{
				idx =  actual.IndexOf(str, idx, actualComparison);

				if (idx < 0)
					return false;

				_matched++;
			}

			return true;
		}

		/// <summary>
		/// Modify the constraint to the specified comparison.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown when a comparison type different
		/// than <paramref name="comparisonType"/> was already set.</exception>
		public SubstringsConstraint Using(StringComparison comparisonType)
		{
			if (_comparisonType == null)
				_comparisonType = comparisonType;
			else if (_comparisonType != comparisonType)
				throw new InvalidOperationException("A different comparison type was already set.");

			return this;
		}
	}
}
