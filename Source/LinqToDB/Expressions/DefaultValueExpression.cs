using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Common;
	using Mapping;

	public class DefaultValueExpression : Expression
	{
		public DefaultValueExpression(MappingSchema? mappingSchema, Type type)
		{
			_mappingSchema = mappingSchema;
			_type          = type;
		}

		readonly MappingSchema? _mappingSchema;
		readonly Type           _type;

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			return Constant(
				_mappingSchema == null ?
					DefaultValue.GetValue(Type) :
					_mappingSchema.GetDefaultValue(Type),
				Type);
		}

		public override string ToString()
		{
			return $"Default({Type.Name})";
		}

		protected bool Equals(DefaultValueExpression other)
		{
			return Equals(_mappingSchema, other._mappingSchema) && _type.Equals(other._type);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((DefaultValueExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = _mappingSchema?.GetHashCode() ?? 0;
				hashCode = (hashCode * 397) ^ _type.GetHashCode();
				return hashCode;
			}
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitDefaultValueExpression(this);
			return base.Accept(visitor);
		}

	}
}
