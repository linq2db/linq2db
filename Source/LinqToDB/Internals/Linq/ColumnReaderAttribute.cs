using System;

namespace LinqToDB.Internals.Linq
{
	/// <summary>
	/// Internal API.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class ColumnReaderAttribute : Attribute
	{
		public ColumnReaderAttribute(int indexParameterIndex)
		{
			IndexParameterIndex = indexParameterIndex;
		}

		public int IndexParameterIndex { get; }
	}
}
