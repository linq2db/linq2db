using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Common;
	using Mapping;

	public class DefaultValueExpression : Expression
	{
		public DefaultValueExpression(MappingSchema mappingSchema, Type type)
		{
			_mappingSchema = mappingSchema;
			_type          = type;
		}

		readonly MappingSchema _mappingSchema;
		readonly Type          _type;

		public override Type           Type      { get { return _type;                    } }
		public override ExpressionType NodeType  { get { return ExpressionType.Extension; } }
		public override bool           CanReduce { get { return true;                     } }

		public override Expression Reduce()
		{
			return Constant(
				_mappingSchema == null ?
					DefaultValue.GetValue(Type) :
					_mappingSchema.GetDefaultValue(Type),
				Type);
		}
	}
}
