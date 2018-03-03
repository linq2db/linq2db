using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using JetBrains.Annotations;

namespace LinqToDB.Reflection
{
	[DebuggerDisplay("Type = {" + nameof(Type) + "}")]
	public abstract class TypeAccessor
	{
		#region Protected Emit Helpers

		protected void AddMember(MemberAccessor member)
		{
			if (member == null) throw new ArgumentNullException("member");

			_members.Add(member);
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

		public IObjectFactory ObjectFactory { get; set; }
		public abstract Type  Type          { get; }

		#endregion

		#region Items

		readonly List<MemberAccessor> _members = new List<MemberAccessor>();
		public   List<MemberAccessor>  Members
		{
			get { return _members; }
		}

		readonly ConcurrentDictionary<string,MemberAccessor> _membersByName = new ConcurrentDictionary<string,MemberAccessor>();

		public MemberAccessor this[string memberName]
		{
			get
			{
				return _membersByName.GetOrAdd(memberName, name =>
				{
					var ma = new MemberAccessor(this, name);
					Members.Add(ma);
					return ma;
				});
			}
		}

		public MemberAccessor this[int index]
		{
			get { return _members[index]; }
		}

		#endregion

		#region Static Members

		static readonly ConcurrentDictionary<Type,TypeAccessor> _accessors = new ConcurrentDictionary<Type,TypeAccessor>();

		public static TypeAccessor GetAccessor([NotNull] Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			TypeAccessor accessor;

			if (_accessors.TryGetValue(type, out accessor))
				return accessor;

			var accessorType = typeof(TypeAccessor<>).MakeGenericType(type);

			accessor = (TypeAccessor)Activator.CreateInstance(accessorType, true);

			_accessors[type] = accessor;

			return accessor;
		}

		public static TypeAccessor<T> GetAccessor<T>()
		{
			TypeAccessor accessor;

			if (_accessors.TryGetValue(typeof(T), out accessor))
				return (TypeAccessor<T>)accessor;

			return (TypeAccessor<T>)(_accessors[typeof(T)] = new TypeAccessor<T>());
		}

		#endregion
	}
}
