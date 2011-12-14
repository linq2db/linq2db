using System;

namespace LinqToDB.Reflection
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			_objectFactory = Activator.CreateInstance(type) as IObjectFactory;

			if (_objectFactory == null)
				throw new ArgumentException(
					string.Format("Type '{0}' does not implement IObjectFactory interface.", type));
		}

		private readonly IObjectFactory _objectFactory;
		public           IObjectFactory  ObjectFactory
		{
			get { return _objectFactory; }
		}
	}
}
