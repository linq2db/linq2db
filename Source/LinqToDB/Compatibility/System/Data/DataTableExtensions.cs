using System;
using System.Collections.Generic;

namespace System.Data
{
	static class DataTableExtensions
	{
		public static IEnumerable<DataRow> AsEnumerable(this DataTable source)
		{
			foreach (DataRow row in source.Rows)
			{
				yield return row;
			}
		}
	}
}
