using System.Collections.Generic;

namespace LinqToDB.CodeGen.Metadata
{
	public record PrimaryKey(IReadOnlyCollection<Column> OrderedColumns)
	{
		public int? GetPosition(Column column)
		{
			var idx = 0;
			foreach (var col in OrderedColumns)
			{
				if (column == col)
					return idx;
				idx++;
			}

			return null;
		}
	}
}
