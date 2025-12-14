using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

namespace LinqToDB.Reflection
{
	[DebuggerDisplay("Type = {" + nameof(Type) + "}")]
	public abstract class TypeAccessor
	{
		#region Protected Emit Helpers

		protected void AddMember(MemberAccessor member)
		{
			if (member == null) throw new ArgumentNullException(nameof(member));

			Members.Add(member);
			_membersByName[member.MemberInfo.Name] = member;
		}

		#endregion

		#region CreateInstance

		[DebuggerStepThrough]
		public virtual object CreateInstance()
		{
			throw new LinqToDBException($"The '{Type.Name}' type must have public default or init constructor.");
		}

		[DebuggerStepThrough]
		public object CreateInstanceEx()
		{
			return ObjectFactory != null ? ObjectFactory.CreateInstance(this) : CreateInstance();
		}

		#endregion

		#region Public Members

		public IObjectFactory?         ObjectFactory { get; set; }
		public abstract Type           Type          { get; }

		#endregion

		#region Init
		private Expression?            _settersInitExpression;
		private ParameterExpression[]? _settersInitArguments;

		public Expression? GetSettersInitExpression(Expression instance)
		{
			return _settersInitExpression?.Transform(
				(parameters: _settersInitArguments!, instance),
				static (context, e) => e == context.parameters[0] ? context.instance : e);
		}

		internal void SetSettersInitExpression(EntityDescriptor entityDescriptor)
		{
			if (entityDescriptor.DynamicColumnStorageInitializer != null)
			{
				_settersInitArguments  = [Expression.Parameter(Type, "obj")];
				_settersInitExpression = entityDescriptor.DynamicColumnStorageInitializer.GetBody(_settersInitArguments[0]);
			}
		}

		#endregion

		#region Items

		public List<MemberAccessor>    Members       { get; } = new();

		readonly ConcurrentDictionary<string,MemberAccessor> _membersByName = new();

		public MemberAccessor this[string memberName] =>
			_membersByName.GetOrAdd(memberName, name =>
			{
				var ma = new MemberAccessor(this, name, null);
				Members.Add(ma);
				return ma;
			});

		public MemberAccessor? GetMemberByName(string memberName)
		{
			return _membersByName.TryGetValue(memberName, out var accessor) ? accessor : null;
		}

		public MemberAccessor this[int index] => Members[index];

		#endregion

		#region Static Members

		static readonly ConcurrentDictionary<Type,TypeAccessor> _accessors = new();

		public static TypeAccessor GetAccessor(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			if (_accessors.TryGetValue(type, out var accessor))
				return accessor;

			var accessorType = typeof(TypeAccessor<>).MakeGenericType(type);

			accessor = ActivatorExt.CreateInstance<TypeAccessor>(accessorType, true);

			_accessors[type] = accessor;

			return accessor;
		}

		public static TypeAccessor<T> GetAccessor<T>()
		{
			if (_accessors.TryGetValue(typeof(T), out var accessor))
				return (TypeAccessor<T>)accessor;
			return (TypeAccessor<T>)(_accessors[typeof(T)] = new TypeAccessor<T>());
		}

		#endregion
	}
}
