using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Metadata;

namespace LinqToDB.DataModel
{
	/// <summary>
	/// Contains mapping entity attributes.
	/// </summary>
	public sealed class EntityModel
	{
		public EntityModel(EntityMetadata metadata, ClassModel @class, PropertyModel? contextProperty)
		{
			Metadata        = metadata;
			Class           = @class;
			ContextProperty = contextProperty;
		}

		/// <summary>
		/// Gets or sets entity mapping metadata.
		/// </summary>
		public EntityMetadata    Metadata             { get; set; }
		/// <summary>
		/// Gets or sets entity class attributes.
		/// </summary>
		public ClassModel        Class                { get; set; }
		/// <summary>
		/// Gets or sets data context property definition for current entity.
		/// Example:
		/// <code>
		/// public class MyDataContext
		/// {
		///     ...
		///     public ITable&lt;MyEntity&gt; MyEntities =&gt; GetTable&lt;MyEntity&gt;();
		///     ...
		/// }
		/// </code>
		/// Type of property must be open-generic type (<see cref="ITable{T}"/> or <see cref="IQueryable{T}"/>) as we
		/// will use it to instantiate final generic type during code model generation.
		/// </summary>
		public PropertyModel?    ContextProperty      { get; set; }
		/// <summary>
		/// Gets or sets enum value that defines which Find entity extension methods to generate.
		/// </summary>
		public FindTypes         FindExtensions       { get; set; }
		/// <summary>
		/// Gets or sets flag indicating that entity class should implement <see cref="IEquatable{T}"/> interface, which compares
		/// entity instances using primary key columns.
		/// Ignored for entities without primary key.
		/// </summary>
		public bool              ImplementsIEquatable { get; set; }
		/// <summary>
		/// Entity columns collection. Code for columns generated in same order as columns ordered in this list.
		/// </summary>
		public List<ColumnModel> Columns              { get;      } = new ();
	}
}
