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
		/// Creates a hub that uses the <see cref="LinqToDBHubOptions"/> registered in the services, or the default
		/// options when none are registered.
		/// <para>
		/// The legacy (.NET Framework / .NET Standard) server gives connections of the long polling transport no request
		/// services, so there such a hub uses the default options: a hub that needs its options on every transport takes
		/// <c>IOptions&lt;LinqToDBHubOptions&gt;</c> in its constructor.
		/// </para>
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
