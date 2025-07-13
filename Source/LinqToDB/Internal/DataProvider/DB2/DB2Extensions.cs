using System.Data.Common;

using LinqToDB.Common;

namespace LinqToDB.Internal.DataProvider.DB2
{
	static class DB2Extensions
	{
		public static string? ToString(this DbDataReader reader, int i)
		{
			var value = Converter.ChangeTypeTo<string?>(reader[i]);
			return value?.TrimEnd();
		}
	}
}
