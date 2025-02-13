using System;

using LinqToDB.Data;

namespace LinqToDB.Remote.Http.Server
{
	public class LinqService<T>(T dataContext) : LinqService
		where T : DataConnection
	{
		public override DataConnection CreateDataContext(string? configuration)
		{
			return dataContext;
		}
	}
}
