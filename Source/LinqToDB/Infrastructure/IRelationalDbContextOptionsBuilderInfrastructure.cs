namespace LinqToDB.Infrastructure
{
    /// <summary>
    ///     Explicitly implemented by <see cref="RelationalDbContextOptionsBuilder{TBuilder,TExtension}" /> to hide
    ///     methods that are used by database provider extension methods but not intended to be called by application
    ///     developers.
    /// </summary>
    public interface IRelationalDbContextOptionsBuilderInfrastructure
    {
        /// <summary>
        ///     Gets the core options builder.
        /// </summary>
        DataContextOptionsBuilder OptionsBuilder { get; }
    }
}
