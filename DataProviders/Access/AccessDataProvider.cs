using System;
using System.Data;
using System.Data.OleDb;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class AccessDataProvider : DataProviderBase
	{
		public AccessDataProvider() : base(new AccessMappingSchema())
		{
			SetCharField("DBTYPE_WCHAR", (r,i) => r.GetString(i).TrimEnd());
		}

		public override string Name           { get { return ProviderName.Access;     } }
		public override Type   ConnectionType { get { return typeof(OleDbConnection); } }
		public override Type   DataReaderType { get { return typeof(OleDbDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new AccessSqlProvider();
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
				case DataType.VarNumeric : ((OleDbParameter)parameter).OleDbType = OleDbType.Currency; return;

				// OleDbType.DBTimeStamp is locale aware, OleDbType.Date is locale neutral.
				//
				case DataType.DateTime   :
				case DataType.DateTime2  : ((OleDbParameter)parameter).OleDbType = OleDbType.Date; return;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
