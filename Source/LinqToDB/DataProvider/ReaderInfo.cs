using System;
using System.Data.Common;
using System.Diagnostics;

using LinqToDB.Interceptors;

namespace LinqToDB.DataProvider
{
	[DebuggerDisplay("DataReaderType={DataReaderType}, ToType={ToType}, ProviderFieldType={ProviderFieldType}, FieldType={FieldType}, DataTypeName={DataTypeName}")]
	public readonly record struct ReaderInfo : IEquatable<ReaderInfo>
	{
		/// <summary>
		/// Expected type (e.g. type of property in mapped entity class). For nullable value types doesn't include <see cref="Nullable{T}"/> wrapper.
		/// </summary>
		public Type? ToType { get; init; }

		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetFieldType(int)"/> for column.
		/// </summary>
		public  Type? FieldType { get; init; }

		/// <summary>
		/// Type, returned by <see cref="DbDataReader.GetProviderSpecificFieldType(int)"/> for column.
		/// </summary>
		public Type? ProviderFieldType { get; init; }

		private readonly string? _dataTypeName;
		/// <summary>
		/// Type name, returned by <see cref="DbDataReader.GetDataTypeName(int)"/> for column.
		/// </summary>
		public string? DataTypeName
		{
			get => _dataTypeName;
			init { _dataTypeName = value?.ToLowerInvariant(); }
		}

		/// <summary>
		/// Type of <see cref="DbDataReader"/> implementation. Could not match Type, implementated by ADO.NET provider if wrapper like MiniProfiler used without proper <see cref="IUnwrapDataObjectInterceptor"/> registration provided.
		/// </summary>
		public Type? DataReaderType { get; init; }
	}
}
