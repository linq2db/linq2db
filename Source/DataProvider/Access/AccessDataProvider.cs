using System;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Runtime.InteropServices;


namespace LinqToDB.DataProvider.Access
{
	using Extensions;
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
			SqlProviderFlags.AcceptsTakeAsParameter    = false;
			SqlProviderFlags.IsSkipSupported           = false;
			SqlProviderFlags.IsCountSubQuerySupported  = false;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			SetCharField("DBTYPE_WCHAR", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<IDataReader,TimeSpan,DateTime>((r,i) => r.GetDateTime(i) - new DateTime(1899, 12, 30));
			SetProviderField<IDataReader,DateTime,DateTime>((r,i) => GetDateTime(r, i));

			_sqlOptimizer = new AccessSqlOptimizer(SqlProviderFlags);
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
		
		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new AccessSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override bool IsCompatibleConnection(IDbConnection connection)
		{
			return typeof(OleDbConnection).IsSameOrParentOf(connection.GetType());
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new AccessSchemaProvider();
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
		class CatalogClass
		{
		}

		public void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName, bool   deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			databaseName = databaseName.Trim();

			if (!databaseName.ToLower().EndsWith(".mdb"))
				databaseName += ".mdb";

			if (File.Exists(databaseName))
			{
				if (!deleteIfExists)
					return;
				File.Delete(databaseName);
			}

			var connectionString = string.Format(
				@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Locale Identifier=1033;Jet OLEDB:Engine Type=5",
				databaseName);

			CreateFileDatabase(
				databaseName, deleteIfExists, ".mdb",
				dbName =>
				{
					dynamic catalog = new CatalogClass();

					var conn = catalog.Create(connectionString);

					if (conn != null)
						conn.Close();
				});
		}

		public void DropDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			DropFileDatabase(databaseName, ".mdb");
		}
	}
}
