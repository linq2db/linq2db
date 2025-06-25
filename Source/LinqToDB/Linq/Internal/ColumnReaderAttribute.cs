using System;

namespace LinqToDB.Linq.Internal
{
	/// <summary>
	/// Internal API.
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
