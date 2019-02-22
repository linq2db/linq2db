using System;

namespace Tests
{
	[AttributeUsage(AttributeTargets.Parameter)]
	public class NorthwindDataContextAttribute : IncludeDataSourcesAttribute
	{
		public NorthwindDataContextAttribute(bool excludeSqlite, bool excludeSqliteMs = false)
			: base(
				excludeSqlite ?
					new[] { TestProvName.Northwind } :
				excludeSqliteMs ?
					new[] { TestProvName.Northwind, TestProvName.NorthwindSQLite } :
					new[] { TestProvName.Northwind, TestProvName.NorthwindSQLite, TestProvName.NorthwindSQLiteMS })
		{
		}

		public NorthwindDataContextAttribute() : this(false)
		{
		}
	}
}
