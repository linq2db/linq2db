using System;

namespace LinqToDB.TypeBuilder.Builders
{
	public abstract class AbstractTypeBuilderAttribute : Attribute
	{
		public abstract IAbstractTypeBuilder TypeBuilder { get; }
	}
}
