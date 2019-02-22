using System;
using System.Reflection;

namespace System.Data
{
	public static class DataRowExtensions
	{
		public static T Field<T>(this DataRow row, string columnName)
		{
			return UnboxT<T>.Unbox(row[columnName]);
		}

		public static T Field<T>(this DataRow row, DataColumn column)
		{
			return UnboxT<T>.Unbox(row[column]);
		}

		public static T Field<T>(this DataRow row, int columnIndex)
		{
			return UnboxT<T>.Unbox(row[columnIndex]);
		}

		public static T Field<T>(this DataRow row, int columnIndex, DataRowVersion version)
		{
			return UnboxT<T>.Unbox(row[columnIndex, version]);
		}

		public static T Field<T>(this DataRow row, string columnName, DataRowVersion version)
		{
			return UnboxT<T>.Unbox(row[columnName, version]);
		}

		public static T Field<T>(this DataRow row, DataColumn column, DataRowVersion version)
		{
			return UnboxT<T>.Unbox(row[column, version]);
		}

		public static void SetField<T>(this DataRow row, int columnIndex, T value)
		{
			row[columnIndex] = (object) value ?? (object) DBNull.Value;
		}

		public static void SetField<T>(this DataRow row, string columnName, T value)
		{
			row[columnName] = (object) value ?? (object) DBNull.Value;
		}

		public static void SetField<T>(this DataRow row, DataColumn column, T value)
		{
			row[column] = (object) value ?? (object) DBNull.Value;
		}

		static class UnboxT<T>
		{
			internal static readonly Converter<object,T> Unbox = Create(typeof(T));

			static Converter<object,T> Create(Type type)
			{
				if (!type.IsValueType)
					return ReferenceField;

				if (!type.IsGenericType || type.IsGenericTypeDefinition || !(typeof (Nullable<>) == type.GetGenericTypeDefinition()))
					return ValueField;

				return (Converter<object,T>)Delegate
					.CreateDelegate(
						typeof(Converter<object,T>),
						typeof(UnboxT<T>).GetMethod(nameof(NullableField),
						BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(type.GetGenericArguments()[0]));
			}

			static T ReferenceField(object value)
			{
				if (DBNull.Value != value)
					return (T) value;
				return default (T);
			}

			static T ValueField(object value)
			{
				if (DBNull.Value == value)
					throw new InvalidCastException();
				return (T) value;
			}

			static TElem? NullableField<TElem>(object value)
				where TElem : struct
			{
				if (DBNull.Value == value)
					return new TElem?();
				return (TElem)value;
			}
		}
	}
}
