using System;

namespace LinqToDB.Reflection
{
	using Common;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			_objectFactory = Activator.CreateInstance(type) as IObjectFactory;

			if (_objectFactory == null)
				throw new ArgumentException("Type '{0}' does not implement IObjectFactory interface.".Args(type));
		}

		private readonly IObjectFactory _objectFactory;
		public           IObjectFactory  ObjectFactory
		{
			get { return _objectFactory; }
		}
	}
}
