using System;
using JetBrains.Annotations;

namespace Tests.Playground.TypeMapping
{
	public class TypeWrapper
	{
		// ReSharper disable InconsistentNaming
		public object __Instance { get; }
		public TypeMapper __Mapper { get; }
		// ReSharper restore InconsistentNaming

		public TypeWrapper()
		{
		}

		public TypeWrapper(object instance, [NotNull] TypeMapper mapper)
		{
			__Instance = instance;
			__Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}
	}
}
