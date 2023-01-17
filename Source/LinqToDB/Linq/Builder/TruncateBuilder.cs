using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using LinqToDB.Expressions;
	using SqlQuery;

	[BuildsMethodCall("Truncate")]
	sealed class TruncateBuilder : MethodCallBuilder
	{
		#region TruncateBuilder

		public static bool CanBuildMethod(MethodCallExpression call, BuildInfo info, ExpressionBuilder builder)
			=> call.IsQueryable();

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression call, BuildInfo info)
		{
			var sequence = (TableBuilder.TableContext)builder.BuildSequence(new BuildInfo(info, call.Arguments[0]));

			var arg   = call.Arguments[1].Unwrap();

			sequence.Statement = new SqlTruncateTableStatement 
			{ 
				Table = sequence.SqlTable, 				
				ResetIdentity = arg.Type == typeof(bool)
					? (bool)arg.EvaluateExpression()!
					: true
			};

			return new TruncateContext(info.Parent, sequence);
		}

		#endregion

		#region TruncateContext

		sealed class TruncateContext : SequenceContextBase
		{
			public TruncateContext(IBuildContext? parent, IBuildContext sequence)
				: base(parent, sequence, null)
			{
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				QueryRunner.SetNonQueryQuery(query);
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TruncateContext(null, context.CloneContext(Sequence));
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException();
			}

			public override IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				throw new NotImplementedException();
			}

			public override SqlStatement GetResultStatement()
			{
				return Sequence.GetResultStatement();
			}
		}

		#endregion
	}
}
