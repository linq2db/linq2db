using System;

namespace LinqToDB.Infrastructure
{
	/// <summary>
	/// <para>
	/// Interface for extensions that are stored in <see cref="IOptions.OptionSets" />.
	/// </para>
	/// <para>
	/// This interface is typically used by database providers (and other extensions).
	/// It is generally not used in application code.
	/// </para>
	/// </summary>
	public interface IOptionSet
	{
		int ConfigurationID { get; }
	}
}
