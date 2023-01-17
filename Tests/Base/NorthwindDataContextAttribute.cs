using System;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class NorthwindDataContextAttribute : IncludeDataSourcesAttribute
	{
		public NorthwindDataContextAttribute(bool excludeSqlite, bool excludeSqliteMs = false)
			: base(
				excludeSqlite ?
					new[] { TestProvName.AllNorthwind } :
				excludeSqliteMs ?
					new[] { TestProvName.AllNorthwind, TestProvName.NorthwindSQLite } :
					new[] { TestProvName.AllNorthwind, TestProvName.AllSQLiteNorthwind })
		{
		}

		public NorthwindDataContextAttribute() : this(false)
		{
		}
	}
}
