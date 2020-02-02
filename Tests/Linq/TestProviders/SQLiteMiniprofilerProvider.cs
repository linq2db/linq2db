using System;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;

namespace Tests
{
	internal class SQLiteMiniprofilerProvider : SQLiteDataProvider
	{
		public SQLiteMiniprofilerProvider(string name)
			: base(name)
		{
		}

		public override MappingSchema MappingSchema => Name == TestProvName.SQLiteClassicMiniProfilerMapped
			? MappingSchemaInstance.MappedMappingSchema
			: MappingSchemaInstance.UnmappedMappingSchema;

		static class MappingSchemaInstance
		{
			public static readonly MappingSchema MappedMappingSchema   = new MappedMappingSchema();
			public static readonly MappingSchema UnmappedMappingSchema = new UnmappedMappingSchema();
		}

		public class MappedMappingSchema : MappingSchema
		{
			public MappedMappingSchema()
				: base(TestProvName.SQLiteClassicMiniProfilerMapped, new SQLiteMappingSchema.ClassicMappingSchema())
			{
				SetConvertExpression<ProfiledDbConnection, IDbConnection>(db => db.WrappedConnection);
				SetConvertExpression<ProfiledDbDataReader, IDataReader>(db => db.WrappedReader);
				SetConvertExpression<ProfiledDbTransaction, IDbTransaction>(db => db.WrappedTransaction);
				SetConvertExpression<ProfiledDbCommand, IDbCommand>(db => db.InternalCommand);
			}
		}

		public class UnmappedMappingSchema : MappingSchema
		{
			public UnmappedMappingSchema()
				: base(TestProvName.SQLiteClassicMiniProfilerUnmapped, new SQLiteMappingSchema.ClassicMappingSchema())
			{
			}
		}

		protected override IDbConnection CreateConnectionInternal(string connectionString)
		{
			return new ProfiledDbConnection((DbConnection)base.CreateConnectionInternal(connectionString), MiniProfiler.Current);
		}

		public static void Init()
		{
			// initialize miniprofiler or it will not wrap non-connection objects
#if NET46
			MiniProfiler.Settings.ProfilerProvider = new SingletonProfilerProvider();
			MiniProfiler.Start();
#else
			MiniProfiler.DefaultOptions.StartProfiler();
#endif

			DataConnection.AddDataProvider(new SQLiteMiniprofilerProvider(TestProvName.SQLiteClassicMiniProfilerMapped));
			DataConnection.AddDataProvider(new SQLiteMiniprofilerProvider(TestProvName.SQLiteClassicMiniProfilerUnmapped));
		}
	}
}
