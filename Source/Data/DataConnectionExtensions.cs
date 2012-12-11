using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.Expressions;

namespace LinqToDB.Data
{
	public static class DataConnectionExtensions
	{
		public static IEnumerable<T> Query<T>(this DataConnection connection, Func<IDataReader,T> objectReader, string sql)
		{
			connection.Command.CommandText = sql;

			using (var rd = connection.Command.ExecuteReader())
				while (rd.Read())
					yield return objectReader(rd);
		}

		public static IEnumerable<T> Query<T>(this DataConnection connection, string sql)
		{
			connection.Command.CommandText = sql;

			using (var rd = connection.Command.ExecuteReader())
			{
				if (rd.Read())
				{
					var objectReader = GetObjectReader<T>(connection, rd);
					do
						yield return objectReader(rd);
					while (rd.Read());
				}
			}
		}

		static readonly ConcurrentDictionary<object,Delegate> _objectReaders = new ConcurrentDictionary<object,Delegate>();

		static Func<IDataReader,T> GetObjectReader<T>(DataConnection dataConnection, IDataReader dataReader)
		{
			var key = new
			{
				Type   = typeof(T),
				Config = dataConnection.ConfigurationString ?? dataConnection.ConnectionString ?? dataConnection.Connection.ConnectionString,
				Sql    = dataConnection.Command.CommandText,
			};

			Delegate func;

			if (!_objectReaders.TryGetValue(key, out func))
			{
				var dataProvider   = dataConnection.DataProvider;
				var parameter      = Expression.Parameter(typeof(IDataReader));
				var dataReaderExpr = dataProvider.ConvertDataReader(parameter);

				if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
				{
					var ex = dataProvider.GetReaderExpression(dataReader, 0, dataReaderExpr, typeof(T));

					if (ex.NodeType == ExpressionType.Lambda)
					{
						var l = (LambdaExpression)ex;
						ex = l.Body.Transform(e => e == l.Parameters[0] ? dataReaderExpr : e);
					}

					var expr = Expression.Lambda<Func<IDataReader,T>>(ex, parameter);

					func = expr.Compile();
				}
				else
				{
					for (var i = 0; i < dataReader.FieldCount; i++)
					{
						var name = dataReader.GetName(i);
					}
				}
			}

			return (Func<IDataReader,T>)func;
		}
	}
}
