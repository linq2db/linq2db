using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Infrastructure;
using LinqToDB.Interceptors;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using LinqToDB.Reflection;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Expressions
{
	sealed class ConvertFromDataReaderExpression : Expression
	{
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter, Expression dataReaderParam, bool? canBeNull)
		{
			_type            = type;
			Converter        = converter;
			CanBeNull        = canBeNull;
			_idx             = idx;
			_dataReaderParam = dataReaderParam;
		}

		// slow mode constructor
		public ConvertFromDataReaderExpression(Type type, int idx, IValueConverter? converter, Expression dataReaderParam, IDataContext dataContext)
			: this(type, idx, converter, dataReaderParam, (bool?)null)
		{
			_slowModeDataContext = dataContext;
		}

		readonly int            _idx;
		readonly Expression     _dataReaderParam;
		readonly Type           _type;
		readonly IDataContext?  _slowModeDataContext;

		public IValueConverter? Converter { get; }
		public bool?            CanBeNull { get; }

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;
		public          int            Index     => _idx;

		public override Expression Reduce()
		{
			return Reduce(_slowModeDataContext, true);
		}

		public Expression Reduce(IDataContext? dataContext, bool slowMode)
		{
			if (dataContext == null)
				return _dataReaderParam;

			var columnReader = new ColumnReader(dataContext, dataContext.MappingSchema, _type, _idx, Converter, slowMode);

			if (slowMode && LinqToDB.Common.Configuration.OptimizeForSequentialAccess)
				return Convert(Call(Constant(columnReader), Methods.LinqToDB.ColumnReader.GetValueSequential, _dataReaderParam, Call(_dataReaderParam, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(_idx)), Call(Methods.LinqToDB.ColumnReader.RawValuePlaceholder)), _type);
			else
				return Convert(Call(Constant(columnReader), Methods.LinqToDB.ColumnReader.GetValue, _dataReaderParam), _type);
		}

		public Expression Reduce(IDataContext dataContext, DbDataReader dataReader)
		{
			if (dataContext is IInterceptable<IUnwrapDataObjectInterceptor> { Interceptor: { } interceptor })
				using (ActivityService.Start(ActivityID.UnwrapDataObjectInterceptorUnwrapDataReader))
					dataReader = interceptor.UnwrapDataReader(dataContext, dataReader);

			return GetColumnReader(dataContext, dataContext.MappingSchema, dataReader, _type, Converter, _idx, _dataReaderParam, forceNullCheck: CanBeNull == true);
		}

		public Expression Reduce(IDataContext dataContext, DbDataReader dataReader, Expression dataReaderParam)
		{
			return GetColumnReader(dataContext, dataContext.MappingSchema, dataReader, _type, Converter, _idx, dataReaderParam, forceNullCheck: CanBeNull == true);
		}

		static Expression ConvertExpressionToType(Expression current, Type toType, MappingSchema mappingSchema)
		{
			var toConvertExpression = mappingSchema.GetConvertExpression(current.Type, toType, false, current.Type != toType, ConversionType.FromDatabase);

			if (toConvertExpression == null)
				return current;

			current = InternalExtensions.ApplyLambdaToExpression(toConvertExpression, current);

			return current;
		}

		static Expression GetColumnReader(
			IDataContext dataContext, MappingSchema mappingSchema, DbDataReader dataReader, Type type, IValueConverter? converter, int idx, Expression dataReaderExpr, bool forceNullCheck)
		{
			var toType = type.ToNullableUnderlying();

			Expression ex;
			Type? mapType = null;

			if (toType.IsEnum)
				mapType = ConvertBuilder.GetDefaultMappingFromEnumType(mappingSchema, toType);

			if (converter != null)
			{
				var expectedProvType = converter.FromProviderExpression.Parameters[0].Type;
				ex = dataContext.GetReaderExpression(dataReader, idx, dataReaderExpr, expectedProvType);
			}
			else
			{
				ex = dataContext.GetReaderExpression(dataReader, idx, dataReaderExpr, mapType?.ToNullableUnderlying() ?? toType);
			}

			if (ex.NodeType == ExpressionType.Lambda)
			{
				var l = (LambdaExpression)ex;

				switch (l.Parameters.Count)
				{
					case 1 : ex = l.GetBody(dataReaderExpr);                                 break;
					case 2 : ex = l.GetBody(dataReaderExpr, ExpressionInstances.Int32(idx)); break;
				}
			}

			if (converter != null)
			{
				// we have to prepare read expression to conversion
				//
				var expectedType = converter.FromProviderExpression.Parameters[0].Type;

				if (expectedType != ex.Type)
					ex = ConvertExpressionToType(ex, expectedType, mappingSchema);

				if (converter.HandlesNulls)
				{
					ex = Condition(
						Call(dataReaderExpr, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(idx)),
						Constant(mappingSchema.GetDefaultValue(expectedType), expectedType),
						ex);
				}

				ex = InternalExtensions.ApplyLambdaToExpression(converter.FromProviderExpression, ex);
				if (toType != ex.Type && toType.IsAssignableFrom(ex.Type))
				{
					ex = Convert(ex, toType);
				}
			}
			else if (toType.IsEnum)
			{
				if (mapType != ex.Type)
				{
					// Use only defined convert
					var econv =
						mappingSchema.GetConvertExpression(ex.Type, type,     false, false, ConversionType.FromDatabase) ??
						mappingSchema.GetConvertExpression(ex.Type, mapType!, false, true,  ConversionType.ToDatabase)!;

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
			if (converter?.HandlesNulls != true && (forceNullCheck || (dataContext.IsDBNullAllowed(dataReader, idx) ?? true)))
			{
				ex = Condition(
					Call(dataReaderExpr, Methods.ADONet.IsDBNull, ExpressionInstances.Int32Array(idx)),
					Constant(mappingSchema.GetDefaultValue(type), type),
					ex);
			}

			return ex;
		}

		internal sealed class ColumnReader
		{
			public ColumnReader(IDataContext dataContext, MappingSchema mappingSchema, Type columnType, int columnIndex, IValueConverter? converter, bool slowMode)
			{
				_dataContext   = dataContext;
				_mappingSchema = mappingSchema;
				ColumnType     = columnType;
				ColumnIndex    = columnIndex;
				_converter     = converter;
				_slowMode      = slowMode;
			}

			/// <summary>
			/// This method is used as placeholder, which will be replaced with raw value variable.
			/// </summary>
			/// <returns></returns>
			public static object? RawValuePlaceholder() => throw new InvalidOperationException("Raw value placeholder replacement failed");

			/*
			 * We could have column readers for same column with different ColumnType types  which results in different
			 * reader expressions.
			 * To make it work with sequential mode we should perform actual column value read from reader only
			 * once and then use it in reader expressions for all types.
			 * For that we add additional method to read raw value and then pass it to GetValueSequential.
			 * We need extra method as we cannot store raw value in field: ColumnReader instance could be used
			 * from multiple threads, so it cannot have state. For same reason it doesn't make much sense to reduce number
			 * of ColumnReader instances in mapper expression to one for single column. It could be done later if we will
			 * see benefits of it, but frankly speaking it doesn't make sense to optimize slow-mode reader.
			 *
			 * Limitation is the same as for non-slow mapper:
			 * column mapping expressions should use same reader method to get column value. This limitation enforced
			 * in GetRawValueSequential method.
			 */
			public object? GetValueSequential(DbDataReader dataReader, bool isNull, object? rawValue)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_slowColumnConverters.TryGetValue(fromType, out var func))
				{
					var dataReaderParameter = Parameter(typeof(DbDataReader));
					var isNullParameter     = Parameter(typeof(bool));
					var rawValueParameter   = Parameter(typeof(object));
					var dataReaderExpr      = Convert(dataReaderParameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext, _mappingSchema, dataReader, ColumnType, _converter, ColumnIndex, dataReaderExpr, _slowMode);
					expr     = SequentialAccessHelper.OptimizeColumnReaderForSequentialAccess(expr, isNullParameter, rawValueParameter, ColumnIndex);

					var lex  = Lambda<Func<bool, object?, object?>>(
						expr.Type == typeof(object) ? expr : Convert(expr, typeof(object)),
						isNullParameter,
						rawValueParameter);

					_slowColumnConverters[fromType] = func = lex.CompileExpression();
				}

				try
				{
					return func(isNull, rawValue);
				}
				catch (LinqToDBConvertException ex)
				{
					ex.ColumnName = dataReader.GetName(ColumnIndex);
					throw;
				}
				catch (Exception ex)
				{
					var name = dataReader.GetName(ColumnIndex);
					throw new LinqToDBConvertException(
							$"Mapping of column '{name}' value failed, see inner exception for details", ex)
					{
						ColumnName = name
					};
				}
			}

			public object GetRawValueSequential(DbDataReader dataReader, Type[] forTypes)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_slowRawReaders.TryGetValue(fromType, out var func))
				{
					var dataReaderParameter = Parameter(typeof(DbDataReader));
					var dataReaderExpr      = Convert(dataReaderParameter, dataReader.GetType());

					MethodCallExpression rawExpr = null!;
					foreach (var type in forTypes)
					{
						var expr           = GetColumnReader(_dataContext, _mappingSchema, dataReader, type, _converter, ColumnIndex, dataReaderExpr, _slowMode);
						var currentRawExpr = SequentialAccessHelper.ExtractRawValueReader(expr, ColumnIndex);

						if (rawExpr == null)
							rawExpr = currentRawExpr;
						else if (rawExpr.Method != currentRawExpr.Method)
							throw new LinqToDBConvertException(
								$"Different data reader methods used for same column: '{rawExpr.Method.DeclaringType?.Name}.{rawExpr.Method.Name}' vs '{currentRawExpr.Method.DeclaringType?.Name}.{currentRawExpr.Method.Name}'");

					}

					var lex  = Lambda<Func<DbDataReader, object>>(
						rawExpr.Type == typeof(object) ? rawExpr : Convert(rawExpr, typeof(object)),
						dataReaderParameter);

					_slowRawReaders[fromType] = func = lex.CompileExpression();
				}

				return func(dataReader);
			}

			public object? GetValue(DbDataReader dataReader)
			{
				var fromType = dataReader.GetFieldType(ColumnIndex);

				if (!_columnConverters.TryGetValue(fromType, out var func))
				{
					var parameter      = Parameter(typeof(DbDataReader));
					var dataReaderExpr = Convert(parameter, dataReader.GetType());

					var expr = GetColumnReader(_dataContext, _mappingSchema, dataReader, ColumnType, _converter, ColumnIndex, dataReaderExpr, _slowMode);

					var lex  = Lambda<Func<DbDataReader, object>>(
						expr.Type == typeof(object) ? expr : Convert(expr, typeof(object)),
						parameter);

					_columnConverters[fromType] = func = lex.CompileExpression();
				}

				try
				{
					return func(dataReader);
				}
				catch (LinqToDBConvertException ex)
				{
					ex.ColumnName = dataReader.GetName(ColumnIndex);
					throw;
				}
				catch (Exception ex)
				{
					var name = dataReader.GetName(ColumnIndex);
					throw new LinqToDBConvertException(
							$"Mapping of column '{name}' value failed, see inner exception for details", ex)
					{
						ColumnName = name
					};
				}
			}

			readonly ConcurrentDictionary<Type, Func<DbDataReader, object?>>  _columnConverters     = new ();
			readonly ConcurrentDictionary<Type, Func<bool, object?, object?>> _slowColumnConverters = new ();
			readonly ConcurrentDictionary<Type, Func<DbDataReader, object>>   _slowRawReaders       = new ();

			readonly IDataContext     _dataContext;
			readonly MappingSchema    _mappingSchema;
			readonly IValueConverter? _converter;
			readonly bool             _slowMode;

			public int  ColumnIndex { get; }
			public Type ColumnType  { get; }
		}

		public override string ToString()
		{
			var result = $"ConvertFromDataReaderExpression[{_type.ShortDisplayName()}]({_idx})";
			if (CanBeNull == true || Type.IsNullable())
				result += "?";
			return result;
		}

		public ConvertFromDataReaderExpression MakeNullable()
		{
			if (!Type.IsNullableType())
			{
				var type = Type.AsNullable();
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam, true);
			}

			return this;
		}

		public ConvertFromDataReaderExpression MakeNotNullable()
		{
			if (Type.IsNullable())
			{
				var type = Type.GetGenericArguments()[0];
				return new ConvertFromDataReaderExpression(type, _idx, Converter, _dataReaderParam, (bool?)null);
			}

			return this;
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitConvertFromDataReaderExpression(this);
			return base.Accept(visitor);
		}

	}
}
