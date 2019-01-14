using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace Tests.Playground
{
	public abstract class BaseEntity
	{
		public PropertyValue this[string name]
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public IDictionary<string, PropertyValue> GetProperties(MappingSchema mappingSchema)
		{
			throw new NotImplementedException();
		}
	}
}