using System;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.Http.Server
{
	[Route("api/linq2db")]
	[ApiController]
	public class LinqToDBController<T>(LinqService<T> linqService) : LinqToDBController
		where T : IDataContext
	{
		protected override ILinqService LinqService => linqService;
	}
}
