using System;
using System.ComponentModel;
using LinqToDB.Common;

namespace LinqToDB.Expressions
{
	public class TypeWrapper
	{
		// ReSharper disable InconsistentNaming
		// Names mangled to do not create collision with Wrapped class
		public object?    instance_ { get; }
		public TypeMapper mapper_   { get; } = null!;
		// ReSharper restore InconsistentNaming

		protected Delegate[] CompiledWrappers { get; } = null!;

		// never called
		protected TypeWrapper()
		{
		}

		public TypeWrapper(object? instance, TypeMapper mapper, Delegate[]? wrappers)
		{
			instance_        = instance;
			mapper_          = mapper ?? throw new ArgumentNullException(nameof(mapper));
			CompiledWrappers = wrappers ?? Array<Delegate>.Empty;
		}

		private EventHandlerList? _events;

		protected internal EventHandlerList Events
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
