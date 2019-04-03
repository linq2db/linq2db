﻿using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using Common;
	using LinqToDB.Extensions;
	using Mapping;

	class ConvertFromDataReaderExpression : Expression
	{
		public ConvertFromDataReaderExpression(
			Type type, int idx, Expression dataReaderParam, IDataContext dataContext)
		{
			_type            = type;
			_idx             = idx;
			_dataReaderParam = dataReaderParam;
			_dataContext     = dataContext;
		}

		readonly int          _idx;
		readonly Expression   _dataReaderParam;
		readonly IDataContext _dataContext;
		         Type         _type;

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;

		static readonly MethodInfo _columnReaderGetValueInfo = MemberHelper.MethodOf<ColumnReader>(r => r.GetValue(null));

		public override Expression Reduce()
		{
			var columnReader = new ColumnReader(_dataContext, _dataContext.MappingSchema, _type, _idx);
			return Convert(Call(Constant(columnReader), _columnReaderGetValueInfo, _dataReaderParam), _type);
		}

		static readonly MethodInfo _isDBNullInfo = MemberHelper.MethodOf<IDataReader>(rd => rd.IsDBNull(0));

		public Expression Reduce(IDataReader dataReader)
		{
			return GetColumnReader(_dataContext, _dataContext.MappingSchema, dataReader, _type, _idx, _dataReaderParam);
		}

		static Expression GetColumnReader(
			IDataContext dataContext, MappingSchema mappingSchema, IDataReader dataReader, Type type, int idx, Expression dataReaderExpr)
		{
			var toType = type.ToNullableUnderlying();

			var ex = dataContext.GetReaderExpression(mappingSchema, dataReader, idx, dataReaderExpr, toType);

			if (ex.NodeType == ExpressionType.Lambda)
			{
				var l = (LambdaExpression)ex;

				switch (l.Parameters.Count)
				{
					case 1 : ex = l.GetBody(dataReaderExpr);                break;
					case 2 : ex = l.GetBody(dataReaderExpr, Constant(idx)); break;
				}
			}

			if (toType.IsEnumEx())
			{
				var mapType = ConvertBuilder.GetDefaultMappingFromEnumType(mappingSchema, toType);

				if (mapType != ex.Type)
				{
					// Use only defined convert
					var econv = mappingSchema.GetConvertExpression(ex.Type, type,    false, false) ??
						        mappingSchema.GetConvertExpression(ex.Type, mapType, false);

					if (econv.Body.GetCount(e => e == econv.Parameters[0]) > 1)
					{
						var variable = Variable(ex.Type);
						var assign   = Assign(variable, ex);

						ex = Block(new[] { variable }, new[] { assign, econv.GetBody(variable) });
					}
					else
					{
						ex = econv.GetBody(ex);
					}
				}
			}

			var conv = mappingSchema.GetConvertExpression(ex.Type, type, false);

			// Replace multiple parameters with single variable or single parameter with the reader expression.
			//
			if (conv.Body.GetCount(e => e == conv.Parameters[0]) > 1)
			{
				var variable = Variable(ex.Type);
				var assign   = Assign(variable, ex);

				ex = Block(new[] { variable }, new[] { assign, conv.GetBody(variable) });
			}
			else
			{
				ex = conv.GetBody(ex);
			}

			// Add check null expression.
			// Note: Oracle may return wrong IsDBNullAllowed, so added additional check toType != type, that means nullable type
			//
			if (toType != type || (dataContext.IsDBNullAllowed(dataReader, idx) ?? true))
			{
				ex = Condition(
					Call(dataReaderExpr, _isDBNullInfo, Constant(idx)),
					Constant(mappingSchema.GetDefaultValue(type), type),
					ex);
			}

			return ex;
		}

		class ColumnReader
		{
			public ColumnReader(IDataContext dataContext, MappingSchema mappingSchema, Type columnType, int columnIndex)
			{
				_dataContext  = dataContext;
				_mappingSchema = mappingSchema;
				_columnType    = columnType;
				_columnIndex   = columnIndex;
				_defaultValue  = mappingSchema.GetDefaultValue(columnType);
			}

			public object GetValue(IDataReader dataReader)
			{
				//var value = dataReader.GetValue(_columnIndex);

				if (dataReader.IsDBNull(_columnIndex))
					return _defaultValue;

				var fromType = dataReader.GetFieldType(_columnIndex);

				if (!_columnConverters.TryGetValue(fromType, out var func))
				{
					var parameter      = Parameter(typeof(IDataReader));
					var dataReaderExpr = Convert(parameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext, _mappingSchema, dataReader, _columnType, _columnIndex, dataReaderExpr);

					var lex  = Lambda<Func<IDataReader, object>>(
						expr.Type == typeof(object) ? expr : Convert(expr, typeof(object)),
						parameter);

					_columnConverters[fromType] = func = lex.Compile();
				}

				try
				{
					return func(dataReader);
				}
				catch (LinqToDBConvertException ex)
				{
					ex.ColumnName = dataReader.GetName(_columnIndex);
					throw;
				}
				catch (Exception ex)
				{
					var name = dataReader.GetName(_columnIndex);
					throw new LinqToDBConvertException(
							$"Mapping of column {name} value failed, see inner exception for details", ex)
					{
						ColumnName = name
					};
				}

				/*
				var value = dataReader.GetValue(_columnIndex);

				if (value is DBNull || value == null)
					return _defaultValue;

				var fromType = value.GetType();

				if (fromType == _columnType)
					return value;

				Func<object,object> func;

				if (!_columnConverters.TryGetValue(fromType, out func))
				{
					var conv = _mappingSchema.GetConvertExpression(fromType, _columnType, false);
					var pex  = Expression.Parameter(typeof(object));
					var ex   = ReplaceParameter(conv, Expression.Convert(pex, fromType));
					var lex  = Expression.Lambda<Func<object, object>>(
						ex.Type == typeof(object) ? ex : Expression.Convert(ex, typeof(object)),
						pex);

					_columnConverters[fromType] = func = lex.Compile();
				}

				return func(value);
				*/
			}

			readonly ConcurrentDictionary<Type,Func<IDataReader,object>> _columnConverters = new ConcurrentDictionary<Type,Func<IDataReader,object>>();

			readonly IDataContext  _dataContext;
			readonly MappingSchema _mappingSchema;
			readonly Type          _columnType;
			readonly int           _columnIndex;
			readonly object        _defaultValue;
		}

		public override string ToString()
		{
			return $"ConvertFromDataReaderExpression<{_type.Name}>({_idx})";
		}

		public ConvertFromDataReaderExpression MakeNullable()
		{
			if (Type.IsValueTypeEx())
			{
				var type = typeof(Nullable<>).MakeGenericType(Type);
				return new ConvertFromDataReaderExpression(type, _idx, _dataReaderParam, _dataContext);
			}

			return this;
		}

	}
}
