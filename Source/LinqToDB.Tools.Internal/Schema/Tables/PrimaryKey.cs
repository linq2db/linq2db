using System;
using System.Collections.Generic;

namespace LinqToDB.Schema
{
	/// <summary>
	/// Table primary key constraint descriptor.
	/// </summary>
	/// <param name="Name">Primary key name.</param>
	/// <param name="Columns">Primary key columns, ordered by constraint ordinal.</param>
	public sealed record PrimaryKey(string? Name, IReadOnlyCollection<string> Columns)
	{
		/// <summary>
		/// Gets position of specified column in primary key.
		/// </summary>
		/// <param name="column">Primary key column.</param>
		/// <returns>Position (0-based ordinal) of column in primary key.</returns>
		/// <exception cref="InvalidOperationException">Provided column not found in primary key.</exception>
		public int GetColumnPositionInKey(Column column)
		{
			// as primary keys contain one or few columns, it doesn't make sense to create any kind of lookup table
			// to get column ordinal
			var idx = 0;
			foreach (var col in Columns)
			{
				if (column.Name == col)
					return idx;
				idx++;
			}

			throw new InvalidOperationException($"column {column} is not a part of primary key {this}");
		}

		public override string ToString() => $"{Name ?? "<unnamed primary key>"}({string.Join(", ", Columns)})";
	}
}
