using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace LinqToDB.Reflection
{
	public delegate object NullValueProvider(Type type);
	public delegate bool   IsNullHandler    (object obj);

	[DebuggerDisplay("Type = {Type}, OriginalType = {OriginalType}")]
	public abstract class TypeAccessor
	{
		#region Protected Emit Helpers

		protected MemberInfo GetMember(int memberType, string memberName)
		{
			const BindingFlags allInstaceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			switch (memberType)
			{
				case 1 : return Type.GetField   (memberName, allInstaceMembers);
				case 2 : return Type.GetProperty(memberName, allInstaceMembers);
				default: throw new InvalidOperationException();
			}
		}

		protected void AddMember(MemberAccessor member)
		{
			if (member == null) throw new ArgumentNullException("member");

			_members.Add(member);
			_memberNames.Add(member.MemberInfo.Name, member);
		}

		#endregion

		#region CreateInstance

		[DebuggerStepThrough]
		public virtual object CreateInstance()
		{
			throw new LinqToDBException(string.Format("The '{0}' type must have public default or init constructor.", Type.Name));
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

		private readonly Dictionary<string,MemberAccessor> _memberNames = new Dictionary<string,MemberAccessor>();

		public MemberAccessor this[string memberName]
		{
			get
			{
				MemberAccessor ma;
				return _memberNames.TryGetValue(memberName, out ma) ? ma : null;
			}
		}

		public MemberAccessor this[int index]
		{
			get { return _members[index]; }
		}

		#endregion

		#region Static Members

		private static readonly Dictionary<Type,TypeAccessor> _accessors = new Dictionary<Type,TypeAccessor>(10);

		public static TypeAccessor GetAccessor(Type originalType)
		{
			if (originalType == null) throw new ArgumentNullException("originalType");

			lock (_accessors)
			{
				TypeAccessor accessor;

				if (_accessors.TryGetValue(originalType, out accessor))
					return accessor;

				var accessorType = typeof(ExprTypeAccessor<>).MakeGenericType(originalType);

				accessor = (TypeAccessor)Activator.CreateInstance(accessorType);

				_accessors.Add(originalType, accessor);

				return accessor;
			}
		}

		#endregion
	}
}
