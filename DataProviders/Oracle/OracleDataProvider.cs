using System;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	public class OracleDataProvider : DataProviderBase
	{
		public OracleDataProvider() : base(new OracleMappingSchema())
		{
		}

		public override string Name           { get { return ProviderName.Access;     } }
		public override Type   ConnectionType { get { return typeof(OracleConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OracleConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(OracleDataReader));
		}

		#region Overrides

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

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
#if DEBUG
			var specificFieldType = ((OracleDataReader)reader).GetProviderSpecificFieldType(idx);
			var fieldType         = ((OracleDataReader)reader).GetFieldType(idx);
			var typeName          = ((OracleDataReader)reader).GetDataTypeName(idx);
			var oracleDbType      = (OracleDbType)((OracleDataReader)reader).GetSchemaTable().Rows[idx]["ProviderType"];
#endif

			if (toType == typeof(DateTimeOffset))
			{
				var providerFieldType = ((OracleDataReader)reader).GetProviderSpecificFieldType(idx);

				if (providerFieldType == typeof(OracleTimeStampTZ))
					return (Expression<Func<OracleDataReader, int, DateTimeOffset>>)((rd, i) => GetOracleTimeStampTZ(rd, i));
				if (providerFieldType == typeof(OracleTimeStampLTZ))
					return (Expression<Func<OracleDataReader, int, DateTimeOffset>>)((rd, i) => GetOracleTimeStampLTZ(rd, i));
			}

			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			var name = ((OracleDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) && (name == "Char" || name == "NChar"))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			if (toType == type)
			{
				if (type == typeof(OracleBFile))        return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleBFile       (0));
				if (type == typeof(OracleBinary))       return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleBinary      (0));
				if (type == typeof(OracleBlob))         return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleBlob        (0));
				if (type == typeof(OracleClob))         return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleClob        (0));
				if (type == typeof(OracleDate))         return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleDate        (0));
				if (type == typeof(OracleDecimal))      return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleDecimal     (0));
				if (type == typeof(OracleIntervalDS))   return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleIntervalDS  (0));
				if (type == typeof(OracleIntervalYM))   return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleIntervalYM  (0));
				if (type == typeof(OracleRef))          return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleRef         (0));
				if (type == typeof(OracleString))       return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleString      (0));
				if (type == typeof(OracleTimeStamp))    return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleTimeStamp   (0));
				if (type == typeof(OracleTimeStampLTZ)) return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleTimeStampLTZ(0));
				if (type == typeof(OracleTimeStampTZ))  return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleTimeStampTZ (0));
				if (type == typeof(OracleXmlType))      return MemberHelper.MethodOf<OracleDataReader>(r => r.GetOracleXmlType     (0));
			}

			return base.GetReaderMethodInfo(reader, idx, toType);
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((OracleDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

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
				case DataType.Byte       : dataType = DataType.Int16;   break;
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Single         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryFloat;  break;
				case DataType.Double         : ((OracleParameter)parameter).OracleDbType = OracleDbType.BinaryDouble; break;
				case DataType.Text           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Clob;         break;
				case DataType.NText          : ((OracleParameter)parameter).OracleDbType = OracleDbType.NClob;        break;
				case DataType.Binary         : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.VarBinary      : ((OracleParameter)parameter).OracleDbType = OracleDbType.Blob;         break;
				case DataType.Date           : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.SmallDateTime  : ((OracleParameter)parameter).OracleDbType = OracleDbType.Date;         break;
				case DataType.DateTime2      : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStamp;    break;
				case DataType.DateTimeOffset : ((OracleParameter)parameter).OracleDbType = OracleDbType.TimeStampTZ;  break;
				default                      : base.SetParameterType(parameter, dataType);                            break;
			}
		}

		#endregion
	}
}
