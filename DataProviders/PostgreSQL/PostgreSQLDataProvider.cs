using System;
using System.Data;

using Npgsql;
using NpgsqlTypes;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class PostgreSQLDataProvider : DataProviderBase
	{
		public PostgreSQLDataProvider(string name)
			: this(name, new PostgreSQLMappingSchema(name))
		{
		}

		public PostgreSQLDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SetCharField("bpchar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<NpgsqlDataReader,BitString        ,BitString>        ((r,i) => r.GetBitString  (i));
			SetProviderField<NpgsqlDataReader,NpgsqlInterval   ,NpgsqlInterval>   ((r,i) => r.GetInterval   (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTime       ,NpgsqlTime>       ((r,i) => r.GetTime       (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeTZ     ,NpgsqlTimeTZ>     ((r,i) => r.GetTimeTZ     (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeStamp  ,NpgsqlTimeStamp>  ((r,i) => r.GetTimeStamp  (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeStampTZ,NpgsqlTimeStampTZ>((r,i) => r.GetTimeStampTZ(i));
			SetProviderField<NpgsqlDataReader,NpgsqlDate       ,NpgsqlDate>       ((r,i) => r.GetDate       (i));
			SetProviderField<NpgsqlDataReader,NpgsqlInet       ,NpgsqlInet>       ((r,i) => (NpgsqlInet)      r.GetProviderSpecificValue(i));
			SetProviderField<NpgsqlDataReader,NpgsqlMacAddress ,NpgsqlMacAddress> ((r,i) => (NpgsqlMacAddress)r.GetProviderSpecificValue(i));

			SetProviderField2<NpgsqlDataReader,DateTimeOffset,NpgsqlTimeStampTZ>((r,i) => (NpgsqlTimeStampTZ)r.GetProviderSpecificValue(i));
		}

		public override Type ConnectionType { get { return typeof(NpgsqlConnection); } }
		public override Type DataReaderType { get { return typeof(NpgsqlDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new NpgsqlConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new PostgreSQLSqlProvider();
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : parameter.DbType = DbType.Int16;                                  break;
				case DataType.UInt16     : parameter.DbType = DbType.Int32;                                  break;
				case DataType.UInt32     : parameter.DbType = DbType.Int64;                                  break;
				case DataType.UInt64     : parameter.DbType = DbType.Decimal;                                break;
				case DataType.DateTime2  : parameter.DbType = DbType.DateTime;                               break;
				case DataType.VarNumeric : parameter.DbType = DbType.Decimal;                                break;
				case DataType.Decimal    :
				case DataType.Money      : break;
				case DataType.Image      :
				case DataType.Binary     :
				case DataType.VarBinary  : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Bytea;   break;
				case DataType.Boolean    : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Boolean; break;
				case DataType.Xml        : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Xml;     break;
				case DataType.Text       :
				case DataType.NText      : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Text;    break;
				default                  : base.SetParameterType(parameter, dataType);                       break;
			}
		}
	}
}
