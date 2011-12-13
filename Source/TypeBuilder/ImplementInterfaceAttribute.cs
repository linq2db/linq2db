using System;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.TypeBuilder.Builders;

namespace LinqToDB.TypeBuilder
{
	[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
	public class ImplementInterfaceAttribute : AbstractTypeBuilderAttribute
	{
		public ImplementInterfaceAttribute(Type type)
		{
			_type = type;
		}

		private readonly Type _type;
		public           Type  Type
		{
			get { return _type;  }
		}

		public override IAbstractTypeBuilder TypeBuilder
		{
			get { return new ImplementInterfaceBuilder(_type); }
		}
	}
}
