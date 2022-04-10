using System;
using System.Data.Common;

namespace LinqToDB.DataProvider
{
	public readonly struct ReaderInfo : IEquatable<ReaderInfo>
	{
		private readonly int _hashCode;

		/// <summary>
		/// Exact type of <see cref="DbDataReader"/> implementation, used for read operation.
		/// </summary>
		public readonly Type?   DataReaderType;
		/// <summary>
		/// Target type (e.g. type of target property to which read data will be assigned).
		/// </summary>
		public readonly Type?   ToType;
		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetProviderSpecificFieldType(int)"/> method for column.
		/// </summary>
		public readonly Type?   ProviderFieldType;
		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetFieldType(int)"/> method for column.
		/// </summary>
		public readonly Type?   FieldType;
		/// <summary>
		/// Database type name, returned by <see cref="DbDataReader.GetDataTypeName(int)"/> method for column.
		/// Comparison done using <see cref="StringComparison.OrdinalIgnoreCase"/> mode.
		/// </summary>
		public readonly string? DataTypeName;

		public ReaderInfo(Type? dataReaderType = null, Type? toType = null, Type? providerFieldType = null, Type? fieldType = null, string? dataTypeName = null)
		{
			DataReaderType    = dataReaderType;
			ToType            = toType;
			ProviderFieldType = providerFieldType;
			FieldType         = fieldType;
			DataTypeName      = dataTypeName;

			unchecked
			{
				var hashCode = 639348056;
				hashCode = hashCode * -1521134295 + (DataReaderType?   .GetHashCode() ?? 0);
				hashCode = hashCode * -1521134295 + (ToType?           .GetHashCode() ?? 0);
				hashCode = hashCode * -1521134295 + (ProviderFieldType?.GetHashCode() ?? 0);
				hashCode = hashCode * -1521134295 + (FieldType?        .GetHashCode() ?? 0);
				hashCode = hashCode * -1521134295 + (DataTypeName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(DataTypeName) : 0);
				_hashCode = hashCode;
			}
		}

		public override bool Equals(object? obj) => obj is ReaderInfo ri && Equals(ri);
		public override int GetHashCode() => _hashCode;

		public bool Equals(ReaderInfo other)
		{
			return
				ToType            == other.ToType &&
				FieldType         == other.FieldType &&
				ProviderFieldType == other.ProviderFieldType &&
				DataTypeName      == other.DataTypeName &&
				StringComparer.OrdinalIgnoreCase.Equals(DataReaderType, other.DataReaderType)
				;
		}
	}
}
