// BASEDON: https://github.com/aspnet/EntityFramework/blob/rel/2.0.0-preview1/src/EFCore.SqlServer/Storage/Internal/SqlServerTransientExceptionDetector.cs

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.DataProvider.SqlServer
{
	/// <summary>
	/// Detects the exceptions caused by SQL Server transient failures.
	/// </summary>
	public static class SqlServerTransientExceptionDetector
	{
		private static readonly ConcurrentDictionary<Type, Func<Exception, IEnumerable<int>>> _exceptionTypes = new ();

		internal static void RegisterExceptionType(Type type, Func<Exception, IEnumerable<int>> errrorNumbersGetter)
		{
			_exceptionTypes.TryAdd(type, errrorNumbersGetter);
		}

		public static bool IsHandled(Exception ex, [NotNullWhen(true)] out IEnumerable<int>? errorNumbers)
		{
			if (_exceptionTypes.TryGetValue(ex.GetType(), out var getter))
			{
				errorNumbers = getter(ex);
				return true;
			}

			errorNumbers = null;
			return false;
		}

		public static bool ShouldRetryOn(Exception ex)
		{
			if (IsHandled(ex, out var errors))
			{
				foreach (var err in errors)
				{
					return err
						// SQL Error Code: 49920
						// Cannot process request. Too many operations in progress for subscription "%ld".
						// The service is busy processing multiple requests for this subscription.
						// Requests are currently blocked for resource optimization. Query sys.dm_operation_status for operation status.
						// Wait until pending requests are complete or delete one of your pending requests and retry your request later.
						is 49920
						// SQL Error Code: 49919
						// Cannot process create or update request. Too many create or update operations in progress for subscription "%ld".
						// The service is busy processing multiple create or update requests for your subscription or server.
						// Requests are currently blocked for resource optimization. Query sys.dm_operation_status for pending operations.
						// Wait till pending create or update requests are complete or delete one of your pending requests and
						// retry your request later.
						or 49919
						// SQL Error Code: 49918
						// Cannot process request. Not enough resources to process request.
						// The service is currently busy.Please retry the request later.
						or 49918
						// SQL Error Code: 41839
						// Transaction exceeded the maximum number of commit dependencies.
						or 41839
						// SQL Error Code: 41325
						// The current transaction failed to commit due to a serializable validation failure.
						or 41325
						// SQL Error Code: 41305
						// The current transaction failed to commit due to a repeatable read validation failure.
						or 41305
						// SQL Error Code: 41302
						// The current transaction attempted to update a record that has been updated since the transaction started.
						or 41302
						// SQL Error Code: 41301
						// Dependency failure: a dependency was taken on another transaction that later failed to commit.
						or 41301
						// SQL Error Code: 40613
						// Database XXXX on server YYYY is not currently available. Please retry the connection later.
						// If the problem persists, contact customer support, and provide them the session tracing ID of ZZZZZ.
						or 40613
						// SQL Error Code: 40501
						// The service is currently busy. Retry the request after 10 seconds. Code: (reason code to be decoded).
						or 40501
						// SQL Error Code: 40197
						// The service has encountered an error processing your request. Please try again.
						or 40197
						// SQL Error Code: 10929
						// Resource ID: %d. The %s minimum guarantee is %d, maximum limit is %d and the current usage for the database is %d.
						// However, the server is currently too busy to support requests greater than %d for this database.
						// For more information, see http://go.microsoft.com/fwlink/?LinkId=267637. Otherwise, please try again.
						or 10929
						// SQL Error Code: 10928
						// Resource ID: %d. The %s limit for the database is %d and has been reached. For more information,
						// see http://go.microsoft.com/fwlink/?LinkId=267637.
						or 10928
						// SQL Error Code: 10060
						// A network-related or instance-specific error occurred while establishing a connection to SQL Server.
						// The server was not found or was not accessible. Verify that the instance name is correct and that SQL Server
						// is configured to allow remote connections. (provider: TCP Provider, error: 0 - A connection attempt failed
						// because the connected party did not properly respond after a period of time, or established connection failed
						// because connected host has failed to respond.)"}
						or 10060
						// SQL Error Code: 10054
						// A transport-level error has occurred when sending the request to the server.
						// (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by the remote host.)
						or 10054
						// SQL Error Code: 10053
						// A transport-level error has occurred when receiving results from the server.
						// An established connection was aborted by the software in your host machine.
						or 10053
						// SQL Error Code: 1205
						// Deadlock
						or 1205
						// SQL Error Code: 233
						// The client was unable to establish a connection because of an error during connection initialization process before login.
						// Possible causes include the following: the client tried to connect to an unsupported version of SQL Server;
						// the server was too busy to accept new connections; or there was a resource limitation (insufficient memory or maximum
						// allowed connections) on the server. (provider: TCP Provider, error: 0 - An existing connection was forcibly closed by
						// the remote host.)
						or 233
						// SQL Error Code: 121
						// The semaphore timeout period has expired
						or 121
						// SQL Error Code: 64
						// A connection was successfully established with the server, but then an error occurred during the login process.
						// (provider: TCP Provider, error: 0 - The specified network name is no longer available.)
						or 64
						// DBNETLIB Error Code: 20
						// The instance of SQL Server you attempted to connect to does not support encryption.
						or 20;
						// This exception can be thrown even if the operation completed successfully, so it's safer to let the application fail.
						// DBNETLIB Error Code: -2
						// Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding. The statement has been terminated.
						//or -2;
				}

				return false;
			}

			return ex is TimeoutException;
		}
	}
}
