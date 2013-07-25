using System;
using System.Data;
using System.Data.OleDb;
using System.Runtime.InteropServices;

namespace LinqToDB.DataProvider.Access
{
	using Mapping;
	using SchemaProvider;
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

		public override string ConnectionNamespace { get { return typeof(OleDbConnection).Namespace; } }
		public override Type   DataReaderType      { get { return typeof(OleDbDataReader);           } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessSchemaProvider();
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

				// OleDbType.DBTimeStamp is locale aware, OleDbType.Date is locale neutral.
				//
				case DataType.DateTime   :
				case DataType.DateTime2  : ((OleDbParameter)parameter).OleDbType = OleDbType.Date;         return;

				case DataType.Text       : ((OleDbParameter)parameter).OleDbType = OleDbType.LongVarChar;  return;
				case DataType.NText      : ((OleDbParameter)parameter).OleDbType = OleDbType.LongVarWChar; return;
			}

			base.SetParameterType(parameter, dataType);
		}

		[ComImport, Guid("00000602-0000-0010-8000-00AA006D2EA4")]
		public class CatalogClass
		{
		}

		public override void CreateDatabase(
			[JetBrains.Annotations.NotNull] string configurationString,
			string databaseName   = null,
			bool   deleteIfExists = false,
			string parameters     = null)
		{
			if (configurationString == null) throw new ArgumentNullException("configurationString");

			CreateFileDatabase(
				configurationString, databaseName, deleteIfExists, ".mdb",
				(connStr,dbName) =>
				{
					dynamic catalog = new CatalogClass();

					var conn = catalog.Create(connStr);

					if (conn != null)
						conn.Close();
				});
		}

		public override void DropDatabase(
			[JetBrains.Annotations.NotNull] string configurationString,
			string databaseName = null)
		{
			if (configurationString == null) throw new ArgumentNullException("configurationString");

			DropFileDatabase(configurationString, databaseName, ".mdb");
		}
	}
}
