using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.Data
{
	using LinqToDB.Common;
	using Mapping;

	[ScalarType]
	public class DataParameter
	{
		private DbDataType? _dbDataType;

		public DataParameter()
		{
		}

		public DataParameter(string? name, object? value)
		{
			Name  = name;
			Value = value;
		}

		public DataParameter(string? name, object? value, DbDataType dbDataType)
		{
			Name        = name;
			Value       = value;
			_dbDataType = dbDataType;
		}

		public DataParameter(string? name, object? value, DataType dataType)
		{
			Name     = name;
			Value    = value;
			DataType = dataType;
		}

		public DataParameter(string? name, object? value, DataType dataType, string? dbType)
		{
			Name     = name;
			Value    = value;
			DataType = dataType;
			DbType   = dbType;
		}

		public DataParameter(string? name, object? value, string dbType)
		{
			Name     = name;
			Value    = value;
			DbType   = dbType;
		}

		/// <summary>
		/// Gets or sets the <see cref="LinqToDB.DataType"/> of the parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="LinqToDB.DataType"/> values. The default is <see cref="DataType.Undefined"/>.
		/// </returns>
		public DataType DataType
		{
			get => _dbDataType?.DataType ?? DataType.Undefined;
			set => _dbDataType = DbDataType.WithDataType(value);
		}

		/// <summary>
		/// Gets or sets Database Type name of the parameter.
		/// </summary>
		/// <returns>
		/// Name of Database Type or empty string.
		/// </returns>
		public string? DbType
		{
			get => _dbDataType?.DbType;
			set => _dbDataType = DbDataType.WithDbType(value);
		}

		/// <summary>
		/// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="ParameterDirection"/> values. The default is Input.
		/// </returns>
		public ParameterDirection? Direction { get; set; }

		/*
				/// <summary>
				/// Gets a value indicating whether the parameter accepts null values.
				/// </summary>
				/// <returns>
				/// true if null values are accepted; otherwise, false. The default is false.
				/// </returns>
				public bool IsNullable { get; set; }
		*/

		/// <summary>
		/// Gets or sets the name of the <see cref="DataParameter"/>.
		/// </summary>
		/// <returns>
		/// The name of the <see cref="DataParameter"/>. The default is an empty string.
		/// </returns>
		public string? Name { get; set; }

		public bool IsArray { get; set; }

		/// <summary>
		/// Gets or sets precision for parameter type.
		/// </summary>
		public int? Precision
		{
			get => _dbDataType?.Precision;
			set => _dbDataType = DbDataType.WithPrecision(value);
		}

		/// <summary>
		/// Gets or sets scale for parameter type.
		/// </summary>
		public int? Scale
		{
			get => _dbDataType?.Scale;
			set => _dbDataType = DbDataType.WithScale(value);
		}

		/// <summary>
		/// Gets or sets the maximum size, in bytes, of the data within the column.
		/// </summary>
		///
		/// <returns>
		/// The maximum size, in bytes, of the data within the column. The default value is inferred from the parameter value.
		/// </returns>
		public int? Size
		{
			get => _dbDataType?.Length;
			set => _dbDataType = DbDataType.WithLength(value);
		}

		/// <summary>
		/// Gets or sets the value of the parameter.
		/// </summary>
		/// <returns>
		/// An <see cref="object"/> that is the value of the parameter. The default value is null.
		/// </returns>
		public object? Value { get; set; }

		/// <summary>
		/// Provider's parameter instance for out, in-out, return parameters.
		/// Could be used to read parameter value for complex types like Oracle's BFile.
		/// </summary>
		public DbParameter? Output { get; internal set; }

		/// <summary>
		/// Parameter <see cref="DbDataType"/> type.
		/// </summary>
		public DbDataType DbDataType
		{
			get => _dbDataType ??= new DbDataType(Value?.GetType() ?? typeof(object), DataType, DbType, Size, Precision, Scale);
			set => _dbDataType = value;
		}

		internal DbDataType GetOrSetDbDataType(DbDataType? columnType) => _dbDataType ?? columnType ?? DbDataType;

		public static DataParameter Char          (string? name, char           value) { return new DataParameter { DataType = DataType.Char,           Name = name, Value = value, }; }
		public static DataParameter Char          (string? name, string?        value) { return new DataParameter { DataType = DataType.Char,           Name = name, Value = value, }; }
		public static DataParameter VarChar       (string? name, char           value) { return new DataParameter { DataType = DataType.VarChar,        Name = name, Value = value, }; }
		public static DataParameter VarChar       (string? name, string?        value) { return new DataParameter { DataType = DataType.VarChar,        Name = name, Value = value, }; }
		public static DataParameter Text          (string? name, string?        value) { return new DataParameter { DataType = DataType.Text,           Name = name, Value = value, }; }
		public static DataParameter NChar         (string? name, char           value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter NChar         (string? name, string?        value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter NVarChar      (string? name, char           value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter NVarChar      (string? name, string?        value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter NText         (string? name, string?        value) { return new DataParameter { DataType = DataType.NText,          Name = name, Value = value, }; }
		public static DataParameter Binary        (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.Binary,         Name = name, Value = value, }; }
		public static DataParameter Binary        (string? name, Binary?        value) { return new DataParameter { DataType = DataType.Binary,         Name = name, Value = value, }; }
		public static DataParameter Blob          (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.Blob,           Name = name, Value = value, }; }
		public static DataParameter VarBinary     (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter VarBinary     (string? name, Binary?        value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Image         (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.Image,          Name = name, Value = value, }; }
		public static DataParameter Boolean       (string? name, bool           value) { return new DataParameter { DataType = DataType.Boolean,        Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter SByte         (string? name, sbyte          value) { return new DataParameter { DataType = DataType.SByte,          Name = name, Value = value, }; }
		public static DataParameter Int16         (string? name, short          value) { return new DataParameter { DataType = DataType.Int16,          Name = name, Value = value, }; }
		public static DataParameter Int32         (string? name, int            value) { return new DataParameter { DataType = DataType.Int32,          Name = name, Value = value, }; }
		public static DataParameter Int64         (string? name, long           value) { return new DataParameter { DataType = DataType.Int64,          Name = name, Value = value, }; }
		public static DataParameter Byte          (string? name, byte           value) { return new DataParameter { DataType = DataType.Byte,           Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt16        (string? name, ushort         value) { return new DataParameter { DataType = DataType.UInt16,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt32        (string? name, uint           value) { return new DataParameter { DataType = DataType.UInt32,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt64        (string? name, ulong          value) { return new DataParameter { DataType = DataType.UInt64,         Name = name, Value = value, }; }
		public static DataParameter Single        (string? name, float          value) { return new DataParameter { DataType = DataType.Single,         Name = name, Value = value, }; }
		public static DataParameter Double        (string? name, double         value) { return new DataParameter { DataType = DataType.Double,         Name = name, Value = value, }; }
		public static DataParameter Decimal       (string? name, decimal        value) { return new DataParameter { DataType = DataType.Decimal,        Name = name, Value = value, }; }
		public static DataParameter Money         (string? name, decimal        value) { return new DataParameter { DataType = DataType.Money,          Name = name, Value = value, }; }
		public static DataParameter SmallMoney    (string? name, decimal        value) { return new DataParameter { DataType = DataType.SmallMoney,     Name = name, Value = value, }; }
		public static DataParameter Guid          (string? name, Guid           value) { return new DataParameter { DataType = DataType.Guid,           Name = name, Value = value, }; }
		public static DataParameter Date          (string? name, DateTime       value) { return new DataParameter { DataType = DataType.Date,           Name = name, Value = value, }; }
		public static DataParameter Time          (string? name, TimeSpan       value) { return new DataParameter { DataType = DataType.Time,           Name = name, Value = value, }; }
		public static DataParameter DateTime      (string? name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime,       Name = name, Value = value, }; }
		public static DataParameter DateTime2     (string? name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime2,      Name = name, Value = value, }; }
		public static DataParameter SmallDateTime (string? name, DateTime       value) { return new DataParameter { DataType = DataType.SmallDateTime,  Name = name, Value = value, }; }
		public static DataParameter DateTimeOffset(string? name, DateTimeOffset value) { return new DataParameter { DataType = DataType.DateTimeOffset, Name = name, Value = value, }; }
		public static DataParameter Timestamp     (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.Timestamp,      Name = name, Value = value, }; }
		public static DataParameter Xml           (string? name, string?        value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter Xml           (string? name, XDocument?     value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter Xml           (string? name, XmlDocument?   value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter BitArray      (string? name, BitArray?      value) { return new DataParameter { DataType = DataType.BitArray,       Name = name, Value = value, }; }
		public static DataParameter Variant       (string? name, object?        value) { return new DataParameter { DataType = DataType.Variant,        Name = name, Value = value, }; }
		public static DataParameter VarNumeric    (string? name, decimal        value) { return new DataParameter { DataType = DataType.VarNumeric,     Name = name, Value = value, }; }
		public static DataParameter Udt           (string? name, object?        value) { return new DataParameter { DataType = DataType.Udt,            Name = name, Value = value, }; }
		public static DataParameter Dictionary    (string? name, IDictionary?   value) { return new DataParameter { DataType = DataType.Dictionary,     Name = name, Value = value, }; }

		public static DataParameter Create        (string? name, char           value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, string?        value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, byte[]?        value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, Binary?        value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, bool           value) { return new DataParameter { DataType = DataType.Boolean,        Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, sbyte          value) { return new DataParameter { DataType = DataType.SByte,          Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, short          value) { return new DataParameter { DataType = DataType.Int16,          Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, int            value) { return new DataParameter { DataType = DataType.Int32,          Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, long           value) { return new DataParameter { DataType = DataType.Int64,          Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, byte           value) { return new DataParameter { DataType = DataType.Byte,           Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, ushort         value) { return new DataParameter { DataType = DataType.UInt16,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, uint           value) { return new DataParameter { DataType = DataType.UInt32,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string? name, ulong          value) { return new DataParameter { DataType = DataType.UInt64,         Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, float          value) { return new DataParameter { DataType = DataType.Single,         Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, double         value) { return new DataParameter { DataType = DataType.Double,         Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, decimal        value) { return new DataParameter { DataType = DataType.Decimal,        Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, Guid           value) { return new DataParameter { DataType = DataType.Guid,           Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, TimeSpan       value) { return new DataParameter { DataType = DataType.Time,           Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime2,      Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, DateTimeOffset value) { return new DataParameter { DataType = DataType.DateTimeOffset, Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, XDocument?     value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, XmlDocument?   value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, BitArray?      value) { return new DataParameter { DataType = DataType.BitArray,       Name = name, Value = value, }; }
		public static DataParameter Create        (string? name, Dictionary<string,string>? value) { return new DataParameter { DataType = DataType.Dictionary,     Name = name, Value = value, }; }
		public static DataParameter Json          (string? name, string?        value) { return new DataParameter { DataType = DataType.Json,           Name = name, Value = value,}; }
		public static DataParameter BinaryJson    (string? name, string?        value) { return new DataParameter { DataType = DataType.BinaryJson,     Name = name, Value = value, }; }
	}
}
