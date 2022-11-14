using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class SqlReaderIsNullExpression : Expression
	{
		public SqlPlaceholderExpression Placeholder { get; }
		public bool                     IsNot       { get; }

		public SqlReaderIsNullExpression(SqlPlaceholderExpression placeholder, bool isNot)
		{
			Placeholder = placeholder;
			IsNot  = isNot;
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => typeof(bool);

		public SqlReaderIsNullExpression Update(SqlPlaceholderExpression placeholder)
		{
			if (ReferenceEquals(placeholder, Placeholder))
				return this;

			return new SqlReaderIsNullExpression(placeholder, IsNot);
		}

		public override string ToString()
		{
			return IsNot ? $"IsNotDbNull({Placeholder})" : $"IsDbNull({Placeholder})";
		}
	}
}
