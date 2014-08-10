using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Text;

using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;

namespace LinqToDB.DataProvider
{
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
				dataConnection.Insert(item);
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

		protected static string GetTableName(ISqlBuilder sqlBuilder, EntityDescriptor descriptor)
		{
			return sqlBuilder.BuildTableName(
				new StringBuilder(),
				descriptor.DatabaseName == null ? null : sqlBuilder.Convert(descriptor.DatabaseName, ConvertType.NameToDatabase).  ToString(),
				descriptor.SchemaName   == null ? null : sqlBuilder.Convert(descriptor.SchemaName,   ConvertType.NameToOwner).     ToString(),
				descriptor.TableName    == null ? null : sqlBuilder.Convert(descriptor.TableName,    ConvertType.NameToQueryTable).ToString())
			.ToString();
		}

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
	}
}
