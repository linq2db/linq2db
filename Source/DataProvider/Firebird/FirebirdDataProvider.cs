using System;
using System.Data;

namespace LinqToDB.DataProvider.Firebird
{
	using Common;
	using Mapping;
	using SqlProvider;

	public class FirebirdDataProvider : DynamicDataProviderBase
	{
		public FirebirdDataProvider()
			: this(ProviderName.Firebird, new FirebirdMappingSchema())
		{
		}

		protected FirebirdDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsIdentityParameterRequired = true;

			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1970, 1, 1));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = new FirebirdSqlOptimizer(SqlProviderFlags);
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1970 && value.Month == 1 && value.Day == 1)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		static Action<IDbDataParameter> _setTimeStamp;

		public    override string ConnectionNamespace { get { return "FirebirdSql.Data.FirebirdClient"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "FbConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "FbDataReader"); } }

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
			//                                             ((FbParameter)parameter).FbDbType =  FbDbType.   TimeStamp;
			_setTimeStamp = GetSetParameter(connectionType, "FbParameter",         "FbDbType", "FbDbType", "TimeStamp");
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new FirebirdSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override SchemaProvider.ISchemaProvider GetSchemaProvider()
		{
			return new FirebirdSchemaProvider();
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
				case DataType.DateTime2  : _setTimeStamp(parameter);    return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
