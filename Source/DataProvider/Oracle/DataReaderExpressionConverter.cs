using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Extensions;

namespace LinqToDB.DataProvider.Oracle
{
	/// <summary>
	///     Converter class to convert returned Sql Records to strongly typed classes
	/// </summary>
	/// <typeparam name="T">Type of the object we'll convert too</typeparam>
	internal class DataReaderExpressionConverter<T> where T : new()
	{
		readonly Func<IDataReader, T> _converter;
		readonly IDataReader          _dataReader;

		internal DataReaderExpressionConverter(IDataReader dataReader)
		{
			_dataReader = dataReader;
			_converter  = GetMapFunc();
		}

		private Func<IDataReader, T> GetMapFunc()
		{
			var exps = new List<Expression>();

			var paramExp  = Expression.Parameter(typeof(IDataRecord), "o7thDR");
			var targetExp = Expression.Variable(typeof(T));

			exps.Add(Expression.Assign(targetExp, Expression.New(targetExp.Type)));

			//does int based lookup
			var indexerInfo = typeof(IDataRecord).GetPropertyEx("Item", typeof(int));
			var columnNames = Enumerable.Range(0, _dataReader.FieldCount).Select(i => new { i, name = _dataReader.GetName(i) });

			foreach (var column in columnNames)
			{
				var property = targetExp.Type.GetPropertyEx(column.name);
				if (property == null)
					continue;

				var columnNameExp = Expression.Constant(column.i);
				var propertyExp   = Expression.MakeIndex(paramExp, indexerInfo, new[] { columnNameExp });
				var convertExp    = Expression.Convert(propertyExp, property.PropertyType);
				var bindExp       = Expression.Assign(Expression.Property(targetExp, property), convertExp);

				exps.Add(bindExp);
			}

			exps.Add(targetExp);

			return Expression.Lambda<Func<IDataReader, T>>(Expression.Block(new[] { targetExp }, exps), paramExp).Compile();
		}

		internal List<T> CreateItemsFromRows()
		{
			var res = new List<T>();
			while (_dataReader.Read())
				res.Add(_converter(_dataReader));
			return res;
		}
	}
}