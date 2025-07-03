using System;

using LinqToDB.Common.Internal;

namespace LinqToDB.Common
{
	/// <summary>
	/// <para>
	/// Interface for extensions that are stored in <see cref="IOptionsContainer.OptionSets" />.
	/// </para>
	/// <para>
	/// This interface is typically used by database providers (and other extensions).
	/// It is generally not used in application code.
	/// </para>
	/// </summary>
	public interface IOptionSet : IConfigurationID
	{
		/// <summary>
		/// Gets the default options.
		/// </summary>
		IOptionSet Default { get; }
	}
}
