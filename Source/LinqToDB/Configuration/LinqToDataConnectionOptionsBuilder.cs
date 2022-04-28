using System;

namespace LinqToDB.Configuration
{
	using Data;

	/// <summary>
	/// Used to build <see cref="DataContextOptions"/>
	/// which is used by <see cref="DataConnection"/>
	/// to determine connection settings.
	/// </summary>
	[Obsolete("Use 'DataContextOptionsBuilder instead.'")]
	public class LinqToDataConnectionOptionsBuilder : DataContextOptionsBuilder
	{
		public DataContextOptions Build() => Options;
	}
}
