using System;

namespace LinqToDB.Infrastructure
{
	/// <summary>
	/// <para>
	/// Interface for extensions that are stored in <see cref="DataContextOptions.Extensions" />.
	/// </para>
	/// <para>
	/// This interface is typically used by database providers (and other extensions). It is generally not used in application code.
	/// </para>
	/// </summary>
	public interface IDataContextOptionsExtension
	{
		/// <summary>
		/// Information/metadata about the extension.
		/// </summary>
		DataContextOptionsExtensionInfo Info { get; }

		/// <summary>
		/// Adds the services required to make the selected options work. This is used when there
		/// is no external <see cref="IServiceProvider" /> and EF is maintaining its own service
		/// provider internally. This allows database providers (and other extensions) to register their
		/// required services when EF is creating an service provider.
		/// </summary>
		void ApplyServices();

		/// <summary>
		/// Gives the extension a chance to validate that all options in the extension are valid.
		/// Most extensions do not have invalid combinations and so this will be a no-op.
		/// If options are invalid, then an exception should be thrown.
		/// </summary>
		/// <param name="options"> The options being validated. </param>
		void Validate(IDataContextOptions options);
	}
}
