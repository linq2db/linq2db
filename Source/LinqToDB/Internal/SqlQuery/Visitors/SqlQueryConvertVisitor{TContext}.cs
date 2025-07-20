using System;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	public sealed class SqlQueryConvertVisitor<TContext> : SqlQueryConvertVisitorBase
	{
		Func<SqlQueryConvertVisitor<TContext>, IQueryElement, IQueryElement> _convertFunc = default!;

		public TContext Context { get; private set; } = default!;

		public SqlQueryConvertVisitor(bool allowMutation) : base(allowMutation, null)
		{
		}

		public ISqlExpression? ColumnExpression { get; private set; }

		public IQueryElement Convert(IQueryElement element, TContext context, Func<SqlQueryConvertVisitor<TContext>, IQueryElement, IQueryElement> convertFunc, bool withStack)
		{
			Context      = context;
			_convertFunc = convertFunc;
			WithStack    = withStack;

			Stack?.Clear();

			return PerformConvert(element);
		}

		public override void Cleanup()
		{
			base.Cleanup();

			_convertFunc = null!;
			Context      = default!;
			WithStack    = false;
			Stack?.Clear();
		}

		public override IQueryElement ConvertElement(IQueryElement element)
		{
			var newElement = _convertFunc(this, element);

			return newElement;
		}

		protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
		{
			ColumnExpression = expression;
			var newExpression = base.VisitSqlColumnExpression(column, expression);
			ColumnExpression = null;

			return newExpression;
		}
	}
}
