using System;
using System.Collections.Generic;

namespace LinqToDB.Infrastructure
{
	/// <summary>
	/// Information/metadata for an <see cref="IDataContextOptionsExtension" />.
	/// </summary>
	public abstract class DataContextOptionsExtensionInfo
	{
		/// <summary>
		/// Creates a new <see cref="DataContextOptionsExtensionInfo" /> instance containing info/metadata for the given extension.
		/// </summary>
		/// <param name="extension"> The extension. </param>
		protected DataContextOptionsExtensionInfo(IDataContextOptionsExtension extension)
		{
			Extension = extension;
		}

		/// <summary>
		/// The extension for which this instance contains metadata.
		/// </summary>
		public virtual IDataContextOptionsExtension Extension { get; }

		/// <summary>
		/// True if the extension is a database provider; false otherwise.
		/// </summary>
		public abstract bool IsDatabaseProvider { get; }

		/// <summary>
		/// A message fragment for logging typically containing information about any useful non-default options that have been configured.
		/// </summary>
		public abstract string LogFragment { get; }

		/// <summary>
		/// Returns a hash code created from any options that would cause a new <see cref="IServiceProvider" />
		/// to be needed. Most extensions do not have any such options and should return zero.
		/// </summary>
		/// <returns> A hash over options that require a new service provider when changed. </returns>
		public abstract long GetServiceProviderHashCode();

		/// <summary>
		/// Populates a dictionary of information that may change between uses of the
		/// extension such that it can be compared to a previous configuration for
		/// this option and differences can be logged. The dictionary key should be prefixed by the
		/// extension name. For example, <c>"SqlServer:"</c>.
		/// </summary>
		/// <param name="debugInfo"> The dictionary to populate. </param>
		public abstract void PopulateDebugInfo(IDictionary<string,string> debugInfo);
	}
}
