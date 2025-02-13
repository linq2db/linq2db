using System;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.Http.Server
{
	[Route("api/linq2db")]
	[ApiController]
	public class LinqToDBController<T>(T linqService) : LinqToDBController
		where T : ILinqService, new()
	{
		protected override ILinqService LinqService => linqService;
	}
}
