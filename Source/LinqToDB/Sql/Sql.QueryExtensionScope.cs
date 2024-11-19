using System;

namespace LinqToDB
{
	using SqlProvider;

	public partial class Sql
	{
		/// <summary>
		/// Defines query extension location/scope.
		/// </summary>
		public enum QueryExtensionScope
		{
			/// <summary>
			/// No scope. Could be used with <see cref="NoneExtensionBuilder"/> builder to skip extension processing by Linq To DB for specific configuration.
			/// </summary>
			None,
			/// <summary>
			/// Extension will be applied to table as table hint.
			/// </summary>
			TableHint,
			/// <summary>
			/// Extension will be applied to all tables in current (sub)query as table hint.
			/// </summary>
			TablesInScopeHint,
			/// <summary>
			/// Extension will be applied to table as index hint.
			/// </summary>
			IndexHint,
			/// <summary>
			/// Extension will be applied to join as join hint/extension.
			/// </summary>
			JoinHint,
			/// <summary>
			/// Extension will be applied to subquery as subquery hint.
			/// </summary>
			SubQueryHint,
			/// <summary>
			/// Extension will be applied to query as query hint.
			/// </summary>
			QueryHint,
			/// <summary>
			/// Extension will be applied to table name (e.g. temporal table modifiers).
			/// </summary>
			TableNameHint,
		}
	}
}
