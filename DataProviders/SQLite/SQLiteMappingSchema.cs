using System;
using System.Globalization;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class SQLiteMappingSchema : MappingSchema
	{
		public SQLiteMappingSchema() : this(ProviderName.SQLite)
		{
		}

		protected SQLiteMappingSchema(string configuration) : base(configuration)
		{
			SetConvertExpression<string,TimeSpan>(s => DateTime.Parse(s, null, DateTimeStyles.NoCurrentDateDefault).TimeOfDay);
		}
	}
}
