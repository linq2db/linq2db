namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Event arguments for <see cref="IEntityServiceInterceptor.EntityCreated(EntityCreatedEventData, object)"/> event.
	/// </summary>
	public readonly struct EntityCreatedEventData
	{
		internal EntityCreatedEventData(
			IDataContext context,
			TableOptions tableOptions,
			string?      tableName,
			string?      schemaName,
			string?      databaseName,
			string?      serverName)
		{
			Context      = context;
			TableOptions = tableOptions;
			TableName    = tableName;
			SchemaName   = schemaName;
			DatabaseName = databaseName;
			ServerName   = serverName;
		}

		/// <summary>
		/// Gets data context, associated with event.
		/// </summary>
		public IDataContext Context      { get; }

		/// <summary>
		/// Gets entity table options.
		/// </summary>
		public TableOptions TableOptions { get; }

		/// <summary>
		/// Gets entity table name.
		/// </summary>
		public string?      TableName    { get; }

		/// <summary>
		/// Gets entity schema name.
		/// </summary>
		public string?      SchemaName   { get; }

		/// <summary>
		/// Gets entity database name.
		/// </summary>
		public string?      DatabaseName { get; }

		/// <summary>
		/// Gets entity linked server name.
		/// </summary>
		public string?      ServerName   { get; }
	}
}
