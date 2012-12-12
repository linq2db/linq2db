using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using LinqToDB.DataProvider;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using System.Linq;

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
				var dataReaderVar  = Expression.Variable(dataReaderExpr.Type, "dr");
				var assignment     = Expression.Assign(dataReaderVar, dataReaderExpr);

				Expression expr;

				Func<Type,int,Expression> getMemberExpression = (type,idx) =>
				{
					var ex = dataProvider.GetReaderExpression(dataReader, idx, dataReaderVar, type);

					if (ex.NodeType == ExpressionType.Lambda)
					{
						var l = (LambdaExpression)ex;
						ex = l.Body.Transform(e => e == l.Parameters[0] ? dataReaderVar : e);
					}

					return ex;
				};

				if (dataConnection.MappingSchema.IsScalarType(typeof(T)))
				{
					expr = getMemberExpression(typeof(T), 0);
				}
				else
				{
					var td    = new TypeDescriptor(dataConnection.MappingSchema, typeof(T));
					var names = new List<string>(dataReader.FieldCount);

					for (var i = 0; i < dataReader.FieldCount; i++)
						names.Add(dataReader.GetName(i));

					var members =
					(
						from n in names.Select((name,idx) => new { name, idx })
						let   member = td.Members.FirstOrDefault(m => m.ColumnName == n.name)
						where member != null
						select new
						{
							Member = member,
							Expr   = getMemberExpression(member.MemberType, n.idx),
						}
					).ToList();

					expr = Expression.MemberInit(
						Expression.New(typeof(T)),
						members.Select(m => Expression.Bind(m.Member.MemberInfo, m.Expr)));
				}

				expr = Expression.Block(new[] { dataReaderVar }, new[] { assignment, expr });

				var lex = Expression.Lambda<Func<IDataReader,T>>(expr, parameter);

				func = lex.Compile();
			}

			return (Func<IDataReader,T>)func;
		}
	}
}
