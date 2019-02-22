using System;

namespace LinqToDB
{
	public interface IEntityServices
	{
		/// <summary>
		/// Occurs when a new entity is created.
		/// </summary>
		Action<EntityCreatedEventArgs> OnEntityCreated { get; set; }
	}
}
