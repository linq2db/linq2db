using System;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlPlaceholderExpression : Expression
	{
		public SqlPlaceholderExpression(ContextRefExpression refExpression, MemberExpression memberExpression, Type? convertType = null)
		{
			RefExpression    = refExpression;
			MemberExpression = memberExpression;
			ConvertType      = convertType ?? MemberExpression.Type;
		}

		public ContextRefExpression RefExpression    { get; }
		public IBuildContext        BuildContext     => RefExpression.BuildContext;
		public MemberExpression     MemberExpression { get; }
		public Type                 ConvertType      { get; }

		internal SqlInfo? Sql             { get; set; }
		internal SqlInfo? ColumnSql       { get; set; }
		public   int?     ProjectionIndex { get; set; }

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => ConvertType;
	}

}
