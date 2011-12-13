using System;
using System.Data;
using System.Data.Common;

using Npgsql;

namespace LinqToDB.Data.DataProvider
{
	using Sql.SqlProvider;
	using LinqToDB.Mapping;

	public class PostgreSQLDataProvider : DataProviderBase
	{
		#region Configurable

		public enum CaseConvert
		{
			None,
			Lower,
			Upper
		}

		public static CaseConvert QueryCaseConvert = CaseConvert.None;

		public static bool QuoteIdentifiers
		{
			get { return PostgreSQLSqlProvider.QuoteIdentifiers; }
			set { PostgreSQLSqlProvider.QuoteIdentifiers = value; }
		}

		public override void Configure(System.Collections.Specialized.NameValueCollection attributes)
		{
			var quoteIdentifiers = attributes["QuoteIdentifiers"];

			if (quoteIdentifiers != null)
				QuoteIdentifiers = Common.Convert.ToBoolean(quoteIdentifiers);

			var queryCaseConcert = attributes["QueryCaseConvert"];
			if (queryCaseConcert != null)
			{
				try
				{
					QueryCaseConvert = (CaseConvert)Enum.Parse(typeof(CaseConvert), queryCaseConcert, true);
				}
				catch { }
			}

			base.Configure(attributes);
		}

		#endregion

		public override IDbConnection CreateConnectionObject()
		{
			return new NpgsqlConnection();
		}

		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new NpgsqlDataAdapter();
		}

		public override bool DeriveParameters(IDbCommand command)
		{
			NpgsqlCommandBuilder.DeriveParameters((NpgsqlCommand)command);
			return true;
		}

		public override void SetParameterValue(IDbDataParameter parameter, object value)
		{
			if(value is Enum)
			{
				var type = Enum.GetUnderlyingType(value.GetType());
				value = (MappingSchema ?? Map.DefaultSchema).ConvertChangeType(value, type);

			}
			base.SetParameterValue(parameter, value);
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is NpgsqlException)
					{
						var ex = (NpgsqlException)value;

						foreach (NpgsqlError error in ex.Errors)
							return error.Code;

						return 0;
					}

					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		public override Type ConnectionType
		{
			get { return typeof(NpgsqlConnection); }
		}

		public override string Name
		{
			get { return DataProvider.ProviderName.PostgreSQL; }
		}

		public override int MaxBatchSize
		{
			get { return 0; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new PostgreSQLSqlProvider();
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			if (QueryCaseConvert == CaseConvert.Lower)
				commandText = commandText.ToLower();
			else if (QueryCaseConvert == CaseConvert.Upper)
				commandText = commandText.ToUpper();

			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);
		}

		public override bool CanReuseCommand(IDbCommand command, CommandType newCommandType)
		{
			return command.CommandType == newCommandType;
		}

		public override IDataReader GetDataReader(MappingSchema schema, IDataReader dataReader)
		{
			return
				dataReader is NpgsqlDataReader
					? new NpgsqlDataReaderEx(schema, (NpgsqlDataReader)dataReader)
					: base.GetDataReader(schema, dataReader);
		}

		class NpgsqlDataReaderEx : IDataReader
		{
			private readonly NpgsqlDataReader _reader;
			private readonly MappingSchema _schema;

			public NpgsqlDataReaderEx(MappingSchema schema, NpgsqlDataReader reader)
			{
				_reader = reader;
				_schema = schema;
			}

			#region IDataReader Members

			public void Close()
			{
				_reader.Close();
			}

			public int Depth
			{
				get { return _reader.Depth; }
			}

			public DataTable GetSchemaTable()
			{
				return _reader.GetSchemaTable();
			}

			public bool IsClosed
			{
				get { return _reader.IsClosed; }
			}

			public bool NextResult()
			{
				return _reader.NextResult();
			}

			public bool Read()
			{
				return _reader.Read();
			}

			public int RecordsAffected
			{
				get { return _reader.RecordsAffected; }
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				_reader.Dispose();
			}

			#endregion

			#region IDataRecord Members

			public int FieldCount
			{
				get { return _reader.FieldCount; }
			}

			public bool GetBoolean(int i)
			{
				return _reader.GetBoolean(i);
			}

			public byte GetByte(int i)
			{
				return _schema.ConvertToByte(_reader.GetValue(i));
			}

			public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
			{
				return _reader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
			}

			public char GetChar(int i)
			{
				return _reader.GetChar(i);
			}

			public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
			{
				return _reader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
			}

			public IDataReader GetData(int i)
			{
				return _reader.GetData(i);
			}

			public string GetDataTypeName(int i)
			{
				return _reader.GetDataTypeName(i);
			}

			public DateTime GetDateTime(int i)
			{
				return _reader.GetDateTime(i);
			}

			public decimal GetDecimal(int i)
			{
				return _reader.GetDecimal(i);
			}

			public double GetDouble(int i)
			{
				return _reader.GetDouble(i);
			}

			public Type GetFieldType(int i)
			{
				return _reader.GetFieldType(i);
			}

			public float GetFloat(int i)
			{
				return _reader.GetFloat(i);
			}

			public Guid GetGuid(int i)
			{
				return _reader.GetGuid(i);
			}

			public short GetInt16(int i)
			{
				return _reader.GetInt16(i);
			}

			public int GetInt32(int i)
			{
				return _reader.GetInt32(i);
			}

			public long GetInt64(int i)
			{
				return _reader.GetInt64(i);
			}

			public string GetName(int i)
			{
				return _reader.GetName(i);
			}

			public int GetOrdinal(string name)
			{
				return _reader.GetOrdinal(name);
			}

			public string GetString(int i)
			{
				return _reader.GetString(i);
			}

			public object GetValue(int i)
			{
				return _reader.GetValue(i);
			}

			public int GetValues(object[] values)
			{
				return _reader.GetValues(values);
			}

			public bool IsDBNull(int i)
			{
				return _reader.IsDBNull(i);
			}

			public object this[string name]
			{
				get { return _reader[name]; }
			}

			public object this[int i]
			{
				get { return _reader[i]; }
			}

			#endregion
		}
	}
}
