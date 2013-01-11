using System;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Npgsql;
using NpgsqlTypes;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	public class PostgreSQLDataProvider : DataProviderBase
	{
		public PostgreSQLDataProvider() : base(new PostgreSQLMappingSchema())
		{
		}

		public override string Name           { get { return ProviderName.PostgreSQL; } }
		public override Type   ConnectionType { get { return typeof(NpgsqlConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new NpgsqlConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(NpgsqlDataReader));
		}

		#region Overrides

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
#if DEBUG
			var specificFieldType = ((NpgsqlDataReader)reader).GetProviderSpecificFieldType(idx);
			var fieldType         = ((NpgsqlDataReader)reader).GetFieldType(idx);
			var typeName          = ((NpgsqlDataReader)reader).GetDataTypeName(idx);
			var npgsqlDbType      = ((NpgsqlDataReader)reader).GetFieldNpgsqlDbType(idx);
#endif

			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			var name = ((NpgsqlDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) && name == "bpchar")
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			if (type == typeof(NpgsqlTimeStampTZ))
			{
				if (toType == typeof(NpgsqlTimeStampTZ)) return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeStampTZ(0));
				if (toType == typeof(DateTimeOffset))    return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeStampTZ(0));
			}

			if (toType == type)
			{
				if (type == typeof(BitString))         return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetBitString  (0));
				if (type == typeof(NpgsqlInterval))    return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetInterval   (0));
				if (type == typeof(NpgsqlTime))        return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTime       (0));
				if (type == typeof(NpgsqlTimeTZ))      return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeTZ     (0));
				if (type == typeof(NpgsqlTimeStamp))   return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeStamp  (0));
				if (type == typeof(NpgsqlDate))        return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetDate       (0));
			}

			return base.GetReaderMethodInfo(reader, idx, toType);
		}


		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((NpgsqlDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null && !(value is string))
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;     break;
				case DataType.UInt16     : dataType = DataType.Int32;     break;
				case DataType.UInt32     : dataType = DataType.Int64;     break;
				case DataType.UInt64     : dataType = DataType.Decimal;   break;
				case DataType.DateTime2  : dataType = DataType.DateTime;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;   break;
				case DataType.Decimal    :
				case DataType.Money      : dataType = DataType.Undefined; break;
				case DataType.Image      : dataType = DataType.VarBinary; goto case DataType.VarBinary;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Undefined  :
					     if (value is Binary)      value = ((Binary)value).ToArray();
					else if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Binary    :
				case DataType.VarBinary : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Bytea;   break;
				case DataType.Boolean   : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Boolean; break;
				case DataType.Xml       : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Xml;     break;
				case DataType.Text      :
				case DataType.NText     : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Text;    break;
				default                 : base.SetParameterType(parameter, dataType);                       break;
			}
		}

		#endregion
	}
}
