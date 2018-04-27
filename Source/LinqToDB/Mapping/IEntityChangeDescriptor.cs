using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Mapping
{
	using Common;
	using Extensions;
	using Linq;
	using Reflection;

	/// <summary>
	/// Mapping entity descriptor change interface.
	/// </summary>
	public interface IEntityChangeDescriptor
	{
		/// <summary>
		/// Gets or sets mapping type accessor.
		/// </summary>
		TypeAccessor TypeAccessor { get; set; }

		/// <summary>
		/// Gets or sets name of table or view in database.
		/// </summary>
		string TableName { get; set; }

		/// <summary>
		/// Gets or sets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		string SchemaName { get; set; }

		/// <summary>
		/// Gets or sets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		string DatabaseName { get; set; }

		/// <summary>
		/// Gets list of change interfaces for column descriptors for current entity.
		/// </summary>
		IEnumerable<IColumnChangeDescriptor> Columns { get; }
	}
}
