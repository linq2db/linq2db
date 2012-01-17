using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Mapping;

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

		public MemberAccessor this[NameOrIndexParameter nameOrIndex]
		{
			get
			{
				return nameOrIndex.ByName ? _memberNames[nameOrIndex.Name] : _members[nameOrIndex.Index];
			}
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

		#region GetNullValue

		private static NullValueProvider _getNullValue = GetNullInternal;
		public  static NullValueProvider  GetNullValue
		{
			get { return _getNullValue ?? (_getNullValue = GetNullInternal);}
			set { _getNullValue = value; }
		}

		static object GetNullInternal(Type type)
		{
			if (type == null) throw new ArgumentNullException("type");

			if (type.IsValueType)
			{
				if (type.IsEnum)
					return GetEnumNullValue(type);

				if (type.IsPrimitive)
				{
					if (type == typeof(Int32))          return Common.Configuration.NullableValues.Int32;
					if (type == typeof(Double))         return Common.Configuration.NullableValues.Double;
					if (type == typeof(Int16))          return Common.Configuration.NullableValues.Int16;
					if (type == typeof(Boolean))        return Common.Configuration.NullableValues.Boolean;
					if (type == typeof(SByte))          return Common.Configuration.NullableValues.SByte;
					if (type == typeof(Int64))          return Common.Configuration.NullableValues.Int64;
					if (type == typeof(Byte))           return Common.Configuration.NullableValues.Byte;
					if (type == typeof(UInt16))         return Common.Configuration.NullableValues.UInt16;
					if (type == typeof(UInt32))         return Common.Configuration.NullableValues.UInt32;
					if (type == typeof(UInt64))         return Common.Configuration.NullableValues.UInt64;
					if (type == typeof(Single))         return Common.Configuration.NullableValues.Single;
					if (type == typeof(Char))           return Common.Configuration.NullableValues.Char;
				}
				else
				{
					if (type == typeof(DateTime))       return Common.Configuration.NullableValues.DateTime;
					if (type == typeof(DateTimeOffset)) return Common.Configuration.NullableValues.DateTimeOffset;
					if (type == typeof(Decimal))        return Common.Configuration.NullableValues.Decimal;
					if (type == typeof(Guid))           return Common.Configuration.NullableValues.Guid;

#if !SILVERLIGHT

					if (type == typeof(SqlInt32))       return SqlInt32.   Null;
					if (type == typeof(SqlString))      return SqlString.  Null;
					if (type == typeof(SqlBoolean))     return SqlBoolean. Null;
					if (type == typeof(SqlByte))        return SqlByte.    Null;
					if (type == typeof(SqlDateTime))    return SqlDateTime.Null;
					if (type == typeof(SqlDecimal))     return SqlDecimal. Null;
					if (type == typeof(SqlDouble))      return SqlDouble.  Null;
					if (type == typeof(SqlGuid))        return SqlGuid.    Null;
					if (type == typeof(SqlInt16))       return SqlInt16.   Null;
					if (type == typeof(SqlInt64))       return SqlInt64.   Null;
					if (type == typeof(SqlMoney))       return SqlMoney.   Null;
					if (type == typeof(SqlSingle))      return SqlSingle.  Null;
					if (type == typeof(SqlBinary))      return SqlBinary.  Null;

#endif
				}
			}
			else
			{
				if (type == typeof(String)) return Common.Configuration.NullableValues.String;
				if (type == typeof(DBNull)) return DBNull.Value;
				if (type == typeof(Stream)) return Stream.Null;
#if !SILVERLIGHT
				if (type == typeof(SqlXml)) return SqlXml.Null;
#endif
			}

			return null;
		}

		const FieldAttributes EnumField = FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal;

		static readonly Dictionary<Type,object> _nullValues = new Dictionary<Type,object>();

		static object GetEnumNullValue(Type type)
		{
			object nullValue;

			lock (_nullValues)
				if (_nullValues.TryGetValue(type, out nullValue))
					return nullValue;

			var fields = type.GetFields();

			foreach (var fi in fields)
			{
				if ((fi.Attributes & EnumField) == EnumField)
				{
					var attrs = Attribute.GetCustomAttributes(fi, typeof(NullValueAttribute));

					if (attrs.Length > 0)
					{
						nullValue = Enum.Parse(type, fi.Name, false);
						break;
					}
				}
			}

			lock (_nullValues)
				if (!_nullValues.ContainsKey(type))
					_nullValues.Add(type, nullValue);

			return nullValue;
		}

		private static IsNullHandler _isNull = IsNullInternal;
		public  static IsNullHandler  IsNull
		{
			get { return _isNull ?? (_isNull = IsNullInternal); }
			set { _isNull = value; }
		}

		private static bool IsNullInternal(object value)
		{
			if (value == null)
				return true;

			var nullValue = GetNullValue(value.GetType());

			return nullValue != null && value.Equals(nullValue);
		}

		#endregion
	}
}
