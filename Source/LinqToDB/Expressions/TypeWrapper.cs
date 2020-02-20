using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
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

		/// <summary>
		/// Creates property setter expression grom property getter.
		/// Limitation - property should have getter.
		/// </summary>
		protected static Expression<Action<TI, TP>> PropertySetter<TI, TP>(Expression<Func<TI, TP>> getter)
		{
			if (!(getter.Body is MemberExpression me)
				|| !(me.Member is PropertyInfo pi))
				throw new LinqToDBException($"Expected property accessor expression");

			var pThis  = Expression.Parameter(typeof(TI));
			var pValue = Expression.Parameter(typeof(TP));

			// use setter call instead of assign, as assign returns value and TypeMapper.BuildWrapper
			// produce Func instead of Action
			return Expression.Lambda<Action<TI, TP>>(
				Expression.Call(
					pThis,
					pi.SetMethod,
					pValue),
				pThis, pValue);
		}
	}
}
