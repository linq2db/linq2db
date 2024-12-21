using System;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;
using LinqToDB.Mapping;

namespace LinqToDB.Expressions
{
	public class SqlQueryRootExpression : Expression, IEquatable<SqlQueryRootExpression>
	{
		public MappingSchema MappingSchema { get; }
		public Type          ContextType   { get; }

		public SqlQueryRootExpression(MappingSchema mappingSchema, Type contextType)
		{
			MappingSchema = mappingSchema;
			ContextType   = contextType;
		}

		public static SqlQueryRootExpression Create(IDataContext dataContext)
		{
			return new SqlQueryRootExpression(dataContext.MappingSchema, dataContext.GetType());
		}

		public static SqlQueryRootExpression Create(IDataContext dataContext, Type contextType)
		{
			return new SqlQueryRootExpression(dataContext.MappingSchema, contextType);
		}

		public static SqlQueryRootExpression Create(MappingSchema mappingSchema, Type contextType)
		{
			return new SqlQueryRootExpression(mappingSchema, contextType);
		}

		public override string ToString()
		{
			return $"Context<{ContextType.Name}>(MS:{((IConfigurationID)MappingSchema).ConfigurationID})";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ContextType;

		public bool Equals(SqlQueryRootExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return ((IConfigurationID)MappingSchema).ConfigurationID ==
			       ((IConfigurationID)other.MappingSchema).ConfigurationID
			       && ContextType == other.ContextType;
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlQueryRootExpression)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (((IConfigurationID)MappingSchema).ConfigurationID.GetHashCode() * 397) ^ ContextType.GetHashCode();
			}
		}

		public static bool operator ==(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlQueryRootExpression? left, SqlQueryRootExpression? right)
		{
			return !Equals(left, right);
		}

		protected override Expression VisitChildren(ExpressionVisitor visitor) => this;

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase expressionVisitorBase)
				return expressionVisitorBase.VisitSqlQueryRootExpression(this);

			return base.Accept(visitor);
		}
	}
}
