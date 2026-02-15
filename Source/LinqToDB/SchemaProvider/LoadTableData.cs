namespace LinqToDB.SchemaProvider
{
	/// <summary>
	/// Contains table information, passed to <see cref="GetSchemaOptions.LoadTable"/> delegate.
	/// </summary>
	public readonly struct LoadTableData
	{
		private readonly TableInfo _info;

		internal LoadTableData(TableInfo info)
		{
			_info = info;
		}

		/// <summary>
		/// Gets table database/catalog name. Could be <see langword="null"/> for some providers.
		/// </summary>
		public string? Database        => _info.CatalogName;

		/// <summary>
		/// Gets table schema/owner name. Could be <see langword="null"/> for some providers.
		/// </summary>
		public string? Schema          => _info.SchemaName;

		/// <summary>
		/// Gets name of current table or view.
		/// </summary>
		public string  Name            => _info.TableName;

		/// <summary>
		/// Gets flag to indicate that table belongs to default schema.
		/// </summary>
		public bool    IsDefaultSchema => _info.IsDefaultSchema;

		/// <summary>
		/// Gets flag to indicate that this is not a table but view.
		/// </summary>
		public bool    IsView          => _info.IsView;

		/// <summary>
		/// Gets flag to indicate system view or table.
		/// </summary>
		public bool    IsSystem        => _info.IsProviderSpecific;
	}
}
