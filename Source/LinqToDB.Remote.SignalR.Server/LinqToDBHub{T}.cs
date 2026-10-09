using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LinqToDB.Remote.SignalR
{
	/// <summary>
	/// <see cref="LinqToDBHub"/> that executes remote calls with the <see cref="ILinqService{T}"/> registered in DI.
	/// </summary>
	/// <typeparam name="T">Data context type.</typeparam>
	public class LinqToDBHub<T> : LinqToDBHub
		where T : IDataContext
	{
		readonly ILinqService<T> _linqService;

		/// <summary>
		/// Creates a hub with default <see cref="LinqToDBHubOptions"/>.
		/// </summary>
		/// <param name="linqService">Service that executes remote calls.</param>
		public LinqToDBHub(ILinqService<T> linqService)
		{
			_linqService                   = linqService;
			_linqService.RemoteClientTag ??= "Signal/R";
		}

		/// <summary>
		/// Creates a hub with the given options.
		/// </summary>
		/// <param name="linqService">Service that executes remote calls.</param>
		/// <param name="options">Hub options.</param>
		[ActivatorUtilitiesConstructor]
		public LinqToDBHub(ILinqService<T> linqService, IOptions<LinqToDBHubOptions> options)
			: base(options)
		{
			_linqService                   = linqService;
			_linqService.RemoteClientTag ??= "Signal/R";
		}

		/// <inheritdoc />
		protected override ILinqService LinqService => _linqService;
	}
}
