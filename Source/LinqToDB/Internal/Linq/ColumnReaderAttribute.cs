using System;

namespace LinqToDB.Internal.Linq
{
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
