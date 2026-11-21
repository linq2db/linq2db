using System;
using System.Collections.Generic;

using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// Per-dialect operator precedence: one <see cref="SqlOperatorPrecedence"/> per rendered operator, prefix
	/// operator, predicate kind and template class. <see cref="Default"/> is conservative: where dialects disagree,
	/// operators are unordered and therefore parenthesized; a dialect derives its own table with <see cref="With"/>.
	/// </summary>
	public sealed class SqlPrecedenceTable
	{
		/// <summary>Group of arithmetic operators and the predicates over them.</summary>
		public const int ArithmeticGroup = 1;
		/// <summary>Group of prefix operators and the predicates over them.</summary>
		public const int PrefixGroup     = 2;
		/// <summary>Group of predicates.</summary>
		public const int PredicateGroup  = 4;
		/// <summary>Every group.</summary>
		public const int AllGroups       = ~0;

		readonly Dictionary<string, SqlOperatorPrecedence> _entries;

		SqlPrecedenceTable(Dictionary<string, SqlOperatorPrecedence> entries)
		{
			_entries = entries;
		}

		static readonly int[] _classes =
		[
			Precedence.Primary, Precedence.Unary, Precedence.Multiplicative, Precedence.Subtraction, Precedence.Additive,
			Precedence.Comparison, Precedence.Bitwise, Precedence.LogicalNegation, Precedence.LogicalConjunction,
			Precedence.LogicalDisjunction, Precedence.Concatenate, Precedence.Unknown,
		];

		/// <summary>
		/// The conservative default, correct on any dialect: only <c>* / %</c> above <c>+ -</c> above the predicates is
		/// ordered; prefix operators, bitwise operators, concatenation and power are unordered with every other operator.
		/// </summary>
		public static SqlPrecedenceTable Default { get; } = CreateDefault();

		static SqlPrecedenceTable CreateDefault()
		{
			const int arithmetic = ArithmeticGroup;
			const int predicate  = ArithmeticGroup | PrefixGroup | PredicateGroup;

			var multiplicative = new SqlOperatorPrecedence(Precedence.Multiplicative, SqlAssociativity.Left, false, arithmetic);
			var additive       = new SqlOperatorPrecedence(Precedence.Additive,       SqlAssociativity.Left, false, arithmetic);
			var prefix         = new SqlOperatorPrecedence(Precedence.Unary,          SqlAssociativity.Left, false, PrefixGroup);
			var bitwise        = new SqlOperatorPrecedence(Precedence.Bitwise,        SqlAssociativity.Left, true,  0);
			var comparison     = new SqlOperatorPrecedence(Precedence.Comparison,     SqlAssociativity.None, false, predicate);

			var entries = new Dictionary<string, SqlOperatorPrecedence>(StringComparer.Ordinal)
			{
				["*"]  = multiplicative,
				["/"]  = multiplicative,
				["%"]  = multiplicative,
				["+"]  = additive,
				["-"]  = additive,
				["&"]  = bitwise,
				["|"]  = bitwise,
				["^"]  = bitwise,

				[SqlPrecedenceKeys.Negation]        = prefix,
				[SqlPrecedenceKeys.NegativeLiteral] = prefix,
				[SqlPrecedenceKeys.BitwiseNegation] = prefix with { Groups = 0 },
				[SqlPrecedenceKeys.Concatenation]   = new(Precedence.Concatenate, SqlAssociativity.Left, true, 0),
				[SqlPrecedenceKeys.Power]           = new(Precedence.Unary + 1,   SqlAssociativity.Left, false, 0),
				[SqlPrecedenceKeys.Cast]            = SqlOperatorPrecedence.Primary,

				[SqlPrecedenceKeys.Between] = comparison,
			};

			var not = new SqlOperatorPrecedence(Precedence.LogicalNegation,    SqlAssociativity.None, false, predicate);
			var and = new SqlOperatorPrecedence(Precedence.LogicalConjunction, SqlAssociativity.Left, false, predicate);
			var or  = new SqlOperatorPrecedence(Precedence.LogicalDisjunction, SqlAssociativity.Left, false, predicate);

			// template classes: a declared Precedence constant behaves like the operators of its class
			entries[SqlPrecedenceKeys.Class(Precedence.Primary)]            = SqlOperatorPrecedence.Primary;
			entries[SqlPrecedenceKeys.Class(Precedence.Unary)]              = prefix;
			entries[SqlPrecedenceKeys.Class(Precedence.Multiplicative)]     = multiplicative;
			entries[SqlPrecedenceKeys.Class(Precedence.Subtraction)]        = additive;
			entries[SqlPrecedenceKeys.Class(Precedence.Additive)]           = additive;
			entries[SqlPrecedenceKeys.Class(Precedence.Comparison)]         = comparison;
			entries[SqlPrecedenceKeys.Class(Precedence.Bitwise)]            = bitwise with { Associative = false };
			entries[SqlPrecedenceKeys.Class(Precedence.LogicalNegation)]    = not;
			entries[SqlPrecedenceKeys.Class(Precedence.LogicalConjunction)] = and;
			entries[SqlPrecedenceKeys.Class(Precedence.LogicalDisjunction)] = or;
			entries[SqlPrecedenceKeys.Class(Precedence.Concatenate)]        = entries[SqlPrecedenceKeys.Concatenation] with { Associative = false };
			entries[SqlPrecedenceKeys.Class(Precedence.Unknown)]            = SqlOperatorPrecedence.Unknown;

			return new SqlPrecedenceTable(entries);
		}

		/// <summary>
		/// A copy of this table with the given entries replaced or added.
		/// </summary>
		public SqlPrecedenceTable With(params (string Key, SqlOperatorPrecedence Precedence)[] entries)
		{
			var copy = new Dictionary<string, SqlOperatorPrecedence>(_entries, StringComparer.Ordinal);

			foreach (var (key, precedence) in entries)
				copy[key] = precedence;

			return new SqlPrecedenceTable(copy) { PrefixReachesEqualLevel = PrefixReachesEqualLevel };
		}

		/// <summary>
		/// A copy of this table with <see cref="PrefixReachesEqualLevel"/> set to <paramref name="value"/>.
		/// </summary>
		public SqlPrecedenceTable WithPrefixReachesEqualLevel(bool value) =>
			new(_entries) { PrefixReachesEqualLevel = value };

		/// <summary>
		/// The entry for <paramref name="key"/>; an unknown key gets a conservative entry that is parenthesized as an
		/// operand and parenthesizes its own non-primary operands.
		/// </summary>
		public SqlOperatorPrecedence this[string key] =>
			_entries.TryGetValue(key, out var precedence)
				? precedence
				: new SqlOperatorPrecedence(1, SqlAssociativity.None, false, 0);

		/// <summary>
		/// The entry for a declared <see cref="Precedence"/> int (a template class or a bare parent precedence): an
		/// exact constant maps to its class, any other value to the nearest constant, ties going up; values above
		/// <see cref="Precedence.Primary"/> mean "always parenthesize".
		/// </summary>
		public SqlOperatorPrecedence ForClass(int precedence)
		{
			if (precedence > Precedence.Primary)
				return new SqlOperatorPrecedence(precedence, SqlAssociativity.None, false, AllGroups);

			var nearest = _classes[0];

			foreach (var c in _classes)
				if (Math.Abs(c - precedence) < Math.Abs(nearest - precedence) || (Math.Abs(c - precedence) == Math.Abs(nearest - precedence) && c > nearest))
					nearest = c;

			return this[SqlPrecedenceKeys.Class(nearest)];
		}

		/// <summary>
		/// Whether an operand needs parentheses under its parent.
		/// </summary>
		/// <param name="child">The operand's entry.</param>
		/// <param name="childKey">The operand's key, or <see langword="null"/> for a template or other non-operator node.</param>
		/// <param name="parent">The parent's entry.</param>
		/// <param name="parentKey">The parent's key, or <see langword="null"/> for an anonymous parent.</param>
		/// <param name="rightOperand"><see langword="true"/> for a right (or only, or unknown-side) operand.</param>
		public static bool NeedsParentheses(SqlOperatorPrecedence child, string? childKey, SqlOperatorPrecedence parent, string? parentKey, bool rightOperand)
		{
			if (child.Level == Precedence.Unknown)
				return true;

			if (parent.Level == Precedence.Unknown)
				return false;

			if (parent.Level > Precedence.Primary)
				return true;

			if (child.Level >= Precedence.Primary)
				return false;

			var sameOperator = childKey != null && string.Equals(childKey, parentKey, StringComparison.Ordinal);

			if (!sameOperator && (child.Groups & parent.Groups) == 0)
				return true;

			if (child.Level != parent.Level)
				return child.Level < parent.Level;

			// an operand with no operator identity (a template) may end in anything, so an equal-level one is
			// parenthesized on either side
			if (!rightOperand)
				return parent.Associativity == SqlAssociativity.None || childKey == null;

			return !(sameOperator && parent.Associative);
		}

		/// <summary>
		/// Whether a prefix operator exposed at the end of a left operand reaches over its parent operator, so the
		/// left operand needs parentheses even though its own level is higher.
		/// </summary>
		public bool PrefixReaches(SqlOperatorPrecedence prefix, SqlOperatorPrecedence parent)
		{
			if (parent.Level is <= Precedence.Unknown or >= Precedence.Primary)
				return false;

			if ((prefix.Groups & parent.Groups) == 0)
				return true;

			return PrefixReachesEqualLevel ? prefix.Level <= parent.Level : prefix.Level < parent.Level;
		}

		/// <summary>
		/// Whether a prefix operator also reaches over an operator of its own level (default <see langword="true"/>,
		/// the conservative reading).
		/// </summary>
		public bool PrefixReachesEqualLevel { get; private init; } = true;
	}
}
