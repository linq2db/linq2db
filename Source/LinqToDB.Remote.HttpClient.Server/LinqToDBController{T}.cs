using System;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.HttpClient.Server
{
	[ApiController]
	public class LinqToDBController<T> : LinqToDBController
		where T : IDataContext
	{
		readonly ILinqService<T> _linqService;

		public LinqToDBController(ILinqService<T> linqService)
		{
			_linqService = linqService;
			_linqService.RemoteClientTag ??= "HttpClient";
		}

		protected override ILinqService LinqService => _linqService;
	}
}
