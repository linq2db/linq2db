namespace LinqToDB.Linq.Builder
{
	using Mapping;

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

			public override IBuildContext Clone(CloningContext context)
			{
				return new OwnedContext(context.CloneContext(Owner), EntityDescriptor);
			}
		}
	}
}
