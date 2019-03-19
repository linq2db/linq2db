using System;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Parser.Clauses
{
	public class AssociationClause : BaseClause, IQuerySource
	{
		public AssociationClause([JetBrains.Annotations.NotNull] Type itemType,
			[JetBrains.Annotations.NotNull] string itemName,
			[JetBrains.Annotations.NotNull] IQuerySource parentSource,
			[JetBrains.Annotations.NotNull] IQuerySource associatedSource,
			[JetBrains.Annotations.NotNull] AssociationDescriptor descriptor,
			[JetBrains.Annotations.NotNull] Sequence innerSequence)
		{
			if (parentSource == null) throw new ArgumentNullException(nameof(parentSource));
			ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
			ItemName = itemName ?? throw new ArgumentNullException(nameof(itemName));
			ParentSource = parentSource ?? throw new ArgumentNullException(nameof(parentSource));
			AssociatedSource = associatedSource ?? throw new ArgumentNullException(nameof(associatedSource));
			Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
			InnerSequence = innerSequence ?? throw new ArgumentNullException(nameof(innerSequence));

			QuerySourceId = QuerySourceHelper.GetNexSourceId();
		}

		public IQuerySource ParentSource { get; }
		public IQuerySource AssociatedSource { get; }
		public AssociationDescriptor Descriptor { get; }
		public Sequence InnerSequence { get; }

		public int QuerySourceId { get; }
		public Type ItemType { get; }
		public string ItemName { get; }

		public override BaseClause Visit(Func<BaseClause, BaseClause> func)
		{
			throw new NotImplementedException();
		}

		public override bool VisitParentFirst(Func<BaseClause, bool> func)
		{
			throw new NotImplementedException();
		}
	}
}
