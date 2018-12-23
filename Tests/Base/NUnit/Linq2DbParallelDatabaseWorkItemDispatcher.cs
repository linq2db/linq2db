#if !NETSTANDARD1_6
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;
using System;
using System.Collections.Generic;
using static NUnit.Framework.Internal.Execution.CompositeWorkItem;

namespace Tests
{
	// TODO: respect level of parallelism from replaced dispatcher
	public class Linq2DbParallelDatabaseWorkItemDispatcher : IWorkItemDispatcher
	{
		private const string DEFAULT_CONTEXT = "default";

		int IWorkItemDispatcher.LevelOfParallelism => _contextDispatchers.Count;

		private readonly IDictionary<string, IWorkItemDispatcher> _contextDispatchers = new Dictionary<string, IWorkItemDispatcher>();

		private readonly IWorkItemDispatcher _oldDispatcher;

		public Linq2DbParallelDatabaseWorkItemDispatcher()
		{
			//System.Diagnostics.Debugger.Launch();
			_oldDispatcher = TestExecutionContext.CurrentContext.Dispatcher;
			_contextDispatchers.Add(DEFAULT_CONTEXT, _oldDispatcher);
		}

		void IWorkItemDispatcher.CancelRun(bool force)
		{
			foreach (var dispatcher in _contextDispatchers.Values)
				dispatcher.CancelRun(force);
		}

		void IWorkItemDispatcher.Dispatch(WorkItem work)
		{
			if (work is OneTimeTearDownWorkItem tearDown && tearDown.Name == "[default namespace] OneTimeTearDown")
			{
				TestExecutionContext.CurrentContext.Dispatcher = _oldDispatcher;
				TestExecutionContext.CurrentContext.Dispatcher.Dispatch(work);
				return;
			}

			var (_, provider, _) = ActiveIssueAttribute.GetTestProperties(work.Test);

			// use provider, so linqservice run on same thread too
			if (provider == null)
				_contextDispatchers[DEFAULT_CONTEXT].Dispatch(work);
			else
			{
				if (!_contextDispatchers.ContainsKey(provider))
				{
					lock (_contextDispatchers)
					{
						if (!_contextDispatchers.ContainsKey(provider))
							_contextDispatchers.Add(provider, new DatabaseWorkItemDispatcher());
					}
				}

				_contextDispatchers[provider].Dispatch(work);
			}
		}

		void IWorkItemDispatcher.Start(WorkItem topLevelWorkItem)
		{
			throw new InvalidOperationException("Shouldn't be called");
		}
	}
}
#endif
