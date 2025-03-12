using System;

namespace LinqToDB.Remote
{
	/// <summary>
	/// A factory for creating <see cref="IDataContext"/> instances.
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	public interface IDataContextFactory<out TContext>
	where TContext : IDataContext
	{
		TContext CreateDataContext(string? configuration = null);
	}
}
