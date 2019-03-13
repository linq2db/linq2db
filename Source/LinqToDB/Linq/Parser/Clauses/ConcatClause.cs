using System;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class ConcatClause : BaseSetClause
	{
		public ConcatClause([NotNull] Type itemType, string itemName, [NotNull] Sequence sequence1,
			[NotNull] Sequence sequence2) : base(itemType, itemName, sequence1, sequence2)
		{
		}

		public override bool AllFieldsRequired => false;
	}
}
