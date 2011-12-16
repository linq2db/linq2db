using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

using SqlException = System.Data.SqlClient.SqlException;
using SqlParameter = System.Data.SqlClient.SqlParameter;

namespace LinqToDB.Data.DataProvider
{
	using Mapping;
	using SqlProvider;

	/// <summary>
	/// Implements access to the Data Provider for SQL Server.
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
	public abstract class SqlDataProviderBase : DataProviderBase
	{
		/// <summary>
		/// Creates the database connection object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>The database connection object.</returns>
		public override IDbConnection CreateConnectionObject()
		{
			return new SqlConnection();
		}

		/// <summary>
		/// Creates the data adapter object.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>A data adapter object.</returns>
		public override DbDataAdapter CreateDataAdapterObject()
		{
			return new SqlDataAdapter();
		}

		/// <summary>
		/// Populates the specified <see cref="IDbCommand"/> object's Parameters collection with 
		/// parameter information for the stored procedure specified in the <see cref="IDbCommand"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <param name="command">The <see cref="IDbCommand"/> referencing the stored procedure for which the parameter
		/// information is to be derived. The derived parameters will be populated into the 
		/// Parameters of this command.</param>
		public override bool DeriveParameters(IDbCommand command)
		{
			SqlCommandBuilder.DeriveParameters((SqlCommand)command);
			
#if !MONO
			foreach (SqlParameter p in command.Parameters)
			{
				// We have to clear UDT type names.
				// Otherwise it will fail with error
				// "Database name is not allowed with a table-valued parameter"
				// but this is exactly the way how they are discovered.
				//
				if (p.SqlDbType == SqlDbType.Structured)
				{
					var firstDot = p.TypeName.IndexOf('.');
					if (firstDot >= 0)
						p.TypeName = p.TypeName.Substring(firstDot + 1);
				}
			}
#endif

			return true;
		}

		public override void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			base.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			if (commandParameters == null)
				return;

			foreach (var p in commandParameters)
			{
				var val = p.Value;

				if (val == null || !val.GetType().IsArray || val is byte[] || val is char[])
					continue;

				var dt = new DataTable();

				dt.Columns.Add("column_value", val.GetType().GetElementType());

				dt.BeginLoadData();

				foreach (object o in (Array)val)
				{
					var row = dt.NewRow();
					row[0] = o;
					dt.Rows.Add(row);
				}

				dt.EndLoadData();

				p.Value = dt;
			}
		}

		public override void SetUserDefinedType(IDbDataParameter parameter, string typeName)
		{
#if !MONO
			if (!(parameter is SqlParameter))
				throw new ArgumentException("SqlParameter expected.", "parameter");

			((SqlParameter)parameter).TypeName = typeName;
#else
			throw new NotSupportedException();
#endif
		}

		public override object Convert(object value, ConvertType convertType)
		{
			switch (convertType)
			{
				case ConvertType.ExceptionToErrorNumber:
					if (value is SqlException)
						return ((SqlException)value).Number;
					break;
			}

			return SqlProvider.Convert(value, convertType);
		}

		/// <summary>
		/// Returns connection type.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <value>An instance of the <see cref="Type"/> class.</value>
		public override Type ConnectionType
		{
			get { return typeof(SqlConnection); }
		}

		public const string NameString = LinqToDB.ProviderName.MsSql;

		/// <summary>
		/// Returns the data provider name.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataProvider Method</seealso>
		/// <value>Data provider name.</value>
		public override string Name
		{
			get { return NameString; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2005SqlProvider();
		}

		public override int MaxParameters
		{
			get { return 2100 - 20; }
		}

		public override int MaxBatchSize
		{
			get { return 65536; }
		}

		#region GetDataReader

		public override IDataReader GetDataReader(MappingSchema schema, IDataReader dataReader)
		{
			return dataReader is SqlDataReader?
				new SqlDataReaderEx((SqlDataReader)dataReader):
				base.GetDataReader(schema, dataReader);
		}

		class SqlDataReaderEx : DataReaderEx<SqlDataReader>
		{
			public SqlDataReaderEx(SqlDataReader rd): base(rd)
			{
			}

			public override DateTimeOffset GetDateTimeOffset(int i)
			{
#if !MONO
				return DataReader.GetDateTimeOffset(i);
#else
				throw new NotSupportedException();
#endif
			}
		}

		#endregion

		public override int InsertBatch<T>(
			DbManager                      db,
			string                         insertText,
			IEnumerable<T>                 collection,
			MemberMapper[]                 members,
			int                            maxBatchSize,
			DbManager.ParameterProvider<T> getParameters)
		{
			if (db.Transaction != null)
				return base.InsertBatch(db, insertText, collection, members, maxBatchSize, getParameters);

			var idx = insertText.IndexOf('\n');
			var tbl = insertText.Substring(0, idx).Substring("INSERT INTO ".Length).TrimEnd('\r');
			var rd  = new BulkCopyReader(members, collection);
			var bc  = new SqlBulkCopy((SqlConnection)db.Connection)
			{
				BatchSize            = maxBatchSize,
				DestinationTableName = tbl,
			};

			foreach (var memberMapper in members)
				bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(memberMapper.Ordinal, memberMapper.Name));

			bc.WriteToServer(rd);

			return rd.Count;
		}

		class BulkCopyReader : IDataReader
		{
			readonly MemberMapper[] _members;
			readonly IEnumerable    _collection;
			readonly IEnumerator    _enumerator;

			public int Count;

			public BulkCopyReader(MemberMapper[] members, IEnumerable collection)
			{
				_members    = members;
				_collection = collection;
				_enumerator = _collection.GetEnumerator();
			}

			#region Implementation of IDisposable

			public void Dispose()
			{
			}

			#endregion

			#region Implementation of IDataRecord

			public string GetName(int i)
			{
				return _members[i].Name;
			}

			public Type GetFieldType(int i)
			{
				return _members[i].Type;
			}

			public object GetValue(int i)
			{
				return _members[i].GetValue(_enumerator.Current);
			}

			public int FieldCount
			{
				get { return _members.Length; }
			}

			public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
			{
				throw new NotImplementedException();
			}

			public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
			{
				throw new NotImplementedException();
			}

			public string      GetDataTypeName(int i)           { throw new NotImplementedException(); }
			public int         GetValues      (object[] values) { throw new NotImplementedException(); }
			public int         GetOrdinal     (string name)     { throw new NotImplementedException(); }
			public bool        GetBoolean     (int i)           { throw new NotImplementedException(); }
			public byte        GetByte        (int i)           { throw new NotImplementedException(); }
			public char        GetChar        (int i)           { throw new NotImplementedException(); }
			public Guid        GetGuid        (int i)           { throw new NotImplementedException(); }
			public short       GetInt16       (int i)           { throw new NotImplementedException(); }
			public int         GetInt32       (int i)           { throw new NotImplementedException(); }
			public long        GetInt64       (int i)           { throw new NotImplementedException(); }
			public float       GetFloat       (int i)           { throw new NotImplementedException(); }
			public double      GetDouble      (int i)           { throw new NotImplementedException(); }
			public string      GetString      (int i)           { throw new NotImplementedException(); }
			public decimal     GetDecimal     (int i)           { throw new NotImplementedException(); }
			public DateTime    GetDateTime    (int i)           { throw new NotImplementedException(); }
			public IDataReader GetData        (int i)           { throw new NotImplementedException(); }
			public bool        IsDBNull       (int i)           { throw new NotImplementedException(); }

			object IDataRecord.this[int i]
			{
				get { throw new NotImplementedException(); }
			}

			object IDataRecord.this[string name]
			{
				get { throw new NotImplementedException(); }
			}

			#endregion

			#region Implementation of IDataReader

			public void Close()
			{
				throw new NotImplementedException();
			}

			public DataTable GetSchemaTable()
			{
				throw new NotImplementedException();
			}

			public bool NextResult()
			{
				throw new NotImplementedException();
			}

			public bool Read()
			{
				var b = _enumerator.MoveNext();

				if (b)
					Count++;

				return b;
			}

			public int Depth
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsClosed
			{
				get { throw new NotImplementedException(); }
			}

			public int RecordsAffected
			{
				get { throw new NotImplementedException(); }
			}

			#endregion
		}
	}
}
