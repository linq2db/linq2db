﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

using JetBrains.Annotations;

// type, readertype, configID, sql, additionalKey, isScalar
using QueryKey = System.ValueTuple<System.Type, System.Type, int, string, string?, bool>;

namespace LinqToDB.Data
{
	using Common;
	using Expressions;
	using Extensions;
	using Mapping;
	using Async;
	using Reflection;
	using Linq;
	using Common.Internal.Cache;

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
		public IEnumerable<T> QueryProc<T>(Func<IDataReader,T> objectReader)
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
		public Task<IEnumerable<T>> QueryProcAsync<T>(Func<IDataReader, T> objectReader, CancellationToken cancellationToken = default)
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
		public IEnumerable<T> Query<T>(Func<IDataReader,T> objectReader)
		{
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return ReadEnumerator(
				DataConnection.ExecuteReader(GetCommandBehavior()),
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
		public async Task<IEnumerable<T>> QueryAsync<T>(Func<IDataReader, T> objectReader, CancellationToken cancellationToken = default)
		{
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return ReadEnumerator(
				await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext),
				objectReader,
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		IEnumerable<T> ReadEnumerator<T>(IDataReader rd, Func<IDataReader, T> objectReader, IDisposable? scope)
		{
			using (scope)
			using (rd)
			{
				while (rd.Read())
					yield return objectReader(rd);
			}

			if (Parameters?.Length > 0)
				RebindParameters(DataConnection, Parameters!);
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
		public async Task<List<T>> QueryToListAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			return list;
		}

		/// <summary>
		/// Executes command asynchronously and returns array of values, mapped using provided mapping function.
		/// </summary>
		/// <typeparam name="T">Result record type.</typeparam>
		/// <param name="objectReader">Record mapping function from data reader.</param>
		/// <param name="cancellationToken">Asynchronous operation cancellation token.</param>
		/// <returns>Returns task with array of query result records.</returns>
		public async Task<T[]> QueryToArrayAsync<T>(Func<IDataReader,T> objectReader, CancellationToken cancellationToken = default)
		{
			var list = new List<T>();
			await QueryForEachAsync(objectReader, list.Add, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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
		public async Task QueryForEachAsync<T>(Func<IDataReader,T> objectReader, Action<T> action, CancellationToken cancellationToken = default)
		{
			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			{
#if NETSTANDARD2_1PLUS
				var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				await using (rd.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
			using (var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
#endif
				while (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
					action(objectReader(rd));
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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return ReadEnumerator<T>(
				DataConnection.ExecuteReader(GetCommandBehavior()),
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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return ReadEnumerator<T>(
				await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext),
				DataConnection.DataProvider.ExecuteScope(DataConnection));
		}

		static bool IsDynamicType(Type type)
		{
			return typeof(object) == type || typeof(ExpandoObject) == type;
		}
		
		IEnumerable<T> ReadEnumerator<T>(IDataReader rd, IDisposable? scope, bool disposeReader = true)
		{
			using (scope)
				try
				{
					if (rd.Read())
					{
						var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
						var objectReader  = GetObjectReader<T>(DataConnection, rd, DataConnection.Command.CommandText,
							additionalKey);
						var isFaulted = false;

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
								objectReader = GetObjectReader2<T>(DataConnection, rd, DataConnection.Command.CommandText,
									additionalKey);
								result = objectReader(rd);
							}

							yield return result;

						} while (rd.Read());
					}
				}
				finally
				{
					if (disposeReader)
						rd.Dispose();
				}

			if (Parameters?.Length > 0)
				RebindParameters(DataConnection, Parameters!);
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
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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
			await QueryForEachAsync<T>(list.Add, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
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
			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			{
#if NETSTANDARD2_1PLUS
				var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				await using (rd.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
			using (var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
#endif
			{
				if (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				{
					var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
					var objectReader  = GetObjectReader<T>(DataConnection, rd, DataConnection.Command.CommandText, additionalKey);
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
							objectReader = GetObjectReader2<T>(DataConnection, rd, DataConnection.Command.CommandText, additionalKey);
							result       = objectReader(rd);
						}

						action(result);

					} while (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext));
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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			T result;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			using (var rd = DataConnection.ExecuteReader(GetCommandBehavior()))
			{
				result = ReadMultipleResultSets<T>(rd);
			}

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

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
			var hasParameters = Parameters?.Length > 0;

			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			T result;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			{
#if NETSTANDARD2_1PLUS
				var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				await using (rd.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
			using (var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
#endif
			{
				result = await ReadMultipleResultSetsAsync<T>(rd, cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
			}
			}

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

			return result;
		}

		Dictionary<int, MemberAccessor> GetMultipleQueryIndexMap<T>(TypeAccessor<T> typeAccessor)
		{
			var members = typeAccessor.Members.Where(m => m.HasSetter).ToArray();

			// Use attribute labels if any exist.
			var indexMap = (from m in members
				let a = m.GetAttributes<ResultSetIndexAttribute>()
				where a != null
				select new { Member = m, a[0].Index }).ToDictionary(e => e.Index, e => e.Member);

			if (indexMap.Count == 0)
			{
				// Use ordering of properties according to reflection.
				for (var i = 0; i < members.Length; i++)
				{
					indexMap[i] = members[i];
				}
			}

			return indexMap;
		}

		static readonly MethodInfo _readAsArrayMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsArray<int>(null!)).GetGenericMethodDefinition();

		static readonly MethodInfo _readAsListMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsList<int>(null!)).GetGenericMethodDefinition();

		static readonly MethodInfo _readSingletMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadSingle<int>(null!)).GetGenericMethodDefinition();

		T[] ReadAsArray<T>(IDataReader rd)
		{
			return ReadEnumerator<T>(rd, null, false).ToArray();
		}

		List<T> ReadAsList<T>(IDataReader rd)
		{
			return ReadEnumerator<T>(rd, null, false).ToList();
		}

		T ReadSingle<T>(IDataReader rd)
		{
			return ReadEnumerator<T>(rd, null, false).FirstOrDefault();
		}

		T ReadMultipleResultSets<T>(IDataReader rd)
			where T : class
		{
			var typeAccessor = TypeAccessor.GetAccessor<T>();
			var indexMap     = GetMultipleQueryIndexMap(typeAccessor);

			var resultIndex = 0;
			var result = typeAccessor.Create();
			do
			{
				// Only process the field if we're reading it into a property.
				if (indexMap.ContainsKey(resultIndex))
				{
					var member = indexMap[resultIndex];
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
					var value = genericMethod.Invoke(this, new object[] { rd });

					member.SetValue(result, value);
				}

				resultIndex++;
			} while (rd.NextResult());

			return result;
		}

		static readonly MethodInfo _readAsArrayAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsArrayAsync<int>(null!, default)).GetGenericMethodDefinition();

		static readonly MethodInfo _readAsListAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadAsListAsync<int>(null!, default)).GetGenericMethodDefinition();

		static readonly MethodInfo _readSingletAsyncMethodInfo =
			MemberHelper.MethodOf<CommandInfo>(ci => ci.ReadSingleAsync<int>(null!, default)).GetGenericMethodDefinition();

		class ReaderAsyncEnumerable<T> : IAsyncEnumerable<T>
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

		class ReaderAsyncEnumerator<T> : IAsyncEnumerator<T>
		{
			readonly CommandInfo      _commandInfo;
			readonly DbDataReader     _rd;
			readonly string?          _additionalKey;
			Func<IDataReader, T>      _objectReader;
			bool                      _isFaulted;
			bool                      _isFinished;
			CancellationToken         _cancellationToken;

			public ReaderAsyncEnumerator(CommandInfo commandInfo, DbDataReader rd, CancellationToken cancellationToken)
			{
				_commandInfo       = commandInfo;
				_rd                = rd;
				_additionalKey     = commandInfo.GetCommandAdditionalKey(rd, typeof(T));
				_objectReader      = GetObjectReader<T>(commandInfo.DataConnection, rd, commandInfo.DataConnection.Command.CommandText, _additionalKey);
				_isFaulted         = false;
				_cancellationToken = cancellationToken;
			}

			public void Dispose()
			{
			}

#if !NATIVE_ASYNC
			public Task DisposeAsync() => TaskEx.CompletedTask;
#else
			public ValueTask DisposeAsync() => default;
#endif

			public T Current { get; private set; } = default!;

#if !NATIVE_ASYNC
			public async Task<bool> MoveNextAsync()
#else
			public async ValueTask<bool> MoveNextAsync()
#endif
			{
				if (_isFinished)
					return false;
				if (!await _rd.ReadAsync(_cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
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
						_commandInfo.DataConnection.Command.CommandText,
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

		Task<T> ReadSingleAsync<T>(DbDataReader rd, CancellationToken cancellationToken)
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
				if (indexMap.ContainsKey(resultIndex))
				{
					var member = indexMap[resultIndex];
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
					var task = (Task)genericMethod.Invoke(this, new object[] { rd, cancellationToken })!;
					await task.ConfigureAwait(Configuration.ContinueOnCapturedContext);

					// Task<T>.Result
					var value = ((dynamic)task).Result;

					member.SetValue(result, value);
				}

				resultIndex++;
			} while (await rd.NextResultAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext));

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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			var commandResult = DataConnection.ExecuteNonQuery();

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

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
			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			var commandResult = await DataConnection.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

			return commandResult;
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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			T result = default!;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			using (var rd = DataConnection.ExecuteReader(GetCommandBehavior()))
			{
				if (rd.Read())
				{
					var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
					var objectReader  = GetObjectReader<T>(DataConnection, rd, CommandText, additionalKey);

#if DEBUG
					//var value = rd.GetValue(0);
					//return default (T);
#endif

					try
					{
						result = objectReader(rd);
					}
					catch (InvalidCastException)
					{
						result = GetObjectReader2<T>(DataConnection, rd, CommandText, additionalKey)(rd);
					}
					catch (FormatException)
					{
						result = GetObjectReader2<T>(DataConnection, rd, CommandText, additionalKey)(rd);
					}
				}
			}

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

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
			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			T result = default!;

			using (DataConnection.DataProvider.ExecuteScope(DataConnection))
			{
#if NETSTANDARD2_1PLUS
				var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);
				await using (rd.ConfigureAwait(Configuration.ContinueOnCapturedContext))
#else
			using (var rd = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
#endif
			{
				if (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
				{
					var additionalKey = GetCommandAdditionalKey(rd, typeof(T));
					try
					{
						result = GetObjectReader<T>(DataConnection, rd, CommandText, additionalKey)(rd);
					}
					catch (InvalidCastException)
					{
						result = GetObjectReader2<T>(DataConnection, rd, CommandText, additionalKey)(rd);
					}
				}
			}
			}

			if (hasParameters)
				RebindParameters(DataConnection, Parameters!);

			return result;
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
			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return new DataReader { CommandInfo = this, Reader = DataConnection.ExecuteReader(GetCommandBehavior()), OnDispose = hasParameters ? () => RebindParameters(DataConnection, Parameters!) : null };
		}

		internal IEnumerable<T> ExecuteQuery<T>(IDataReader rd, string sql)
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
		internal T ExecuteScalar<T>(IDataReader rd, string sql)
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
			await DataConnection.EnsureConnectionAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext);

			var hasParameters = Parameters?.Length > 0;

			DataConnection.InitCommand(CommandType, CommandText, Parameters, null, hasParameters);

			if (hasParameters)
				SetParameters(DataConnection, Parameters!);

			return new DataReaderAsync { CommandInfo = this, Reader = await DataConnection.ExecuteReaderAsync(GetCommandBehavior(), cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext), OnDispose = hasParameters ? () => RebindParameters(DataConnection, Parameters!) : null };
		}

		internal async Task ExecuteQueryAsync<T>(DbDataReader rd, string sql, Action<T> action, CancellationToken cancellationToken)
		{
			if (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
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

				} while (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext));
			}
		}

		internal async Task<T> ExecuteScalarAsync<T>(DbDataReader rd, string sql, CancellationToken cancellationToken)
		{
			if (await rd.ReadAsync(cancellationToken).ConfigureAwait(Configuration.ContinueOnCapturedContext))
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
				var p          = dataConnection.Command.CreateParameter();
				var dbDataType = parameter.DbDataType;
				var value      = parameter.Value;

				if (dbDataType.DataType == DataType.Undefined && value != null)
					dbDataType = dbDataType.WithDataType(dataConnection.MappingSchema.GetDataType(value.GetType()).Type.DataType);

				if (parameter.Direction != null) p.Direction =       parameter.Direction.Value;
				if (parameter.Size      != null) p.Size      =       parameter.Size     .Value;
				if (parameter.Precision != null) p.Precision = (byte)parameter.Precision.Value;
				if (parameter.Scale     != null) p.Scale     = (byte)parameter.Scale    .Value;

				dataConnection.DataProvider.SetParameter(dataConnection, p, parameter.Name!, dbDataType, value);
				dataConnection.Command.Parameters.Add(p);
			}
		}

		static object ConvertParameterValue<TFrom>(TFrom value, MappingSchema mappingSchema)
		{
			var converter = mappingSchema.GetConverter<TFrom, object>();
			var result    = converter!(value);
			return result;
		}

		private static readonly MethodInfo _convertParameterValueMethodInfo =
			MemberHelper.MethodOf(() => ConvertParameterValue(1, MappingSchema.Default)).GetGenericMethodDefinition();

		static object? ConvertParameterValue(object? value, MappingSchema mappingSchema)
		{
			if (ReferenceEquals(value, null))
				return null;

			var methodInfo = _convertParameterValueMethodInfo.MakeGenericMethod(value.GetType());
			var result     = methodInfo.Invoke(null, new[] { value, mappingSchema });

			return result;
		}

		static void RebindParameters(DataConnection dataConnection, DataParameter[] parameters)
		{
			var dbParameters = dataConnection.Command.Parameters;

			for (var i = 0; i < parameters.Length; i++)
			{
				var dataParameter = parameters[i];

				if (dataParameter.Direction.HasValue &&
					(dataParameter.Direction == ParameterDirection.Output || dataParameter.Direction == ParameterDirection.InputOutput || dataParameter.Direction == ParameterDirection.ReturnValue))
				{
					var dbParameter      = (IDbDataParameter)dbParameters[i]!;
					dataParameter.Output = dbParameter;

					if (!Equals(dataParameter.Value, dbParameter.Value))
					{
						dataParameter.Value = ConvertParameterValue(dbParameter.Value, dataConnection.MappingSchema);
					}
				}
			}
		}

		static readonly MemoryCache<(Type type, int contextId)>  _parameterReaders = new (new ());

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
				(type: parameters.GetType(), dataConnection.ID),
				dataConnection,
				static (o, dataConnection) =>
				{
					var type = o.Key.type;
					o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

					var td  = dataConnection.MappingSchema.GetEntityDescriptor(type);
					var p   = Expression.Parameter(typeof(object), "p");
					var obj = Expression.Parameter(type, "obj");

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

		static readonly MemoryCache<QueryKey>                                   _objectReaders       = new (new ());
		static readonly MemoryCache<(Type readerType, Type providerReaderType)> _dataReaderConverter = new (new ());

		/// <summary>
		/// Clears global cache of object mapping functions from query results and mapping functions from value to <see cref="DataParameter"/>.
		/// </summary>
		public static void ClearObjectReaderCache()
		{
			_objectReaders.      Clear();
			_dataReaderConverter.Clear();
			_parameterReaders.   Clear();
		}

		string? GetCommandAdditionalKey(IDataReader rd, Type resultType)
		{
			return IsDynamicType(resultType) || DataConnection.Command.CommandType == CommandType.StoredProcedure
				? GetFieldsKey(rd)
				: null;
		}

		static string GetFieldsKey(IDataReader dataReader)
		{
			var sb = new StringBuilder();

			for (int i = 0; i < dataReader.FieldCount; i++)
			{
				sb.Append(dataReader.GetName(i))
					.Append(',')
					.Append(dataReader.GetFieldType(i))
					.Append(';');
			}

			return sb.ToString();
		}

		static Func<IDataReader, T> GetObjectReader<T>(DataConnection dataConnection, IDataReader dataReader,
			string sql, string? additionalKey)
		{
			var func = _objectReaders.GetOrCreate(
				new QueryKey(typeof(T), dataReader.GetType(), dataConnection.ID, sql, additionalKey, dataReader.FieldCount <= 1),
				(dataConnection, dataReader),
				static (e, context) =>
				{
					e.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

					if (!e.Key.Item6 && IsDynamicType(typeof(T)))
					{
						// dynamic case
						//
						return CreateDynamicObjectReader<T>(context.dataConnection, context.dataReader, (dc, dr, type, idx, dataReaderExpr) =>
						new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr).Reduce(dc, dr));
					}

					return CreateObjectReader<T>(context.dataConnection, context.dataReader, (dc, dr, type, idx, dataReaderExpr) =>
						new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr).Reduce(dc, dr));
				});

			return func;
		}

		static Func<IDataReader, T> GetObjectReader2<T>(DataConnection dataConnection, IDataReader dataReader,
			string sql, string? additionalKey)
		{
			var key = (typeof(T), dataReader.GetType(), dataConnection.ID, sql, additionalKey, dataReader.FieldCount <= 1);

			Delegate func;
			if (!key.Item6 && IsDynamicType(typeof(T)))
			{
				// dynamic case
				//
				func = CreateDynamicObjectReader<T>(dataConnection, dataReader, (dc, dr, type, idx, dataReaderExpr) =>
				new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr).Reduce(dc, slowMode: true));
			}
			else
			{
				func = CreateObjectReader<T>(dataConnection, dataReader, (dc, dr, type, idx, dataReaderExpr) =>
				new ConvertFromDataReaderExpression(type, idx, null, dataReaderExpr).Reduce(dc, slowMode: true));
			}

			_objectReaders.Set(key, func,
				new MemoryCacheEntryOptions<QueryKey>() {SlidingExpiration = Configuration.Linq.CacheSlidingExpiration});

			return (Func<IDataReader, T>)func;
		}

		static Func<IDataReader,T> CreateObjectReader<T>(
			DataConnection dataConnection,
			IDataReader    dataReader,
			Func<DataConnection, IDataReader, Type, int,Expression,Expression> getMemberExpression)
		{
			var parameter      = Expression.Parameter(typeof(IDataReader));
			var dataReaderExpr = (Expression)Expression.Convert(parameter, dataReader.GetType());

			Expression? expr;

			var readerType = dataReader.GetType();
			LambdaExpression? converterExpr = null;
			if (dataConnection.DataProvider.DataReaderType != readerType)
			{
				converterExpr    = _dataReaderConverter.GetOrCreate(
					(readerType, dataConnection.DataProvider.DataReaderType),
					dataConnection,
					static (o, dataConnection) =>
					{
						o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

						var expr = dataConnection.MappingSchema.GetConvertExpression(o.Key.readerType, typeof(IDataReader), false, false);
						if (expr != null)
						{
							expr      = Expression.Lambda(Expression.Convert(expr.Body, dataConnection.DataProvider.DataReaderType), expr.Parameters);
						}

						return expr;
					});
			}

			dataReaderExpr = converterExpr != null ? converterExpr.GetBody(dataReaderExpr) : dataReaderExpr;

			if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
			{
				expr = getMemberExpression(dataConnection, dataReader, typeof(T), 0, dataReaderExpr);
			}
			else
			{
				var td = dataConnection.MappingSchema.GetEntityDescriptor(typeof(T));

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
									dataReaderExpr) :
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
							Expr   = getMemberExpression(dataConnection, dataReader, member.MemberType, n.idx, dataReaderExpr),
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

				if (Configuration.OptimizeForSequentialAccess)
					expr = SequentialAccessHelper.OptimizeMappingExpressionForSequentialAccess(expr, dataReader.FieldCount, reduce: false);
			}

			var lex = Expression.Lambda<Func<IDataReader,T>>(expr, parameter);

			return lex.CompileExpression();
		}


		static readonly ConstructorInfo _expandoObjectConstructor = MemberHelper.ConstructorOf(() => new ExpandoObject());
		static readonly MethodInfo      _expandoAddMethodInfo     = MemberHelper.MethodOf(() => ((IDictionary<string, object>)null!).Add("", ""));
		
		static Func<IDataReader, T> CreateDynamicObjectReader<T>(
			DataConnection dataConnection,
			IDataReader dataReader,
			Func<DataConnection, IDataReader, Type, int, Expression, Expression> getMemberExpression)
		{
			var parameter      = Expression.Parameter(typeof(IDataReader));
			var dataReaderExpr = (Expression)Expression.Convert(parameter, dataReader.GetType());

			var readerType = dataReader.GetType();
			LambdaExpression? converterExpr = null;
			if (dataConnection.DataProvider.DataReaderType != readerType)
			{
				converterExpr    = _dataReaderConverter.GetOrCreate(
					(readerType, dataConnection.DataProvider.DataReaderType),
					dataConnection,
					static (o, dataConnection) =>
					{
						o.SlidingExpiration = Configuration.Linq.CacheSlidingExpiration;

						var expr = dataConnection.MappingSchema.GetConvertExpression(o.Key.readerType, typeof(IDataReader), false, false);
						if (expr != null)
						{
							expr = Expression.Lambda(Expression.Convert(expr.Body, dataConnection.DataProvider.DataReaderType), expr.Parameters);
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

			var lex = Expression.Lambda<Func<IDataReader,T>>(expr, parameter);

			return lex.CompileExpression();
		}


		#endregion
	}
}
