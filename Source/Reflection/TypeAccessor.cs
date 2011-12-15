using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Mapping;
using LinqToDB.TypeBuilder;
using LinqToDB.TypeBuilder.Builders;

using JNotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Reflection
{
	public delegate object NullValueProvider(Type type);
	public delegate bool   IsNullHandler    (object obj);

	[DebuggerDisplay("Type = {Type}, OriginalType = {OriginalType}")]
	public abstract class TypeAccessor : ICollection<MemberAccessor>
	{
		#region Protected Emit Helpers

		protected MemberInfo GetMember(int memberType, string memberName)
		{
			const BindingFlags allInstaceMembers =
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			MemberInfo mi;

			switch (memberType)
			{
				case 1: mi = Type.GetField   (memberName, allInstaceMembers); break;
				case 2:
					mi =
						Type.        GetProperty(memberName, allInstaceMembers) ??
						OriginalType.GetProperty(memberName, allInstaceMembers);
					break;
				default:
					throw new InvalidOperationException();
			}

			return mi;
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
			throw new TypeBuilderException(string.Format(
				"The '{0}' type must have public default or init constructor.",
				OriginalType.Name));
		}

		[DebuggerStepThrough]
		public object CreateInstanceEx()
		{
			return _objectFactory != null ? _objectFactory.CreateInstance(this) : CreateInstance();
		}

		#endregion

		#region ObjectFactory

		private IObjectFactory _objectFactory;
		public  IObjectFactory  ObjectFactory
		{
			get { return _objectFactory;  }
			set { _objectFactory = value; }
		}

		#endregion

		#region Copy & AreEqual

		internal static object CopyInternal(object source, object dest, TypeAccessor ta)
		{
			foreach (MemberAccessor ma in ta)
				ma.CloneValue(source, dest);

			return dest;
		}

		public static object Copy(object source, object dest)
		{
			if (source == null) throw new ArgumentNullException("source");
			if (dest   == null) throw new ArgumentNullException("dest");

			TypeAccessor ta;

			var sType = source.GetType();
			var dType = dest.  GetType();

			if      (TypeHelper.IsSameOrParent(sType, dType)) ta = GetAccessor(sType);
			else if (TypeHelper.IsSameOrParent(dType, sType)) ta = GetAccessor(dType);
			else
				throw new ArgumentException();

			return CopyInternal(source, dest, ta);
		}

		public static object Copy(object source)
		{
			if (source == null) throw new ArgumentNullException("source");

			var ta = GetAccessor(source.GetType());

			return CopyInternal(source, ta.CreateInstanceEx(), ta);
		}

		public static bool AreEqual(object obj1, object obj2)
		{
			if (ReferenceEquals(obj1, obj2))
				return true;

			if (obj1 == null || obj2 == null)
				return false;

			TypeAccessor ta;

			var sType = obj1.GetType();
			var dType = obj2.GetType();

			if      (TypeHelper.IsSameOrParent(sType, dType)) ta = GetAccessor(sType);
			else if (TypeHelper.IsSameOrParent(dType, sType)) ta = GetAccessor(dType);
			else
				return false;

			foreach (MemberAccessor ma in ta)
				if ((!Equals(ma.GetValue(obj1), ma.GetValue(obj2))))
					return false;

			return true;
		}

		public static int GetHashCode(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			var hash = 0;

			foreach (MemberAccessor ma in GetAccessor(obj.GetType()))
			{
				var value = ma.GetValue(obj);
				hash = ((hash << 5) + hash) ^ (value == null ? 0 : value.GetHashCode());
			}

			return hash;
		}

		#endregion

		#region Abstract Members

		public abstract Type Type         { get; }
		public abstract Type OriginalType { get; }

		#endregion

		#region Items

		private readonly List<MemberAccessor>              _members     = new List<MemberAccessor>();
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

				if (IsAssociatedType(originalType))
					return _accessors[originalType];

				var instanceType = originalType;

				var accessorType = TypeFactory.GetType(originalType, originalType, new TypeAccessorBuilder(instanceType, originalType));

				accessor = (TypeAccessor)Activator.CreateInstance(accessorType);

				_accessors.Add(originalType, accessor);

				if (originalType != instanceType)
					_accessors.Add(instanceType, accessor);

				return accessor;
			}
		}

		public static TypeAccessor GetAccessor([JNotNull] object obj)
		{
			if (obj == null) throw new ArgumentNullException("obj");
			return GetAccessor(obj.GetType());
		}

		public static TypeAccessor GetAccessor<T>()
		{
			return TypeAccessor<T>.Instance;
		}

		private static bool IsAssociatedType(Type type)
		{
			if (AssociatedTypeHandler != null)
			{
				var child = AssociatedTypeHandler(type);

				if (child != null)
				{
					AssociateType(type, child);
					return true;
				}
			}

			return false;
		}

		public static object CreateInstance(Type type)
		{
			return GetAccessor(type).CreateInstance();
		}

		public static object CreateInstanceEx(Type type)
		{
			return GetAccessor(type).CreateInstanceEx();
		}

		public static T CreateInstance<T>()
		{
			return TypeAccessor<T>.CreateInstance();
		}

		public static T CreateInstanceEx<T>()
		{
			return TypeAccessor<T>.CreateInstanceEx();
		}

		public static TypeAccessor AssociateType(Type parent, Type child)
		{
			if (!TypeHelper.IsSameOrParent(parent, child))
				throw new ArgumentException(
					string.Format("'{0}' must be a base type of '{1}'", parent, child),
					"child");

			var accessor = GetAccessor(child);

			accessor = (TypeAccessor)Activator.CreateInstance(accessor.GetType());

			lock (_accessors)
				_accessors.Add(parent, accessor);

			return accessor;
		}

		public delegate Type GetAssociatedType(Type parent);
		public static event GetAssociatedType AssociatedTypeHandler;

		#endregion

		#region GetNullValue

		private static NullValueProvider _getNullValue = GetNullInternal;
		public  static NullValueProvider  GetNullValue
		{
			get { return _getNullValue ?? (_getNullValue = GetNullInternal);}
			set { _getNullValue = value; }
		}

		private static object GetNullInternal(Type type)
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

		#region ICollection Members

		void ICollection<MemberAccessor>.Add(MemberAccessor item)
		{
			_members.Add(item);
		}

		void ICollection<MemberAccessor>.Clear()
		{
			_members.Clear();
		}

		bool ICollection<MemberAccessor>.Contains(MemberAccessor item)
		{
			return _members.Contains(item);
		}

		void ICollection<MemberAccessor>.CopyTo(MemberAccessor[] array, int arrayIndex)
		{
			_members.CopyTo(array, arrayIndex);
		}

		bool ICollection<MemberAccessor>.Remove(MemberAccessor item)
		{
			return _members.Remove(item);
		}

		public int Count
		{
			get { return _members.Count; }
		}

		bool ICollection<MemberAccessor>.IsReadOnly
		{
			get { return ((ICollection<MemberAccessor>)_members).IsReadOnly; }
		}

		public int IndexOf(MemberAccessor ma)
		{
			return _members.IndexOf(ma);
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _members.GetEnumerator();
		}

		#endregion

		#region IEnumerable<MemberAccessor> Members

		IEnumerator<MemberAccessor> IEnumerable<MemberAccessor>.GetEnumerator()
		{
			foreach (var member in _members)
				yield return member;
		}

		#endregion

		#region Write Object Info

		public static void WriteDebug(object o)
		{
#if DEBUG
			Write(o, DebugWriteLine);
#endif
		}

		public static void WriteConsole(object o)
		{
			Write(o, Console.WriteLine);
		}

		private static string MapTypeName(Type type)
		{
			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
					return string.Format("{0}?", MapTypeName(Nullable.GetUnderlyingType(type)));

				var name = type.Name;
				var idx  = name.IndexOf('`');

				if (idx >= 0)
					name = name.Substring(0, idx);

				name += "<";

				foreach (var t in type.GetGenericArguments())
					name += MapTypeName(t) + ',';

				if (name[name.Length - 1] == ',')
					name = name.Substring(0, name.Length - 1);

				name += ">";

				return name;
			}

			if (type.IsPrimitive ||
				type == typeof(string) ||
				type == typeof(object) ||
				type == typeof(decimal))
			{
				if (type == typeof(int))    return "int";
				if (type == typeof(bool))   return "bool";
				if (type == typeof(short))  return "short";
				if (type == typeof(long))   return "long";
				if (type == typeof(ushort)) return "ushort";
				if (type == typeof(uint))   return "uint";
				if (type == typeof(ulong))  return "ulong";
				if (type == typeof(float))  return "float";

				return type.Name.ToLower();
			}

			return type.Name;
		}

		public delegate void WriteLine(string text);

		public static void Write(object o, WriteLine writeLine)
		{
			if (o == null)
			{
				writeLine("*** (null) ***");
				return;
			}

			MemberAccessor ma;

			var ta      = GetAccessor(o.GetType());
			var nameLen = 0;
			var typeLen = 0;

			foreach (var de in ta._memberNames)
			{
				if (nameLen < de.Key.Length)
					nameLen = de.Key.Length;

				ma = de.Value;

				if (typeLen < MapTypeName(ma.Type).Length)
					typeLen = MapTypeName(ma.Type).Length;
			}

			var text = "*** " + o.GetType().FullName + ": ***";

			writeLine(text);

			var format = string.Format("{{0,-{0}}} {{1,-{1}}} : {{2}}", typeLen, nameLen);

			foreach (var de in ta._memberNames)
			{
				ma = de.Value;

				var value = ma.GetValue(o);

				if (value == null)
					value = "(null)";
				else if (value is ICollection)
					value = string.Format("(Count = {0})", ((ICollection)value).Count);

				text = string.Format(format, MapTypeName(ma.Type), de.Key, value);

				writeLine(text);
			}

			writeLine("***");
		}

		#endregion
	}
}
