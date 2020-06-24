using System;

namespace LinqToDB
{
	public interface IEntityServices
	{
		/// <summary>
		/// Occurs when a new entity is created during query materialization. Not triggered for explicitly constructed objects.
		/// <example>
		///  In code below event could be triggered only for first query:
		///  <code>
		/// // r created by linq2db
		/// <br />
		/// from r in db.table select r;
		/// <br />
		/// <br />
		/// // Entity constructor specified explicitly by user (projection)
		/// <br />
		/// from r in db.table select new Entity() { field = r.field };
		/// </code>
		/// </example>.
		/// </summary>
		Action<EntityCreatedEventArgs>? OnEntityCreated { get; set; }
	}
}
