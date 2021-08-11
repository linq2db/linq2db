namespace LinqToDB
{
	/// <summary>
	/// Provides data for the <see cref="IEntityServices.OnEntityCreated"/> event.
	/// </summary>
	public class EntityCreatedEventArgs
	{
		/// <summary>
		/// Get or sets the entity that created.
		/// </summary>
		public object       Entity      { get; set; } = null!;

		/// <summary>
		/// DataContext that created a new entity.
		/// </summary>
		public IDataContext DataContext { get; set; } = null!;

		/// <summary>
		/// TableOptions of the current entity
		/// </summary>
		public TableOptions TableOptions { get; set; }

		/// <summary>
		/// TableName of the current entity
		/// </summary>
		public string? TableName { get; set; }

		/// <summary>
		/// SchemaName of the current entity
		/// </summary>
		public string? SchemaName { get; set; }

		/// <summary>
		/// DatabaseName of the current entity
		/// </summary>
		public string? DatabaseName { get; set; }

		/// <summary>
		/// SchemaName of the current entity
		/// </summary>
		public string? ServerName { get; set; }
	}
}
