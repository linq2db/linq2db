using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;

namespace NUnit.ParallelByResource
{
	/// <summary>
	/// Installs a <see cref="ResourceLaneDispatcher"/> over NUnit's stock parallel dispatcher.
	/// </summary>
	public static class ResourceLaneDispatcherInstaller
	{
		/// <summary>
		/// If the current execution context is running with NUnit's parallel dispatcher
		/// (<see cref="ParallelWorkItemDispatcher"/>), wrap it with a
		/// <see cref="ResourceLaneDispatcher"/> and return <see langword="true"/>. A non-parallel run
		/// is left untouched and returns <see langword="false"/>.
		/// </summary>
		/// <remarks>
		/// Call from assembly-level one-time setup (after NUnit has chosen its dispatcher). The
		/// dispatcher delegates composite suites and globally-exclusive work back to the original, so
		/// NUnit's completion / shift machinery stays intact.
		/// </remarks>
		/// <param name="installed">The installed dispatcher, so the host can call
		/// <see cref="ResourceLaneDispatcher.Shutdown"/> when the run is over.</param>
		public static bool TryInstall(IResourceLaneStrategy strategy, IParallelDiagnostics? diagnostics, int maxLanes, out int levelOfParallelism, out ResourceLaneDispatcher? installed)
		{
			var context = TestExecutionContext.CurrentContext;

			if (context.Dispatcher is ParallelWorkItemDispatcher original)
			{
				var dispatcher = new ResourceLaneDispatcher(original, strategy, maxLanes, diagnostics);

				levelOfParallelism = original.LevelOfParallelism;
				context.Dispatcher = dispatcher;
				installed          = dispatcher;
				return true;
			}

			levelOfParallelism = 0;
			installed          = null;
			return false;
		}
	}
}
