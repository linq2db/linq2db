using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;
using System.Data;
using System.Data.Common;
using System.Collections;

namespace Tests
{
	internal class TestNoopConnection : IDbConnection
	{
		private ConnectionState _state = ConnectionState.Closed;

		public TestNoopConnection(string connectionString)
		{
		}

		string IDbConnection.ConnectionString
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		int IDbConnection.ConnectionTimeout
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		string IDbConnection.Database
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		ConnectionState IDbConnection.State
		{
			get
			{
				return _state;
			}
		}

		IDbTransaction IDbConnection.BeginTransaction()
		{
			throw new NotImplementedException();
		}

		IDbTransaction IDbConnection.BeginTransaction(IsolationLevel il)
		{
			throw new NotImplementedException();
		}

		void IDbConnection.ChangeDatabase(string databaseName)
		{
			throw new NotImplementedException();
		}

		void IDbConnection.Close()
		{
			throw new NotImplementedException();
		}

		IDbCommand IDbConnection.CreateCommand()
		{
			return new TestNoopDbCommand();
		}

		void IDbConnection.Open()
		{
			_state = ConnectionState.Open;
		}

		void IDisposable.Dispose()
		{
			_state = ConnectionState.Closed;
		}
	}

	internal class TestNoopDataReader : IDataReader
	{
		int IDataReader.Depth
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		bool IDataReader.IsClosed
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		int IDataReader.RecordsAffected
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		int IDataRecord.FieldCount
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		object IDataRecord.this[string name]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		object IDataRecord.this[int i]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		void IDataReader.Close()
		{
			throw new NotImplementedException();
		}

		DataTable IDataReader.GetSchemaTable()
		{
			throw new NotImplementedException();
		}

		bool IDataReader.NextResult()
		{
			throw new NotImplementedException();
		}

		bool IDataReader.Read()
		{
			throw new NotImplementedException();
		}

		bool IDataRecord.GetBoolean(int i)
		{
			throw new NotImplementedException();
		}

		byte IDataRecord.GetByte(int i)
		{
			throw new NotImplementedException();
		}

		long IDataRecord.GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		char IDataRecord.GetChar(int i)
		{
			throw new NotImplementedException();
		}

		long IDataRecord.GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
		{
			throw new NotImplementedException();
		}

		IDataReader IDataRecord.GetData(int i)
		{
			throw new NotImplementedException();
		}

		string IDataRecord.GetDataTypeName(int i)
		{
			throw new NotImplementedException();
		}

		DateTime IDataRecord.GetDateTime(int i)
		{
			throw new NotImplementedException();
		}

		decimal IDataRecord.GetDecimal(int i)
		{
			throw new NotImplementedException();
		}

		double IDataRecord.GetDouble(int i)
		{
			throw new NotImplementedException();
		}

		Type IDataRecord.GetFieldType(int i)
		{
			throw new NotImplementedException();
		}

		float IDataRecord.GetFloat(int i)
		{
			throw new NotImplementedException();
		}

		Guid IDataRecord.GetGuid(int i)
		{
			throw new NotImplementedException();
		}

		short IDataRecord.GetInt16(int i)
		{
			throw new NotImplementedException();
		}

		int IDataRecord.GetInt32(int i)
		{
			throw new NotImplementedException();
		}

		long IDataRecord.GetInt64(int i)
		{
			throw new NotImplementedException();
		}

		string IDataRecord.GetName(int i)
		{
			throw new NotImplementedException();
		}

		int IDataRecord.GetOrdinal(string name)
		{
			throw new NotImplementedException();
		}

		string IDataRecord.GetString(int i)
		{
			throw new NotImplementedException();
		}

		object IDataRecord.GetValue(int i)
		{
			throw new NotImplementedException();
		}

		int IDataRecord.GetValues(object[] values)
		{
			throw new NotImplementedException();
		}

		bool IDataRecord.IsDBNull(int i)
		{
			throw new NotImplementedException();
		}

		void IDisposable.Dispose()
		{
			throw new NotImplementedException();
		}
	}

	internal class TestNoopDbCommand : DbCommand
	{
		private readonly DbParameterCollection _parameters = new TestNoopDbParameterCollection();

		public override string CommandText { get; set; }

		public override int CommandTimeout
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override CommandType CommandType { get; set; }

		public override bool DesignTimeVisible
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		protected override DbConnection DbConnection
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		protected override DbParameterCollection DbParameterCollection
		{
			get
			{
				return _parameters;
			}
		}

		protected override DbTransaction DbTransaction
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override void Cancel()
		{
			throw new NotImplementedException();
		}

		public override int ExecuteNonQuery()
		{
			return 0;
		}

		public override object ExecuteScalar()
		{
			return null;
		}

		public override void Prepare()
		{
			throw new NotImplementedException();
		}

		protected override DbParameter CreateDbParameter()
		{
			return new TestNoopDbParameter();
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			return new TestNoopDbDataReader();
		}
	}

	internal class TestNoopDbParameter : DbParameter
	{
		public override DbType DbType { get; set; }

		public override ParameterDirection Direction
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override bool IsNullable
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override string ParameterName { get; set; }

		public override int Size
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override string SourceColumn
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

		public override bool SourceColumnNullMapping
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}

#if !NETSTANDARD
		public override DataRowVersion SourceVersion
		{
			get
			{
				throw new NotImplementedException();
			}

			set
			{
				throw new NotImplementedException();
			}
		}
#endif

		public override object Value { get; set; }

		public override void ResetDbType()
		{
			throw new NotImplementedException();
		}
	}

	internal class TestNoopDbDataReader : DbDataReader
	{
		public override int Depth
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override int FieldCount
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool HasRows
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool IsClosed
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override int RecordsAffected
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override object this[string name]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override object this[int ordinal]
		{
			get
			{
				throw new NotImplementedException();
			}
		}

#if !NETSTANDARD
		public override void Close()
		{
		}
#endif

		public override bool GetBoolean(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override byte GetByte(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override char GetChar(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
		{
			throw new NotImplementedException();
		}

		public override string GetDataTypeName(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override DateTime GetDateTime(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override decimal GetDecimal(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override double GetDouble(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override IEnumerator GetEnumerator()
		{
			throw new NotImplementedException();
		}

		public override Type GetFieldType(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override float GetFloat(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override Guid GetGuid(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override short GetInt16(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override int GetInt32(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override long GetInt64(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override string GetName(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override int GetOrdinal(string name)
		{
			throw new NotImplementedException();
		}

#if !NETSTANDARD
		public override DataTable GetSchemaTable()
		{
			throw new NotImplementedException();
		}
#endif

		public override string GetString(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override object GetValue(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override int GetValues(object[] values)
		{
			throw new NotImplementedException();
		}

		public override bool IsDBNull(int ordinal)
		{
			throw new NotImplementedException();
		}

		public override bool NextResult()
		{
			throw new NotImplementedException();
		}

		public override bool Read()
		{
			return false;
		}
	}

	internal class TestNoopDbParameterCollection : DbParameterCollection
	{
		private List<TestNoopDbParameter> _parameters = new List<TestNoopDbParameter>();

		public override int Count
		{
			get
			{
				return _parameters.Count;
			}
		}

#if !NETSTANDARD
		public override bool IsFixedSize
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool IsReadOnly
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool IsSynchronized
		{
			get
			{
				throw new NotImplementedException();
			}
		}
#endif

		public override object SyncRoot
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override int Add(object value)
		{
			_parameters.Add((TestNoopDbParameter)value);
			return _parameters.Count - 1;
		}

		public override void AddRange(Array values)
		{
			throw new NotImplementedException();
		}

		public override void Clear()
		{
			_parameters.Clear();
		}

		public override bool Contains(string value)
		{
			throw new NotImplementedException();
		}

		public override bool Contains(object value)
		{
			throw new NotImplementedException();
		}

		public override void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public override IEnumerator GetEnumerator()
		{
			return new IDbDataParameter[0].GetEnumerator();
		}

		public override int IndexOf(string parameterName)
		{
			throw new NotImplementedException();
		}

		public override int IndexOf(object value)
		{
			throw new NotImplementedException();
		}

		public override void Insert(int index, object value)
		{
			throw new NotImplementedException();
		}

		public override void Remove(object value)
		{
			throw new NotImplementedException();
		}

		public override void RemoveAt(string parameterName)
		{
			throw new NotImplementedException();
		}

		public override void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		protected override DbParameter GetParameter(string parameterName)
		{
			throw new NotImplementedException();
		}

		protected override DbParameter GetParameter(int index)
		{
			throw new NotImplementedException();
		}

		protected override void SetParameter(string parameterName, DbParameter value)
		{
			throw new NotImplementedException();
		}

		protected override void SetParameter(int index, DbParameter value)
		{
			throw new NotImplementedException();
		}
	}

	internal class TestNoopProvider : DynamicDataProviderBase
	{
		public TestNoopProvider()
			: base(TestProvName.NoopProvider, new MappingSchema())
		{
		}

		static TestNoopProvider()
		{
			DataConnection.AddDataProvider(new TestNoopProvider());
		}

		public override string ConnectionNamespace
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		protected override string ConnectionTypeName
		{
			get
			{
				return "Tests.TestNoopConnection, linq2db.Tests";
			}
		}

		protected override string DataReaderTypeName
		{
			get
			{
				return "Tests.TestNoopDataReader, linq2db.Tests";
			}
		}

		public static void Init()
		{
			// Just for triggering of static constructor
		}

		public override ISqlBuilder CreateSqlBuilder()
		{
			return new TestNoopSqlBuilder();
		}

#if !NETSTANDARD
		public override ISchemaProvider GetSchemaProvider()
		{
			throw new NotImplementedException();
		}
#endif

		public override ISqlOptimizer GetSqlOptimizer()
		{
			return TestNoopSqlOptimizer.Instance;
		}

		protected override void OnConnectionTypeCreated(Type connectionType)
		{
		}
	}

	internal class TestNoopSqlBuilder : BasicSqlBuilder
	{
		public TestNoopSqlBuilder()
			: base(TestNoopSqlOptimizer.Instance, new SqlProviderFlags(), new ValueToSqlConverter())
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			throw new NotImplementedException();
		}

		protected override void BuildInsertOrUpdateQuery()
		{
			BuildInsertOrUpdateQueryAsMerge(null);
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
