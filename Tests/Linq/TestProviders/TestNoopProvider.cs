using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;

namespace Tests
{
	internal class TestNoopConnection : DbConnection
	{
		private ConnectionState _state;

		public TestNoopConnection(string connectionString)
		{
			ConnectionString = connectionString;
		}

		[AllowNull]
		public override string          ConnectionString { get; set; }
		public override string          Database         { get; } = null!;
		public override string          DataSource       => throw new NotImplementedException();
		public override string          ServerVersion    => throw new NotImplementedException();
		public override ConnectionState State            => _state;

		public bool IsDisposed { get; private set; }

		public override void Close()
		{
			_state = ConnectionState.Closed;
		}

		public override void Open()
		{
			_state = ConnectionState.Open;
		}

		public    override void          ChangeDatabase    (string databaseName          ) => throw new NotImplementedException();
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotImplementedException();
		protected override DbCommand     CreateDbCommand   (                             ) => new TestNoopDbCommand();

		protected override void Dispose(bool disposing)
		{
			Close();
			base.Dispose(disposing);
			IsDisposed = true;
		}
	}

	internal class TestNoopDataReader : IDataReader
	{
		int  IDataReader.Depth           => throw new NotImplementedException();
		bool IDataReader.IsClosed        => throw new NotImplementedException();
		int  IDataReader.RecordsAffected => throw new NotImplementedException();
		int  IDataRecord.FieldCount      => throw new NotImplementedException();

		object IDataRecord.this[string name] => throw new NotImplementedException();
		object IDataRecord.this[int i      ] => throw new NotImplementedException();

		void        IDataReader.Close          (                                                                    ) => throw new NotImplementedException();
		DataTable   IDataReader.GetSchemaTable (                                                                    ) => throw new NotImplementedException();
		bool        IDataReader.NextResult     (                                                                    ) => throw new NotImplementedException();
		bool        IDataReader.Read           (                                                                    ) => throw new NotImplementedException();

		bool        IDataRecord.GetBoolean     (int i                                                               ) => throw new NotImplementedException();
		byte        IDataRecord.GetByte        (int i                                                               ) => throw new NotImplementedException();
		long        IDataRecord.GetBytes       (int i, long fieldOffset, byte[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
		char        IDataRecord.GetChar        (int i                                                               ) => throw new NotImplementedException();
		long        IDataRecord.GetChars       (int i, long fieldoffset, char[] buffer, int bufferoffset, int length) => throw new NotImplementedException();
		IDataReader IDataRecord.GetData        (int i                                                               ) => throw new NotImplementedException();
		string      IDataRecord.GetDataTypeName(int i                                                               ) => throw new NotImplementedException();
		DateTime    IDataRecord.GetDateTime    (int i                                                               ) => throw new NotImplementedException();
		decimal     IDataRecord.GetDecimal     (int i                                                               ) => throw new NotImplementedException();
		double      IDataRecord.GetDouble      (int i                                                               ) => throw new NotImplementedException();
		Type        IDataRecord.GetFieldType   (int i                                                               ) => throw new NotImplementedException();
		float       IDataRecord.GetFloat       (int i                                                               ) => throw new NotImplementedException();
		Guid        IDataRecord.GetGuid        (int i                                                               ) => throw new NotImplementedException();
		short       IDataRecord.GetInt16       (int i                                                               ) => throw new NotImplementedException();
		int         IDataRecord.GetInt32       (int i                                                               ) => throw new NotImplementedException();
		long        IDataRecord.GetInt64       (int i                                                               ) => throw new NotImplementedException();
		string      IDataRecord.GetName        (int i                                                               ) => throw new NotImplementedException();
		int         IDataRecord.GetOrdinal     (string name                                                         ) => throw new NotImplementedException();
		string      IDataRecord.GetString      (int i                                                               ) => throw new NotImplementedException();
		object      IDataRecord.GetValue       (int i                                                               ) => throw new NotImplementedException();
		int         IDataRecord.GetValues      (object[] values                                                     ) => throw new NotImplementedException();
		bool        IDataRecord.IsDBNull       (int i                                                               ) => throw new NotImplementedException();

		void        IDisposable.Dispose        (                                                                    ) => throw new NotImplementedException();
	}

	internal class TestNoopDbCommand : DbCommand
	{
		private readonly DbParameterCollection _parameters = new TestNoopDbParameterCollection();

		[AllowNull]
		public override string CommandText { get; set; } = null!;

		public override int CommandTimeout
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override CommandType CommandType { get; set; }

		public override bool DesignTimeVisible
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		protected override DbConnection? DbConnection
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		protected override DbParameterCollection DbParameterCollection => _parameters;

		protected override DbTransaction? DbTransaction
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override    void         Cancel             (                        ) => throw new NotImplementedException();
		public override    int          ExecuteNonQuery    (                        ) => 0;
		public override    object?      ExecuteScalar      (                        ) => null;
		public override    void         Prepare            (                        ) => throw new NotImplementedException();
		protected override DbParameter  CreateDbParameter  (                        ) => new TestNoopDbParameter();
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new TestNoopDbDataReader();
	}

	internal class TestNoopDbParameter : DbParameter
	{
		public override DbType DbType { get; set; }

		public override ParameterDirection Direction
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override bool IsNullable
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		[AllowNull]
		public override string ParameterName { get; set; } = null!;

		public override int Size
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		[AllowNull]
		public override string SourceColumn
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override bool SourceColumnNullMapping
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override DataRowVersion SourceVersion
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public override object? Value { get; set; }

		public override void ResetDbType() => throw new NotImplementedException();
	}

	internal class TestNoopDbDataReader : DbDataReader
	{
		public override int  Depth           => throw new NotImplementedException();
		public override int  FieldCount      => throw new NotImplementedException();
		public override bool HasRows         => throw new NotImplementedException();
		public override bool IsClosed        => throw new NotImplementedException();
		public override int  RecordsAffected => throw new NotImplementedException();

		public override object this[string name] => throw new NotImplementedException();
		public override object this[int ordinal] => throw new NotImplementedException();

		public override void Close() { }

		public override bool        GetBoolean     (int ordinal                                                               ) => throw new NotImplementedException();
		public override byte        GetByte        (int ordinal                                                               ) => throw new NotImplementedException();
		public override long        GetBytes       (int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
		public override char        GetChar        (int ordinal                                                               ) => throw new NotImplementedException();
		public override long        GetChars       (int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
		public override string      GetDataTypeName(int ordinal                                                               ) => throw new NotImplementedException();
		public override DateTime    GetDateTime    (int ordinal                                                               ) => throw new NotImplementedException();
		public override decimal     GetDecimal     (int ordinal                                                               ) => throw new NotImplementedException();
		public override double      GetDouble      (int ordinal                                                               ) => throw new NotImplementedException();
		public override IEnumerator GetEnumerator  (                                                                          ) => throw new NotImplementedException();
		public override Type        GetFieldType   (int ordinal                                                               ) => throw new NotImplementedException();
		public override float       GetFloat       (int ordinal                                                               ) => throw new NotImplementedException();
		public override Guid        GetGuid        (int ordinal                                                               ) => throw new NotImplementedException();
		public override short       GetInt16       (int ordinal                                                               ) => throw new NotImplementedException();
		public override int         GetInt32       (int ordinal                                                               ) => throw new NotImplementedException();
		public override long        GetInt64       (int ordinal                                                               ) => throw new NotImplementedException();
		public override string      GetName        (int ordinal                                                               ) => throw new NotImplementedException();
		public override int         GetOrdinal     (string name                                                               ) => throw new NotImplementedException();
		public override DataTable   GetSchemaTable (                                                                          ) => throw new NotImplementedException();
		public override string      GetString      (int ordinal                                                               ) => throw new NotImplementedException();
		public override object      GetValue       (int ordinal                                                               ) => throw new NotImplementedException();
		public override int         GetValues      (object[] values                                                           ) => throw new NotImplementedException();
		public override bool        IsDBNull       (int ordinal                                                               ) => throw new NotImplementedException();
		public override bool        NextResult     (                                                                          ) => throw new NotImplementedException();
		public override bool        Read           (                                                                          ) => false;
	}

	internal class TestNoopDbParameterCollection : DbParameterCollection
	{
		private List<TestNoopDbParameter> _parameters = new List<TestNoopDbParameter>();

		public override int Count => _parameters.Count;

		public override bool IsFixedSize => throw new NotImplementedException();
		public override bool IsReadOnly => throw new NotImplementedException();
		public override bool IsSynchronized => throw new NotImplementedException();
		public override object SyncRoot => throw new NotImplementedException();

		public override int Add(object value)
		{
			_parameters.Add((TestNoopDbParameter)value);
			return _parameters.Count - 1;
		}


		public override void Clear()
		{
			_parameters.Clear();
		}

		public    override IEnumerator GetEnumerator(                                       ) => new IDbDataParameter[0].GetEnumerator();
		public    override void        AddRange     (Array values                           ) => throw new NotImplementedException();
		public    override bool        Contains     (string value                           ) => throw new NotImplementedException();
		public    override bool        Contains     (object value                           ) => throw new NotImplementedException();
		public    override void        CopyTo       (Array array, int index                 ) => throw new NotImplementedException();
		public    override int         IndexOf      (string parameterName                   ) => throw new NotImplementedException();
		public    override int         IndexOf      (object value                           ) => throw new NotImplementedException();
		public    override void        Insert       (int index, object value                ) => throw new NotImplementedException();
		public    override void        Remove       (object value                           ) => throw new NotImplementedException();
		public    override void        RemoveAt     (string parameterName                   ) => throw new NotImplementedException();
		public    override void        RemoveAt     (int index                              ) => throw new NotImplementedException();
		protected override DbParameter GetParameter (string parameterName                   ) => throw new NotImplementedException();
		protected override DbParameter GetParameter (int index                              ) => throw new NotImplementedException();
		protected override void        SetParameter (string parameterName, DbParameter value) => throw new NotImplementedException();
		protected override void        SetParameter (int index, DbParameter value           ) => throw new NotImplementedException();
	}

	internal class TestNoopProviderAdapter : IDynamicProviderAdapter
	{
		Type IDynamicProviderAdapter.ConnectionType  => typeof(TestNoopConnection );
		Type IDynamicProviderAdapter.DataReaderType  => typeof(TestNoopDataReader );
		Type IDynamicProviderAdapter.ParameterType   => typeof(TestNoopDbParameter);
		Type IDynamicProviderAdapter.CommandType     => typeof(TestNoopDbCommand  );
		Type IDynamicProviderAdapter.TransactionType => throw new NotImplementedException();
	}

	internal class TestNoopProvider : DynamicDataProviderBase<TestNoopProviderAdapter>
	{
		public TestNoopProvider()
			: base(TestProvName.NoopProvider, new MappingSchema(), new TestNoopProviderAdapter())
		{
		}

		static TestNoopProvider()
		{
			DataConnection.AddDataProvider(new TestNoopProvider());
		}

		public static void Init()
		{
			// Just for triggering of static constructor
		}

		public override ISqlBuilder     CreateSqlBuilder (MappingSchema mappingSchema) => new TestNoopSqlBuilder(MappingSchema);
		public override ISchemaProvider GetSchemaProvider(                           ) => throw new NotImplementedException();
		public override ISqlOptimizer   GetSqlOptimizer  (                           ) => TestNoopSqlOptimizer.Instance;
		public override TableOptions    SupportedTableOptions => TableOptions.None;
	}

	internal class TestNoopSqlBuilder : BasicSqlBuilder
	{
		public TestNoopSqlBuilder(MappingSchema mappingSchema)
			: base(mappingSchema, TestNoopSqlOptimizer.Instance, new SqlProviderFlags())
		{
		}

		protected override ISqlBuilder CreateSqlBuilder() => throw new NotImplementedException();

		protected override void BuildInsertOrUpdateQuery(SqlInsertOrUpdateStatement insertOrUpdate)
		{
			BuildInsertOrUpdateQueryAsMerge(insertOrUpdate, null);
		}
	}

	internal class TestNoopSqlOptimizer : BasicSqlOptimizer
	{
		public static ISqlOptimizer Instance = new TestNoopSqlOptimizer();

		private TestNoopSqlOptimizer()
			: base(new SqlProviderFlags())
		{
		}
	}
}
