using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
	public sealed class DefaultValueExpression : Expression
	{
		public DefaultValueExpression(MappingSchema? mappingSchema, Type type, bool isNull = false)
		{
			MappingSchema = mappingSchema;
			_type         = type;
			IsNull        = isNull;
		}

		public          MappingSchema? MappingSchema { get; }
		readonly        Type           _type;

		public bool IsNull { get; }

		public override Type           Type      => _type;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			return Constant(
				MappingSchema == null ?
					DefaultValue.GetValue(Type) :
					MappingSchema.GetDefaultValue(Type),
				Type);
		}

		public override string ToString()
		{
			return $"Default({Type.Name})";
		}

		bool Equals(DefaultValueExpression other)
		{
			return Equals(MappingSchema, other.MappingSchema) && _type.Equals(other._type);
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
			return HashCode.Combine(MappingSchema, _type);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitDefaultValueExpression(this);
			return base.Accept(visitor);
		}

	}
}
