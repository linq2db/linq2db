using System;

namespace LinqToDB.Infrastructure
{
	using DataProvider.Access;
	using Internal;

	/// <summary>
	/// <para>
	/// Allows SQL Server specific configuration to be performed on <see cref="DataContextOptions" />.
	/// </para>
	/// </summary>
	public class AccessDataContextOptionsBuilder
		: RelationalDataContextOptionsBuilder<AccessDataContextOptionsBuilder,AccessOptionsExtension>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AccessDataContextOptionsBuilder" /> class.
		/// </summary>
		/// <param name="optionsBuilder"> The options builder. </param>
		public AccessDataContextOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
			: base(optionsBuilder)
		{
		}

//		/// <summary>
//		/// SQL Server dialect will be detected automatically.
//		/// </summary>
//		public virtual AccessDataContextOptionsBuilder AutodetectServerVersion()
//		{
//			return WithOption(e => e.WithServerVersion(null));
//		}
	}
}
