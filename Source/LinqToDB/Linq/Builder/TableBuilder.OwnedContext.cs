using LinqToDB.Mapping;

namespace LinqToDB.Linq.Builder
{
	partial class TableBuilder
	{
		public class OwnedContext : PassThroughContext
		{
			public EntityDescriptor EntityDescriptor { get; }
			public IBuildContext    Owner            => Context;

			public OwnedContext(IBuildContext owner, EntityDescriptor entityDescriptor) : base(owner)
			{
				EntityDescriptor = entityDescriptor;
			}

		}
	}
}
