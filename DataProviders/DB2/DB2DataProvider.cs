using System;
using System.Data;

using IBM.Data.DB2;
using IBM.Data.DB2Types;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SqlProvider;

	public class DB2DataProvider : DataProviderBase
	{
		public DB2DataProvider()
			: this(ProviderName.DB2, new DB2MappingSchema())
		{
		}

		protected DB2DataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter       = false;
			SqlProviderFlags.AcceptsTakeAsParameterIfSkip = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<DB2DataReader,DB2Int64,       Int64>    ((r,i) => r.GetDB2Int64       (i));
			SetProviderField<DB2DataReader,DB2Int32,       Int32>    ((r,i) => r.GetDB2Int32       (i));
			SetProviderField<DB2DataReader,DB2Int16,       Int16>    ((r,i) => r.GetDB2Int16       (i));
			SetProviderField<DB2DataReader,DB2Decimal,     Decimal>  ((r,i) => r.GetDB2Decimal     (i));
			SetProviderField<DB2DataReader,DB2DecimalFloat,Decimal>  ((r,i) => r.GetDB2DecimalFloat(i));
			SetProviderField<DB2DataReader,DB2Real,        Single>   ((r,i) => r.GetDB2Real        (i));
			SetProviderField<DB2DataReader,DB2Real370,     Single>   ((r,i) => r.GetDB2Real370     (i));
			SetProviderField<DB2DataReader,DB2Double,      Double>   ((r,i) => r.GetDB2Double      (i));
			SetProviderField<DB2DataReader,DB2String,      String>   ((r,i) => r.GetDB2String      (i));
			SetProviderField<DB2DataReader,DB2Clob,        String>   ((r,i) => r.GetDB2Clob        (i));
			SetProviderField<DB2DataReader,DB2Binary,      byte[]>   ((r,i) => r.GetDB2Binary      (i));
			SetProviderField<DB2DataReader,DB2Blob,        byte[]>   ((r,i) => r.GetDB2Blob        (i));
			SetProviderField<DB2DataReader,DB2Date,        DateTime> ((r,i) => r.GetDB2Date        (i));
			SetProviderField<DB2DataReader,DB2Time,        TimeSpan> ((r,i) => r.GetDB2Time        (i));
			SetProviderField<DB2DataReader,DB2TimeStamp,   DateTime> ((r,i) => r.GetDB2TimeStamp   (i));
			SetProviderField<DB2DataReader,DB2Xml,         string>   ((r,i) => r.GetDB2Xml         (i));
			SetProviderField<DB2DataReader,DB2RowId,       byte[]>   ((r,i) => r.GetDB2RowId       (i));
		}

		public override Type ConnectionType { get { return typeof(DB2Connection); } }
		public override Type DataReaderType { get { return typeof(DB2DataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new DB2Connection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new DB2SqlProvider(SqlProviderFlags);
		}

		public override void InitCommand(DataConnection dataConnection)
		{
			dataConnection.Command = null;
			base.InitCommand(dataConnection);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is sbyte)
			{
				value    = (short)(sbyte)value;
				dataType = DataType.Int16;
			}
			else if (value is byte)
			{
				value    = (short)(byte)value;
				dataType = DataType.Int16;
			}

			switch (dataType)
			{
				case DataType.UInt16     : dataType = DataType.Int32;    break;
				case DataType.UInt32     : dataType = DataType.Int64;    break;
				case DataType.UInt64     : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;  break;
				case DataType.DateTime2  : dataType = DataType.DateTime; break;
				case DataType.Char       :
				case DataType.VarChar    :
				case DataType.NChar      :
				case DataType.NVarChar   :
					     if (value is Guid) value = ((Guid)value).ToString();
					else if (value is bool)
						value = Common.ConvertTo<char>.From((bool)value);
					break;
				case DataType.Boolean    :
				case DataType.Int16      :
					if (value is bool)
					{
						value    = (bool)value ? 1 : 0;
						dataType = DataType.Int16;
					}
					break;
				case DataType.Guid       :
					if (value is Guid)
					{
						value    = ((Guid)value).ToByteArray();
						dataType = DataType.VarBinary;
					}
					break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
			}

			base.SetParameter(parameter, "@" + name, dataType, value);
		}
	}
}
