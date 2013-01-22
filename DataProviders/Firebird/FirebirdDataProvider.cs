using System;
using System.Data;

using FirebirdSql.Data.FirebirdClient;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class FirebirdDataProvider : DataProviderBase
	{
		public FirebirdDataProvider() : base(new FirebirdMappingSchema())
		{
			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		public override string Name           { get { return ProviderName.Firebird; } }
		public override Type   ConnectionType { get { return typeof(FbConnection);  } }
		public override Type   DataReaderType { get { return typeof(FbDataReader);  } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new FbConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new FirebirdSqlProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is bool)
			{
				value = (bool)value ? "1" : "0";
				dataType = DataType.Char;
			}
			else if (value is Guid)
			{
				value = value.ToString();
				dataType = DataType.Char;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.DateTime   :
				case DataType.DateTime2  : ((FbParameter)parameter).FbDbType = FbDbType.TimeStamp; return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
