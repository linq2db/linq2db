using System;

namespace LinqToDB
{
	/// <summary>
	/// Provides data for the <see cref="IDataContext.OnEntityCreated"/> event.
	/// </summary>
	public class EntityCreatedEventArgs
	{
		/// <summary>
		/// Get or sets the entity that created.
		/// </summary>
		public object       Entity      { get; set; }

		/// <summary>
		/// DataContext that created a new entity.
		/// </summary>
		public IDataContext DataContext { get; set; }
	}
}
