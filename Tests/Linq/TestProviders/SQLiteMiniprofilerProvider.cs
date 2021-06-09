using System.Data;
using System.Data.Common;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Tests
{
	internal class SQLiteMiniprofilerProvider : SQLiteDataProvider
	{
		private readonly bool _mapped;

		public SQLiteMiniprofilerProvider(bool mapped)
			: base(ProviderName.SQLiteClassic)
		{
			_mapped = mapped;
		}

		public override MappingSchema MappingSchema => _mapped
			? MappingSchemaInstance.MappedMappingSchema
			: MappingSchemaInstance.UnmappedMappingSchema;

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema MappedMappingSchema   = new MappedMappingSchema  ();
			public static readonly MappingSchema UnmappedMappingSchema = new UnmappedMappingSchema();
		}

		public class MappedMappingSchema : MappingSchema
		{
			public MappedMappingSchema()
				: base(TestProvName.SQLiteClassicMiniProfilerMapped, new SQLiteMappingSchema.ClassicMappingSchema())
			{
				SetConvertExpression<ProfiledDbConnection , DbConnection >(db => db.WrappedConnection );
				SetConvertExpression<ProfiledDbDataReader , DbDataReader >(db => db.WrappedReader     );
				SetConvertExpression<ProfiledDbTransaction, DbTransaction>(db => db.WrappedTransaction);
				SetConvertExpression<ProfiledDbCommand    , DbCommand    >(db => db.InternalCommand   );
			}
		}

		public class UnmappedMappingSchema : MappingSchema
		{
			public UnmappedMappingSchema()
				: base(TestProvName.SQLiteClassicMiniProfilerUnmapped, new SQLiteMappingSchema.ClassicMappingSchema())
			{
			}
		}

		protected override DbConnection CreateConnectionInternal(string connectionString)
		{
			return new ProfiledDbConnection(base.CreateConnectionInternal(connectionString), MiniProfiler.Current);
		}

		public static void Init()
		{
			// initialize miniprofiler or it will not wrap non-connection objects
#if NET472
			MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
			MiniProfiler.Start();
#else
			MiniProfiler.DefaultOptions.StartProfiler();
#endif

			DataConnection.AddDataProvider(TestProvName.SQLiteClassicMiniProfilerMapped  , new SQLiteMiniprofilerProvider(true ));
			DataConnection.AddDataProvider(TestProvName.SQLiteClassicMiniProfilerUnmapped, new SQLiteMiniprofilerProvider(false));
		}
	}
}
