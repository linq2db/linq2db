using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Allows relational database specific configuration to be performed on <see cref="DataContextOptions" />.
    ///     </para>
    ///     <para>
    ///         Instances of this class are typically returned from methods that configure the context to use a
    ///         particular relational database provider.
    ///     </para>
    /// </summary>
    public abstract class RelationalDbContextOptionsBuilder<TBuilder, TExtension> : IRelationalDbContextOptionsBuilderInfrastructure
        where TBuilder : RelationalDbContextOptionsBuilder<TBuilder, TExtension>
        where TExtension : RelationalOptionsExtension, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RelationalDbContextOptionsBuilder{TBuilder, TExtension}" /> class.
        /// </summary>
        /// <param name="optionsBuilder"> The core options builder. </param>
        protected RelationalDbContextOptionsBuilder([NotNull] DataContextOptionsBuilder optionsBuilder)
        {
	        if (optionsBuilder == null)
	        {
		        throw new ArgumentNullException(nameof(optionsBuilder));
	        }

            OptionsBuilder = optionsBuilder;
        }

        /// <summary>
        ///     Gets the core options builder.
        /// </summary>
        protected virtual DataContextOptionsBuilder OptionsBuilder { get; }

        /// <inheritdoc />
        DataContextOptionsBuilder IRelationalDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

        /// <summary>
        ///     Sets an option by cloning the extension used to store the settings. This ensures the builder
        ///     does not modify options that are already in use elsewhere.
        /// </summary>
        /// <param name="setAction"> An action to set the option. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        protected virtual TBuilder WithOption([NotNull] Func<TExtension, TExtension> setAction)
        {
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
                setAction(OptionsBuilder.Options.FindExtension<TExtension>() ?? new TExtension()));

            return (TBuilder)this;
        }

        #region Hidden System.Object members

        /// <summary>
        ///     Returns a string that represents the current object.
        /// </summary>
        /// <returns> A string that represents the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override string? ToString() => base.ToString();

        /// <summary>
        ///     Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj"> The object to compare with the current object. </param>
        /// <returns> true if the specified object is equal to the current object; otherwise, false. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj) => base.Equals(obj);

        /// <summary>
        ///     Serves as the default hash function.
        /// </summary>
        /// <returns> A hash code for the current object. </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode() => base.GetHashCode();

        #endregion
    }
}
