using System.Collections.Generic;
using System.Data.Common;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Database-specific scaffold options. Defines default values.
	/// </summary>
	public class DatabaseOptions
	{
		public static readonly DatabaseOptions Default = new();

		protected DatabaseOptions() { }

		/// <summary>
		/// Indicates that database requires that invoked scalar function should specify schema name.
		/// Default value: <c>false</c>.
		/// </summary>
		public virtual bool ScalarFunctionSchemaRequired { get; }
	}
}
