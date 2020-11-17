using System;
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
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter,
			Expression dataReaderParam)
		{
			_type            = type;
			Converter        = converter;
			_idx             = idx;
			_dataReaderParam = dataReaderParam;
		}

		// slow mode constructor
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter,
			Expression dataReaderParam, IDataContext dataContext)
			: this(type, idx, converter, dataReaderParam)
		{
			_slowModeDataContext = dataContext;
		}

		readonly int              _idx;
		readonly Expression       _dataReaderParam;
		readonly Type             _type;
		readonly IDataContext?    _slowModeDataContext;
		
		public IValueConverter?   Converter { get; }

		public override Type           Type        => _type;
		public override ExpressionType NodeType    => ExpressionType.Extension;
		public override bool           CanReduce   => true;
		public          int            Index       => _idx;

		static readonly MethodInfo _columnReaderGetValueInfo = MemberHelper.MethodOf<ColumnReader>(r => r.GetValue(null!));

		public override Expression Reduce()
		{
			return Reduce(_slowModeDataContext!, true);
		}

		public Expression Reduce(IDataContext dataContext, bool slowMode)
		{
			var columnReader = new ColumnReader(dataContext, dataContext.MappingSchema, _type, _idx, Converter, slowMode);
			return Convert(Call(Constant(columnReader), _columnReaderGetValueInfo, _dataReaderParam), _type);
		}

		static readonly MethodInfo _isDBNullInfo = MemberHelper.MethodOf<IDataReader>(rd => rd.IsDBNull(0));

		public Expression Reduce(IDataContext dataContext, IDataReader dataReader)
		{
			return GetColumnReader(dataContext, dataContext.MappingSchema, dataReader, _type, Converter, _idx, _dataReaderParam, false);
		}

		public Expression Reduce(IDataContext dataContext, IDataReader dataReader, Expression dataReaderParam)
		{
			return GetColumnReader(dataContext, dataContext.MappingSchema, dataReader, _type, Converter, _idx, dataReaderParam, false);
		}

		static Expression ConvertExpressionToType(Expression current, Type toType, MappingSchema mappingSchema)
		{
			var toConvertExpression = mappingSchema.GetConvertExpression(current.Type, toType, false, current.Type != toType)!;

			if (toConvertExpression == null)
				return current;

			current = InternalExtensions.ApplyLambdaToExpression(toConvertExpression, current);

			return current;
		}

		static Expression GetColumnReader(
			IDataContext dataContext, MappingSchema mappingSchema, IDataReader dataReader, Type type, IValueConverter? converter, int idx, Expression dataReaderExpr, bool forceNullCheck)
		{
			var toType = type.ToNullableUnderlying();

			Expression ex;
			if (converter != null)
			{
				var expectedProvType = converter.FromProviderExpression.Parameters[0].Type;
				ex = dataContext.GetReaderExpression(dataReader, idx, dataReaderExpr, expectedProvType);
			}
			else
			{ 
				ex = dataContext.GetReaderExpression(dataReader, idx, dataReaderExpr, toType);
			}

			if (ex.NodeType == ExpressionType.Lambda)
			{
				var l = (LambdaExpression)ex;

				switch (l.Parameters.Count)
				{
					case 1 : ex = l.GetBody(dataReaderExpr);                break;
					case 2 : ex = l.GetBody(dataReaderExpr, Constant(idx)); break;
				}
			}

			if (converter != null)
			{
				// we have to prepare read expression to conversion
				//
				var expectedType = converter.FromProviderExpression.Parameters[0].Type;
				
				if (converter.HandlesNulls)
				{
					ex = Condition(
						Call(dataReaderExpr, _isDBNullInfo, Constant(idx)),
						Constant(mappingSchema.GetDefaultValue(expectedType), expectedType),
						ex);
				}

				if (expectedType != ex.Type)
				{
					ex = ConvertExpressionToType(ex, expectedType, mappingSchema);
				}

				ex = InternalExtensions.ApplyLambdaToExpression(converter.FromProviderExpression, ex);
				if (toType != ex.Type && toType.IsAssignableFrom(ex.Type))
				{
					ex = Expression.Convert(ex, toType);
				}
					
			}
			else if (toType.IsEnum)
			{
				var mapType = ConvertBuilder.GetDefaultMappingFromEnumType(mappingSchema, toType)!;

				if (mapType != ex.Type)
				{
					// Use only defined convert
					var econv = mappingSchema.GetConvertExpression(ex.Type, type,    false, false) ??
					            mappingSchema.GetConvertExpression(ex.Type, mapType, false)!;

					ex = InternalExtensions.ApplyLambdaToExpression(econv, ex);
				}
			}

			if (ex.Type != type)
				ex = ConvertExpressionToType(ex, type, mappingSchema)!;

			// Try to search postprocessing converter TType -> TType
			//
			ex = ConvertExpressionToType(ex, ex.Type, mappingSchema)!;

			// Add check null expression.
			// If converter handles nulls, do not provide IsNull check
			// Note: some providers may return wrong IsDBNullAllowed, so we enforce null check in slow mode. E.g.:
			// Microsoft.Data.SQLite
			// Oracle (group by columns)
			// MySql.Data and some other providers enforce null check in IsDBNullAllowed implementation
			if (converter?.HandlesNulls != true &&
			    (forceNullCheck || (dataContext.IsDBNullAllowed(dataReader, idx) ?? true)))
			{
				ex = Condition(
					Call(dataReaderExpr, _isDBNullInfo, Constant(idx)),
					Constant(mappingSchema.GetDefaultValue(type), type),
					ex);
			}

			return ex;
		}

		internal class ColumnReader
		{
			public ColumnReader(IDataContext dataContext, MappingSchema mappingSchema, Type columnType, int columnIndex, IValueConverter? converter, bool slowMode)
			{
				_dataContext   = dataContext;
				_mappingSchema = mappingSchema;
				_columnType    = columnType;
				_columnIndex   = columnIndex;
				_converter     = converter;
				_slowMode      = slowMode;
			}

			public object? GetValue(IDataReader dataReader)
			{
				var fromType = dataReader.GetFieldType(_columnIndex);

				if (!_columnConverters.TryGetValue(fromType, out var func))
				{
					var parameter      = Parameter(typeof(IDataReader));
					var dataReaderExpr = Convert(parameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext, _mappingSchema, dataReader, _columnType, _converter, _columnIndex, dataReaderExpr, _slowMode);

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
			}

			readonly ConcurrentDictionary<Type,Func<IDataReader,object>> _columnConverters = new ConcurrentDictionary<Type,Func<IDataReader,object>>();

			readonly IDataContext     _dataContext;
			readonly MappingSchema    _mappingSchema;
			readonly Type             _columnType;
			readonly int              _columnIndex;
			readonly IValueConverter? _converter;
			readonly bool             _slowMode;
		}

		public override string ToString()
		{
			return $"ConvertFromDataReaderExpression<{_type.Name}>({_idx})";
		}

		public ConvertFromDataReaderExpression MakeNullable()
		{
			if (Type.IsValueType && !Type.IsNullable())
			{
				var type = typeof(Nullable<>).MakeGenericType(Type);
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam);
			}

			return this;
		}

		public ConvertFromDataReaderExpression MakeNotNullable()
		{
			if (typeof(Nullable<>).IsSameOrParentOf(Type))
			{
				var type = Type.GetGenericArguments()[0];
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam);
			}

			return this;
		}

	}
}
