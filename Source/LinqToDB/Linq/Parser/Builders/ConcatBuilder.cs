using System;
using System.Reflection;
using LinqToDB.Linq.Parser.Clauses;

namespace LinqToDB.Linq.Parser.Builders
{
	public class ConcatBuilder : BaseSetBuilder
	{
		private static readonly MethodInfo[] _supported =
			{ ParsingMethods.Concat };

		public override MethodInfo[] SupportedMethods() => _supported;

		protected override BaseSetClause CreateSetClause(Type itemType, string itemName, Sequence sequence1, Sequence sequence2)
		{
			return new ConcatClause(itemType, itemName, sequence1, sequence2);
		}

	}
}
