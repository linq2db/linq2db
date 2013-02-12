using System;
using System.Data;
using System.Data.OleDb;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class AccessDataProvider : DataProviderBase
	{
		public AccessDataProvider()
			: this(ProviderName.Access, new AccessMappingSchema())
		{
		}

		protected AccessDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.AcceptsTakeAsParameter = false;
			SqlProviderFlags.IsSkipSupported        = false;

			SetCharField("DBTYPE_WCHAR", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));
		}

		static DateTime GetDateTime(IDataReader dr, int idx)
		{
			var value = dr.GetDateTime(idx);

			if (value.Year == 1899 && value.Month == 12 && value.Day == 30)
				return new DateTime(1, 1, 1, value.Hour, value.Minute, value.Second, value.Millisecond);

			return value;
		}

		public override Type ConnectionType { get { return typeof(OleDbConnection); } }
		public override Type DataReaderType { get { return typeof(OleDbDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new AccessSqlProvider(SqlProviderFlags);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			// Do some magic to workaround 'Data type mismatch in criteria expression' error
			// in JET for some european locales.
			//
			switch (dataType)
			{
				// OleDbType.Decimal is locale aware, OleDbType.Currency is locale neutral.
				//
				case DataType.Decimal    :
				case DataType.VarNumeric : 
					((OleDbParameter)parameter).OleDbType = OleDbType.Decimal; return;
					//((OleDbParameter)parameter).OleDbType = OleDbType.Currency; return;

				// OleDbType.DBTimeStamp is locale aware, OleDbType.Date is locale neutral.
				//
				case DataType.DateTime   :
				case DataType.DateTime2  : ((OleDbParameter)parameter).OleDbType = OleDbType.Date; return;

				//case DataType.Int32      : ((OleDbParameter)parameter).OleDbType = OleDbType.Integer; return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
