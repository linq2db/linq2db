using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class SqlReaderIsNullExpression : Expression
	{
		public SqlPlaceholderExpression Placeholder { get; }

		public SqlReaderIsNullExpression(SqlPlaceholderExpression placeholder)
		{
			Placeholder = placeholder;
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => typeof(bool);

		public SqlReaderIsNullExpression Update(SqlPlaceholderExpression placeholder)
		{
			if (ReferenceEquals(placeholder, Placeholder))
				return this;

			return new SqlReaderIsNullExpression(placeholder);
		}

		public override string ToString()
		{
			return $"IsDbNull({Placeholder})";
		}
	}
}
