using LinqToDB.Data;

namespace LinqToDB.DataProvider.Firebird
{
	class FirebirdMergeBuilder<TTarget, TSource> : BasicMergeBuilder<TTarget, TSource>
		where TTarget : class
		where TSource : class
	{
		public FirebirdMergeBuilder(DataConnection connection, IMerge<TTarget, TSource> merge)
			: base(connection, merge)
		{
		}

		protected override bool SupportsSourceDirectValues
		{
			get
			{
				// VALUES (...) syntax not supported by firebird
				return false;
			}
		}

		protected override string FakeSourceTable
		{
			get
			{
				// table with exactly one record to replace VALUES for enumerable source
				return "rdb$database";
			}
		}

		protected override bool ProviderUsesAlternativeUpdate
		{
			get
			{
				// Firebird doesn't support INSERT FROM
				return true;
			}
		}

		protected override bool SupportsParametersInSource
		{
			get
			{
				// source subquery select list shouldn't contain parameters otherwise following error will be
				// generated:
				//
				// FirebirdSql.Data.Common.IscException : Dynamic SQL Error
				// SQL error code = -804
				//Data type unknown
				return false;
			}
		}
	}
}
