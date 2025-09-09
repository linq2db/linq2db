using System;
using System.Data;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Attribute specify parameter index of reader column ordinal for custom reader method that accepts reader and column ordinal parameters.
	/// This information is used by mapper builder for queries with <see cref="CommandBehavior.SequentialAccess"/> behavior enabled.
	/// Alternative approach (when possible) is to move reading of raw value from reader to external expression and passing it to custom reader.
	/// <code>
	/// // such helper requires attribute to work in SequentialAccess mode
	/// [ColumnReader(1)]
	/// static int GetCustomInt(DbDataReader rd, int ordinal) => int.Parse(rd.GetString(ordinal));
	/// ReaderExpressions[...] = (rd, i) => GetCustomInt(rd, i);
	/// 
	/// // such helper will work without attribute
	/// static int GetCustomInt(string value) => int.Parse(value);
	/// ReaderExpressions[...] = (rd, i) => GetCustomInt(rd.GetString(i));
	/// </code>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ColumnReaderAttribute : Attribute
	{
		public ColumnReaderAttribute(int indexParameterIndex)
		{
			IndexParameterIndex = indexParameterIndex;
		}

		public int IndexParameterIndex { get; }
	}
}
