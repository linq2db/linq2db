using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

#if NETFRAMEWORK || NETSTANDARD2_0
using System.Text;
#endif

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Common.Internal.Cache;
using LinqToDB.Common.Internal;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Interceptors;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.Tools;
using LinqToDB.Compatibility.System;

namespace LinqToDB.Data
{
	/// <summary>
	/// Provides database connection command abstraction.
	/// </summary>
	[PublicAPI]
	public class CommandInfo
	{
		/// <summary>
		/// Instance of database connection, associated with command.
		/// </summary>
		public DataConnection   DataConnection;
		/// <summary>
		/// Command text.
		/// </summary>
		public string           CommandText;
		/// <summary>
		/// Command parameters.
		/// </summary>
		public DataParameter[]? Parameters;
		/// <summary>
		/// Type of command. See <see cref="System.Data.CommandType"/> for all supported types.
		/// Default value: <see cref="CommandType.Text"/>.
		/// </summary>
		public CommandType     CommandType = CommandType.Text;
		/// <summary>
		/// Command behavior flags. See <see cref="System.Data.CommandBehavior"/> for more details.
		/// Default value: <see cref="CommandBehavior.Default"/>.
		/// </summary>
		public CommandBehavior CommandBehavior;

		#region Init
		/// <summary>
		/// Creates database command instance using provided database connection and command text.
		/// </summary>
		/// <param name="dataConnection">Database connection instance.</param>
		/// <param name="commandText">Command text.</param>
		public CommandInfo(DataConnection dataConnection, string commandText)
		{
			DataConnection = dataConnection;
			CommandText    = commandText;
		}

		/// <summary>
		/// Creates database command instance using provided database connection, command text and parameters.
		/// </summary>
		/// <param name="dataConnection">Database connection instance.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">List of command parameters.</param>
		public CommandInfo(DataConnection dataConnection, string commandText, params DataParameter[] parameters)
		{
			DataConnection = dataConnection;
			CommandText    = commandText;
			Parameters     = parameters;
		}

		/// <summary>
		/// Creates database command instance using provided database connection, command text and single parameter.
		/// </summary>
		/// <param name="dataConnection">Database connection instance.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameter">Command parameter.</param>
		public CommandInfo(DataConnection dataConnection, string commandText, DataParameter parameter)
		{
			DataConnection = dataConnection;
			CommandText    = commandText;
			Parameters     = new[] { parameter };
		}

		/// <summary>
		/// Creates database command instance using provided database connection, command text and parameters.
		/// </summary>
		/// <param name="dataConnection">Database connection instance.</param>
		/// <param name="commandText">Command text.</param>
		/// <param name="parameters">Command parameters. Supported values:
		/// <para> - <c>null</c> for command without parameters;</para>
		/// <para> - single <see cref="DataParameter"/> instance;</para>
		/// <para> - array of <see cref="DataParameter"/> parameters;</para>
		/// <para> - mapping class entity.</para>
		/// <para>Last case will convert all mapped columns to <see cref="DataParameter"/> instances using following logic:</para>
		/// <para> - if column is of <see cref="DataParameter"/> type, column value will be used. If parameter name (<see cref="DataParameter.Name"/>) is not set, column name will be used;</para>
		/// <para> - if converter from column type to <see cref="DataParameter"/> is defined in mapping schema, it will be used to create parameter with colum name passed to converter;</para>
		/// <para> - otherwise column value will be converted to <see cref="DataParameter"/> using column name as parameter name and column value will be converted to parameter value using conversion, defined by mapping schema.</para>
		/// </param>
		public CommandInfo(DataConnection dataConnection, string commandText, object? parameters)
		{
			DataConnection = dataConnection;
			CommandText    = commandText;
			Parameters     = GetDataParameters(dataConnection, parameters);
		}

		private CommandBehavior GetCommandBehavior()
		{
			return DataConnection.GetCommandBehavior(CommandBehavior);
		}

		#endregion

		#region Query with object reader

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> QueryProc<T>(Func<DbDataReader,T> objectReader)
		{
			CommandType = CommandType.StoredProcedure;
			return Query(objectReader);
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public Task<IEnumerable<T>> QueryProcAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken = default)
		{
			CommandType = CommandType.StoredProcedure;
			return QueryAsync(objectReader, cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> Query<T>(Func<DbDataReader, T> objectReader)
		{
			InitCommand();

			return ReadEnumerator(
				DataConnection.ExecuteDataReader(GetCommandBehavior()),
				objectReader,
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		/// <summary>
		/// Executes command asynchronously and returns results as collection of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public async Task<IEnumerable<T>> QueryAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken = default)
		{
			await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			return ReadEnumerator(
				await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false),
				objectReader,
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		IEnumerable<T> ReadEnumerator<T>(DataReaderWrapper rd, Func<DbDataReader, T> objectReader, IExecutionScope? scope)
		{
			using (scope)
			using (rd)
			{
				while (rd.DataReader!.Read())
					yield return objectReader(rd.DataReader!);

				SetRebindParameters(rd);
			}
		}

		#endregion

		#region Query with object reader async
		/// <summary>
		/// Executes command asynchronously and returns list of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public async Task<List<T>> QueryToListAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(false);
			return list;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public async Task<T[]> QueryToArrayAsync<T>(Func<DbDataReader, T> objectReader, CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(false);
			return list.ToArray();
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public async Task QueryForEachAsync<T>(Func<DbDataReader, T> objectReader, Action<T> action, CancellationToken cancellationToken = default)
		{
			await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
			{
				var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
				await using (rd.ConfigureAwait(false))
					while (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false))
						action(objectReader(rd.DataReader!));
			}
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <returns>Async sequence of records returned by the query.</returns>
		public IAsyncEnumerable<T> QueryToAsyncEnumerable<T>(Func<DbDataReader, T> objectReader)
		{
			return Impl(objectReader);

			async IAsyncEnumerable<T> Impl(Func<DbDataReader, T> objectReader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await InitCommandAsync(cancellationToken).ConfigureAwait(false);

				await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
				{
#if NET6_0_OR_GREATER
					var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
					await using (rd.ConfigureAwait(false))
#else
					using (var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false))
#endif
						while (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false))
							yield return objectReader(rd.DataReader!);
				}
			}
		}

		#endregion

		#region Query

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> QueryProc<T>()
		{
			CommandType = CommandType.StoredProcedure;
			return Query<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public Task<IEnumerable<T>> QueryProcAsync<T>(CancellationToken cancellationToken = default)
		{
			CommandType = CommandType.StoredProcedure;
			return QueryAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> Query<T>()
		{
			InitCommand();

			return ReadEnumerator<T>(
				DataConnection.ExecuteDataReader(GetCommandBehavior()),
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		/// <summary>
		/// Executes command asynchronously and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public async Task<IEnumerable<T>> QueryAsync<T>(CancellationToken cancellationToken = default)
		{
			await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			return ReadEnumerator<T>(
				await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false),
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		static bool IsDynamicType(Type type)
		{
			return typeof(object) == type || typeof(ExpandoObject) == type;
		}

		IEnumerable<T> ReadEnumerator<T>(DataReaderWrapper rd, IExecutionScope? scope, bool disposeReader = true)
		{
			var startedOn = DateTime.UtcNow;
			var stopwatch = Stopwatch.StartNew();
			var rowCount  = 0;

			using (scope)
				try
				{
					if (rd.DataReader!.Read())
					{
						var additionalKey = GetCommandAdditionalKey(rd.DataReader!, typeof(T));

						DbDataReader reader;

						if (DataConnection is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: {} interceptor })
						{
							using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
								reader = interceptor.UnwrapDataReader(DataConnection, rd.DataReader!);
						}
						else
						{
							reader = rd.DataReader!;
						}

						var objectReader = GetObjectReader<T>(DataConnection, reader, CommandText, additionalKey);
						var isFaulted    = false;

						do
						{
							T result;

							try
							{
								result = objectReader(reader);
							}
							catch (InvalidCastException)
							{
								if (isFaulted)
									throw;

								isFaulted    = true;
								objectReader = GetObjectReader2<T>(DataConnection, reader, CommandText, additionalKey);
								result       = objectReader(reader);
							}

							rowCount++;
							yield return result;

						} while (rd.DataReader!.Read());
					}

					SetRebindParameters(rd);
				}
				finally
				{
					stopwatch.Stop();

					if (disposeReader)
					{
						if (DataConnection.TraceSwitchConnection.TraceInfo)
						{
							DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: false)
							{
								TraceLevel      = TraceLevel.Info,
								Command         = rd.Command,
								StartTime       = startedOn,
								ExecutionTime   = stopwatch.Elapsed,
								RecordsAffected = rowCount
							});
						}

						rd.Dispose();
					}
				}
		}

		#endregion

		#region Query async

		/// <summary>
		/// Executes command asynchronously and returns list of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with list of query result records.</returns>
		public async Task<List<T>> QueryToListAsync<T>(CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(false);
			return list;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public async Task<T[]> QueryToArrayAsync<T>(CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(false);
			return list.ToArray();
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="action">Action, applied to each result record.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task.</returns>
		public async Task QueryForEachAsync<T>(Action<T> action, CancellationToken cancellationToken = default)
		{
			await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
			{
				var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
				await using (rd.ConfigureAwait(false))
				{
					if (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false))
					{
						var additionalKey = GetCommandAdditionalKey(rd.DataReader!, typeof(T));

						DbDataReader reader;

						if (DataConnection is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
						{
							using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
								reader = interceptor.UnwrapDataReader(DataConnection, rd.DataReader!);
						}
						else
						{
							reader = rd.DataReader!;
						}

						var objectReader = GetObjectReader<T>(DataConnection, reader, CommandText, additionalKey);
						var isFaulted    = false;

						do
						{
							T result;

							try
							{
								result = objectReader(reader);
							}
							catch (InvalidCastException)
							{
								if (isFaulted)
									throw;

								isFaulted    = true;
								objectReader = GetObjectReader2<T>(DataConnection, reader, CommandText, additionalKey);
								result       = objectReader(reader);
							}

							action(result);

						} while (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false));
					}
				}
			}
		}

		/// <summary>
		/// Executes command asynchronously and apply provided action to each record.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <returns>Async sequence of records returned by the query.</returns>
		public IAsyncEnumerable<T> QueryToAsyncEnumerable<T>()
		{
			return Impl();

			async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				await InitCommandAsync(cancellationToken).ConfigureAwait(false);

				await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
				{
#if NET6_0_OR_GREATER
					var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
					await using (rd.ConfigureAwait(false))
#else
					using (var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false))
#endif
					{
						if (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false))
						{
							var additionalKey = GetCommandAdditionalKey(rd.DataReader!, typeof(T));

							DbDataReader reader;

							if (DataConnection is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
							{
								using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
									reader = interceptor.UnwrapDataReader(DataConnection, rd.DataReader!);
							}
							else
							{
								reader = rd.DataReader!;
							}

							var objectReader  = GetObjectReader<T>(DataConnection, reader, CommandText, additionalKey);
							var isFaulted     = false;

							do
							{
								T result;

								try
								{
									result = objectReader(reader);
								}
								catch (InvalidCastException)
								{
									if (isFaulted)
										throw;

									isFaulted = true;
									objectReader = GetObjectReader2<T>(DataConnection, reader, CommandText, additionalKey);
									result = objectReader(reader);
								}

								yield return result;

							} while (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false));
						}
					}
				}
			}
		}

		#endregion

		#region Query with template

		/// <summary>
		/// Executes command and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> Query<T>(T template)
		{
			return Query<T>();
		}

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <returns>Returns collection of query result records.</returns>
		public IEnumerable<T> QueryProc<T>(T template)
		{
			return QueryProc<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns results as collection of values of specified type.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="template">This value used only for <typeparamref name="T"/> parameter type inference, which makes this method usable with anonymous types.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns collection of query result records.</returns>
		public Task<IEnumerable<T>> QueryProcAsync<T>(T template, CancellationToken cancellationToken = default)
		{
			return QueryProcAsync<T>(cancellationToken);
		}

		#endregion

		#region Query with multiple result sets

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <returns>Returns result.</returns>
		public T QueryProcMultiple<T>()
			where T : class
		{
			CommandType = CommandType.StoredProcedure;

			return QueryMultiple<T>();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns a result containing multiple result sets.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		public Task<T> QueryProcMultipleAsync<T>(CancellationToken cancellationToken = default)
			where T : class
		{
			CommandType = CommandType.StoredProcedure;

			return QueryMultipleAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns a result containing multiple result sets.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <returns>Returns result.</returns>
		public T QueryMultiple<T>()
			where T : class
		{
			InitCommand();

			T result;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			using (var rd = DataConnection.ExecuteDataReader(GetCommandBehavior()))
			{
				result = ReadMultipleResultSets<T>(rd);

				SetRebindParameters(rd);
			}

			return result;
		}

		/// <summary>
		/// Executes command asynchronously and returns a result containing multiple result sets.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <typeparam name="T">Result set type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>
		///     A task that represents the asynchronous operation.
		///     The task result contains object with multiply result sets.
		/// </returns>
		public async Task<T> QueryMultipleAsync<T>(CancellationToken cancellationToken = default)
			where T : class
		{
			await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			T result;

			await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
			{
				var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
				await using (rd.ConfigureAwait(false))
			{
					result = await ReadMultipleResultSetsAsync<T>(rd.DataReader!, cancellationToken).ConfigureAwait(false);

					SetRebindParameters(rd);
			}
			}

			return result;
		}

		static Dictionary<int, MemberAccessor> GetMultipleQueryIndexMap<T>(TypeAccessor<T> typeAccessor)
		{
			var indexMap = new Dictionary<int, MemberAccessor>();

			// Use attribute labels if any exist.
			foreach (var m in typeAccessor.Members)
			{
				if (!m.HasGetter)
					continue;

				var attr = m.MemberInfo.GetAttribute<ResultSetIndexAttribute>();

				if (attr != null)
					indexMap.Add(attr.Index, m);
			}

			if (indexMap.Count == 0)
			{
				// Use ordering of properties according to reflection.
				var i = 0;
				foreach (var m in typeAccessor.Members)
				{
					if (m.HasGetter)
					{
						indexMap[i] = m;
						i++;
					}
				}
			}

			return indexMap;
		}

		static readonly MethodInfo _readAsArrayMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsArray<int>(null!)).GetGenericMethodDefinition();

		static readonly MethodInfo _readAsListMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsList<int>(null!)).GetGenericMethodDefinition();

		static readonly MethodInfo _readSingletMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadFirstOrDefault<string>(null!)).GetGenericMethodDefinition();

		T[] ReadAsArray<T>(DataReaderWrapper rd)
		{
			return ReadEnumerator<T>(rd, null, false).ToArray();
		}

		List<T> ReadAsList<T>(DataReaderWrapper rd)
		{
			return ReadEnumerator<T>(rd, null, false).ToList();
		}

		T? ReadFirstOrDefault<T>(DataReaderWrapper rd)
		{
			return ReadEnumerator<T>(rd, null, false).FirstOrDefault();
		}

		T ReadMultipleResultSets<T>(DataReaderWrapper rd)
			where T : class
		{
			var typeAccessor = TypeAccessor.GetAccessor<T>();
			var indexMap     = GetMultipleQueryIndexMap(typeAccessor);

			var resultIndex = 0;
			var result = typeAccessor.Create();
			do
			{
				// Only process the field if we're reading it into a property.
				if (indexMap.TryGetValue(resultIndex, out var member))
				{
					MethodInfo valueMethodInfo;
					Type elementType;
					if (member.Type.IsArray)
					{
						valueMethodInfo = _readAsArrayMethodInfo;
						elementType     = member.Type.GetItemType()!;
					}
					else if (member.Type.IsGenericEnumerableType())
					{
						valueMethodInfo = _readAsListMethodInfo;
						elementType     = member.Type.GetGenericArguments()[0];
					}
					else
					{
						valueMethodInfo = _readSingletMethodInfo;
						elementType     = member.Type;
					}

					var genericMethod = valueMethodInfo.MakeGenericMethod(elementType);
					var value = genericMethod.InvokeExt(this, new object[] { rd });

					member.SetValue(result, value);
				}

				resultIndex++;
			} while (rd.DataReader!.NextResult());

			return result;
		}

		static readonly MethodInfo _readAsArrayAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsArrayAsync<int>(null!, default)).GetGenericMethodDefinition();

		static readonly MethodInfo _readAsListAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsListAsync<int>(null!, default)).GetGenericMethodDefinition();

		static readonly MethodInfo _readSingletAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadFirstOrDefaultAsync<int>(null!, default)).GetGenericMethodDefinition();

		sealed class ReaderAsyncEnumerable<T> : IAsyncEnumerable<T>
		{
			readonly CommandInfo  _commandInfo;
			readonly DbDataReader _rd;

			public ReaderAsyncEnumerable(CommandInfo commandInfo, DbDataReader rd)
			{
				_commandInfo = commandInfo;
				_rd          = rd;
			}

			public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken)
			{
				return new ReaderAsyncEnumerator<T>(_commandInfo, _rd, cancellationToken);
			}
		}

		sealed class ReaderAsyncEnumerator<T> : IAsyncEnumerator<T>
		{
			readonly CommandInfo      _commandInfo;
			readonly DbDataReader     _rd;
			readonly string?          _additionalKey;
			Func<DbDataReader, T>     _objectReader;
			bool                      _isFaulted;
			bool                      _isFinished;
			CancellationToken         _cancellationToken;

			public ReaderAsyncEnumerator(CommandInfo commandInfo, DbDataReader rd, CancellationToken cancellationToken)
			{
				_commandInfo       = commandInfo;
				_rd                = rd;
				_additionalKey     = commandInfo.GetCommandAdditionalKey(rd, typeof(T));
				_objectReader      = GetObjectReader<T>(commandInfo.DataConnection, rd, commandInfo.CommandText, _additionalKey);
				_isFaulted         = false;
				_cancellationToken = cancellationToken;
			}

			public void Dispose()
			{
			}

			public ValueTask DisposeAsync() => default;

			public T Current { get; private set; } = default!;

			public async ValueTask<bool> MoveNextAsync()
			{
				if (_isFinished)
					return false;
				if (!await _rd.ReadAsync(_cancellationToken).ConfigureAwait(false))
				{
					_isFinished = true;
					return false;
				}

				try
				{
					Current = _objectReader(_rd);
				}
				catch (InvalidCastException)
				{
					if (_isFaulted)
						throw;

					_isFaulted = true;
					_objectReader = GetObjectReader2<T>(_commandInfo.DataConnection, _rd,
						_commandInfo.CommandText,
						_additionalKey);
					Current = _objectReader(_rd);
				}

				return true;
			}
		}

		Task<T[]> ReadAsArrayAsync<T>(DbDataReader rd, CancellationToken cancellationToken)
		{
			return new ReaderAsyncEnumerable<T>(this, rd).ToArrayAsync(cancellationToken: cancellationToken);
		}

		Task<List<T>> ReadAsListAsync<T>(DbDataReader rd, CancellationToken cancellationToken)
		{
			return new ReaderAsyncEnumerable<T>(this, rd).ToListAsync(cancellationToken: cancellationToken);
		}

		Task<T?> ReadFirstOrDefaultAsync<T>(DbDataReader rd, CancellationToken cancellationToken)
		{
			return new ReaderAsyncEnumerable<T>(this, rd).FirstOrDefaultAsync(cancellationToken: cancellationToken);
		}

		async Task<T> ReadMultipleResultSetsAsync<T>(DbDataReader rd, CancellationToken cancellationToken)
			where T : class
		{
			var typeAccessor = TypeAccessor.GetAccessor<T>();
			var indexMap     = GetMultipleQueryIndexMap(typeAccessor);

			var resultIndex = 0;
			var result = typeAccessor.Create();
			do
			{
				// Only process the field if we're reading it into a property.
				if (indexMap.TryGetValue(resultIndex, out var member))
				{
					MethodInfo valueMethodInfo;
					Type elementType;
					if (member.Type.IsArray)
					{
						valueMethodInfo = _readAsArrayAsyncMethodInfo;
						elementType     = member.Type.GetItemType()!;
					}
					else if (member.Type.IsGenericEnumerableType())
					{
						valueMethodInfo = _readAsListAsyncMethodInfo;
						elementType     = member.Type.GetGenericArguments()[0];
					}
					else
					{
						valueMethodInfo = _readSingletAsyncMethodInfo;
						elementType     = member.Type;
					}

					var genericMethod = valueMethodInfo.MakeGenericMethod(elementType);
					var task          = genericMethod.InvokeExt<Task>(this, new object[] { rd, cancellationToken });

					await task.ConfigureAwait(false);

					var value = task.GetType().GetProperty(nameof(Task<int>.Result))!.GetValue(task);

					member.SetValue(result, value);
				}

				resultIndex++;
			} while (await rd.NextResultAsync(cancellationToken).ConfigureAwait(false));

			return result;
		}

		#endregion

		#region Execute
		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns number of affected records.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <returns>Number of records, affected by command execution.</returns>
		public int ExecuteProc()
		{
			CommandType = CommandType.StoredProcedure;
			return Execute();
		}

		/// <summary>
		/// Executes command and returns number of affected records.
		/// </summary>
		/// <returns>Number of records, affected by command execution.</returns>
		public int Execute()
		{
			using var m = ActivityService.Start(ActivityID.CommandInfoExecute);

			var startedOn = DateTime.UtcNow;
			var stopwatch = Stopwatch.StartNew();

			InitCommand();

			var commandResult = DataConnection.ExecuteNonQuery();

			stopwatch.Stop();

			if (DataConnection.TraceSwitchConnection.TraceInfo)
			{
				DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: false)
				{
					TraceLevel      = TraceLevel.Info,
					Command         = DataConnection.CurrentCommand,
					StartTime       = startedOn,
					ExecutionTime   = stopwatch.Elapsed,
					RecordsAffected = commandResult,
				});
			}

			if (DataConnection.CurrentCommand?.Parameters.Count > 0 && Parameters?.Length > 0)
				RebindParameters(DataConnection.CurrentCommand);

			return commandResult;
		}

		/// <summary>
		/// Executes command using custom execute method and returns number of affected records.
		/// </summary>
		/// <param name="customExecute">Custom execute method.</param>
		/// <returns>Number of records, affected by command execution.</returns>
		internal int ExecuteCustom(Func<DbCommand, int> customExecute)
		{
			using var m = ActivityService.Start(ActivityID.CommandInfoExecuteCustom);

			var startedOn = DateTime.UtcNow;
			var stopwatch = Stopwatch.StartNew();

			InitCommand();

			var commandResult = DataConnection.ExecuteNonQueryCustom(customExecute);

			stopwatch.Stop();

			if (DataConnection.TraceSwitchConnection.TraceInfo)
			{
				DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: false)
				{
					TraceLevel      = TraceLevel.Info,
					Command         = DataConnection.CurrentCommand,
					StartTime       = startedOn,
					ExecutionTime   = stopwatch.Elapsed,
					RecordsAffected = commandResult,
				});
			}

			if (DataConnection.CurrentCommand?.Parameters.Count > 0 && Parameters?.Length > 0)
				RebindParameters(DataConnection.CurrentCommand);

			return commandResult;
		}

		#endregion

		#region Execute async

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns number of affected records.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public Task<int> ExecuteProcAsync(CancellationToken cancellationToken = default)
		{
			CommandType = CommandType.StoredProcedure;
			return ExecuteAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns number of affected records.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with number of records, affected by command execution.</returns>
		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInfoExecuteAsync))
			{
				var startedOn = DateTime.UtcNow;
				var stopwatch = Stopwatch.StartNew();

				await InitCommandAsync(cancellationToken).ConfigureAwait(false);

				var commandResult = await DataConnection.ExecuteNonQueryDataAsync(cancellationToken).ConfigureAwait(false);

				stopwatch.Stop();
				if (DataConnection.TraceSwitchConnection.TraceInfo)
				{
					DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: true)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = DataConnection.CurrentCommand,
						StartTime       = startedOn,
						ExecutionTime   = stopwatch.Elapsed,
						RecordsAffected = commandResult,
					});
				}

				if (DataConnection.CurrentCommand?.Parameters.Count > 0 && Parameters?.Length > 0)
					RebindParameters(DataConnection.CurrentCommand);

				return commandResult;
			}
		}

		#endregion

		#region Execute scalar

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <returns>Resulting value.</returns>
		public T ExecuteProc<T>()
		{
			CommandType = CommandType.StoredProcedure;
			return Execute<T>();
		}

		/// <summary>
		/// Executes command and returns single value.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <returns>Resulting value.</returns>
		public T Execute<T>()
		{
			using var m = ActivityService.Start(ActivityID.CommandInfoExecuteT);

			var startedOn     = DateTime.UtcNow;
			var stopwatch     = Stopwatch.StartNew();
			var hasParameters = InitCommand();

			T result = default!;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			using (var rd = DataConnection.ExecuteDataReader(GetCommandBehavior()))
			{
				if (rd.DataReader!.Read())
				{
					var additionalKey = GetCommandAdditionalKey(rd.DataReader!, typeof(T));

					DbDataReader reader;

					if (DataConnection is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
					{
						using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
							reader = interceptor.UnwrapDataReader(DataConnection, rd.DataReader!);
					}
					else
					{
						reader = rd.DataReader!;
					}

					var objectReader = GetObjectReader<T>(DataConnection, reader, CommandText, additionalKey);

					try
					{
						result = objectReader(reader);
					}
					catch (InvalidCastException)
					{
						result = GetObjectReader2<T>(DataConnection, reader, CommandText, additionalKey)(reader);
					}
					catch (FormatException)
					{
						result = GetObjectReader2<T>(DataConnection, reader, CommandText, additionalKey)(reader);
					}
				}

				stopwatch.Stop();

				if (DataConnection.TraceSwitchConnection.TraceInfo)
				{
					DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: false)
					{
						TraceLevel      = TraceLevel.Info,
						Command         = rd.Command,
						StartTime       = startedOn,
						ExecutionTime   = stopwatch.Elapsed,
						RecordsAffected = 1,
					});
				}

				SetRebindParameters(rd);
			}

			return result;
		}

		#endregion

		#region Execute scalar async

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type asynchronously and returns single value.
		/// Saves result values for output and reference parameters to corresponding <see cref="DataParameter"/> object.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public Task<T> ExecuteProcAsync<T>(CancellationToken cancellationToken = default)
		{
			CommandType = CommandType.StoredProcedure;
			return ExecuteAsync<T>(cancellationToken);
		}

		/// <summary>
		/// Executes command asynchronously and returns single value.
		/// </summary>
		/// <typeparam name="T">Resulting value type.</typeparam>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with resulting value.</returns>
		public async Task<T> ExecuteAsync<T>(CancellationToken cancellationToken = default)
		{
			await using (ActivityService.StartAndConfigureAwait(ActivityID.CommandInfoExecuteAsyncT))
			{
				var startedOn     = DateTime.UtcNow;
				var stopwatch     = Stopwatch.StartNew();
				var hasParameters = await InitCommandAsync(cancellationToken).ConfigureAwait(false);

				T result = default!;

				await using ((DataConnection.DataProvider.ExecuteScope(DataConnection) ?? EmptyIAsyncDisposable.Instance).ConfigureAwait(false))
				{
					var rd = await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false);
					await using (rd.ConfigureAwait(false))
					{
						if (await rd.DataReader!.ReadAsync(cancellationToken).ConfigureAwait(false))
						{
							var additionalKey = GetCommandAdditionalKey(rd.DataReader!, typeof(T));
							try
							{
								result = GetObjectReader<T>(DataConnection, rd.DataReader!, CommandText, additionalKey)(rd.DataReader!);
							}
							catch (InvalidCastException)
							{
								result = GetObjectReader2<T>(DataConnection, rd.DataReader!, CommandText, additionalKey)(rd.DataReader!);
							}
						}

						stopwatch.Stop();

						if (DataConnection.TraceSwitchConnection.TraceInfo)
						{
							DataConnection.OnTraceConnection(new TraceInfo(DataConnection, TraceInfoStep.Completed, TraceOperation.DisposeQuery, isAsync: true)
							{
								TraceLevel      = TraceLevel.Info,
								Command         = rd.Command,
								StartTime       = startedOn,
								ExecutionTime   = stopwatch.Elapsed,
								RecordsAffected = 1,
							});
						}

						SetRebindParameters(rd);
					}
				}

				return result;
			}
		}

		#endregion

		#region ExecuteReader

		/// <summary>
		/// Executes command using <see cref="CommandType.StoredProcedure"/> command type and returns data reader instance.
		/// </summary>
		/// <returns>Data reader object.</returns>
		public DataReader ExecuteReaderProc()
		{
			CommandType = CommandType.StoredProcedure;
			return ExecuteReader();
		}

		/// <summary>
		/// Executes command asynchronously using <see cref="CommandType.StoredProcedure"/> command type and returns data reader instance.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Data reader object.</returns>
		public Task<DataReaderAsync> ExecuteReaderProcAsync(CancellationToken cancellationToken = default)
		{
			CommandType = CommandType.StoredProcedure;
			return ExecuteReaderAsync(cancellationToken);
		}

		/// <summary>
		/// Executes command and returns data reader instance.
		/// </summary>
		/// <returns>Data reader object.</returns>
		public DataReader ExecuteReader()
		{
			var hasParameters = InitCommand();

			var dataReader = new DataReader(this, DataConnection.ExecuteDataReader(GetCommandBehavior()));

			if (hasParameters && dataReader.ReaderWrapper?.Command?.Parameters.Count > 0)
				dataReader.ReaderWrapper.OnBeforeCommandDispose = RebindParameters;

			return dataReader;
		}

		internal IEnumerable<T> ExecuteQuery<T>(DbDataReader rd, string sql)
		{
			if (rd.Read())
			{
				var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
				var objectReader  = GetObjectReader<T>(DataConnection, rd, sql, additionalKey);
				var isFaulted     = false;

				do
				{
					T result;

					try
					{
						result = objectReader(rd);
					}
					catch (InvalidCastException)
					{
						if (isFaulted)
							throw;

						isFaulted    = true;
						objectReader = GetObjectReader2<T>(DataConnection, rd, sql, additionalKey);
						result       = objectReader(rd);
					}

					yield return result;

				} while (rd.Read());
			}
		}

		[return: MaybeNull]
		internal T ExecuteScalar<T>(DbDataReader rd, string sql)
		{
			if (rd.Read())
			{
				var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
				try
				{
					return GetObjectReader<T>(DataConnection, rd, sql, additionalKey)(rd);
				}
				catch (InvalidCastException)
				{
					return GetObjectReader2<T>(DataConnection, rd, sql, additionalKey)(rd);
				}
			}

			return default;
		}

		#endregion

		#region ExecuteReader async

		/// <summary>
		/// Executes command asynchronously and returns data reader instance.
		/// </summary>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Task with data reader object.</returns>
		public async Task<DataReaderAsync> ExecuteReaderAsync(CancellationToken cancellationToken = default)
		{
			var hasParameters = await InitCommandAsync(cancellationToken).ConfigureAwait(false);

			var dataReader = new DataReaderAsync(this, await DataConnection.ExecuteDataReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(false));

			if (hasParameters && dataReader.ReaderWrapper?.Command?.Parameters.Count > 0)
				dataReader.ReaderWrapper.OnBeforeCommandDispose = RebindParameters;

			return dataReader;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private async ValueTask<bool> InitCommandAsync(CancellationToken cancellationToken)
		{
			// make sure command creation will not lead to sync connection.Open call
			await DataConnection.EnsureConnectionAsync(connect: true, cancellationToken).ConfigureAwait(false);

			var hasParameters = Parameters?.Length > 0;

			await DataConnection.InitCommandAsync(CommandType, CommandText, Parameters, null, hasParameters, cancellationToken).ConfigureAwait(false);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			DataConnection.CommitCommandInit();

			return hasParameters;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool InitCommand()
		{
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			DataConnection.CommitCommandInit();

			return hasParameters;
		}

		internal async Task ExecuteQueryAsync<T>(DbDataReader rd, string sql, Action<T> action, CancellationToken cancellationToken)
		{
			if (await rd.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
				var objectReader  = GetObjectReader<T>(DataConnection, rd, sql, additionalKey);
				var isFaulted     = false;

				do
				{
					T result;

					try
					{
						result = objectReader(rd);
					}
					catch (InvalidCastException)
					{
						if (isFaulted)
							throw;

						isFaulted    = true;
						objectReader = GetObjectReader2<T>(DataConnection, rd, sql, additionalKey);
						result       = objectReader(rd);
					}

					action(result);

				} while (await rd.ReadAsync(cancellationToken).ConfigureAwait(false));
			}
		}

		internal IAsyncEnumerable<T> ExecuteQueryAsync<T>(DbDataReader rd, string sql)
		{
			return Impl(rd, sql);

			async IAsyncEnumerable<T> Impl(DbDataReader rd, string sql, [EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				if (await rd.ReadAsync(cancellationToken).ConfigureAwait(false))
				{
					var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
					var objectReader  = GetObjectReader<T>(DataConnection, rd, sql, additionalKey);
					var isFaulted     = false;

					do
					{
						T result;

						try
						{
							result = objectReader(rd);
						}
						catch (InvalidCastException)
						{
							if (isFaulted)
								throw;

							isFaulted = true;
							objectReader = GetObjectReader2<T>(DataConnection, rd, sql, additionalKey);
							result = objectReader(rd);
						}

						yield return result;

					} while (await rd.ReadAsync(cancellationToken).ConfigureAwait(false));
				}
			}
		}

		internal async Task<T> ExecuteScalarAsync<T>(DbDataReader rd, string sql, CancellationToken cancellationToken)
		{
			if (await rd.ReadAsync(cancellationToken).ConfigureAwait(false))
			{
				var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
				try
				{
					return GetObjectReader<T>(DataConnection, rd, sql, additionalKey)(rd);
				}
				catch (InvalidCastException)
				{
					return GetObjectReader2<T>(DataConnection, rd, sql, additionalKey)(rd);
				}
			}

			return default!;
		}

		#endregion

		#region SetParameters

		static void SetParameters(DataConnection dataConnection, DataParameter[] parameters)
		{
			foreach (var parameter in parameters)
			{
				var p          = dataConnection.CurrentCommand!.CreateParameter();
				var dbDataType = parameter.DbDataType;
				var value      = parameter.Value;

				if (dbDataType.DataType == DataType.Undefined && value != null)
					dbDataType = dbDataType.WithDataType(dataConnection.MappingSchema.GetDataType(value.GetType()).Type.DataType);

				if (parameter.Direction != null) p.Direction =       parameter.Direction.Value;
				if (parameter.Size      != null) p.Size      =       parameter.Size     .Value;
				if (parameter.Precision != null) p.Precision = (byte)parameter.Precision.Value;
				if (parameter.Scale     != null) p.Scale     = (byte)parameter.Scale    .Value;

				// we don't normalize parameter names here as they are passed from user code and it is user's responsibility
				// to pass correct names. And we cannot add normalization as it will be breaking change for existing users
				dataConnection.DataProvider.SetParameter(dataConnection, p, parameter.Name!, dbDataType, value);
				// some providers (e.g. managed sybase provider) could change parameter name
				// which breaks parameters rebind logic
				parameter.Name = p.ParameterName;
				dataConnection.CurrentCommand!.Parameters.Add(p);
			}
		}

		static object ConvertParameterValue<TFrom>(TFrom value, MappingSchema mappingSchema)
		{
			var converter = mappingSchema.GetConverter<TFrom, object>();
			var result    = converter!(value);
			return result;
		}

		private static readonly MethodInfo _convertParameterValueMethodInfo =
			MemberHelper.MethodOf(() => ConvertParameterValue(1, null!)).GetGenericMethodDefinition();

		static object? ConvertParameterValue(object? value, MappingSchema mappingSchema)
		{
			if (ReferenceEquals(value, null))
				return null;

			var methodInfo = _convertParameterValueMethodInfo.MakeGenericMethod(value.GetType());
			var result     = methodInfo.InvokeExt(null, new[] { value, mappingSchema });

			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void SetRebindParameters(DataReaderWrapper rd)
		{
			if (rd.Command?.Parameters.Count > 0 && Parameters?.Length > 0)
				rd.OnBeforeCommandDispose = RebindParameters;
		}

		void RebindParameters(DbCommand command)
		{
			foreach (var dataParameter in Parameters!)
			{
				if (dataParameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue)
				{
					var dbParameter = command.Parameters[dataParameter.Name!];

					dataParameter.Output = dbParameter;

					if (!Equals(dataParameter.Value, dbParameter.Value))
					{
						dataParameter.Value = ConvertParameterValue(dbParameter.Value, DataConnection.MappingSchema);
					}
				}
			}
		}

		static readonly MemoryCache<(Type type, int contextId),Func<object,DataParameter[]>> _parameterReaders = new (new ());

		static readonly PropertyInfo _dataParameterName       = MemberHelper.PropertyOf<DataParameter>(p => p.Name);
		static readonly PropertyInfo _dataParameterDbDataType = MemberHelper.PropertyOf<DataParameter>(p => p.DbDataType);
		static readonly PropertyInfo _dataParameterValue      = MemberHelper.PropertyOf<DataParameter>(p => p.Value);

		static DataParameter[]? GetDataParameters(DataConnection dataConnection, object? parameters)
		{
			if (parameters == null)
				return null;

			switch (parameters)
			{
				case DataParameter[] dataParameters : return dataParameters;
				case DataParameter   dataParameter  : return new[] { dataParameter };
			}

			var func = _parameterReaders.GetOrCreate(
				(type: parameters.GetType(), ((IConfigurationID)dataConnection).ConfigurationID),
				dataConnection,
				static (o, dataConnection) =>
			{
				o.SlidingExpiration = dataConnection.Options.LinqOptions.CacheSlidingExpirationOrDefault;

				var type = o.Key.type;
				var td   = dataConnection.MappingSchema.GetEntityDescriptor(type, dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);
				var p    = Expression.Parameter(typeof(object), "p");
				var obj  = Expression.Parameter(type,           "obj");

				var expr = Expression.Lambda<Func<object,DataParameter[]>>(
					Expression.Block(
						new[] { obj },
						new Expression[]
						{
							Expression.Assign(obj, Expression.Convert(p, type)),
							Expression.NewArrayInit(
								typeof(DataParameter),
								td.Columns.Select(column =>
								{
									if (column.MemberType == typeof(DataParameter))
									{
										var pobj = Expression.Parameter(typeof(DataParameter));

										return Expression.Block(
											new[] { pobj },
											new Expression[]
											{
												Expression.Assign(pobj, ExpressionHelper.PropertyOrField(obj, column.MemberName)),
												Expression.MemberInit(
													Expression.New(typeof(DataParameter)),
													Expression.Bind(
														_dataParameterName,
														Expression.Coalesce(
															Expression.MakeMemberAccess(pobj, _dataParameterName),
															Expression.Constant(column.ColumnName))),
													Expression.Bind(
														_dataParameterDbDataType,
														Expression.MakeMemberAccess(pobj, _dataParameterDbDataType)),
													Expression.Bind(
														_dataParameterValue,
														Expression.Convert(
															Expression.MakeMemberAccess(pobj, _dataParameterValue),
															typeof(object))))
											});
									}

									var memberType  = column.MemberType.ToNullableUnderlying();
									var valueGetter = ExpressionHelper.PropertyOrField(obj, column.MemberName) as Expression;
									var mapper      = dataConnection.MappingSchema.GetConvertExpression(memberType, typeof(DataParameter), createDefault : false);

									if (mapper != null)
									{
										return Expression.Call(
											MemberHelper.MethodOf(() => PrepareDataParameter(null!, null!)),
											mapper.GetBody(valueGetter),
											Expression.Constant(column.ColumnName));
									}

									if (memberType.IsEnum)
									{
										var mapType  = ConvertBuilder.GetDefaultMappingFromEnumType(dataConnection.MappingSchema, memberType)!;
										var convExpr = dataConnection.MappingSchema.GetConvertExpression(column.MemberType, mapType)!;

										memberType  = mapType;
										valueGetter = convExpr.GetBody(valueGetter);
									}

									var columnDbDataType = new DbDataType(memberType, column.DataType, column.DbType, column.Length, column.Precision, column.Scale);
									if (columnDbDataType.DataType == DataType.Undefined)
										columnDbDataType = columnDbDataType.WithDataType(dataConnection.MappingSchema.GetDataType(memberType).Type.DataType);

									return (Expression)Expression.MemberInit(
										Expression.New(typeof(DataParameter)),
										Expression.Bind(
											_dataParameterName,
											Expression.Constant(column.ColumnName)),
										Expression.Bind(
											_dataParameterDbDataType,
											Expression.Constant(columnDbDataType, typeof(DbDataType))),
										Expression.Bind(
											_dataParameterValue,
											Expression.Convert(valueGetter, typeof(object))));
								}))
						}
					),
					p);

				return expr.CompileExpression();
			});

			return func(parameters);
		}

		static DataParameter PrepareDataParameter(DataParameter dataParameter, string name)
		{
			if (dataParameter == null)
				return new DataParameter { Name = name };

			dataParameter.Name = name;

			return dataParameter;
		}

		#endregion

		#region GetObjectReader

		record struct QueryKey(Type TargetType, Type DbReaderType, int ConfigId, string Sql, string? ExtraKey, bool IsScalar, Type? ScalarSourceType);

		static readonly MemoryCache<QueryKey,Delegate>                                            _objectReaders       = new (new ());
		static readonly MemoryCache<(Type readerType, Type providerReaderType),LambdaExpression?> _dataReaderConverter = new (new ());

		/// <summary>
		/// Clears global cache of object mapping functions from query results and mapping functions from value to <see cref="DataParameter"/>.
		/// </summary>
		public static void ClearObjectReaderCache()
		{
			_objectReaders.      Clear();
			_dataReaderConverter.Clear();
			_parameterReaders.   Clear();
		}

		string? GetCommandAdditionalKey(DbDataReader rd, Type resultType)
		{
			return IsDynamicType(resultType) || CommandType == CommandType.StoredProcedure
				? GetFieldsKey(rd)
				: null;
		}

		static string GetFieldsKey(DbDataReader dataReader)
		{
			using var sb = Pools.StringBuilder.Allocate();

			for (var i = 0; i < dataReader.FieldCount; i++)
			{
				sb.Value.Append(CultureInfo.InvariantCulture, $"{dataReader.GetName(i)},{dataReader.GetFieldType(i)};");
			}

			return sb.Value.ToString();
		}

		static Func<DbDataReader,T> GetObjectReader<T>(DataConnection dataConnection, DbDataReader dataReader, string sql, string? additionalKey)
		{
			var func = _objectReaders.GetOrCreate(
				new QueryKey(typeof(T), dataReader.GetType(), ((IConfigurationID)dataConnection).ConfigurationID, sql, additionalKey, dataReader.FieldCount <= 1, dataReader.FieldCount == 1 ? dataReader.GetFieldType(0) : null),
				(dataConnection, dataReader),
				static (e, context) =>
			{
				e.SlidingExpiration = context.dataConnection.Options.LinqOptions.CacheSlidingExpirationOrDefault;

				if (!e.Key.IsScalar && IsDynamicType(typeof(T)))
				{
					// dynamic case
					//
					return CreateDynamicObjectReader<T>(context.dataConnection, context.dataReader,
						(dc, dr, type, idx, dataReaderExpr) =>
							new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr, canBeNull: (bool?)null).Reduce(dc, dr));
				}

				return CreateObjectReader<T>(context.dataConnection, context.dataReader, (dc, dr, type, idx, dataReaderExpr, conv) =>
					new ConvertFromDataReaderExpression(type, idx, conv, dataReaderExpr, canBeNull: (bool?)null).Reduce(dc, dr));
			});

			return (Func<DbDataReader,T>)func;
		}

		static Func<DbDataReader,T> GetObjectReader2<T>(
			DataConnection dataConnection,
			DbDataReader   dataReader,
			string         sql,
			string?        additionalKey)
		{
			var key = new QueryKey(typeof(T), dataReader.GetType(), ((IConfigurationID)dataConnection).ConfigurationID, sql, additionalKey, dataReader.FieldCount <= 1, dataReader.FieldCount == 1 ? dataReader.GetFieldType(0) : null);

			Delegate func;

			if (!key.IsScalar && IsDynamicType(typeof(T)))
			{
				// dynamic case
				//
				func = CreateDynamicObjectReader<T>(dataConnection, dataReader, (dc, dr, type, idx, dataReaderExpr) =>
				new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr, canBeNull: true).Reduce(dc, slowMode: true));
			}
			else
			{
				func = CreateObjectReader<T>(dataConnection, dataReader, (dc, dr, type, idx, dataReaderExpr, conv) =>
				new ConvertFromDataReaderExpression(type, idx, conv, dataReaderExpr, canBeNull: true).Reduce(dc, slowMode: true));
			}

			_objectReaders.Set(key, func,
				new MemoryCacheEntryOptions<QueryKey> { SlidingExpiration = dataConnection.Options.LinqOptions.CacheSlidingExpirationOrDefault });

			return (Func<DbDataReader,T>)func;
		}

		static Func<DbDataReader, T> CreateObjectReader<T>(
			DataConnection dataConnection,
			DbDataReader   dataReader,
			Func<DataConnection, DbDataReader, Type, int,Expression,IValueConverter?,Expression> getMemberExpression)
		{
			var parameter      = Expression.Parameter(typeof(DbDataReader));
			var dataReaderExpr = (Expression)Expression.Convert(parameter, dataReader.GetType());
			var readerType     = dataReader.GetType();

			Expression? expr;
			LambdaExpression? converterExpr = null;

			if (dataConnection.DataProvider.DataReaderType != readerType)
			{
				converterExpr = _dataReaderConverter.GetOrCreate(
					(readerType, dataConnection.DataProvider.DataReaderType),
					dataConnection,
					static (o, ctx) =>
				{
					o.SlidingExpiration = ctx.Options.LinqOptions.CacheSlidingExpirationOrDefault;

					var expr = ctx.MappingSchema.GetConvertExpression(o.Key.readerType, typeof(DbDataReader), false, false);

					///// !!!!!
					if (expr != null)
					{
						expr = Expression.Lambda(Expression.Convert(expr.Body, ctx.DataProvider.DataReaderType), expr.Parameters);
					}

					return expr;
				});
			}

			dataReaderExpr = converterExpr != null ? converterExpr.GetBody(dataReaderExpr) : dataReaderExpr;

			if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
			{
				expr = getMemberExpression(dataConnection, dataReader, typeof(T), 0, dataReaderExpr, null);
			}
			else
			{
				var td = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T), dataConnection.Options.ConnectionOptions.OnEntityDescriptorCreated);

				if (td.InheritanceMapping.Count > 0 || td.HasComplexColumns)
				{
					var    readerBuilder = new RecordReaderBuilder(dataConnection, typeof(T), dataReader, converterExpr);
					return readerBuilder.BuildReaderFunction<T>();
				}

				var names = new string[dataReader.FieldCount];

				for (var i = 0; i < dataReader.FieldCount; i++)
					names[i] = dataReader.GetName(i);

				expr = null;

				var ctors = typeof(T).GetConstructors().Select(c => new { c, ps = c.GetParameters() }).ToList();

				if (ctors.Count > 0 && ctors.All(c => c.ps.Length > 0))
				{
					var q =
						from c in ctors
						let count = c.ps.Count(p => names.Contains(p.Name, dataConnection.MappingSchema.ColumnNameComparer))
						orderby count descending
						select c;

					var ctor = q.FirstOrDefault();

					if (ctor != null)
					{
						expr = Expression.New(
							ctor.c,
							ctor.ps.Select(p => names.Contains(p.Name, dataConnection.MappingSchema.ColumnNameComparer) ?
								getMemberExpression(
									dataConnection,
									dataReader,
									p.ParameterType,
									(names
										.Select((n,i) => new { n, i })
										.FirstOrDefault(n => dataConnection.MappingSchema.ColumnNameComparer.Compare(n.n, p.Name) == 0) ?? new { n="", i=-1 }).i,
									dataReaderExpr,
									null) :
								Expression.Constant(dataConnection.MappingSchema.GetDefaultValue(p.ParameterType), p.ParameterType)));
					}
				}

				if (expr == null)
				{
					var members =
					(
						from n in names.Select((name,idx) => new { name, idx })
						let   member = td.Columns.FirstOrDefault(m =>
							dataConnection.MappingSchema.ColumnNameComparer.Compare(m.ColumnName, n.name) == 0)
						where member != null
						select new
						{
							Member = member,
							Expr   = getMemberExpression(dataConnection, dataReader, member.MemberType, n.idx, dataReaderExpr, member.ValueConverter),
						}
					).ToList();

					expr = Expression.MemberInit(
						Expression.New(typeof(T)),
						members.Select(m => Expression.Bind(m.Member.MemberInfo, m.Expr)));
				}
			}

			if (expr.GetCount(dataReaderExpr, static (dataReaderExpr, e) => e == dataReaderExpr) > 1)
			{
				var dataReaderVar = Expression.Variable(dataReaderExpr.Type, "ldr");
				var assignment    = Expression.Assign(dataReaderVar, dataReaderExpr);

				expr = expr.Replace(dataReaderExpr, dataReaderVar);
				expr = Expression.Block(new[] { dataReaderVar }, assignment, expr);

				if (Common.Configuration.OptimizeForSequentialAccess)
					expr = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expr, dataReader.FieldCount, reduce: false);
			}

			var lex = Expression.Lambda<Func<DbDataReader,T>>(expr, parameter);

			return lex.CompileExpression();
		}

		static readonly ConstructorInfo _expandoObjectConstructor = MemberHelper.ConstructorOf(() => new ExpandoObject());
		static readonly MethodInfo      _expandoAddMethodInfo     = MemberHelper.MethodOf(() => ((IDictionary<string, object>)null!).Add("", ""));

		static Func<DbDataReader, T> CreateDynamicObjectReader<T>(
			DataConnection dataConnection,
			DbDataReader dataReader,
			Func<DataConnection, DbDataReader, Type, int, Expression, Expression> getMemberExpression)
		{
			var parameter      = Expression.Parameter(typeof(DbDataReader));
			var dataReaderExpr = (Expression)Expression.Convert(parameter, dataReader.GetType());
			var readerType     = dataReader.GetType();

			LambdaExpression? converterExpr = null;

			if (dataConnection.DataProvider.DataReaderType != readerType)
			{
				converterExpr = _dataReaderConverter.GetOrCreate(
					(readerType, dataConnection.DataProvider.DataReaderType),
					dataConnection,
					static (o, ctx) =>
				{
					o.SlidingExpiration = ctx.Options.LinqOptions.CacheSlidingExpirationOrDefault;

					var expr = ctx.MappingSchema.GetConvertExpression(o.Key.readerType, typeof(DbDataReader), false, false);

					if (expr != null)
					{
						expr = Expression.Lambda(Expression.Convert(expr.Body, ctx.DataProvider.DataReaderType), expr.Parameters);
					}

					return expr;
				});
			}

			dataReaderExpr = converterExpr != null ? converterExpr.GetBody(dataReaderExpr) : dataReaderExpr;

			var expr = (Expression)Expression.ListInit(
				Expression.New(_expandoObjectConstructor),
				Enumerable.Range(0, dataReader.FieldCount).Select(idx =>
				{
					var readerExpr = getMemberExpression(dataConnection, dataReader, typeof(object), idx,
						dataReaderExpr);
					return Expression.ElementInit(_expandoAddMethodInfo, Expression.Constant(dataReader.GetName(idx)), readerExpr);
				}));

			if (expr.GetCount(dataReaderExpr, static (dataReaderExpr, e) => e == dataReaderExpr) > 1)
			{
				var dataReaderVar = Expression.Variable(dataReaderExpr.Type, "ldr");
				var assignment    = Expression.Assign(dataReaderVar, dataReaderExpr);

				expr = expr.Replace(dataReaderExpr, dataReaderVar);
				expr = Expression.Block(new[] { dataReaderVar }, assignment, expr);
			}

			var lex = Expression.Lambda<Func<DbDataReader,T>>(expr, parameter);

			return lex.CompileExpression();
		}

		#endregion
	}
}
