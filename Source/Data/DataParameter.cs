using System;
using System.Collections;
using System.Data;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace LinqToDB.Data
{
	using Mapping;

	[ScalarType]
	public class DataParameter
	{
		public DataParameter()
		{
		}

		public DataParameter(string name, object value)
		{
			Name  = name;
			Value = value;
		}

		public DataParameter(string name, object value, DataType dataType)
		{
			Name     = name;
			Value    = value;
			DataType = dataType;
		}

		/// <summary>
		/// Gets or sets the <see cref="T:LinqToDB.DataType"/> of the parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:LinqToDB.DataType"/> values. The default is <see cref="F:LinqToDB.DataType.Undefined"/>.
		/// </returns>
		public DataType DataType { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:System.Data.ParameterDirection"/> values. The default is Input.
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
		/// Gets or sets the name of the <see cref="T:LinqToDB.Data.DataParameter"/>.
		/// </summary>
		/// <returns>
		/// The name of the <see cref="T:LinqToDB.Data.DataParameter"/>. The default is an empty string.
		/// </returns>
		public string Name { get; set; }

/*
		/// <summary>
		/// Gets or sets the maximum number of digits used to represent the <see cref="P:LinqToDB.Data.DataParameter.Value"/> property.
		/// </summary>
		/// <returns>
		/// The maximum number of digits used to represent the <see cref="P:LinqToDB.Data.DataParameter.Value"/> property. The default value is 0. This indicates that the data provider sets the precision for <see cref="P:System.Data.SqlClient.SqlParameter.Value"/>.
		/// </returns>
		public int Precision { get; set; }
*/

/*
		/// <summary>
		/// Gets or sets the number of decimal places to which <see cref="P:LinqToDB.Data.DataParameter.Value"/> is resolved.
		/// </summary>
		/// 
		/// <returns>
		/// The number of decimal places to which <see cref="P:LinqToDB.Data.DataParameter.Value"/> is resolved. The default is 0.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		public int Scale { get; set; }
*/

		/// <summary>
		/// Gets or sets the maximum size, in bytes, of the data within the column.
		/// </summary>
		/// 
		/// <returns>
		/// The maximum size, in bytes, of the data within the column. The default value is inferred from the parameter value.
		/// </returns>
		public int? Size { get; set; }

		/// <summary>
		/// Gets or sets the value of the parameter.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"/> that is the value of the parameter. The default value is null.
		/// </returns>
		public object Value { get; set; }

		public static DataParameter Char          (string name, char           value) { return new DataParameter { DataType = DataType.Char,           Name = name, Value = value, }; }
		public static DataParameter Char          (string name, string         value) { return new DataParameter { DataType = DataType.Char,           Name = name, Value = value, }; }
		public static DataParameter VarChar       (string name, char           value) { return new DataParameter { DataType = DataType.VarChar,        Name = name, Value = value, }; }
		public static DataParameter VarChar       (string name, string         value) { return new DataParameter { DataType = DataType.VarChar,        Name = name, Value = value, }; }
		public static DataParameter Text          (string name, string         value) { return new DataParameter { DataType = DataType.Text,           Name = name, Value = value, }; }
		public static DataParameter NChar         (string name, char           value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter NChar         (string name, string         value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter NVarChar      (string name, char           value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter NVarChar      (string name, string         value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter NText         (string name, string         value) { return new DataParameter { DataType = DataType.NText,          Name = name, Value = value, }; }
		public static DataParameter Binary        (string name, byte[]         value) { return new DataParameter { DataType = DataType.Binary,         Name = name, Value = value, }; }
		public static DataParameter Binary        (string name, Binary         value) { return new DataParameter { DataType = DataType.Binary,         Name = name, Value = value, }; }
		public static DataParameter Blob          (string name, byte[]         value) { return new DataParameter { DataType = DataType.Blob,           Name = name, Value = value, }; }
		public static DataParameter VarBinary     (string name, byte[]         value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter VarBinary     (string name, Binary         value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Image         (string name, byte[]         value) { return new DataParameter { DataType = DataType.Image,          Name = name, Value = value, }; }
		public static DataParameter Boolean       (string name, bool           value) { return new DataParameter { DataType = DataType.Boolean,        Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter SByte         (string name, sbyte          value) { return new DataParameter { DataType = DataType.SByte,          Name = name, Value = value, }; }
		public static DataParameter Int16         (string name, Int16          value) { return new DataParameter { DataType = DataType.Int16,          Name = name, Value = value, }; }
		public static DataParameter Int32         (string name, Int32          value) { return new DataParameter { DataType = DataType.Int32,          Name = name, Value = value, }; }
		public static DataParameter Int64         (string name, Int64          value) { return new DataParameter { DataType = DataType.Int64,          Name = name, Value = value, }; }
		public static DataParameter Byte          (string name, Byte           value) { return new DataParameter { DataType = DataType.Byte,           Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt16        (string name, UInt16         value) { return new DataParameter { DataType = DataType.UInt16,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt32        (string name, UInt32         value) { return new DataParameter { DataType = DataType.UInt32,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter UInt64        (string name, UInt64         value) { return new DataParameter { DataType = DataType.UInt64,         Name = name, Value = value, }; }
		public static DataParameter Single        (string name, Single         value) { return new DataParameter { DataType = DataType.Single,         Name = name, Value = value, }; }
		public static DataParameter Double        (string name, Double         value) { return new DataParameter { DataType = DataType.Double,         Name = name, Value = value, }; }
		public static DataParameter Decimal       (string name, Decimal        value) { return new DataParameter { DataType = DataType.Decimal,        Name = name, Value = value, }; }
		public static DataParameter Money         (string name, decimal        value) { return new DataParameter { DataType = DataType.Money,          Name = name, Value = value, }; }
		public static DataParameter SmallMoney    (string name, decimal        value) { return new DataParameter { DataType = DataType.SmallMoney,     Name = name, Value = value, }; }
		public static DataParameter Guid          (string name, Guid           value) { return new DataParameter { DataType = DataType.Guid,           Name = name, Value = value, }; }
		public static DataParameter Date          (string name, DateTime       value) { return new DataParameter { DataType = DataType.Date,           Name = name, Value = value, }; }
		public static DataParameter Time          (string name, TimeSpan       value) { return new DataParameter { DataType = DataType.Time,           Name = name, Value = value, }; }
		public static DataParameter DateTime      (string name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime,       Name = name, Value = value, }; }
		public static DataParameter DateTime2     (string name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime2,      Name = name, Value = value, }; }
		public static DataParameter SmallDateTime (string name, DateTime       value) { return new DataParameter { DataType = DataType.SmallDateTime,  Name = name, Value = value, }; }
		public static DataParameter DateTimeOffset(string name, DateTimeOffset value) { return new DataParameter { DataType = DataType.DateTimeOffset, Name = name, Value = value, }; }
		public static DataParameter Timestamp     (string name, byte[]         value) { return new DataParameter { DataType = DataType.Timestamp,      Name = name, Value = value, }; }
		public static DataParameter Xml           (string name, string         value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
		public static DataParameter Xml           (string name, XDocument      value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
#if !SILVERLIGHT && !NETFX_CORE
		public static DataParameter Xml           (string name, XmlDocument    value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
#endif
		public static DataParameter BitArray      (string name, BitArray       value) { return new DataParameter { DataType = DataType.BitArray,       Name = name, Value = value, }; }
		public static DataParameter Variant       (string name, object         value) { return new DataParameter { DataType = DataType.Variant,        Name = name, Value = value, }; }
		public static DataParameter VarNumeric    (string name, decimal        value) { return new DataParameter { DataType = DataType.VarNumeric,     Name = name, Value = value, }; }
		public static DataParameter Udt           (string name, object         value) { return new DataParameter { DataType = DataType.Udt,            Name = name, Value = value, }; }
		public static DataParameter Dictionary    (string name, IDictionary    value) { return new DataParameter { DataType = DataType.Dictionary,     Name = name, Value = value, }; }

		public static DataParameter Create        (string name, char           value) { return new DataParameter { DataType = DataType.NChar,          Name = name, Value = value, }; }
		public static DataParameter Create        (string name, string         value) { return new DataParameter { DataType = DataType.NVarChar,       Name = name, Value = value, }; }
		public static DataParameter Create        (string name, byte[]         value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Binary         value) { return new DataParameter { DataType = DataType.VarBinary,      Name = name, Value = value, }; }
		public static DataParameter Create        (string name, bool           value) { return new DataParameter { DataType = DataType.Boolean,        Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string name, sbyte          value) { return new DataParameter { DataType = DataType.SByte,          Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Int16          value) { return new DataParameter { DataType = DataType.Int16,          Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Int32          value) { return new DataParameter { DataType = DataType.Int32,          Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Int64          value) { return new DataParameter { DataType = DataType.Int64,          Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Byte           value) { return new DataParameter { DataType = DataType.Byte,           Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string name, UInt16         value) { return new DataParameter { DataType = DataType.UInt16,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string name, UInt32         value) { return new DataParameter { DataType = DataType.UInt32,         Name = name, Value = value, }; }
		[CLSCompliant(false)]
		public static DataParameter Create        (string name, UInt64         value) { return new DataParameter { DataType = DataType.UInt64,         Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Single         value) { return new DataParameter { DataType = DataType.Single,         Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Double         value) { return new DataParameter { DataType = DataType.Double,         Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Decimal        value) { return new DataParameter { DataType = DataType.Decimal,        Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Guid           value) { return new DataParameter { DataType = DataType.Guid,           Name = name, Value = value, }; }
		public static DataParameter Create        (string name, TimeSpan       value) { return new DataParameter { DataType = DataType.Time,           Name = name, Value = value, }; }
		public static DataParameter Create        (string name, DateTime       value) { return new DataParameter { DataType = DataType.DateTime2,      Name = name, Value = value, }; }
		public static DataParameter Create        (string name, DateTimeOffset value) { return new DataParameter { DataType = DataType.DateTimeOffset, Name = name, Value = value, }; }
		public static DataParameter Create        (string name, XDocument      value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
#if !SILVERLIGHT && !NETFX_CORE
		public static DataParameter Create        (string name, XmlDocument    value) { return new DataParameter { DataType = DataType.Xml,            Name = name, Value = value, }; }
#endif
		public static DataParameter Create        (string name, BitArray       value) { return new DataParameter { DataType = DataType.BitArray,       Name = name, Value = value, }; }
		public static DataParameter Create        (string name, Dictionary<string,string> value) { return new DataParameter { DataType = DataType.Dictionary,     Name = name, Value = value, }; }
	}
}
