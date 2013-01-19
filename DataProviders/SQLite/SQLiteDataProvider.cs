using System;
using System.Data;
using System.Data.SQLite;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class SQLiteDataProvider : DataProviderBase
	{
		public SQLiteDataProvider() : base(new SQLiteMappingSchema())
		{
			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());
		}

		public override string Name           { get { return ProviderName.SQLite;      } }
		public override Type   ConnectionType { get { return typeof(SQLiteConnection); } }
		public override Type   DataReaderType { get { return typeof(SQLiteDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new SQLiteConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SQLiteSqlProvider();
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.UInt32 : dataType = DataType.Int64;   break;
				case DataType.UInt64 : dataType = DataType.Decimal; break;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
