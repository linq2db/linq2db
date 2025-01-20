using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.Http.Server
{
	[Route("api/linq2db")]
	[ApiController]
	public class LinqToDBController : ControllerBase //, ILinqService
	{
		readonly LinqService _linqService = new () { AllowUpdates = true, RemoteClientTag = "Http" };

		[HttpPost("GetInfo/{configuration?}")]
		public virtual ActionResult<LinqServiceInfo> GetInfo(string? configuration)
		{
			return _linqService.GetInfo(configuration);
		}

		[HttpPost("ExecuteNonQuery/{configuration?}")]
		public virtual ActionResult<int> ExecuteNonQuery(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteNonQuery(configuration, queryData);
		}

		[HttpPost("ExecuteScalar/{configuration?}")]
		public virtual ActionResult<string?> ExecuteScalar(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteScalar(configuration, queryData);
		}

		[HttpPost("ExecuteReader/{configuration?}")]
		public virtual ActionResult<string> ExecuteReader(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteReader(configuration, queryData);
		}

		[HttpPost("ExecuteBatch/{configuration?}")]
		public virtual ActionResult<int> ExecuteBatch(string? configuration, [FromBody] string queryData)
		{
			return _linqService.ExecuteBatch(configuration, queryData);
		}

		[HttpPost("GetInfoAsync/{configuration?}")]
		[ActionName("GetInfoAsync")]
		public virtual async Task<ActionResult<LinqServiceInfo>> GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return await _linqService.GetInfoAsync(configuration, cancellationToken).ConfigureAwait(false);
		}

		[HttpPost("ExecuteNonQueryAsync/{configuration?}")]
		public virtual async Task<ActionResult<int>> ExecuteNonQueryAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return await _linqService.ExecuteNonQueryAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
		}

		[HttpPost("ExecuteScalarAsync/{configuration?}")]
		public virtual Task<string?> ExecuteScalarAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return _linqService.ExecuteScalarAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteReaderAsync/{configuration?}")]
		public virtual async Task<ActionResult<string>> ExecuteReaderAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return await _linqService.ExecuteReaderAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
		}

		[HttpPost("ExecuteBatchAsync/{configuration?}")]
		public virtual async Task<ActionResult<int>> ExecuteBatchAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return await _linqService.ExecuteBatchAsync(configuration, queryData, cancellationToken).ConfigureAwait(false);
		}
	}
}
