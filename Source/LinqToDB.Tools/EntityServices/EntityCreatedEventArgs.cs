namespace LinqToDB
{
	public sealed class EntityCreatedEventArgs
	{
		internal EntityCreatedEventArgs(IDataContext context, object entity)
		{
			DataContext = context;
			Entity      = entity;
		}

		/// <summary>
		/// Get or sets the entity that created.
		/// </summary>
		public object Entity { get; set; }

		/// <summary>
		/// DataContext that created a new entity.
		/// </summary>
		public IDataContext DataContext { get; }
	}
}
