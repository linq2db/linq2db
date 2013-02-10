using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;
	using Linq;

	class ConvertFromDataReaderExpression : Expression
	{
		public ConvertFromDataReaderExpression(
			Type type, int idx, MethodInfo checkNullFunction, Expression checkNullParameter, Expression dataReaderParam, IDataContextInfo contextInfo)
		{
			_type               = type;
			_idx                = idx;
			_checkNullFunction  = checkNullFunction;
			_checkNullParameter = checkNullParameter;
			_dataReaderParam    = dataReaderParam;
			_contextInfo        = contextInfo;
		}

		readonly int              _idx;
		readonly MethodInfo       _checkNullFunction;
		readonly Expression       _checkNullParameter;
		readonly Expression       _dataReaderParam;
		readonly IDataContextInfo _contextInfo;
		readonly Type             _type;

		public override Type           Type      { get { return _type;                    } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }
		public override bool           CanReduce { get { return true;                     } }

		public override Expression Reduce()
		{
			var expr = Call(_dataReaderParam, ReflectionHelper.DataReader.GetValue, Constant(_idx));

			if (_checkNullFunction != null)
				expr = Call(null, _checkNullFunction, expr, _checkNullParameter);

			Expression mapper;

			if (_type.IsEnum)
			{
				mapper =
					Convert(
						Call(
							Constant(_contextInfo.MappingSchema),
							ReflectionHelper.MapSchema.MapValueToEnum,
								expr,
								Constant(_type)),
						_type);
			}
			else
			{
				MethodInfo mi;

				if (!ReflectionHelper.MapSchema.Converters.TryGetValue(_type, out mi))
				{
					mapper =
						Convert(
							Call(
								Constant(_contextInfo.MappingSchema),
								ReflectionHelper.MapSchema.ChangeType,
									expr,
									Constant(_type)),
							_type);
				}
				else
				{
					mapper = Call(Constant(_contextInfo.MappingSchema), mi, expr);
				}
			}

			return mapper;
		}

		static readonly MethodInfo _isDBNullInfo = MemberHelper.MethodOf<IDataReader>(rd => rd.IsDBNull(0));

		public Expression Reduce(IDataReader dataReader)
		{
			return Reduce();

			var ex = _contextInfo.DataContext.GetReaderExpression(
				_contextInfo.MappingSchema.NewSchema, dataReader, _idx, _dataReaderParam, _type.ToNullableUnderlying());

			if (ex.NodeType == ExpressionType.Lambda)
			{
				var l = (LambdaExpression)ex;

				if (l.Parameters.Count == 1)
					ex = l.Body.Transform(e => e == l.Parameters[0] ? _dataReaderParam : e);
				else if (l.Parameters.Count == 2)
					ex = l.Body.Transform(e =>
						e == l.Parameters[0] ? _dataReaderParam :
						e == l.Parameters[1] ? Constant(_idx) :
						e);
			}

			var conv = _contextInfo.MappingSchema.NewSchema.GetConvertExpression(ex.Type, _type, false);

			// Replace multiple parameters with single variable or single parameter with the reader expression.
			//
			if (conv.Body.GetCount(e => e == conv.Parameters[0]) > 1)
			{
				var variable = Variable(ex.Type);
				var assign   = Assign(variable, ex);

				return Block(new[] { variable }, new[] { assign, conv.Body.Transform(e => e == conv.Parameters[0] ? variable : e) });
			}

			ex = conv.Body.Transform(e => e == conv.Parameters[0] ? ex : e);

			// Add check null expression.
			//
			if (_contextInfo.DataContext.IsDBNullAllowed(dataReader, _idx) ?? true)
			{
				ex = Condition(
					Call(_dataReaderParam, _isDBNullInfo, Constant(_idx)),
					Constant(_contextInfo.MappingSchema.NewSchema.GetDefaultValue(_type), _type),
					ex);
			}

			return ex;
		}
	}
}
