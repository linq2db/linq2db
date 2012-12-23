using System;

using LinqToDB.Mapping;

namespace LinqToDB.Data
{
	[ScalarType]
	public class DataParameter
	{
		/// <summary>
		/// Gets or sets the <see cref="T:LinqToDB.DataType"/> of the parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:LinqToDB.DataType"/> values. The default is <see cref="F:LinqToDB.DataType.Undefined"/>.
		/// </returns>
		public DataType DataType { get; set; }

/*
		/// <summary>
		/// Gets or sets a value indicating whether the parameter is input-only, output-only, bidirectional, or a stored procedure return value parameter.
		/// </summary>
		/// <returns>
		/// One of the <see cref="T:System.Data.ParameterDirection"/> values. The default is Input.
		/// </returns>
		public ParameterDirection Direction { get; set; }
*/

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

/*
		/// <summary>
		/// Gets or sets the maximum size, in bytes, of the data within the column.
		/// </summary>
		/// 
		/// <returns>
		/// The maximum size, in bytes, of the data within the column. The default value is inferred from the parameter value.
		/// </returns>
		public int Size { get; set; }
*/

		/// <summary>
		/// Gets or sets the value of the parameter.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Object"/> that is the value of the parameter. The default value is null.
		/// </returns>
		public object Value { get; set; }

		public static DataParameter Char(string name, char value)
		{
			return new DataParameter { DataType = DataType.Char, Name = name, Value = value, };
		}

		public static DataParameter Char(string name, string value)
		{
			return new DataParameter { DataType = DataType.Char, Name = name, Value = value, };
		}

		public static DataParameter VarChar(string name, char value)
		{
			return new DataParameter { DataType = DataType.VarChar, Name = name, Value = value, };
		}

		public static DataParameter VarChar(string name, string value)
		{
			return new DataParameter { DataType = DataType.VarChar, Name = name, Value = value, };
		}

		public static DataParameter Text(string name, string value)
		{
			return new DataParameter { DataType = DataType.Text, Name = name, Value = value, };
		}

		public static DataParameter NChar(string name, char value)
		{
			return new DataParameter { DataType = DataType.NChar, Name = name, Value = value, };
		}

		public static DataParameter NChar(string name, string value)
		{
			return new DataParameter { DataType = DataType.NChar, Name = name, Value = value, };
		}

		public static DataParameter NVarChar(string name, char value)
		{
			return new DataParameter { DataType = DataType.NVarChar, Name = name, Value = value, };
		}

		public static DataParameter NVarChar(string name, string value)
		{
			return new DataParameter { DataType = DataType.NVarChar, Name = name, Value = value, };
		}

		public static DataParameter NText(string name, string value)
		{
			return new DataParameter { DataType = DataType.NText, Name = name, Value = value, };
		}

		public static DataParameter Binary(string name, byte[] value)
		{
			return new DataParameter { DataType = DataType.Binary, Name = name, Value = value, };
		}

		public static DataParameter VarBinary(string name, byte[] value)
		{
			return new DataParameter { DataType = DataType.VarBinary, Name = name, Value = value, };
		}


		public static DataParameter Create(string name, char value)
		{
			return new DataParameter { DataType = DataType.NChar, Name = name, Value = value, };
		}

		public static DataParameter Create(string name, string value)
		{
			return new DataParameter { DataType = DataType.NVarChar, Name = name, Value = value, };
		}

		public static DataParameter Create(string name, byte[] value)
		{
			return new DataParameter { DataType = DataType.VarBinary, Name = name, Value = value, };
		}
	}
}
