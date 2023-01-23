using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines optimistic concurrency column modification strategy.
	/// Used with <see cref="ConcurrencyPropertyAttribute" /> attribute and <see cref="ConcurrencyExtensions" /> extensions.
	/// E.g. <see cref="ConcurrencyExtensions.UpdateConcurrent{T}(IDataContext, T)"/> or <see cref="ConcurrencyExtensions.UpdateConcurrentAsync{T}(IDataContext, T, System.Threading.CancellationToken)"/> methods.
	/// </summary>
	public enum VersionBehavior
	{
		/// <summary>
		/// Column value modified by database automatically on update. E.g. using SQL Server rowversion/timestamp column or database trigger.
		/// </summary>
		Auto,
		/// <summary>
		/// Column value should be incremented by 1.
		/// </summary>
		AutoIncrement,
		/// <summary>
		/// Use <see cref="System.Guid"/> value.
		/// Supported column types:
		/// <list type="bullet">
		/// <item><see cref="System.Guid"/></item>
		/// <item><see cref="string"/> using <see cref="Guid.ToString()"/></item>
		/// <item><see cref="T:byte[]"/> using <see cref="Guid.ToByteArray()"/></item>
		/// </list>
		/// </summary>
		Guid
	}
}
