using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public sealed class ObjectMapperAttribute : Attribute
	{
		public ObjectMapperAttribute(Type objectMapperType)
		{
			if (objectMapperType == null) throw new ArgumentNullException("objectMapperType");

			_objectMapper = Activator.CreateInstance(objectMapperType) as ObjectMapper;

			if (_objectMapper == null)
				throw new ArgumentException(
					string.Format("Type '{0}' does not implement IObjectMapper interface.", objectMapperType));
		}

		private readonly ObjectMapper _objectMapper;
		public           ObjectMapper  ObjectMapper
		{
			get { return _objectMapper; }
		}
	}
}
