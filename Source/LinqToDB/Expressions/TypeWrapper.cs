using System;
using System.ComponentModel;
using JetBrains.Annotations;

namespace LinqToDB.Expressions
{
	public class TypeWrapper
	{
		// ReSharper disable InconsistentNaming
		// Names mangled to do not create collision with Wrapped class
		public object?    instance_ { get; }
		public TypeMapper mapper_   { get; } = null!;
		// ReSharper restore InconsistentNaming

		public TypeWrapper()
		{
		}

		public TypeWrapper(object? instance, [NotNull] TypeMapper mapper)
		{
			instance_ = instance;
			mapper_   = mapper ?? throw new ArgumentNullException(nameof(mapper));
		}

		private EventHandlerList? _events;

		internal EventHandlerList Events
		{
			get
			{
				if (_events == null)
					_events = new EventHandlerList();

				return _events;
			}
		}
	}
}
