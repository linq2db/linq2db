using System;
using LinqToDB.Infrastructure.Internal;

namespace LinqToDB.Infrastructure
{
    /// <summary>
    ///     <para>
    ///         Allows SQL Server specific configuration to be performed on <see cref="DataContextOptions" />.
    ///     </para>
    /// </summary>
    public class SqlServerDbContextOptionsBuilder
        : RelationalDbContextOptionsBuilder<SqlServerDbContextOptionsBuilder, SqlServerOptionsExtension>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SqlServerDbContextOptionsBuilder" /> class.
        /// </summary>
        /// <param name="optionsBuilder"> The options builder. </param>
        public SqlServerDbContextOptionsBuilder(DataContextOptionsBuilder optionsBuilder)
            : base(optionsBuilder)
        {
        }

        /// <summary>
        ///     Use a ROW_NUMBER() in queries instead of OFFSET/FETCH. This method is backwards-compatible to SQL Server 2005.
        /// </summary>
        [Obsolete("Row-number paging is no longer supported. See https://aka.ms/AA6h122 for more information.")]
        public virtual SqlServerDbContextOptionsBuilder UseRowNumberForPaging(bool useRowNumberForPaging = true)
            => WithOption(e => e.WithRowNumberPaging(useRowNumberForPaging));

    }
}
