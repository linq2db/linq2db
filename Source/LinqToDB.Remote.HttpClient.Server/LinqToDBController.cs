using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace LinqToDB.Remote.HttpClient.Server
{
	[ApiController]
	public class LinqToDBController : ControllerBase
	{
		[HttpPost("GetInfo/{configuration?}")]
		public virtual LinqServiceInfo GetInfo(string? configuration)
		{
			return LinqService.GetInfo(configuration);
		}

		[HttpPost("ExecuteNonQuery/{configuration?}")]
		public virtual int ExecuteNonQuery(string? configuration, [FromBody] string queryData)
		{
			return LinqService.ExecuteNonQuery(configuration, queryData);
		}

		[HttpPost("ExecuteScalar/{configuration?}")]
		public virtual string? ExecuteScalar(string? configuration, [FromBody] string queryData)
		{
			return LinqService.ExecuteScalar(configuration, queryData);
		}

		[HttpPost("ExecuteReader/{configuration?}")]
		public virtual string ExecuteReader(string? configuration, [FromBody] string queryData)
		{
			return LinqService.ExecuteReader(configuration, queryData);
		}

		[HttpPost("ExecuteBatch/{configuration?}")]
		public virtual int ExecuteBatch(string? configuration, [FromBody] string queryData)
		{
			return LinqService.ExecuteBatch(configuration, queryData);
		}

		[HttpPost("GetInfoAsync/{configuration?}")]
		[ActionName("GetInfoAsync")]
		public virtual Task<LinqServiceInfo> GetInfoAsync(string? configuration, CancellationToken cancellationToken)
		{
			return LinqService.GetInfoAsync(configuration, cancellationToken);
		}

		[HttpPost("ExecuteNonQueryAsync/{configuration?}")]
		public virtual Task<int> ExecuteNonQueryAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return LinqService.ExecuteNonQueryAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteScalarAsync/{configuration?}")]
		public virtual Task<string?> ExecuteScalarAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return LinqService.ExecuteScalarAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteReaderAsync/{configuration?}")]
		public virtual Task<string> ExecuteReaderAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return LinqService.ExecuteReaderAsync(configuration, queryData, cancellationToken);
		}

		[HttpPost("ExecuteBatchAsync/{configuration?}")]
		public virtual Task<int> ExecuteBatchAsync(string? configuration, [FromBody] string queryData, CancellationToken cancellationToken)
		{
			return LinqService.ExecuteBatchAsync(configuration, queryData, cancellationToken);
		}

		ILinqService? _linqService;

		protected virtual ILinqService LinqService => _linqService ??= CreateLinqService();

		protected virtual ILinqService CreateLinqService()
		{
			return new LinqService { AllowUpdates = false, RemoteClientTag = "HttpClient" };
		}
	}
}
