using System;

using LinqToDB.Internal.Common;

namespace LinqToDB.Reflection
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			ArgumentNullException.ThrowIfNull(type);

			ObjectFactory = ActivatorExt.CreateInstance<IObjectFactory>(type);
		}

		public IObjectFactory ObjectFactory { get; }
	}
}
