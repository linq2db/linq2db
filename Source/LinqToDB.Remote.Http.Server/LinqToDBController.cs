using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.Http.Server
{
	[Route("api/linq2db")]
	[ApiController]
	public class LinqToDBController : ControllerBase
	{
		readonly LinqService _linqService = new () { AllowUpdates = true, RemoteClientTag = "Http" };

		[HttpPost("GetInfo/{configuration?}")]
		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		[HttpPost("ExecuteNonQuery/{configuration?}")]
		public virtual int ExecuteNonQuery(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		[HttpPost("ExecuteScalar/{configuration?}")]
		public virtual string? ExecuteScalar(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}

		[HttpPost("ExecuteReader/{configuration?}")]
		public virtual string ExecuteReader(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		[HttpPost("ExecuteBatch/{configuration?}")]
		public virtual int ExecuteBatch(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		[HttpPost("GetInfoAsync/{configuration?}")]
		[ActionName("GetInfoAsync")]
		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return _linqService.GetInfoAsync(configuration, cancellationToken);
		}

		[HttpPost("ExecuteNonQueryAsync/{configuration?}")]
		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteNonQueryAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteScalarAsync/{configuration?}")]
		public virtual Task<string?> ExecuteScalarAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteScalarAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteReaderAsync/{configuration?}")]
		public virtual Task<string> ExecuteReaderAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteReaderAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteBatchAsync/{configuration?}")]
		public virtual Task<int> ExecuteBatchAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteBatchAsync(configuration, queryData, cancellationToken);
		}
	}
}
