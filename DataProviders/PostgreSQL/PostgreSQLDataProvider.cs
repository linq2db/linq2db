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

			if (expr.Type == typeof(string) && (name == "char"))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			if (toType == type)
			{
				if (type == typeof(BitString))         return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetBitString  (0));
				if (type == typeof(NpgsqlInterval))    return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetInterval   (0));
				if (type == typeof(NpgsqlTime))        return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTime       (0));
				if (type == typeof(NpgsqlTimeTZ))      return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeTZ     (0));
				if (type == typeof(NpgsqlTimeStamp))   return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeStamp  (0));
				if (type == typeof(NpgsqlTimeStampTZ)) return MemberHelper.MethodOf<NpgsqlDataReader>(r => r.GetTimeStampTZ(0));
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
			switch (dataType)
			{
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Undefined  :
					     if (value is Binary)       value = ((Binary)value).ToArray();
					else if (value is XDocument)    value = value.ToString();
					else if (value is XmlDocument)  value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Xml : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Xml; break;
				default           : base.SetParameterType(parameter, dataType);                   break;
			}
		}

		#endregion
	}
}
