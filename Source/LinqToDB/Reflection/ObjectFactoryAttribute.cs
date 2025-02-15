using System;

using LinqToDB.Common.Internal;

namespace LinqToDB.Reflection
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			ObjectFactory = ActivatorExt.CreateInstance<IObjectFactory>(type);
		}

		public IObjectFactory ObjectFactory { get; }
	}
}
