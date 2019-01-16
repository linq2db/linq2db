using System;
using System.Collections.Generic;
using LinqToDB.Mapping;

namespace Tests.Playground
{
	public abstract class BaseEntity
	{
		private PropertyValue GetPropertyValue(string name)
		{
			var prop = GetType().GetProperty(name);
			if (prop == null)
				throw new Exception($"Property {name} not found");
			return new PropertyValue(this, prop);
		}

		public PropertyValue this[[JetBrains.Annotations.NotNull] string name]
		{
			get
			{
				if (string.IsNullOrEmpty(name))
					throw new ArgumentException("Value cannot be null or empty.", nameof(name));
				return GetPropertyValue(name);
			}
			set => GetPropertyValue(name).Value = value?.Value;
		}

		public IDictionary<string, PropertyValue> GetProperties(MappingSchema mappingSchema)
		{
			throw new NotImplementedException();
		}
	}
}
