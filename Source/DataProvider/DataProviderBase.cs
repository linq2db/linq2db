using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Linq;

namespace LinqToDB.DataProvider
{
	using Common;
	using Data;
	using Mapping;
	using SqlProvider;

	/// <summary>
	/// The <b>DataProviderBase</b> is a class that provides specific data provider information
	/// for the <see cref="DbManager"/> class. 
	/// </summary>
	/// <remarks>
	/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
	/// </remarks>
	/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
	public abstract partial class DataProviderBase : IMappingSchemaProvider
	{
		#region Abstract Properties

		/// <summary>
		/// Returns an actual type of the connection object used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <value>An instance of the <see cref="Type"/> class.</value>
		public abstract Type ConnectionType { get; }

		/// <summary>
		/// Returns the data manager name.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <value>The data manager name.</value>
		public abstract string Name { get; }

		private  string _uniqueName;
		/// <summary>
		/// Same as <see cref="Name"/>, but may be overridden to add two or more providers of same type.
		/// </summary>
		public string  UniqueName
		{
			get { return _uniqueName ?? Name; }
			internal set { _uniqueName = value; }
		}

		#endregion

		#region Abstract Methods

		/// <summary>
		/// Creates a new instance of the <see cref="IDbConnection"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>The <see cref="IDbConnection"/> object.</returns>
		public abstract IDbConnection CreateConnectionObject();

		/// <summary>
		/// Creates a new connection object with same connection string.
		/// </summary>
		/// <param name="connection">A connection object used as prototype.</param>
		/// <returns>New connection instance.</returns>
		public virtual IDbConnection CloneConnection(IDbConnection connection)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");

			var cloneable = connection as ICloneable;

			if (cloneable != null)
				return (IDbConnection)cloneable.Clone();

			var newConnection = CreateConnectionObject();

			// This is definitelly not enought when PersistSecurityInfo set to false.
			//
			newConnection.ConnectionString = connection.ConnectionString;

			return newConnection;
		}

		/// <summary>
		/// Creates an instance of the <see cref="DbDataAdapter"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <returns>The <see cref="DbDataAdapter"/> object.</returns>
		public abstract DbDataAdapter CreateDataAdapterObject();

		/// <summary>
		/// Populates the specified <see cref="IDbCommand"/> object's Parameters collection with 
		/// parameter information for the stored procedure specified in the <see cref="IDbCommand"/>.
		/// </summary>
		/// <remarks>
		/// See the <see cref="DbManager.AddDataProvider(DataProviderBase)"/> method to find an example.
		/// </remarks>
		/// <seealso cref="DbManager.AddDataProvider(DataProviderBase)">AddDataManager Method</seealso>
		/// <param name="command">The <see cref="IDbCommand"/> referencing the stored procedure 
		/// for which the parameter information is to be derived.
		/// The derived parameters will be populated into the Parameters of this command.</param>
		/// <returns>true - parameters can be derive.</returns>
		public abstract bool DeriveParameters(IDbCommand command);

		#endregion

		#region Factory methods

		public virtual IDbCommand CreateCommandObject(IDbConnection connection)
		{
			return connection.CreateCommand();
		}

		public virtual IDbDataParameter CreateParameterObject(IDbCommand command)
		{
			return command.CreateParameter();
		}

		#endregion

		#region IDbDataParameter methods

		public virtual IDbDataParameter GetParameter(
			IDbCommand           command,
			NameOrIndexParameter nameOrIndex)
		{
			return (IDbDataParameter)(nameOrIndex.ByName ?
				command.Parameters[nameOrIndex.Name] : command.Parameters[nameOrIndex.Index]);
		}

		public virtual void AttachParameter(
			IDbCommand       command,
			IDbDataParameter parameter)
		{
			command.Parameters.Add(parameter);
		}

		public virtual void SetUserDefinedType(IDbDataParameter _parameter, string typeName)
		{
			throw new NotSupportedException(Name + " data provider does not support UDT.");
		}

		public virtual bool IsValueParameter(IDbDataParameter parameter)
		{
			return parameter.Direction != ParameterDirection.ReturnValue;
		}

		public virtual IDbDataParameter CloneParameter(IDbDataParameter parameter)
		{
			return (IDbDataParameter)((ICloneable)parameter).Clone();
		}

		public virtual bool InitParameter(IDbDataParameter parameter)
		{
			return false;
		}

		#endregion

		#region Virtual Members

		public virtual object Convert(object value, ConvertType convertType)
		{
			return SqlProvider.Convert(value, convertType);
		}

		public virtual void InitDbManager(DbManager dbManager)
		{
			var schema = MappingSchema;

			if (schema != null)
				dbManager.MappingSchema = schema;
		}

		/// <summary>
		/// One time initialization from a configuration file.
		/// </summary>
		/// <param name="attributes">Provider specific attributes.</param>
		public virtual void Configure(System.Collections.Specialized.NameValueCollection attributes)
		{
		}

		public virtual MappingSchema MappingSchema { get; set; }

		public virtual void PrepareCommand(ref CommandType commandType, ref string commandText, ref IDbDataParameter[] commandParameters)
		{
			/*
			if (commandParameters != null) foreach (var p in commandParameters)
			{
				if (p.Value is System.Data.Linq.Binary)
				{
					var arr = ((System.Data.Linq.Binary)p.Value).ToArray();

					p.Value  = arr;
					p.DbType = DbType.Binary;
					p.Size   = arr.Length;
				}
			}
			*/
		}

		public virtual bool CanReuseCommand(IDbCommand command, CommandType newCommandType)
		{
			return true;
		}

		public virtual void SetParameterValue(IDbDataParameter parameter, object value)
		{
			if (value is System.Data.Linq.Binary)
			{
				var arr = ((System.Data.Linq.Binary)value).ToArray();

				parameter.Value  = arr;
				parameter.DbType = DbType.Binary;
				parameter.Size   = arr.Length;
			}
			else
				parameter.Value = value;
		}

		public abstract ISqlProvider CreateSqlProvider();

		private   ISqlProvider _sqlProvider;
		protected ISqlProvider  SqlProvider
		{
			get { return _sqlProvider ?? (_sqlProvider = CreateSqlProvider()); }
		}

		public virtual IDataReader GetDataReader(MappingSchema schema, IDataReader dataReader)
		{
			return dataReader;
		}

		public virtual bool ParameterNamesEqual(string paramName1, string paramName2)
		{
			// default implementation is case-insensitive, because if we make it 
			// case-sensitive and don't overload it in all existing providers - client code may break
			return string.Equals(paramName1, paramName2, StringComparison.OrdinalIgnoreCase);
		}

		public virtual DbType GetDbType(Type systemType)
		{
			if (systemType == typeof(Binary))
				return DbType.Binary;

			return DbType.Object;
		}

		public virtual string ProviderName  { get { return ConnectionType.Namespace; } }
		public virtual int    MaxParameters { get { return 100;   } }
		public virtual int    MaxBatchSize  { get { return 65536; } }
		public virtual string EndOfSql      { get { return ";";   } }

		#endregion

		#region DataReaderEx

		protected class DataReaderBase<T> : IDataReader
			where T: IDataReader
		{
			public readonly T DataReader;

			protected DataReaderBase(T rd)
			{
				DataReader = rd;
			}

			#region Implementation of IDisposable

			public void Dispose()
			{
				DataReader.Dispose();
			}

			#endregion

			#region Implementation of IDataRecord

			public string      GetName        (int i)           { return DataReader.GetName        (i); }
			public string      GetDataTypeName(int i)           { return DataReader.GetDataTypeName(i); }
			public Type        GetFieldType   (int i)           { return DataReader.GetFieldType   (i); }
			public object      GetValue       (int i)           { return DataReader.GetValue       (i); }
			public int         GetValues      (object[] values) { return DataReader.GetValues      (values); }
			public int         GetOrdinal     (string   name)   { return DataReader.GetOrdinal     (name);   }
			public bool        GetBoolean     (int i)           { return DataReader.GetBoolean     (i); }
			public byte        GetByte        (int i)           { return DataReader.GetByte        (i); }
			public char        GetChar        (int i)           { return DataReader.GetChar        (i); }
			public Guid        GetGuid        (int i)           { return DataReader.GetGuid        (i); }
			public short       GetInt16       (int i)           { return DataReader.GetInt16       (i); }
			public int         GetInt32       (int i)           { return DataReader.GetInt32       (i); }
			public long        GetInt64       (int i)           { return DataReader.GetInt64       (i); }
			public float       GetFloat       (int i)           { return DataReader.GetFloat       (i); }
			public double      GetDouble      (int i)           { return DataReader.GetDouble      (i); }
			public string      GetString      (int i)           { return DataReader.GetString      (i); }
			public decimal     GetDecimal     (int i)           { return DataReader.GetDecimal     (i); }
			public DateTime    GetDateTime    (int i)           { return DataReader.GetDateTime    (i); }
			public IDataReader GetData        (int i)           { return DataReader.GetData        (i); }
			public bool        IsDBNull       (int i)           { return DataReader.IsDBNull       (i); }

			public int FieldCount { get { return DataReader.FieldCount; } }

			object IDataRecord.this[int i]       { get { return ((IDataRecord)DataReader)[i];    } }
			object IDataRecord.this[string name] { get { return ((IDataRecord)DataReader)[name]; } }

			public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
			{
				return DataReader.GetBytes(i, fieldOffset, buffer, bufferoffset, length);
			}

			public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
			{
				return DataReader.GetChars(i, fieldoffset, buffer, bufferoffset, length);
			}

			#endregion

			#region Implementation of IDataReader

			public void      Close         () {        DataReader.Close         (); }
			public DataTable GetSchemaTable() { return DataReader.GetSchemaTable(); }
			public bool      NextResult    () { return DataReader.NextResult    (); }
			public bool      Read          () { return DataReader.Read          (); }
			public int       Depth           { get { return DataReader.Depth;           } }
			public bool      IsClosed        { get { return DataReader.IsClosed;        } }
			public int       RecordsAffected { get { return DataReader.RecordsAffected; } }

			#endregion
		}

		protected abstract class DataReaderEx<T> : DataReaderBase<T>, IDataReaderEx
			where T: IDataReader
		{
			protected DataReaderEx(T rd) : base(rd)
			{
			}

			#region Implementation of IDataReaderEx

			public abstract DateTimeOffset GetDateTimeOffset(int i);

			#endregion
		}

		#endregion

		#region InsertBatch

		public virtual int InsertBatch<T>(
			DbManager      db,
			string         insertText,
			IEnumerable<T> collection,
			MemberMapper[] members,
			int            maxBatchSize,
			DbManager.ParameterProvider<T> getParameters)
		{
			db.SetCommand(insertText);
			return db.ExecuteForEach(collection, members, maxBatchSize, getParameters);
		}

		#endregion
	}
}
