using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	class ExpressionContext : SequenceContextBase
	{
		public ExpressionContext(IBuildContext parent, IBuildContext[] sequences, LambdaExpression lambda)
			: base(parent, sequences, lambda)
		{
		}

		public ExpressionContext(IBuildContext parent, IBuildContext sequence, LambdaExpression lambda)
			: base(parent, sequence, lambda)
		{
		}

		public ExpressionContext(IBuildContext parent, IBuildContext sequence, LambdaExpression lambda, SelectQuery selectQuery)
			: base(parent, sequence, lambda)
		{
			SelectQuery = selectQuery;
		}

		public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
		{
			throw new InvalidOperationException();
		}

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			if (level == 0)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :
						{
							var root = expression.GetRootObject(Builder.MappingSchema);

							if (root.NodeType == ExpressionType.Parameter)
							{
								var ctx = Builder.GetContext(this, root);

								if (ctx != null)
								{
									if (ctx != this)
										return ctx.ConvertToSql(expression, 0, flags);

									for (var i = 0; i < Lambda.Parameters.Count; i++)
									{
										if (ReferenceEquals(root, Lambda.Parameters[i]))
											return root == expression ?
												Sequences[i].ConvertToSql(null,       0,         flags) :
												Sequences[i].ConvertToSql(expression, level + 1, flags);
									}

									return root == expression ?
										Sequence.ConvertToSql(null,       0,         flags) :
										Sequence.ConvertToSql(expression, level + 1, flags);
								}
							}

							break;
						}
				}

				throw new LinqException("'{0}' cannot be converted to SQL.", expression);
			}

			throw new InvalidOperationException();
		}

		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			throw new InvalidOperationException();
		}

		public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
		{
			switch (requestFlag)
			{
				case RequestFor.Root        :
					return new IsExpressionResult(Lambda.Parameters.Count == 1 ?
						ReferenceEquals(expression, Lambda.Parameters[0]) :
						Lambda.Parameters.Any(p => ReferenceEquals(expression, p)));

				case RequestFor.Table       :
				case RequestFor.Association :
				case RequestFor.Object      :
				case RequestFor.GroupJoin   :
				case RequestFor.Field       :
				case RequestFor.Expression  :
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

						if (Lambda.Parameters.Count > 1)
						{
							for (var i = 0; i < Lambda.Parameters.Count; i++)
							{
								var root = expression.GetRootObject(Builder.MappingSchema);

								if (ReferenceEquals(root, Lambda.Parameters[i]))
								{
									return ReferenceEquals(levelExpression, expression) ?
										Sequences[i].IsExpression(null,       0,         requestFlag) :
										Sequences[i].IsExpression(expression, level + 1, requestFlag);
								}
							}
						}

						return ReferenceEquals(levelExpression, expression) ?
							Sequence.IsExpression(null,       0,         requestFlag) :
							Sequence.IsExpression(expression, level + 1, requestFlag);
					}
			}

			return IsExpressionResult.False;
		}

		public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
		{
			for (var i = 0; i < Lambda.Parameters.Count; i++)
				if (ReferenceEquals(expression, Lambda.Parameters[i]))
					return Sequences[i].GetContext(null, 0, buildInfo);

			switch (expression.NodeType)
			{
				case ExpressionType.Constant   :
				case ExpressionType.New        :
				case ExpressionType.MemberInit : return null;
			}

			return Sequence.GetContext(expression, level + 1, buildInfo);
		}
	}
}
