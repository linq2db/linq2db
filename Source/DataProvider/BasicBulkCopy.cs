using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace LinqToDB.DataProvider
{
	using Data;
	using Expressions;
	using Mapping;
	using SqlProvider;

	class BasicBulkCopy
	{
		public virtual BulkCopyRowsCopied BulkCopy<T>(BulkCopyType bulkCopyType, DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			switch (bulkCopyType)
			{
				case BulkCopyType.MultipleRows : return MultipleRowsCopy    (dataConnection, options, source);
				case BulkCopyType.RowByRow     : return RowByRowCopy        (dataConnection, options, source);
				default                        : return ProviderSpecificCopy(dataConnection, options, source);
			}
		}

		protected virtual BulkCopyRowsCopied ProviderSpecificCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return MultipleRowsCopy(dataConnection, options, source);
		}

		protected virtual BulkCopyRowsCopied MultipleRowsCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			return RowByRowCopy(dataConnection, options, source);
		}

		protected virtual BulkCopyRowsCopied RowByRowCopy<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			var rowsCopied = new BulkCopyRowsCopied();

			foreach (var item in source)
			{
				dataConnection.Insert(item, options.TableName, options.DatabaseName, options.SchemaName);
				rowsCopied.RowsCopied++;

				if (options.NotifyAfter != 0 && options.RowsCopiedCallback != null && rowsCopied.RowsCopied % options.NotifyAfter == 0)
				{
					options.RowsCopiedCallback(rowsCopied);

					if (rowsCopied.Abort)
						break;
				}
			}

			return rowsCopied;
		}

		protected internal static string GetTableName(ISqlBuilder sqlBuilder, BulkCopyOptions options, EntityDescriptor descriptor)
		{
			var databaseName = options.DatabaseName ?? descriptor.DatabaseName;
			var schemaName   = options.SchemaName   ?? descriptor.SchemaName;
			var tableName    = options.TableName    ?? descriptor.TableName;

			return sqlBuilder.BuildTableName(
				new StringBuilder(),
				databaseName == null ? null : sqlBuilder.Convert(databaseName, ConvertType.NameToDatabase).  ToString(),
				schemaName   == null ? null : sqlBuilder.Convert(schemaName,   ConvertType.NameToOwner).     ToString(),
				tableName    == null ? null : sqlBuilder.Convert(tableName,    ConvertType.NameToQueryTable).ToString())
			.ToString();
		}

		#region ProviderSpecific Support

		protected Func<IDbConnection,int,IDisposable> CreateBulkCopyCreator(
			Type connectionType, Type bulkCopyType, Type bulkCopyOptionType)
		{
			var p1 = Expression.Parameter(typeof(IDbConnection), "pc");
			var p2 = Expression.Parameter(typeof(int),           "po");
			var l  = Expression.Lambda<Func<IDbConnection,int,IDisposable>>(
				Expression.Convert(
					Expression.New(
						bulkCopyType.GetConstructor(new[] { connectionType, bulkCopyOptionType }),
						Expression.Convert(p1, connectionType),
						Expression.Convert(p2, bulkCopyOptionType)),
					typeof(IDisposable)),
				p1, p2);

			return l.Compile();
		}

		protected Func<int,string,object> CreateColumnMappingCreator(Type columnMappingType)
		{
			var p1 = Expression.Parameter(typeof(int),    "p1");
			var p2 = Expression.Parameter(typeof(string), "p2");
			var l  = Expression.Lambda<Func<int,string,object>>(
				Expression.Convert(
					Expression.New(
						columnMappingType.GetConstructor(new[] { typeof(int), typeof(string) }),
						new [] { p1, p2 }),
					typeof(object)),
				p1, p2);

			return l.Compile();
		}

		protected Action<object,Action<object>> CreateBulkCopySubscriber(object bulkCopy, string eventName)
		{
			var eventInfo   = bulkCopy.GetType().GetEvent(eventName);
			var handlerType = eventInfo.EventHandlerType;
			var eventParams = handlerType.GetMethod("Invoke").GetParameters();

			// Expression<Func<Action<object>,Delegate>> lambda =
			//     actionParameter => Delegate.CreateDelegate(
			//         typeof(int),
			//         (Action<object,DB2RowsCopiedEventArgs>)((o,e) => actionParameter(e)),
			//         "Invoke",
			//         false);

			var actionParameter = Expression.Parameter(typeof(Action<object>), "p1");
			var senderParameter = Expression.Parameter(eventParams[0].ParameterType, eventParams[0].Name);
			var argsParameter   = Expression.Parameter(eventParams[1].ParameterType, eventParams[1].Name);

			var lambda = Expression.Lambda<Func<Action<object>, Delegate>>(
				Expression.Call(
					null,
					MemberHelper.MethodOf(() => Delegate.CreateDelegate(typeof(string), (object)null, "", false)),
					new Expression[]
					{
						Expression.Constant(handlerType, typeof(Type)),
						//Expression.Convert(
							Expression.Lambda(
								Expression.Invoke(actionParameter, new Expression[] { argsParameter }),
								new[] { senderParameter, argsParameter }),
						//	typeof(Action<object, EventArgs>)),
						Expression.Constant("Invoke", typeof(string)),
						Expression.Constant(false, typeof(bool))
					}),
				new[] { actionParameter });

			var dgt = lambda.Compile();

			return (obj,action) => eventInfo.AddEventHandler(obj, dgt(action));
		}

		protected void TraceAction(DataConnection dataConnection, string commandText, Func<int> action)
		{
			if (DataConnection.TraceSwitch.TraceInfo)
			{
				DataConnection.OnTrace(new TraceInfo
				{
					BeforeExecute  = true,
					TraceLevel     = TraceLevel.Info,
					DataConnection = dataConnection,
					CommandText    = commandText,
				});
			}

			var now = DateTime.Now;

			try
			{
				var count = action();

				if (DataConnection.TraceSwitch.TraceInfo)
				{
					DataConnection.OnTrace(new TraceInfo
					{
						TraceLevel      = TraceLevel.Info,
						DataConnection  = dataConnection,
						CommandText     = commandText,
						ExecutionTime   = DateTime.Now - now,
						RecordsAffected = count,
					});
				}
			}
			catch (Exception ex)
			{
				if (DataConnection.TraceSwitch.TraceError)
				{
					DataConnection.OnTrace(new TraceInfo
					{
						TraceLevel     = TraceLevel.Error,
						DataConnection = dataConnection,
						CommandText    = commandText,
						ExecutionTime  = DateTime.Now - now,
						Exception      = ex,
					});
				}

				throw;
			}
		}

		#endregion

		#region MultipleRows Support

		protected BulkCopyRowsCopied MultipleRowsCopy1<T>(
			DataConnection dataConnection, BulkCopyOptions options, bool enforceKeepIdentity, IEnumerable<T> source)
		{
			return MultipleRowsCopy1(
				new MultipleRowsHelper<T>(dataConnection, options, enforceKeepIdentity),
				dataConnection,
				options,
				source);
		}

		protected BulkCopyRowsCopied MultipleRowsCopy1<T>(
			MultipleRowsHelper<T> helper, DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.AppendLine()
					.Append("\t")
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(")");

			helper.StringBuilder
				.AppendLine()
				.Append("VALUES");

			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("(");
				helper.BuildColumns(item);
				helper.StringBuilder.Append("),");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.Length--;
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.Length--;
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		protected  BulkCopyRowsCopied MultipleRowsCopy2<T>(
			DataConnection dataConnection, BulkCopyOptions options, bool enforceKeepIdentity, IEnumerable<T> source, string from)
		{
			return MultipleRowsCopy2<T>(
				new MultipleRowsHelper<T>(dataConnection, options, enforceKeepIdentity),
				dataConnection,
				options,
				source,
				from);
		}

		protected  BulkCopyRowsCopied MultipleRowsCopy2<T>(
			MultipleRowsHelper<T> helper, DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source, string from)
		{
			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.AppendLine()
					.Append("\t")
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.Append(")");

			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("SELECT ");
				helper.BuildColumns(item);
				helper.StringBuilder.Append(from);
				helper.StringBuilder.Append(" UNION ALL");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.Length -= " UNION ALL".Length;
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.Length -= " UNION ALL".Length;
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		protected  BulkCopyRowsCopied MultipleRowsCopy3<T>(DataConnection dataConnection, BulkCopyOptions options, IEnumerable<T> source, string from)
		{
			var helper = new MultipleRowsHelper<T>(dataConnection, options, false);

			helper.StringBuilder
				.AppendFormat("INSERT INTO {0}", helper.TableName).AppendLine()
				.Append("(");

			foreach (var column in helper.Columns)
				helper.StringBuilder
					.AppendLine()
					.Append("\t")
					.Append(helper.SqlBuilder.Convert(column.ColumnName, ConvertType.NameToQueryField))
					.Append(",");

			helper.StringBuilder.Length--;
			helper.StringBuilder
				.AppendLine()
				.AppendLine(")")
				.AppendLine("SELECT * FROM")
				.Append("(");

			helper.SetHeader();

			foreach (var item in source)
			{
				helper.StringBuilder
					.AppendLine()
					.Append("\tSELECT ");
				helper.BuildColumns(item);
				helper.StringBuilder.Append(from);
				helper.StringBuilder.Append(" UNION ALL");

				helper.RowsCopied.RowsCopied++;
				helper.CurrentCount++;

				if (helper.CurrentCount >= helper.BatchSize || helper.Parameters.Count > 10000 || helper.StringBuilder.Length > 100000)
				{
					helper.StringBuilder.Length -= " UNION ALL".Length;
					helper.StringBuilder
						.AppendLine()
						.Append(")");
					if (!helper.Execute())
						return helper.RowsCopied;
				}
			}

			if (helper.CurrentCount > 0)
			{
				helper.StringBuilder.Length -= " UNION ALL".Length;
				helper.StringBuilder
					.AppendLine()
					.Append(")");
				helper.Execute();
			}

			return helper.RowsCopied;
		}

		#endregion
	}
}
