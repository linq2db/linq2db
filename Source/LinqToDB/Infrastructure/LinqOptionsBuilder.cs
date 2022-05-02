using System;
using LinqToDB.Infrastructure.Internal;

namespace LinqToDB.Infrastructure
{
	/// <summary>
	///     <para>
	///         Allows SQL Server specific configuration to be performed on <see cref="DataContextOptions" />.
	///     </para>
	/// </summary>
	public class LinqOptionsBuilder
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="SqlServerDataContextOptionsBuilder" /> class.
		/// </summary>
		/// <param name="optionsBuilder"> The options builder. </param>
		public LinqOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
		{
			OptionsBuilder = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
		}

		/// <summary>
		///     Gets the core options builder.
		/// </summary>
		protected virtual DataContextOptionsBuilder OptionsBuilder { get; }

		/// <summary>
		///     Sets an option by cloning the extension used to store the settings. This ensures the builder
		///     does not modify options that are already in use elsewhere.
		/// </summary>
		/// <param name="setAction"> An action to set the option. </param>
		/// <returns> The same builder instance so that multiple calls can be chained. </returns>
		protected virtual LinqOptionsBuilder WithOption(Func<LinqOptionsExtension, LinqOptionsExtension> setAction)
		{
			((IDataContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
				setAction(OptionsBuilder.Options.FindExtension<LinqOptionsExtension>() ?? new LinqOptionsExtension()));

			return this;
		}


	}
}
