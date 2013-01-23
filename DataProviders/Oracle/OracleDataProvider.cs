using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Reflection;
using System.Xml;

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace LinqToDB.DataProvider
{
	using Data;
	using Mapping;
	using SqlProvider;

	public class OracleDataProvider : DataProviderBase
	{
		public OracleDataProvider(string name)
			: this(name, new OracleMappingSchema(name))
		{
		}

		public OracleDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SetCharField("Char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<OracleDataReader,OracleBFile       ,OracleBFile       >((r,i) => r.GetOracleBFile       (i));
			SetProviderField<OracleDataReader,OracleBinary      ,OracleBinary      >((r,i) => r.GetOracleBinary      (i));
			SetProviderField<OracleDataReader,OracleBlob        ,OracleBlob        >((r,i) => r.GetOracleBlob        (i));
			SetProviderField<OracleDataReader,OracleClob        ,OracleClob        >((r,i) => r.GetOracleClob        (i));
			SetProviderField<OracleDataReader,OracleDate        ,OracleDate        >((r,i) => r.GetOracleDate        (i));
			SetProviderField<OracleDataReader,OracleDecimal     ,OracleDecimal     >((r,i) => r.GetOracleDecimal     (i));
			SetProviderField<OracleDataReader,OracleIntervalDS  ,OracleIntervalDS  >((r,i) => r.GetOracleIntervalDS  (i));
			SetProviderField<OracleDataReader,OracleIntervalYM  ,OracleIntervalYM  >((r,i) => r.GetOracleIntervalYM  (i));
			SetProviderField<OracleDataReader,OracleRef         ,OracleRef         >((r,i) => r.GetOracleRef         (i));
			SetProviderField<OracleDataReader,OracleString      ,OracleString      >((r,i) => r.GetOracleString      (i));
			SetProviderField<OracleDataReader,OracleTimeStamp   ,OracleTimeStamp   >((r,i) => r.GetOracleTimeStamp   (i));
			SetProviderField<OracleDataReader,OracleTimeStampLTZ,OracleTimeStampLTZ>((r,i) => r.GetOracleTimeStampLTZ(i));
			SetProviderField<OracleDataReader,OracleTimeStampTZ ,OracleTimeStampTZ >((r,i) => r.GetOracleTimeStampTZ (i));
			SetProviderField<OracleDataReader,OracleXmlType     ,OracleXmlType     >((r,i) => r.GetOracleXmlType     (i));
			SetProviderField<OracleDataReader,DateTimeOffset    ,OracleTimeStampTZ >((r,i) => GetOracleTimeStampTZ (r,i));
			SetProviderField<OracleDataReader,DateTimeOffset    ,OracleTimeStampLTZ>((r,i) => GetOracleTimeStampLTZ(r,i));
		}

		/*
		static OracleDataProvider()
		{
			// Fix Oracle.Net bug #1: Array types are not handled.
			//
			var oraDbDbTypeTableType = typeof(OracleParameter).Assembly.GetType("Oracle.DataAccess.Client.OraDb_DbTypeTable");

			if (oraDbDbTypeTableType != null)
			{
				var typeTable = (Hashtable)oraDbDbTypeTableType.InvokeMember(
					"s_table", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.GetField,
					null, null, Type.EmptyTypes);

				if (typeTable != null)
				{
					typeTable[typeof(DateTime[])]          = OracleDbType.TimeStamp;
					typeTable[typeof(Int16[])]             = OracleDbType.Int16;
					typeTable[typeof(Int32[])]             = OracleDbType.Int32;
					typeTable[typeof(Int64[])]             = OracleDbType.Int64;
					typeTable[typeof(Single[])]            = OracleDbType.Single;
					typeTable[typeof(Double[])]            = OracleDbType.Double;
					typeTable[typeof(Decimal[])]           = OracleDbType.Decimal;
					typeTable[typeof(TimeSpan[])]          = OracleDbType.IntervalDS;
					typeTable[typeof(String[])]            = OracleDbType.Varchar2;
					typeTable[typeof(OracleBFile[])]       = OracleDbType.BFile;
					typeTable[typeof(OracleBinary[])]      = OracleDbType.Raw;
					typeTable[typeof(OracleBlob[])]        = OracleDbType.Blob;
					typeTable[typeof(OracleClob[])]        = OracleDbType.Clob;
					typeTable[typeof(OracleDate[])]        = OracleDbType.Date;
					typeTable[typeof(OracleDecimal[])]     = OracleDbType.Decimal;
					typeTable[typeof(OracleIntervalDS[])]  = OracleDbType.IntervalDS;
					typeTable[typeof(OracleIntervalYM[])]  = OracleDbType.IntervalYM;
					typeTable[typeof(OracleRefCursor[])]   = OracleDbType.RefCursor;
					typeTable[typeof(OracleString[])]      = OracleDbType.Varchar2;
					typeTable[typeof(OracleTimeStamp[])]   = OracleDbType.TimeStamp;
					typeTable[typeof(OracleTimeStampLTZ[])]= OracleDbType.TimeStampLTZ;
					typeTable[typeof(OracleTimeStampTZ[])] = OracleDbType.TimeStampTZ;
					typeTable[typeof(OracleXmlType[])]     = OracleDbType.XmlType;

					typeTable[typeof(Boolean)]             = OracleDbType.Byte;
					typeTable[typeof(Guid)]                = OracleDbType.Raw;
					typeTable[typeof(SByte)]               = OracleDbType.Decimal;
					typeTable[typeof(UInt16)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt32)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt64)]              = OracleDbType.Decimal;

					typeTable[typeof(Boolean[])]           = OracleDbType.Byte;
					typeTable[typeof(Guid[])]              = OracleDbType.Raw;
					typeTable[typeof(SByte[])]             = OracleDbType.Decimal;
					typeTable[typeof(UInt16[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt32[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt64[])]            = OracleDbType.Decimal;

					typeTable[typeof(Boolean?)]            = OracleDbType.Byte;
					typeTable[typeof(Guid?)]               = OracleDbType.Raw;
					typeTable[typeof(SByte?)]              = OracleDbType.Decimal;
					typeTable[typeof(UInt16?)]             = OracleDbType.Decimal;
					typeTable[typeof(UInt32?)]             = OracleDbType.Decimal;
					typeTable[typeof(UInt64?)]             = OracleDbType.Decimal;
					typeTable[typeof(DateTime?[])]         = OracleDbType.TimeStamp;
					typeTable[typeof(Int16?[])]            = OracleDbType.Int16;
					typeTable[typeof(Int32?[])]            = OracleDbType.Int32;
					typeTable[typeof(Int64?[])]            = OracleDbType.Int64;
					typeTable[typeof(Single?[])]           = OracleDbType.Single;
					typeTable[typeof(Double?[])]           = OracleDbType.Double;
					typeTable[typeof(Decimal?[])]          = OracleDbType.Decimal;
					typeTable[typeof(TimeSpan?[])]         = OracleDbType.IntervalDS;
					typeTable[typeof(Boolean?[])]          = OracleDbType.Byte;
					typeTable[typeof(Guid?[])]             = OracleDbType.Raw;
					typeTable[typeof(SByte?[])]            = OracleDbType.Decimal;
					typeTable[typeof(UInt16?[])]           = OracleDbType.Decimal;
					typeTable[typeof(UInt32?[])]           = OracleDbType.Decimal;
					typeTable[typeof(UInt64?[])]           = OracleDbType.Decimal;

					typeTable[typeof(XmlReader)]           = OracleDbType.XmlType;
					typeTable[typeof(XmlDocument)]         = OracleDbType.XmlType;
					typeTable[typeof(MemoryStream)]        = OracleDbType.Blob;
					typeTable[typeof(XmlReader[])]         = OracleDbType.XmlType;
					typeTable[typeof(XmlDocument[])]       = OracleDbType.XmlType;
					typeTable[typeof(MemoryStream[])]      = OracleDbType.Blob;
				}
			}
		}
		*/

		static DateTimeOffset GetOracleTimeStampTZ(OracleDataReader rd, int idx)
		{
			var tstz = rd.GetOracleTimeStampTZ(idx);
			return new DateTimeOffset(
				tstz.Year, tstz.Month,  tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
		}

		static DateTimeOffset GetOracleTimeStampLTZ(OracleDataReader rd, int idx)
		{
			var tstz = rd.GetOracleTimeStampLTZ(idx).ToOracleTimeStampTZ();
			return new DateTimeOffset(
				tstz.Year, tstz.Month,  tstz.Day,
				tstz.Hour, tstz.Minute, tstz.Second, (int)tstz.Millisecond,
				TimeSpan.Parse(tstz.TimeZone.TrimStart('+')));
		}

		public override Type ConnectionType { get { return typeof(OracleConnection); } }
		public override Type DataReaderType { get { return typeof(OracleDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OracleConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new OracleSqlProvider();
		}

		public override void InitCommand(DataConnection dataConnection)
		{
			dataConnection.Command = null;
			((OracleCommand)dataConnection.Command).BindByName = true;
			//base.InitCommand(dataConnection);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.DateTimeOffset  :
					if (value is DateTimeOffset)
					{
						var dto  = (DateTimeOffset)value;
						var zone = dto.Offset.ToString("hh\\:mm");
						if (!zone.StartsWith("-") && !zone.StartsWith("+"))
							zone = "+" + zone;
						value = new OracleTimeStampTZ(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second, dto.Millisecond, zone);
					}
					break;
				case DataType.Boolean    :
					dataType = DataType.Byte;
					if (value is bool)
						value = (bool)value ? (byte)1 : (byte)0;
					break;
				case DataType.Guid       :
					if (value is Guid) value = ((Guid)value).ToByteArray();
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Byte           : parameter.DbType = DbType.Int16;                                       break;
				case DataType.SByte          : parameter.DbType = DbType.Int16;                                       break;
				case DataType.UInt16         : parameter.DbType = DbType.Int32;                                       break;
				case DataType.UInt32         : parameter.DbType = DbType.Int64;                                       break;
				case DataType.UInt64         : parameter.DbType = DbType.Decimal;                                     break;
				case DataType.VarNumeric     : parameter.DbType = DbType.Decimal;                                     break;
				case DataType.Single         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryFloat;  break;
				case DataType.Double         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryDouble; break;
				case DataType.Text           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Clob;         break;
				case DataType.NText          : ((OracleParameter)parameter).OracleDbType = OracleDbType.NClob;        break;
				case DataType.Image          : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.Binary         : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.VarBinary      : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.Date           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.SmallDateTime  : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.DateTime2      : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStamp;    break;
				case DataType.DateTimeOffset : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStampTZ;  break;
				case DataType.Guid           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Raw;          break;
				default                      : base.SetParameterType(parameter, dataType);                            break;
			}
		}
	}
}
