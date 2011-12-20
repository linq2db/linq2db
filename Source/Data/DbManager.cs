using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Text;
using LinqToDB.DataProvider;

#region ReSharper disable
// ReSharper disable UnusedParameter.Local
#pragma warning disable 1589
#endregion

namespace LinqToDB.Data
{
	using Mapping;
	using Properties;
	using SqlBuilder;
	using SqlProvider;

	/// <summary>
	/// The <b>DbManager</b> is a primary class of the <see cref="LinqToDB.Data"/> namespace
	/// that can be used to execute commands of different database providers.
	/// </summary>
	/// <remarks>
	/// When the <b>DbManager</b> goes out of scope, it does not close the internal connection object.
	/// Therefore, you must explicitly close the connection by calling <see cref="Close"/> or 
	/// <see cref="Dispose"/>. Also, you can use the C# <b>using</b> statement.
	/// </remarks>
	/// <include file="Examples.xml" path='examples/db[@name="DbManager"]/*' />
	[DesignerCategory(@"Code")]
	public partial class DbManager
	{
		#region Init

		public DbManager(DataProviderBase dataProvider, string connectionString)
		{
			if (dataProvider     == null) throw new ArgumentNullException("dataProvider");
			if (connectionString == null) throw new ArgumentNullException("connectionString");

			_dataProvider = dataProvider;
			_connection   = dataProvider.CreateConnectionObject();

			_connection.ConnectionString = connectionString;

			_dataProvider.InitDbManager(this);
		}

		public DbManager(DataProviderBase dataProvider, IDbConnection connection)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (connection   == null) throw new ArgumentNullException("connection");

			_dataProvider = dataProvider;
			_connection   = connection;

			_dataProvider.InitDbManager(this);
		}

		public DbManager(DataProviderBase dataProvider, IDbTransaction transaction)
		{
			if (dataProvider == null) throw new ArgumentNullException("dataProvider");
			if (transaction  == null) throw new ArgumentNullException("transaction");

			_dataProvider     = dataProvider;
			_connection       = transaction.Connection;
			_transaction      = transaction;
			_closeTransaction = false;

			_dataProvider.InitDbManager(this);
		}

		DbManager(int n)
		{
		}

		public virtual DbManager Clone()
		{
			var clone =
				new DbManager(0)
				{
					_configurationString = _configurationString,
					_dataProvider        = _dataProvider,
					_mappingSchema       = _mappingSchema
				};

			if (_connection != null)
				clone._connection = CloneConnection();

			return clone;
		}

		public string LastQuery;

		#endregion

		#region Public Properties

		private MappingSchema _mappingSchema = Map.DefaultSchema;
		/// <summary>
		/// Gets the <see cref="LinqToDB.Mapping.MappingSchema"/> 
		/// used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// A mapping schema.
		/// </value>
		public MappingSchema MappingSchema
		{
			[DebuggerStepThrough]
			get { return _mappingSchema; }
			set { _mappingSchema = value ?? Map.DefaultSchema; }
		}

		private DataProviderBase _dataProvider;
		/// <summary>
		/// Gets the <see cref="DataProviderBase"/> 
		/// used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// A data provider.
		/// </value>
		/// <include file="Examples.xml" path='examples/db[@name="DataProvider"]/*' />
		public DataProviderBase DataProvider
		{
			[DebuggerStepThrough]
			get           { return _dataProvider;  }
			protected set { _dataProvider = value; }
		}

		private static TraceSwitch _traceSwitch;
		public  static TraceSwitch  TraceSwitch
		{
			get { return _traceSwitch ?? (_traceSwitch = new TraceSwitch("DbManager", "DbManager trace switch",
#if DEBUG
				"Warning"
#else
				"Off"
#endif
				)); }
			set { _traceSwitch = value; }
		}

		public static void TurnTraceSwitchOn()
		{
			TraceSwitch = new TraceSwitch("DbManager", "DbManager trace switch", "Info");
		}

		public static Action<string,string> WriteTraceLine = (message, displayName) => Debug.WriteLine(message, displayName);

		#endregion

		#region Connection

		private bool          _closeConnection;
		private IDbConnection _connection;
		/// <summary>
		/// Gets or sets the <see cref="IDbConnection"/> used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// The connection to the data source.
		/// </value>
		/// <remarks>
		/// Then you set a connection object, it has to match the data source type.
		/// </remarks>
		/// <exception cref="DataException">
		/// A connection does not match the data source type.
		/// </exception>
		/// <include file="Examples.xml" path='examples/db[@name="Connection"]/*' />
		public IDbConnection Connection
		{
			[DebuggerStepThrough]
			get
			{
				if (_connection.State == ConnectionState.Closed)
					OpenConnection();
				return _connection;
			}

			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value.GetType() != _dataProvider.ConnectionType)
					InitDataProvider(value);

				_connection      = value;
				_closeConnection = false;
			}
		}

		private void OpenConnection()
		{
			ExecuteOperation(OperationType.OpenConnection, _connection.Open);
			_closeConnection = true;
		}

		/// <summary>
		/// Closes the connection to the database.
		/// </summary>
		/// <remarks>
		/// The <b>Close</b> method rolls back any pending transactions
		/// and then closes the connection.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="Close()"]/*' />
		/// <seealso cref="Dispose"/>
		public void Close()
		{
			if (OnClosing != null)
				OnClosing(this, EventArgs.Empty);

			if (_selectCommand != null) { _selectCommand.Dispose(); _selectCommand = null; }

			if (_transaction != null && _closeTransaction)
			{
				ExecuteOperation(OperationType.DisposeTransaction, _transaction.Dispose);
				_transaction = null;
			}

			if (_connection != null && _closeConnection)
			{
				ExecuteOperation(OperationType.CloseConnection, _connection.Dispose);
				_connection = null;
			}

			if (OnClosed != null)
				OnClosed(this, EventArgs.Empty);
		}

		#endregion

		#region Transactions

		private bool           _closeTransaction = true;
		private IDbTransaction _transaction;
		/// <summary>
		/// Gets the <see cref="IDbTransaction"/> used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// The <see cref="IDbTransaction"/>. The default value is a null reference.
		/// </value>
		/// <remarks>
		/// You have to call the <see cref="BeginTransaction()"/> method to begin a transaction.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="Transaction"]/*' />
		/// <seealso cref="BeginTransaction()"/>
		public IDbTransaction Transaction
		{
			[DebuggerStepThrough]
			get { return _transaction; }
		}

		/// <summary>
		/// Begins a database transaction.
		/// </summary>
		/// <remarks>
		/// Once the transaction has completed, you must explicitly commit or roll back the transaction
		/// by using the <see cref="System.Data.IDbTransaction.Commit"/>> or 
		/// <see cref="System.Data.IDbTransaction.Rollback"/> methods.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="BeginTransaction()"]/*' />
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		/// <seealso cref="Transaction"/>
		public virtual DbManager BeginTransaction()
		{
			return BeginTransaction(IsolationLevel.ReadCommitted);
		}

		/// <summary>
		/// Begins a database transaction with the specified <see cref="IsolationLevel"/> value.
		/// </summary>
		/// <remarks>
		/// Once the transaction has completed, you must explicitly commit or roll back the transaction
		/// by using the <see cref="System.Data.IDbTransaction.Commit"/> or 
		/// <see cref="System.Data.IDbTransaction.Rollback"/> methods.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="BeginTransaction(IsolationLevel)"]/*' />
		/// <param name="il">One of the <see cref="IsolationLevel"/> values.</param>
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		public virtual DbManager BeginTransaction(IsolationLevel il)
		{
			// If transaction is open, we dispose it, it will rollback all changes.
			//
			if (_transaction != null)
			{
				ExecuteOperation(OperationType.DisposeTransaction, _transaction.Dispose);
			}

			// Create new transaction object.
			//
			_transaction = ExecuteOperation(
				OperationType.BeginTransaction,
				() => Connection.BeginTransaction(il));

			_closeTransaction = true;

			// If the active command exists.
			//
			if (_selectCommand != null) _selectCommand.Transaction = _transaction;

			return this;
		}

		/// <summary>
		/// Commits the database transaction.
		/// </summary>
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		public virtual DbManager CommitTransaction()
		{
			if (_transaction != null)
			{
				ExecuteOperation(OperationType.CommitTransaction, _transaction.Commit);

				if (_closeTransaction)
				{
					ExecuteOperation(OperationType.DisposeTransaction, _transaction.Dispose);
					_transaction = null;
				}
			}

			return this;
		}

		/// <summary>
		/// Rolls back a transaction from a pending state.
		/// </summary>
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		public virtual DbManager RollbackTransaction()
		{
			if (_transaction != null)
			{
				ExecuteOperation(OperationType.RollbackTransaction, _transaction.Rollback);

				if (_closeTransaction)
				{
					ExecuteOperation(OperationType.DisposeTransaction, _transaction.Dispose);
					_transaction = null;
				}
			}

			return this;
		}

		#endregion

		#region Commands

		private IDbCommand _selectCommand;
		/// <summary>
		/// Gets the <see cref="IDbCommand"/> used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// A <see cref="IDbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>Command</b> can be used to access command parameters.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="Command"]/*' />
		public IDbCommand Command
		{
			[DebuggerStepThrough]
			get { return SelectCommand; }
		}

		/// <summary>
		/// Gets the select <see cref="IDbCommand"/> used by this instance of the <see cref="DbManager"/>.
		/// </summary>
		/// <value>
		/// A <see cref="IDbCommand"/> used during executing query.
		/// </value>
		/// <remarks>
		/// The <b>SelectCommand</b> can be used to access select command parameters.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="Command"]/*' />
		public IDbCommand SelectCommand
		{
			[DebuggerStepThrough]
			get { return _selectCommand = OnInitCommand(_selectCommand); }
		}

		/// <summary>
		/// Initializes a command.
		/// </summary>
		protected virtual IDbCommand OnInitCommand(IDbCommand command)
		{
			if (command == null)
			{
				// Create a command object.
				//
				command = _dataProvider.CreateCommandObject(Connection);

				// If an active transaction exists.
				//
				if (Transaction != null)
				{
					command.Transaction = Transaction;
				}
			}

			return command;
		}

		/// <summary>
		/// Helper function. Creates the command object and sets command type and command text.
		/// </summary>
		/// <param name="commandType">The <see cref="System.Data.CommandType"/>
		/// (stored procedure, text, etc.)</param>
		/// <param name="sql">The SQL statement.</param>
		/// <returns>The command object.</returns>
		private IDbCommand GetCommand(CommandType commandType, string sql)
		{
			var command = GetCommand(commandType);

			command.Parameters.Clear();
			command.CommandType = commandType;
			command.CommandText = sql;

			return command;
		}

		#endregion

		#region Events

		public event EventHandler OnClosing;
		public event EventHandler OnClosed;

		#endregion

		#region Protected Methods

		private IDataReader ExecuteReaderInternal()
		{
			return ExecuteReader(CommandBehavior.Default);
		}

		private IDataReader ExecuteReaderInternal(CommandBehavior commandBehavior)
		{
			return ExecuteOperation(
				OperationType.ExecuteReader,
				() =>
					_dataProvider.GetDataReader(_mappingSchema, SelectCommand.ExecuteReader(commandBehavior)));
		}

		private int ExecuteNonQueryInternal()
		{
			return ExecuteOperation<int>(OperationType.ExecuteNonQuery, SelectCommand.ExecuteNonQuery);
		}

		#endregion

		#region Parameters

		///<summary>
		/// Creates an one-dimension array of <see cref="IDbDataParameter"/>
		/// from any combination on IDbDataParameter, IDbDataParameter[] or null references.
		/// Null references are stripped, arrays and single parameters are combined
		/// into a new array.
		///</summary>
		/// <remarks>When two or more parameters has the same name,
		/// the first parameter is used, all the rest are ignored.</remarks>
		///<param name="parameters">Array of IDbDataParameter, IDbDataParameter[] or null references.</param>
		///<returns>An normalized array of <see cref="IDbDataParameter"/> without null references.</returns>
		///<exception cref="ArgumentException">The parameter <paramref name="parameters"/>
		/// contains anything except IDbDataParameter, IDbDataParameter[] or null reference.</exception>
		public virtual IDbDataParameter[] PrepareParameters(object[] parameters)
		{
			if (parameters == null || parameters.Length == 0)
				return null;

			// Little optimization.
			// Check if we have only one single ref parameter.
			//
			object refParam = null;

			foreach (var p in parameters)
				if (p != null)
				{
					if (refParam != null)
					{
						refParam = null;
						break;
					}

					refParam = p;
				}

			if (refParam is IDbDataParameter[])
			{
				return (IDbDataParameter[])refParam;
			}

			if (refParam is IDbDataParameter)
			{
				var oneParameterArray = new IDbDataParameter[1];
				oneParameterArray[0] = (IDbDataParameter)refParam;
				return oneParameterArray;
			}

			var list = new List<IDbDataParameter>(parameters.Length);
			var hash = new Dictionary<string, IDbDataParameter>(parameters.Length);

			foreach (var o in parameters)
				if (o is IDbDataParameter)
				{
					var p = (IDbDataParameter) o;

					if (!hash.ContainsKey(p.ParameterName))
					{
						list.Add(p);
						hash.Add(p.ParameterName, p);
					}
				}
				else if (o is IDbDataParameter[])
				{
					foreach (var p in (IDbDataParameter[]) o)
						if (!hash.ContainsKey(p.ParameterName))
						{
							list.Add(p);
							hash.Add(p.ParameterName, p);
						}
				}
				else if (o != null && o != DBNull.Value)
					throw new ArgumentException(
						Resources.DbManager_NotDbDataParameter, "parameters");

			return list.ToArray();
		}

		/// <summary>
		/// This method is used to attach array of <see cref="IDbDataParameter"/> to a <see cref="IDbCommand"/>.
		/// </summary>
		/// <param name="command">The command to which the parameters will be added</param>
		/// <param name="commandParameters">An array of IDbDataParameters tho be added to command</param>
		private void AttachParameters(IDbCommand command, IEnumerable<IDbDataParameter> commandParameters)
		{
			command.Parameters.Clear();

			foreach (var p in commandParameters)
				_dataProvider.AttachParameter(command, p);
		}

		private static readonly Dictionary<string, IDbDataParameter[]> _paramCache =
			new Dictionary<string, IDbDataParameter[]>();
		private static readonly object _paramCacheLock = new object();

		/// <summary>
		/// Resolve at run time the appropriate set of parameters for a stored procedure.
		/// </summary>
		/// <param name="spName">The name of the stored procedure.</param>
		/// <param name="includeReturnValueParameter">Whether or not to include their return value parameter.</param>
		/// <param name="openNewConnection"></param>
		/// <returns></returns>
		protected virtual IDbDataParameter[] DiscoverSpParameters(string spName, bool includeReturnValueParameter, bool openNewConnection)
		{
			var con = openNewConnection ? CloneConnection() : _connection;

			try
			{
				if (con.State == ConnectionState.Closed)
				{
					ExecuteOperation(OperationType.OpenConnection, con.Open);
					if (!openNewConnection)
						_closeConnection = true;
				}

				using (var cmd = con.CreateCommand())
				{
					cmd.CommandType = CommandType.StoredProcedure;
					cmd.CommandText = spName;

					var res = ExecuteOperation(OperationType.DeriveParameters, () => _dataProvider.DeriveParameters(cmd));

					if (openNewConnection)
						ExecuteOperation(OperationType.CloseConnection, con.Close);

					if (res == false)
						return null;

					if (includeReturnValueParameter == false)
					{
						// All known data providers always treat
						// the return value as first parameter.
						//
						cmd.Parameters.RemoveAt(0);
					}

					var discoveredParameters = new IDbDataParameter[cmd.Parameters.Count];

					for (var i = 0; i < cmd.Parameters.Count; i++)
						discoveredParameters[i] = (IDbDataParameter)cmd.Parameters[i];

					return discoveredParameters;
				}
			}
			finally
			{
				if (con != null && openNewConnection)
					con.Dispose();
			}
		}

		/// <summary>
		/// Copies cached parameter array.
		/// </summary>
		/// <param name="originalParameters">The original parameter array.</param>
		/// <returns>The result array.</returns>
		private IDbDataParameter[] CloneParameters(IDbDataParameter[] originalParameters)
		{
			if (originalParameters == null)
				return null;

			var clonedParameters = new IDbDataParameter[originalParameters.Length];

			for (var i = 0; i < originalParameters.Length; i++)
				clonedParameters[i] = _dataProvider.CloneParameter(originalParameters[i]);

			return clonedParameters;
		}

		/// <summary>
		/// Retrieves the set of parameters appropriate for the stored procedure.
		/// </summary>
		/// <remarks>
		/// This method will query the database for this information, 
		/// and then store it in a cache for future requests.
		/// </remarks>
		/// <param name="spName">The name of the stored procedure.</param>
		/// <param name="includeReturnValueParameter">A boolean value indicating
		/// whether the return value parameter should be included in the results.</param>
		/// <param name="openNewConnectionToDiscoverParameters"></param>
		/// <returns>An array of the <see cref="IDbDataParameter"/>.</returns>
		public IDbDataParameter[] GetSpParameters(string spName, bool includeReturnValueParameter, bool openNewConnectionToDiscoverParameters)
		{
			var key = string.Format("{0}:{1}:{2}", GetConnectionHash(), spName, includeReturnValueParameter);

			IDbDataParameter[] cachedParameters;

			// It is thread safe enought to check for a key and get its value without a lock.
			//
			if (!_paramCache.TryGetValue(key, out cachedParameters))
			{
				lock (_paramCacheLock)
				{
					// There is a possible race condition since the operation may take a time.
					//
					if (!_paramCache.TryGetValue(key, out cachedParameters))
					{
						cachedParameters = DiscoverSpParameters(spName, includeReturnValueParameter, openNewConnectionToDiscoverParameters);
						_paramCache.Add(key, cachedParameters);
					}
				}
			}
		
			return CloneParameters(cachedParameters);
		}

		/// <overloads>
		/// Assigns a business object to command parameters.
		/// </overloads>
		/// <summary>
		/// Assigns the <see cref="DataRow"/> to command parameters.
		/// </summary>
		/// <include file="Examples1.xml" path='examples/db[@name="AssignParameterValues(DataRow)"]/*' />
		/// <remarks>
		/// The method is used in addition to the <see cref="CreateParameters(object,IDbDataParameter[])"/> method.
		/// </remarks>
		/// <param name="dataRow">The <see cref="DataRow"/> to assign.</param>
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		public DbManager AssignParameterValues(DataRow dataRow)
		{
			if (dataRow == null)
				throw new ArgumentNullException("dataRow");

			foreach (DataColumn c in dataRow.Table.Columns)
				if (c.AutoIncrement == false && c.ReadOnly == false)
				{
					var o = dataRow[c.ColumnName];
					var name = _dataProvider.Convert(c.ColumnName, GetConvertTypeToParameter()).ToString();

					Parameter(name).Value =
						c.AllowDBNull && _mappingSchema.IsNull(o) ? DBNull.Value : o;
				}

			if (_prepared)
				InitParameters();

			return this;
		}

		/// <summary>
		/// Assigns a business object to command parameters.
		/// </summary>
		/// <remarks>
		/// The method is used in addition to the <see cref="CreateParameters(object,IDbDataParameter[])"/> method.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="AssignParameterValues(object)"]/*' />
		/// <param name="obj">An object to assign.</param>
		/// <returns>This instance of the <see cref="DbManager"/>.</returns>
		public DbManager AssignParameterValues(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			var om = _mappingSchema.GetObjectMapper(obj.GetType());

			foreach (MemberMapper mm in om)
			{
				var name = _dataProvider.Convert(mm.Name, GetConvertTypeToParameter()).ToString();

				if (Command.Parameters.Contains(name))
				{
					var value = mm.GetValue(obj);

					_dataProvider.SetParameterValue(
						Parameter(name),
						value == null || mm.MapMemberInfo.Nullable && _mappingSchema.IsNull(value)?
							DBNull.Value: value);
				}
			}

			if (_prepared)
				InitParameters();

			return this;
		}

		private static Array SortArray(Array array, IComparer comparer)
		{
			if (array == null)
				return null;

			var arrayClone = (Array)array.Clone();

			Array.Sort(arrayClone, comparer);

			return arrayClone;
		}

		/// <summary>
		/// Creates an array of parameters from the <see cref="DataRow"/> object.
		/// </summary>
		/// <remarks>
		/// The method can take an additional parameter list, 
		/// which can be created by using the same method.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="CreateParameters(DataRow,IDbDataParameter[])"]/*' />
		/// <param name="dataRow">The <see cref="DataRow"/> to create parameters.</param>
		/// <param name="commandParameters">An array of parameters to be added to the result array.</param>
		/// <returns>An array of parameters.</returns>
		public IDbDataParameter[] CreateParameters(
			DataRow dataRow, params IDbDataParameter[] commandParameters)
		{
			return CreateParameters(dataRow, null, null, null, commandParameters);
		}

		/// <summary>
		/// Creates an array of parameters from the <see cref="DataRow"/> object.
		/// </summary>
		/// <remarks>
		/// The method can take an additional parameter list, 
		/// which can be created by using the same method.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="CreateParameters(DataRow,IDbDataParameter[])"]/*' />
		/// <param name="dataRow">The <see cref="DataRow"/> to create parameters.</param>
		/// <param name="outputParameters">Output parameters names.</param>
		/// <param name="inputOutputParameters">InputOutput parameters names.</param>
		/// <param name="ignoreParameters">Parameters names to skip.</param>
		/// <param name="commandParameters">An array of parameters to be added to the result array.</param>
		/// <returns>An array of parameters.</returns>
		public IDbDataParameter[] CreateParameters(
			DataRow                   dataRow,
			string[]                  outputParameters,
			string[]                  inputOutputParameters,
			string[]                  ignoreParameters,
			params IDbDataParameter[] commandParameters)
		{
			if (dataRow == null)
				throw new ArgumentNullException("dataRow");

			var paramList = new ArrayList();
			IComparer comparer  = CaseInsensitiveComparer.Default;

			outputParameters      = (string[])SortArray(outputParameters,      comparer);
			inputOutputParameters = (string[])SortArray(inputOutputParameters, comparer);
			ignoreParameters      = (string[])SortArray(ignoreParameters,      comparer);

			foreach (DataColumn c in dataRow.Table.Columns)
			{
				if (ignoreParameters != null && Array.BinarySearch(ignoreParameters, c.ColumnName, comparer) >= 0)
					continue;

				if (c.AutoIncrement || c.ReadOnly)
					continue;

				var name = _dataProvider.Convert(c.ColumnName, GetConvertTypeToParameter()).ToString();
				var parameter =
					c.AllowDBNull
						? NullParameter(name, dataRow[c.ColumnName])
						: Parameter    (name, dataRow[c.ColumnName]);

				if (outputParameters != null && Array.BinarySearch(outputParameters, c.ColumnName, comparer) >= 0)
					parameter.Direction = ParameterDirection.Output;
				else if (inputOutputParameters != null && Array.BinarySearch(inputOutputParameters, c.ColumnName, comparer) >= 0)
					parameter.Direction = ParameterDirection.InputOutput;

				paramList.Add(parameter);
			}

			if (commandParameters != null)
				paramList.AddRange(commandParameters);

			return (IDbDataParameter[])paramList.ToArray(typeof(IDbDataParameter));
		}

		/// <summary>
		/// Creates an array of parameters from a business object.
		/// </summary>
		/// <remarks>
		/// The method can take an additional parameter list, 
		/// which can be created by using the same method.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="CreateParameters(object,IDbDataParameter[])"]/*' />
		/// <param name="obj">An object.</param>
		/// <param name="commandParameters">An array of parameters to be added to the result array.</param>
		/// <returns>An array of parameters.</returns>
		public IDbDataParameter[] CreateParameters(
			object                    obj,
			params IDbDataParameter[] commandParameters)
		{
			return CreateParameters(obj, null, null, null, commandParameters);
		}

		/// <summary>
		/// Creates an array of parameters from a business object.
		/// </summary>
		/// <remarks>
		/// The method can take an additional parameter list, 
		/// which can be created by using the same method.
		/// </remarks>
		/// <include file="Examples.xml" path='examples/db[@name="CreateParameters(object,IDbDataParameter[])"]/*' />
		/// <param name="obj">An object.</param>
		/// <param name="outputParameters">Output parameters names.</param>
		/// <param name="inputOutputParameters">InputOutput parameters names.</param>
		/// <param name="ignoreParameters">Parameters names to skip.</param>
		/// <param name="commandParameters">An array of parameters to be added to the result array.</param>
		/// <returns>An array of parameters.</returns>
		public IDbDataParameter[] CreateParameters(
			object                    obj,
			string[]                  outputParameters,
			string[]                  inputOutputParameters,
			string[]                  ignoreParameters,
			params IDbDataParameter[] commandParameters)
		{
			if (obj == null)
				throw new ArgumentNullException("obj");

			var isType    = obj is Type;
			var type      = isType? (Type)obj: obj.GetType();
			var om        = _mappingSchema.GetObjectMapper(type);
			var paramList = new ArrayList();
			var comparer  = CaseInsensitiveComparer.Default;

			outputParameters       = (string[])SortArray(outputParameters,      comparer);
			inputOutputParameters  = (string[])SortArray(inputOutputParameters, comparer);
			ignoreParameters       = (string[])SortArray(ignoreParameters,      comparer);

			foreach (MemberMapper mm in om)
			{
				if (ignoreParameters != null && Array.BinarySearch(ignoreParameters, mm.Name, comparer) >= 0)
					continue;
				
				var value = isType? null: mm.GetValue(obj);
				var name  = _dataProvider.Convert(mm.Name, GetConvertTypeToParameter()).ToString();

				var parameter =
					value == null ?
						NullParameter(name, null, mm.MapMemberInfo.NullValue) :
						(mm.DbType != DbType.Object) ?
							Parameter(name, value, mm.DbType):
							Parameter(name, value);

				if (outputParameters != null && Array.BinarySearch(outputParameters, mm.Name, comparer) >= 0)
					parameter.Direction = ParameterDirection.Output;
				else if (inputOutputParameters != null && Array.BinarySearch(inputOutputParameters, mm.Name, comparer) >= 0)
					parameter.Direction = ParameterDirection.InputOutput;

				paramList.Add(parameter);
			}

			if (commandParameters != null)
				paramList.AddRange(commandParameters);

			return (IDbDataParameter[])paramList.ToArray(typeof(IDbDataParameter));
		}

		/// <summary>
		/// Maps all parameters returned from the server to an object.
		/// </summary>
		/// <param name="obj">An <see cref="System.Object"/> to map from command parameters.</param>
		public void MapOutputParameters(object obj)
		{
			MapOutputParameters(null, obj);
		}

		/// <summary>
		/// Maps all parameters returned from the server to all given objects.
		/// </summary>
		/// <param name="returnValueMember">Name of the member used to map the
		/// return value. Can be null.</param>
		/// <param name="objects">An array of <see cref="System.Object"/> to map
		/// from command parameters.</param>
		public void MapOutputParameters(string returnValueMember, params object[] objects)
		{
			if (objects == null)
				return;

			foreach (var obj in objects)
				MapOutputParameters(returnValueMember, obj);
		}

		/// <summary>
		/// Maps all parameters returned from the server to an object.
		/// </summary>
		/// <param name="objects">An array of <see cref="System.Object"/> to map
		/// from command parameters.</param>
		public void MapOutputParameters(params object[] objects)
		{
			MapOutputParameters(null, objects);
		}

		/// <overloads>
		/// Adds a parameter to the <see cref="Command"/> or returns existing one.
		/// </overloads>
		/// <summary>
		/// Returns an existing parameter.
		/// </summary>
		/// <remarks>
		/// The method can be used to retrieve return and output parameters.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="Parameter(string)"]/*' />
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(string parameterName)
		{
			return _dataProvider.GetParameter(Command, parameterName);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="Parameter(string,object)"]/*' />
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(string parameterName, object value)
		{
			return Parameter(ParameterDirection.Input, parameterName, value);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(string parameterName, DbType dbType)
		{
			return Parameter(ParameterDirection.Input, parameterName, dbType);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(string parameterName, DbType dbType, int size)
		{
			return Parameter(ParameterDirection.Input, parameterName, dbType, size);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// If the parameter is null, it's converted to <see cref="DBNull"/>.<see cref="DBNull.Value"/>.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="NullParameter(string,object)"]/*' />
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter NullParameter(string parameterName, object value)
		{
			if (_mappingSchema.IsNull(value))
				@value = DBNull.Value;

			return Parameter(ParameterDirection.Input, parameterName, value);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="nullValue">The null equivalent to compare with the value.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter NullParameter(string parameterName, object value, object nullValue)
		{
			if (value == null || value.Equals(nullValue))
				@value = DBNull.Value;

			return Parameter(ParameterDirection.Input, parameterName, value);
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter InputParameter(string parameterName, object value)
		{
			return Parameter(ParameterDirection.Input, parameterName, value);
		}

		/// <summary>
		/// Adds an output parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Output</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter OutputParameter(string parameterName, object value)
		{
			return Parameter(ParameterDirection.Output, parameterName, value);
		}

		/// <summary>
		/// Adds an output parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Output</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter OutputParameter(string parameterName, DbType dbType)
		{
			return Parameter(ParameterDirection.Output, parameterName, dbType);
		}

		/// <summary>
		/// Adds an output parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Output</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter OutputParameter(string parameterName, DbType dbType, int size)
		{
			return Parameter(ParameterDirection.Output, parameterName, dbType, size);
		}

		/// <summary>
		/// Adds an input-output parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.InputOutput</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter InputOutputParameter(string parameterName, object value)
		{
			return Parameter(ParameterDirection.InputOutput,parameterName, value);
		}

		/// <summary>
		/// Adds a return value parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.ReturnValue</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter ReturnValue(string parameterName)
		{
			return Parameter(ParameterDirection.ReturnValue, parameterName, null);
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string             parameterName,
			object             value)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;

			_dataProvider.SetParameterValue(parameter, value ?? DBNull.Value);

			return parameter;
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string             parameterName,
			object             value,
			DbType             dbType)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;
			parameter.DbType        = dbType;

			_dataProvider.SetParameterValue(parameter, value ?? DBNull.Value);

			return parameter;
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			object value,
			DbType dbType)
		{
			return Parameter(ParameterDirection.Input, parameterName, value, dbType);
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string             parameterName,
			object             value,
			DbType             dbType,
			int                size)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;
			parameter.DbType        = dbType;
			parameter.Size          = size;

			_dataProvider.SetParameterValue(parameter, value);

			return parameter;
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="typeName">User defined type name for a table-valued parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string             parameterName,
			object             value,
			string             typeName)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;
			_dataProvider.SetUserDefinedType(parameter, typeName);
			_dataProvider.SetParameterValue (parameter, value);

			return parameter;
		}

		/// <summary>
		/// Adds an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="value">The <see cref="System.Object"/>
		/// that is the value of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			object value,
			DbType dbType,
			int    size)
		{
			return Parameter(ParameterDirection.Input, parameterName, value, dbType, size);
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string             parameterName,
			DbType             dbType)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;
			parameter.DbType        = dbType;

			return parameter;
		}

		/// <summary>
		/// Adds a parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the specified
		/// <see cref="System.Data.ParameterDirection"/> type.
		/// </remarks>
		/// <param name="parameterDirection">One of the <see cref="System.Data.ParameterDirection"/> values.</param>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <seealso cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			ParameterDirection parameterDirection,
			string parameterName,
			DbType dbType,
			int    size)
		{
			var parameter = _dataProvider.CreateParameterObject(Command);

			parameter.ParameterName = parameterName;
			parameter.Direction     = parameterDirection;
			parameter.DbType        = dbType;
			parameter.Size          = size;

			return parameter;
		}

		/// <summary>
		/// Creates an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type
		/// and <see cref="System.Data.DataRowVersion">DataRowVersion.Current</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <param name="sourceColumn">Source column for a parameter in the <see cref="DataTable"/>.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			DbType dbType,
			int    size,
			string sourceColumn)
		{
			var param = Parameter(ParameterDirection.Input, parameterName, dbType, size);

			param.SourceColumn  = sourceColumn;
			param.SourceVersion = DataRowVersion.Current;

			return param;
		}

		/// <summary>
		/// Creates an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type
		/// and <see cref="System.Data.DataRowVersion">DataRowVersion.Current</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="sourceColumn">Source column for a parameter in the <see cref="DataTable"/>.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			DbType dbType,
			string sourceColumn)
		{
			var param = Parameter(ParameterDirection.Input, parameterName, dbType);

			param.SourceColumn  = sourceColumn;
			param.SourceVersion = DataRowVersion.Current;

			return param;
		}

		/// <summary>
		/// Creates an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type
		/// and <see cref="System.Data.DataRowVersion">DataRowVersion.Current</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="size">Size of the parameter.</param>
		/// <param name="sourceColumn">Source column for a parameter in the <see cref="DataTable"/>.</param>
		/// <param name="dataRowVersion">Version of data to use for a parameter in the <see cref="DataTable"/>.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			DbType dbType,
			int    size,
			string sourceColumn,
			DataRowVersion dataRowVersion)
		{
			var param = Parameter(ParameterDirection.Input, parameterName, dbType, size);

			param.SourceColumn  = sourceColumn;
			param.SourceVersion = dataRowVersion;

			return param;
		}

		/// <summary>
		/// Creates an input parameter to the <see cref="Command"/>.
		/// </summary>
		/// <remarks>
		/// The method creates a parameter with the
		/// <see cref="System.Data.ParameterDirection">ParameterDirection.Input</see> type
		/// and <see cref="System.Data.DataRowVersion">DataRowVersion.Current</see> type.
		/// </remarks>
		/// <param name="parameterName">The name of the parameter.</param>
		/// <param name="dbType">One of the <see cref="DbType"/> values.</param>
		/// <param name="sourceColumn">Source column for a parameter in the <see cref="DataTable"/>.</param>
		/// <param name="dataRowVersion">Version of data to use for a parameter in the <see cref="DataTable"/>.</param>
		/// <returns>The <see cref="IDbDataParameter"/> object.</returns>
		public IDbDataParameter Parameter(
			string parameterName,
			DbType dbType,
			string sourceColumn,
			DataRowVersion dataRowVersion)
		{
			var param = Parameter(ParameterDirection.Input, parameterName, dbType);

			param.SourceColumn  = sourceColumn;
			param.SourceVersion = dataRowVersion;

			return param;
		}

		public ConvertType GetConvertTypeToParameter()
		{
			return Command.CommandType == CommandType.StoredProcedure ?
				ConvertType.NameToSprocParameter:
				ConvertType.NameToCommandParameter;
		}

		#endregion

		#region SetCommand

		private bool _executed;
		private bool _prepared;

		private IDbDataParameter[] _selectCommandParameters;

		private void SetCommand(IDbCommand command)
		{
			_selectCommand = command;
		}

		private IDbCommand GetCommand()
		{
			return SelectCommand;
		}

		private IDbCommand GetCommand(CommandType commandType)
		{
			var command = _selectCommand;

			if (command != null && !DataProvider.CanReuseCommand(command, commandType))
			{
				command.Dispose();
				_selectCommand = null;
			}

			return GetCommand();
		}

		private void SetCommandParameters(IDbDataParameter[] commandParameters)
		{
			_selectCommandParameters = commandParameters;
		}

		private IDbDataParameter[] GetCommandParameters()
		{
			return _selectCommandParameters;
		}

		private DbManager SetCommand(
			CommandType   commandType,
			string        commandText,
			params        IDbDataParameter[] commandParameters)
		{
			if (_executed)
			{
				_executed = false;
				_prepared = false;
			}

			PrepareCommand(commandType, commandText, commandParameters);
			
			return this;
		}

		#region Select

		/// <summary>
		/// Creates a SQL statement.
		/// </summary>
		/// <param name="commandText">The command text to execute.</param>
		/// <returns>Current instance.</returns>
		public DbManager SetCommand(
			string commandText)
		{
			return SetCommand(CommandType.Text, commandText, null);
		}

		/// <summary>
		/// Creates a SQL statement.
		/// </summary>
		/// <param name="commandType">The <see cref="System.Data.CommandType"/> (stored procedure, text, etc.)</param>
		/// <param name="commandText">The command text to execute.</param>
		/// <returns>Current instance.</returns>
		public DbManager SetCommand(
			CommandType commandType,
			string      commandText)
		{
			return SetCommand(commandType, commandText, null);
		}

		/// <summary>
		/// Creates a SQL statement.
		/// </summary>
		/// <remarks>
		/// The method can be used to create the <i>INSERT</i>, <i>UPDATE</i>, and <i>DELETE</i> SQL statements.
		/// </remarks>
		/// <param name="commandText">The command text to execute.</param>
		/// <param name="commandParameters">An array of parameters used to executes the command.</param>
		/// <returns>Current instance.</returns>
		public DbManager SetCommand(
			string commandText,
			params IDbDataParameter[] commandParameters)
		{
			return SetCommand(CommandType.Text, commandText, commandParameters);
		}

		public DbManager SetCommand(SqlQuery sql, params IDbDataParameter[] commandParameters)
		{
			var sb = new StringBuilder();

			DataProvider.CreateSqlProvider().BuildSql(0, sql, sb, 0, 0, false);

			var command = sb.ToString();

			if (TraceSwitch.TraceInfo)
			{
				var info = string.Format("{0} {1}\n{2}", DataProvider.Name, ConfigurationString, command);

				if (commandParameters != null && commandParameters.Length > 0)
					foreach (var p in commandParameters)
						info += string.Format("\n{0}\t{1}", p.ParameterName, p.Value);

				WriteTraceLine(info, TraceSwitch.DisplayName);
			}

			return SetCommand(command, commandParameters);
		}

		#endregion

		#endregion

		#region Prepare

		private void PrepareCommand(
			CommandType        commandType,
			string             commandText,
			IDbDataParameter[] commandParameters)
		{
			DataProvider.PrepareCommand(ref commandType, ref commandText, ref commandParameters);

			LastQuery = commandText;

			var command = GetCommand(commandType, commandText);

			SetCommand          (command);
			SetCommandParameters(commandParameters);

			if (commandParameters != null)
			{
				AttachParameters(command, commandParameters);
			}
		}

		/// <summary>
		/// Prepares a command for execution.
		/// </summary>
		/// <returns>Current instance.</returns>
		public DbManager Prepare()
		{
			var command = GetCommand();

			if (InitParameters() == false)
				ExecuteOperation(OperationType.PrepareCommand, command.Prepare);

			_prepared = true;

			return this;
		}

		bool InitParameters()
		{
			var prepare = false;

			var commandParameters = GetCommandParameters();

			if (commandParameters != null)
			{
				foreach (var p in commandParameters)
				{
					if (_dataProvider.InitParameter(p))
						continue;

					// It forces parameter's filed 'MetaType' to be set.
					// Same for p.Size = p.Size below.
					//
					p.DbType = p.DbType;

					if (p.Value is string)
					{
						var len = ((string)p.Value).Length;

						if (p.Size < len)
						{
							p.Size  = len;
							prepare = true;
						}
						else
							p.Size = p.Size;
					}
					else if (p.Value is DBNull)
					{
						p.Size = 1;
					}
					else if (p.Value is byte[])
					{
						var len = ((byte[])p.Value).Length;

						if (p.Size < len)
						{
							p.Size  = len;
							prepare = true;
						}
						else
							p.Size  = p.Size;
					}
					else if (p.Value is char[])
					{
						var len = ((char[])p.Value).Length;

						if (p.Size < len)
						{
							p.Size  = len;
							prepare = true;
						}
						else
							p.Size  = p.Size;
					}
					else if (p.Value is decimal)
					{
						SqlDecimal d = (decimal)p.Value;

						if (p.Precision < d.Precision)
						{
							p.Precision = d.Precision;
							prepare = true;
						}
						else
							p.Precision = p.Precision;

						if (p.Scale < d.Scale)
						{
							p.Scale = d.Scale;
							prepare = true;
						}
						else
							p.Scale = p.Scale;
					}
				}

				// Re-prepare command to avoid truncation.
				//
				if (prepare)
				{
					var command = GetCommand();

					AttachParameters(command, commandParameters);
					command.Prepare();
				}
			}

			return prepare;
		}

		#endregion

		#region ExecuteForEach

		/// <summary>
		/// Executes a SQL statement for a given collection of objects and 
		/// returns the number of rows affected.
		/// </summary>
		/// <remarks>
		/// The method prepares the <see cref="Command"/> object 
		/// and calls the <see cref="ExecuteNonQuery()"/> method for each item of the list.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="Execute(CommandType,string,IList)"]/*' />
		/// <param name="collection">The list of objects used to execute the command.</param>
		/// <returns>The number of rows affected by the command.</returns>
		public int ExecuteForEach(ICollection collection)
		{
			var rowsTotal = 0;

			if (collection != null && collection.Count != 0)
			{
				var initParameters = true;

				foreach (var o in collection)
				{
					if (initParameters)
					{
						initParameters = false;

						var parameters = GetCommandParameters();

						if (parameters == null || parameters.Length == 0)
						{
							parameters = CreateParameters(o);

							SetCommandParameters(parameters);
							AttachParameters(SelectCommand, parameters);
							Prepare();
						}
					}

					AssignParameterValues(o);
					rowsTotal += ExecuteNonQueryInternal();
					MapOutputParameters(o);
				}
			}
		
			return rowsTotal;
		}

		/// <summary>
		/// Executes a SQL statement for a given collection of objects and 
		/// returns the number of rows affected.
		/// </summary>
		/// <remarks>
		/// The method prepares the <see cref="Command"/> object 
		/// and calls the <see cref="ExecuteNonQuery()"/> method for each item of the list.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="Execute(CommandType,string,IList)"]/*' />
		/// <param name="collection">The list of objects used to execute the command.</param>
		/// <returns>The number of rows affected by the command.</returns>
		public int ExecuteForEach<T>(ICollection<T> collection)
		{
			var rowsTotal = 0;

			if (collection != null && collection.Count != 0)
			{
				var initParameters = true;

				foreach (var o in collection)
				{
					if (initParameters)
					{
						initParameters = false;

						var parameters = GetCommandParameters();

						if (parameters == null || parameters.Length == 0)
						{
							parameters = CreateParameters(o);

							SetCommandParameters(parameters);
							AttachParameters(SelectCommand, parameters);
							Prepare();
						}
					}

					AssignParameterValues(o);
					rowsTotal += ExecuteNonQueryInternal();
					MapOutputParameters(o);
				}
			}

			return rowsTotal;
		}

		public int ExecuteForEach<T>(int maxBatchSize, IEnumerable<T> collection)
		{
			var om  = _mappingSchema.GetObjectMapper(typeof(T));
			var mms = new List<MemberMapper>();

			foreach (MemberMapper mm in om)
			{
				var name = _dataProvider.Convert(mm.Name, GetConvertTypeToParameter()).ToString();

				if (Command.Parameters.Contains(name))
					mms.Add(mm);
			}

			return
				ExecuteForEach(
					collection,
					mms.ToArray(),
					maxBatchSize,
					obj => CreateParameters(obj));
		}

		public delegate IDbDataParameter[] ParameterProvider<T>(T obj);

		internal int ExecuteForEach<T>(IEnumerable<T> collection, MemberMapper[] members, int maxBatchSize, ParameterProvider<T> getParameters)
		{
			if (collection == null)
				return 0;

			var maxRows =
				Math.Max(
					Math.Min(
						Math.Max(
							members.Length == 0? 1000 : _dataProvider.MaxParameters / members.Length,
							members.Length),
						maxBatchSize),
					1);
			var baseSql          = SelectCommand.CommandText;
			var paramName        = _dataProvider.Convert(".", ConvertType.NameToQueryParameter).ToString();
			var rowsTotal        = 0;
			var nRows            = 0;
			var initParameters   = true;

			var                sb             = new StringBuilder();
			var                rowSql         = new List<int>(maxRows);
			IDbDataParameter[] baseParameters = null;
			var                parameters     = new List<IDbDataParameter>();
			var                hasValue       = new List<bool>();

			var  isPrepared = false;

			foreach (var obj in collection)
			{
				if (initParameters)
				{
					initParameters = false;
					baseParameters = getParameters(obj);

					if (maxRows != 1)
					{
						var n = 0;

						foreach (var p in baseParameters)
							n += p.ParameterName.Length + 3 - "{0}".Length + _dataProvider.EndOfSql.Length;

						maxRows = Math.Max(1, Math.Min(maxRows, _dataProvider.MaxBatchSize / (baseSql.Length + n)));
					}

					if (maxRows != 1)
						baseSql += _dataProvider.EndOfSql;
				}

				if (rowSql.Count < maxRows)
				{
// ReSharper disable AccessToModifiedClosure
					Converter<IDbDataParameter,string> c1 = p => p.ParameterName + nRows;
// ReSharper restore AccessToModifiedClosure
					Converter<IDbDataParameter,string> c2 = p => p.ParameterName;

					sb
						.Append("\n")
						.AppendFormat(
							baseSql,
							Array.ConvertAll(
								baseParameters,
								baseParameters.Length > 0 && baseParameters[0].ParameterName != paramName? c1 : c2));

					rowSql.Add(sb.Length);

					for (var i = 0; i < members.Length; i++)
					{
						var value  = members[i].GetValue(obj);
						var type   = members[i].MemberAccessor.Type;
						//var dbType = members[i].GetDbType();

						IDbDataParameter p;

						if ((value == null || value == DBNull.Value) && type == typeof(byte[]) || type == typeof(System.Data.Linq.Binary))
						{
							p = Parameter(baseParameters[i].ParameterName + nRows, DBNull.Value, DbType.Binary);
						}
						else
						{
							if (value != null && value.GetType().IsEnum)
								value = MappingSchema.MapEnumToValue(value, true);

							p = Parameter(baseParameters[i].ParameterName + nRows, value ?? DBNull.Value/*, dbType*/);
						}

						parameters.Add(p);
						hasValue.Add(value != null);
					}
				}
				else
				{
					var n = nRows * members.Length;

					for (var i = 0; i < members.Length; i++)
					{
						var value = members[i].GetValue(obj);

						if (!hasValue[n + i] && value != null)
						{
							isPrepared = false;

							if (value.GetType().IsEnum)
								value = MappingSchema.MapEnumToValue(value, true);

							var p = Parameter(baseParameters[i].ParameterName + nRows, value ?? DBNull.Value/*, dbType*/);

							parameters[n + i] = p;
							hasValue  [n + i] = true;
						}
						else
						{
							if (value != null && value.GetType().IsEnum)
								value = MappingSchema.MapEnumToValue(value, true);

							_dataProvider.SetParameterValue(
								parameters[n + i],
								value ?? DBNull.Value);
						}

					}
				}

				nRows++;

				if (nRows >= maxRows)
				{
					if (!isPrepared)
					{
						SetCommand(sb.ToString(), parameters.ToArray());
						Prepare();
						isPrepared = true;
					}
					else
					{
						InitParameters();
					}

					var n = ExecuteNonQueryInternal();
					if (n > 0)
						rowsTotal += n;

					nRows = 0;
				}
			}

			if (nRows > 0)
			{
				if (rowSql.Count >= maxRows)
				{
					var nps = nRows * members.Length;
					parameters.RemoveRange(nps, parameters.Count - nps);

					sb.Length = rowSql[nRows - 1];
				}

				SetCommand(sb.ToString(), parameters.ToArray());
				Prepare();

				var n = ExecuteNonQueryInternal();
				if (n > 0)
					rowsTotal += n;
			}

			return rowsTotal;
		}

		#endregion

		#region ExecuteNonQuery

		/// <summary>
		/// Executes a SQL statement and returns the number of rows affected.
		/// </summary>
		/// <remarks>
		/// The method can be used to execute the <i>INSERT</i>, <i>UPDATE</i>, and <i>DELETE</i> SQL statements.
		/// </remarks>
		/// <include file="Examples1.xml" path='examples/db[@name="ExecuteNonQuery()"]/*' />
		/// <returns>The number of rows affected by the command.</returns>
		public int ExecuteNonQuery()
		{
			if (_prepared)
				InitParameters();

			return ExecuteNonQueryInternal();
		}

		#endregion

		#region ExecuteScalar

		/// <summary>
		/// Executes the query, and returns the first column of the first row
		/// in the resultset returned by the query. Extra columns or rows are
		/// ignored.
		/// </summary>
		/// <returns>The first column of the first row in the resultset.</returns>
		public object ExecuteScalar()
		{
			if (_prepared)
				InitParameters();

			using (var rd = ExecuteReaderInternal(CommandBehavior.Default))
				return rd.Read() && rd.FieldCount > 0 ? rd.GetValue(0) : null;
		}

		#endregion

		#region ExecuteReader

		/// <summary>
		/// Executes the command and builds an <see cref="IDataReader"/>.
		/// </summary>
		/// <returns>An instance of the <see cref="IDataReader"/> class.</returns>
		public IDataReader ExecuteReader()
		{
			if (_prepared)
				InitParameters();

			return ExecuteReaderInternal();
		}

		/// <summary>
		/// Executes the command and builds an <see cref="IDataReader"/>.
		/// </summary>
		/// <param name="commandBehavior">One of the <see cref="CommandBehavior"/> values.</param>
		/// <returns>An instance of the <see cref="IDataReader"/> class.</returns>
		public IDataReader ExecuteReader(CommandBehavior commandBehavior)
		{
			if (_prepared)
				InitParameters();

			return ExecuteReaderInternal(commandBehavior);
		}

		#endregion

		#region ExecuteOperation

		private void ExecuteOperation(OperationType operationType, Action operation)
		{
			try
			{
				operation();
			}
			catch (Exception ex)
			{
				HandleOperationException(operationType, ex);
				throw;
			}
		}

		private T ExecuteOperation<T>(OperationType operationType, Func<T> operation)
		{
			try
			{
				return operation();
			}
			catch (Exception ex)
			{
				HandleOperationException(operationType, ex);
				throw;
			}
		}

		private void HandleOperationException(OperationType op, Exception ex)
		{
			var dex = new DataException(this, ex);

			if (TraceSwitch.TraceError)
				WriteTraceLine(string.Format("Operation '{0}' throws exception '{1}'", op, dex), TraceSwitch.DisplayName);

			throw dex;
		}

		#endregion

		#region IDisposable interface

		/// <summary>
		/// Releases the unmanaged resources used by the <see cref="DbManager"/> and 
		/// optionally releases the managed resources.
		/// </summary>
		/// <remarks>
		/// This method is called by the public <see cref="IDisposable.Dispose()"/> method 
		/// and the Finalize method.
		/// </remarks>
		public void Dispose()
		{
			Close();
		}

		#endregion
	}
}
