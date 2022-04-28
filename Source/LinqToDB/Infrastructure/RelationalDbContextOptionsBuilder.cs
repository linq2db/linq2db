// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Allows relational database specific configuration to be performed on <see cref="DbContextOptions" />.
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
        protected RelationalDbContextOptionsBuilder([NotNull] DbContextOptionsBuilder optionsBuilder)
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
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        /// <inheritdoc />
        DbContextOptionsBuilder IRelationalDbContextOptionsBuilderInfrastructure.OptionsBuilder => OptionsBuilder;

        /// <summary>
        ///     Configures the wait time (in seconds) before terminating the attempt to execute a command and generating an error.
        /// </summary>
        /// <param name="commandTimeout"> The time in seconds to wait for the command to execute. </param>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual TBuilder CommandTimeout(int? commandTimeout)
            => WithOption(e => (TExtension)e.WithCommandTimeout(commandTimeout));

        /// <summary>
        ///     Configures the context to use relational database semantics when comparing null values. By default,
        ///     Entity Framework will use C# semantics for null values, and generate SQL to compensate for differences
        ///     in how the database handles nulls.
        /// </summary>
        /// <returns> The same builder instance so that multiple calls can be chained. </returns>
        public virtual TBuilder UseRelationalNulls(bool useRelationalNulls = true)
            => WithOption(e => (TExtension)e.WithUseRelationalNulls(useRelationalNulls));

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
