using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Expressions
{
	using Linq;
	using Mapping;

	class ConvertFromDataReaderExpression : Expression
	{
		public ConvertFromDataReaderExpression(
			Type type, int idx, MethodInfo checkNullFunction, Expression context, Expression dataReaderParam, MappingSchemaOld mappingSchema)
		{
			_type              = type;
			_idx               = idx;
			_checkNullFunction = checkNullFunction;
			_context           = context;
			_dataReaderParam   = dataReaderParam;
			_mappingSchema     = mappingSchema;
		}

		readonly int              _idx;
		readonly MethodInfo       _checkNullFunction;
		readonly Expression       _context;
		readonly Expression       _dataReaderParam;
		readonly MappingSchemaOld _mappingSchema;
		readonly Type             _type;

		public override Type           Type      { get { return _type;                    } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }
		public override bool           CanReduce { get { return true;                     } }

		public override Expression Reduce()
		{
			var expr = Call(_dataReaderParam, ReflectionHelper.DataReader.GetValue, Constant(_idx));

			if (_checkNullFunction != null)
				expr = Call(null, _checkNullFunction, expr, _context);

			Expression mapper;

			if (_type.IsEnum)
			{
				mapper =
					Convert(
						Call(
							Constant(_mappingSchema),
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
								Constant(_mappingSchema),
								ReflectionHelper.MapSchema.ChangeType,
									expr,
									Constant(_type)),
							_type);
				}
				else
				{
					mapper = Call(Constant(_mappingSchema), mi, expr);
				}
			}

			return mapper;
		}
	}
}
