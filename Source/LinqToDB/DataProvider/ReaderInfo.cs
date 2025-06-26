using System;
using System.Data.Common;
using System.Diagnostics;

using LinqToDB.Interceptors;

namespace LinqToDB.DataProvider
{
	// TODO: V6: refactor to readonly struct
	[DebuggerDisplay("DataReaderType={DataReaderType}, ToType={ToType}, ProviderFieldType={ProviderFieldType}, FieldType={FieldType}, DataTypeName={DataTypeName}")]
	public struct ReaderInfo : IEquatable<ReaderInfo>
	{
		int _hashCode;

		private Type? _toType;
		/// <summary>
		/// Expected type (e.g. type of property in mapped entity class). For nullable value types doesn't include <see cref="Nullable{T}"/> wrapper.
		/// </summary>
		public  Type?  ToType
		{
			readonly get => _toType;
			set { _toType = value; CalcHashCode(); }
		}

		private Type? _fieldType;
		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetFieldType(int)"/> for column.
		/// </summary>
		public  Type?  FieldType
		{
			readonly get => _fieldType;
			set { _fieldType = value; CalcHashCode(); }
		}

		private Type? _providerFieldType;
		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetProviderSpecificFieldType(int)"/> for column.
		/// </summary>
		public Type?  ProviderFieldType
		{
			readonly get => _providerFieldType;
			set { _providerFieldType = value; CalcHashCode(); }
		}

		private string? _dataTypeName;
		/// <summary>
		/// Type name, returned by <see cref="DbDataReader.GetDataTypeName(int)"/> for column.
		/// </summary>
		public string?  DataTypeName
		{
			readonly get => _dataTypeName;
			set { _dataTypeName = value?.ToLowerInvariant(); CalcHashCode(); }
		}

		private Type? _dataReaderType;
		/// <summary>
		/// Type of <see cref="DbDataReader"/> implementation. Could not match Type, implementated by ADO.NET provider if wrapper like MiniProfiler used without proper <see cref="IUnwrapDataObjectInterceptor"/> registration provided.
		/// </summary>
		public Type? DataReaderType
		{
			readonly get => _dataReaderType;
			set { _dataReaderType = value; CalcHashCode(); }
		}

		void CalcHashCode()
		{
			var hashCode = new HashCode();
			hashCode.Add(ToType);
			hashCode.Add(FieldType);
			hashCode.Add(ProviderFieldType);
			hashCode.Add(DataTypeName);
			hashCode.Add(DataReaderType);
			_hashCode = hashCode.ToHashCode();
		}

		public override bool Equals(object? obj)
		{
			return obj is ReaderInfo ri && Equals(ri);
		}

		public readonly override int GetHashCode()
		{
			return _hashCode;
		}

		public bool Equals(ReaderInfo other)
		{
			return
				ToType            == other.ToType &&
				FieldType         == other.FieldType &&
				ProviderFieldType == other.ProviderFieldType &&
				DataTypeName      == other.DataTypeName &&
				DataReaderType    == other.DataReaderType
				;
		}
	}
}
