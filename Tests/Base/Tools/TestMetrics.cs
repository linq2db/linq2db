using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using LinqToDB.Tools;

namespace Tests.Tools
{
	public static class TestMetrics
	{
		static TestMetrics()
		{
			All = new ITestMetric[]
			{
				QueryProviderExecuteT           = new("IQueryProvider.Execute<T>"),
				QueryProviderExecute            = new("IQueryProvider.Execute"),
				QueryProviderGetEnumeratorT     = new("IQueryProvider.GetEnumerator<T>"),
				QueryProviderGetEnumerator      = new("IQueryProvider.GetEnumerator"),
				GetQueryTotal                   = new("  GetQuery"),
				GetQueryFind                    = new("    Find"),
				GetQueryFindExpose              = new("      Expose"),
				GetQueryFindFind                = new("      Find"),
				GetQueryCreate                  = new("    Create"),
				Build                           = new("      Build"),
				BuildSequence                   = new("        BuildSequence"),
				BuildSequenceCanBuild           = new("          CanBuild"),
				BuildSequenceBuild              = new("          Build"),
				ReorderBuilders                 = new("        ReorderBuilders"),
				BuildQuery                      = new("        BuildQuery"),
				FinalizeQuery                   = new("          FinalizeQuery"),
				GetIEnumerable                  = new("  GetIEnumerable"),

				ExecuteTotal                    = new("Execute",
					ExecuteQuery                = new("  Execute Query"),
					ExecuteQueryAsync           = new("  Execute Query Async"),
					ExecuteElement              = new("  Execute Element"),
					ExecuteElementAsync         = new("  Execute Element Async"),
					ExecuteScalar               = new("  Execute Scalar"),
					ExecuteScalarAsync          = new("  Execute Scalar Async"),
					ExecuteScalar2              = new("  Execute Scalar 2"),
					ExecuteScalar2Async         = new("  Execute Scalar 2 Async"),
					ExecuteNonQuery             = new("  Execute NonQuery"),
					ExecuteNonQueryAsync        = new("  Execute NonQuery Async"),
					ExecuteNonQuery2            = new("  Execute NonQuery 2"),
					ExecuteNonQuery2Async       = new("  Execute NonQuery 2Async"),

					CommandInfoExecute          = new("  SQL Execute"),
					CommandInfoExecuteT         = new("  SQL Execute<T>"),
					CommandInfoExecuteCustom    = new("  SQL ExecuteCustom"),
					CommandInfoExecuteAsync     = new("  SQL ExecuteAsync"),
					CommandInfoExecuteAsyncT    = new("  SQL ExecuteAsync<T>"),

					CreateTable                 = new("  CreateTable"),
					CreateTableAsync            = new("  CreateTable Async"),
					DropTable                   = new("  DropTable"),
					DropTableAsync              = new("  DropTable Async")
				),
				ExecuteQuery,
				ExecuteQueryAsync,
				ExecuteElement,
				ExecuteElementAsync,
				ExecuteScalar,
				ExecuteScalarAsync,
				ExecuteScalar2,
				ExecuteScalar2Async,
				ExecuteNonQuery,
				ExecuteNonQueryAsync,
				ExecuteNonQuery2,
				ExecuteNonQuery2Async,

				CreateTable,
				CreateTableAsync,
				DropTable,
				DropTableAsync,

				BuildSql                        = new("    BuildSql"),

				CommandInfoExecute,
				CommandInfoExecuteT,
				CommandInfoExecuteCustom,
				CommandInfoExecuteAsync,
				CommandInfoExecuteAsyncT,

				ExecuteCommand                  = new("    Execute Command",
					CommandExecuteScalar        = new("      ExecuteScalar"),
					CommandExecuteScalarAsync   = new("      ExecuteScalarAsync"),
					CommandExecuteReader        = new("      ExecuteReader"),
					CommandExecuteReaderAsync   = new("      ExecuteReaderAsync"),
					CommandExecuteNonQuery      = new("      ExecuteNonQuery"),
					CommandExecuteNonQueryAsync = new("      ExecuteNonQueryAsync")
				),
				CommandExecuteScalar,
				CommandExecuteScalarAsync,
				CommandExecuteReader,
				CommandExecuteReaderAsync,
				CommandExecuteNonQuery,
				CommandExecuteNonQueryAsync,

				OnTraceInternal                 = new("    OnTraceInternal"),

				GetSqlText                      = new("  GetSqlText"),

				TestTotal                       = new("Total")
			};

			ActivityService.SetFactory(TestMetricFactory);
		}

		static IActivity TestMetricFactory(ActivityID metric)
		{
			var m = (metric switch
			{
				ActivityID.QueryProviderExecuteT       => QueryProviderExecuteT,
				ActivityID.QueryProviderExecute        => QueryProviderExecute,
				ActivityID.QueryProviderGetEnumeratorT => QueryProviderGetEnumeratorT,
				ActivityID.QueryProviderGetEnumerator  => QueryProviderGetEnumerator,
				ActivityID.GetQueryTotal               => GetQueryTotal,
				ActivityID.GetQueryFind                => GetQueryFind,
				ActivityID.GetQueryFindExpose          => GetQueryFindExpose,
				ActivityID.GetQueryFindFind            => GetQueryFindFind,
				ActivityID.GetQueryCreate              => GetQueryCreate,
				ActivityID.Build                       => Build,
				ActivityID.BuildSequence               => BuildSequence,
				ActivityID.BuildSequenceCanBuild       => BuildSequenceCanBuild,
				ActivityID.BuildSequenceBuild          => BuildSequenceBuild,
				ActivityID.ReorderBuilders             => ReorderBuilders,
				ActivityID.BuildQuery                  => BuildQuery,
				ActivityID.FinalizeQuery               => FinalizeQuery,
				ActivityID.GetIEnumerable              => GetIEnumerable,
				ActivityID.ExecuteQuery                => ExecuteQuery,
				ActivityID.ExecuteQueryAsync           => ExecuteQueryAsync,
				ActivityID.ExecuteElement              => ExecuteElement,
				ActivityID.ExecuteElementAsync         => ExecuteElementAsync,
				ActivityID.ExecuteScalar               => ExecuteScalar,
				ActivityID.ExecuteScalarAsync          => ExecuteScalarAsync,
				ActivityID.ExecuteScalar2              => ExecuteScalar2,
				ActivityID.ExecuteScalar2Async         => ExecuteScalar2Async,
				ActivityID.ExecuteNonQuery             => ExecuteNonQuery,
				ActivityID.ExecuteNonQueryAsync        => ExecuteNonQueryAsync,
				ActivityID.ExecuteNonQuery2            => ExecuteNonQuery2,
				ActivityID.ExecuteNonQuery2Async       => ExecuteNonQuery2Async,

				ActivityID.CreateTable                 => CreateTable,
				ActivityID.CreateTableAsync            => CreateTableAsync,
				ActivityID.DropTable                   => DropTable,
				ActivityID.DropTableAsync              => DropTableAsync,

				ActivityID.BuildSql                    => BuildSql,

				ActivityID.CommandExecuteScalar        => CommandExecuteScalar,
				ActivityID.CommandExecuteScalarAsync   => CommandExecuteScalarAsync,
				ActivityID.CommandExecuteReader        => CommandExecuteReader,
				ActivityID.CommandExecuteReaderAsync   => CommandExecuteReaderAsync,
				ActivityID.CommandExecuteNonQuery      => CommandExecuteNonQuery,
				ActivityID.CommandExecuteNonQueryAsync => CommandExecuteNonQueryAsync,
				ActivityID.CommandInfoExecute          => CommandInfoExecute,
				ActivityID.CommandInfoExecuteT         => CommandInfoExecuteT,
				ActivityID.CommandInfoExecuteCustom    => CommandInfoExecuteCustom,
				ActivityID.CommandInfoExecuteAsync     => CommandInfoExecuteAsync,
				ActivityID.CommandInfoExecuteAsyncT    => CommandInfoExecuteAsyncT,
				ActivityID.GetSqlText                  => GetSqlText,
				ActivityID.OnTraceInternal             => OnTraceInternal,

				_ => throw new InvalidOperationException($"Unknown metric type {metric}")
			});

			//return m.Start();
			return new ActivityHierarchy(metric, m);
		}

		static TestMetric QueryProviderExecuteT;
		static TestMetric QueryProviderExecute;
		static TestMetric QueryProviderGetEnumeratorT;
		static TestMetric QueryProviderGetEnumerator;
		static TestMetric GetQueryTotal;
		static TestMetric GetQueryFind;
		static TestMetric GetQueryFindExpose;
		static TestMetric GetQueryFindFind;
		static TestMetric GetQueryCreate;
		static TestMetric Build;
		static TestMetric BuildSequence;
		static TestMetric BuildSequenceCanBuild;
		static TestMetric BuildSequenceBuild;
		static TestMetric ReorderBuilders;
		static TestMetric BuildQuery;
		static TestMetric FinalizeQuery;
		static TestMetric GetIEnumerable;

		static TestMetricSum ExecuteTotal;

		static TestMetric ExecuteQuery;
		static TestMetric ExecuteQueryAsync;
		static TestMetric ExecuteElement;
		static TestMetric ExecuteElementAsync;
		static TestMetric ExecuteScalar;
		static TestMetric ExecuteScalarAsync;
		static TestMetric ExecuteScalar2;
		static TestMetric ExecuteScalar2Async;
		static TestMetric ExecuteNonQuery;
		static TestMetric ExecuteNonQueryAsync;
		static TestMetric ExecuteNonQuery2;
		static TestMetric ExecuteNonQuery2Async;

		static TestMetric CreateTable;
		static TestMetric CreateTableAsync;
		static TestMetric DropTable;
		static TestMetric DropTableAsync;

		static TestMetric BuildSql;

		static TestMetric CommandInfoExecute;
		static TestMetric CommandInfoExecuteT;
		static TestMetric CommandInfoExecuteCustom;
		static TestMetric CommandInfoExecuteAsync;
		static TestMetric CommandInfoExecuteAsyncT;

		static TestMetricSum ExecuteCommand;

		static TestMetric CommandExecuteScalar;
		static TestMetric CommandExecuteScalarAsync;
		static TestMetric CommandExecuteReader;
		static TestMetric CommandExecuteReaderAsync;
		static TestMetric CommandExecuteNonQuery;
		static TestMetric CommandExecuteNonQueryAsync;

		static TestMetric GetSqlText;

		static TestMetric OnTraceInternal;


		public static TestMetric TestTotal;

		public static ITestMetric[] All;

		class ActivityHierarchy : IActivity
		{
			readonly ActivityID              _activityID;
			readonly TestMetric              _metric;
			readonly TestMetric.Watcher      _watcher;
			readonly ActivityHierarchy?      _parent;
			readonly List<ActivityHierarchy> _children = new();

			int _count = 1;

			public ActivityHierarchy(ActivityID activityID, TestMetric metric)
			{
				_activityID = activityID;
				_parent     = _current;
				_current    = this;
				_metric     = metric;
				_watcher    = metric.Start();

				if (_parent != null)
				{
					if (_parent._children.Count == 0)
					{
						_parent._children.Add(this);
					}
					else
					{
						var p = _parent._children[^1];

						if (p._metric == _metric)
							p._count++;
						else
							_parent._children.Add(this);
					}
				}

				if (activityID == ActivityID.FinalizeQuery && _parent == null)
				{
				}
			}

#pragma warning disable RS0030
			[ThreadStatic]
			static ActivityHierarchy? _current;
#pragma warning restore RS0030

			public void Dispose()
			{
				_watcher.Dispose();

				_current = _parent;

				if (_parent == null)
				{
					var sb = new StringBuilder();

					Print(this, 0);

					Debug.      WriteLine(sb.ToString());
					//TestContext.WriteLine(sb.ToString());

					void Print(ActivityHierarchy a, int indent)
					{
						sb
							.Append(' ', indent)
							.Append(a._metric.Name.TrimStart());

						if (a._count > 1)
							sb.Append($" ({a._count})");

						sb.AppendLine();

						foreach (var c in a._children)
							Print(c, indent + 2);
					}
				}
			}
		}
	}
}
