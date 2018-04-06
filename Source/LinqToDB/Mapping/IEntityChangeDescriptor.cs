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
	/// Stores mapping entity descriptor.
	/// </summary>
	public interface IEntityChangeDescriptor
	{
		/// <summary>
		/// Gets mapping type accessor.
		/// </summary>
		TypeAccessor TypeAccessor { get; set; }

		/// <summary>
		/// Gets name of table or view in database.
		/// </summary>
		string TableName { get; set; }

		/// <summary>
		/// Gets optional schema/owner name, to override default name. See <see cref="LinqExtensions.SchemaName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		string SchemaName { get; set; }

		/// <summary>
		/// Gets optional database name, to override default database name. See <see cref="LinqExtensions.DatabaseName{T}(ITable{T}, string)"/> method for support information per provider.
		/// </summary>
		string DatabaseName { get; set; }

		/// <summary>
		/// Gets list of column descriptors for current entity.
		/// </summary>
		List<IColumnChangeDescriptor> Columns { get; }
	}
}
