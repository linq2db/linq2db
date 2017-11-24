using System;

namespace LinqToDB.Reflection
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			ObjectFactory = Activator.CreateInstance(type) as IObjectFactory;

			if (ObjectFactory == null)
				throw new ArgumentException($"Type '{type}' does not implement IObjectFactory interface.");
		}

		public IObjectFactory ObjectFactory { get; }
	}
}
