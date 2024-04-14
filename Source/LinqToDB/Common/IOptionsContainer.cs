using System;
using System.Collections.Generic;

namespace LinqToDB.Common
{
	/// <summary>
	/// <para>
	/// The options to be used by a <see cref="IDataContext" />. Use an <see cref="OptionsContainer{T}" />
	/// to create instances of classes that implement this interface, they are not designed to be directly created
	/// in your application code.
	/// </para>
	/// <para>
	/// The implementation does not need to be thread-safe.
	/// </para>
	/// </summary>
	interface IOptionsContainer
	{
		/// <summary>
		/// Gets the extensions that store the configured options.
		/// </summary>
		IEnumerable<IOptionSet> OptionSets { get; }

		/// <summary>
		/// Gets the extension of the specified type. Returns null if no extension of the specified type is configured.
		/// </summary>
		/// <typeparam name="TSet">The type of the option set to get.</typeparam>
		/// <param name="createKnownOptions">if <see langword="true" />, the implementation should create a new instance of the known option type set if none is found.</param>
		/// <returns>The extension, or <see langword="null" /> if none was found.</returns>
		TSet? Find<TSet>(bool createKnownOptions)
			where TSet : class, IOptionSet;
	}
}
