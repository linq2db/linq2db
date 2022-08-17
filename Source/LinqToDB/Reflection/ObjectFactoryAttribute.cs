﻿using System;

namespace LinqToDB.Reflection
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ObjectFactoryAttribute : Attribute
	{
		public ObjectFactoryAttribute(Type type)
		{
			if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(type));

			ObjectFactory = (Activator.CreateInstance(type) as IObjectFactory)!;

			if (ObjectFactory == null)
				ThrowHelper.ThrowArgumentException(nameof(type), $"Type '{type}' does not implement IObjectFactory interface.");
		}

		public IObjectFactory ObjectFactory { get; }
	}
}
