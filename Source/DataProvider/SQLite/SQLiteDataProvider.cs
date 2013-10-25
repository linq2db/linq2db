using System;
using System.Data;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SQLite
{
	using Common;
	using Mapping;
	using SchemaProvider;
	using SqlProvider;

	public class SQLiteDataProvider : DynamicDataProviderBase
	{
		public SQLiteDataProvider()
			: this(ProviderName.SQLite, new SQLiteMappingSchema())
		{
		}

		protected SQLiteDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SqlProviderFlags.IsSkipSupported           = false;
			SqlProviderFlags.IsSkipSupportedIfTake     = true;
			SqlProviderFlags.IsInsertOrUpdateSupported = false;

			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());

			_sqlOptimizer = new SQLiteSqlOptimizer(SqlProviderFlags);
		}

		public    override string ConnectionNamespace { get { return "System.Data.SQLite"; } }
		protected override string ConnectionTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "SQLiteConnection"); } }
		protected override string DataReaderTypeName  { get { return "{0}.{1}, {0}".Args(ConnectionNamespace, "SQLiteDataReader"); } }

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new SQLiteSqlBuilder(GetSqlOptimizer(), SqlProviderFlags);
		}

		readonly ISqlOptimizer _sqlOptimizer;

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return _sqlOptimizer;
		}

		public override ISchemaProvider GetSchemaProvider()
		{
			return new SQLiteSchemaProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			if (SQLiteTools.AlwaysCheckDbNull)
				return true;

			return base.IsDBNullAllowed(reader, idx);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			base.SetParameter(parameter, "@" + name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt32    : dataType = DataType.Int64;    break;
				case DataType.UInt64    : dataType = DataType.Decimal;  break;
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
			}

			base.SetParameterType(parameter, dataType);
		}

		static Action<string> _createDatabase;

		public void CreateDatabase([JetBrains.Annotations.NotNull] string databaseName, bool deleteIfExists = false)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			CreateFileDatabase(
				databaseName, deleteIfExists, ".sqlite",
				dbName =>
				{
					if (_createDatabase == null)
					{
						var p = Expression.Parameter(typeof(string));
						var l = Expression.Lambda<Action<string>>(
							Expression.Call(GetConnectionType(), "CreateFile", null, p),
							p);
						_createDatabase = l.Compile();
					}

					_createDatabase(dbName);
				});
		}

		public void DropDatabase([JetBrains.Annotations.NotNull] string databaseName)
		{
			if (databaseName == null) throw new ArgumentNullException("databaseName");

			DropFileDatabase(databaseName, ".sqlite");
		}
	}
}
