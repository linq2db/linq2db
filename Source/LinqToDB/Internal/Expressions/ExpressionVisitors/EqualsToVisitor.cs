using System;
using System.Linq.Expressions;

using LinqToDB.Extensions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	static class EqualsToVisitor
	{
		internal static bool EqualsTo(
			this Expression expr1,
			Expression      expr2,
			IDataContext    dataContext,
			bool            compareConstantValues = false)
		{
			var equalsInfo = PrepareEqualsInfo(dataContext, compareConstantValues);
			var result     = EqualsTo(expr1, expr2, equalsInfo);

			return result;
		}

		/// <summary>
		/// Creates reusable equality context.
		/// </summary>
		internal static EqualsToInfo PrepareEqualsInfo(
			IDataContext dataContext,
			bool         compareConstantValues = false)
		{
			return new EqualsToInfo(dataContext, compareConstantValues);
		}

		internal sealed class EqualsToInfo
		{
			public EqualsToInfo(
				IDataContext dataContext,
				bool         compareConstantValues)
			{
				DataContext                = dataContext;
				CompareConstantValues      = compareConstantValues;
			}

			public IDataContext DataContext { get; }
			public bool         CompareConstantValues { get; }
		}

		internal static bool EqualsTo(this Expression? expr1, Expression? expr2, EqualsToInfo info)
		{
			if (expr1 == expr2)
			{
				return true;
			}

			if (expr1 == null || expr2 == null || expr1.Type != expr2.Type)
				return false;

			if (expr1.NodeType != expr2.NodeType)
			{
				// special cache case
				if (expr1.NodeType == ExpressionType.Extension && expr2.NodeType == ExpressionType.Constant && expr1 is ConstantPlaceholderExpression)
				{
					return true;
				}

				return false;
			}

			switch (expr1.NodeType)
			{
				case ExpressionType.Add               :
				case ExpressionType.AddChecked        :
				case ExpressionType.And               :
				case ExpressionType.AndAlso           :
				case ExpressionType.ArrayIndex        :
				case ExpressionType.Assign            :
				case ExpressionType.Coalesce          :
				case ExpressionType.Divide            :
				case ExpressionType.Equal             :
				case ExpressionType.ExclusiveOr       :
				case ExpressionType.GreaterThan       :
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LeftShift         :
				case ExpressionType.LessThan          :
				case ExpressionType.LessThanOrEqual   :
				case ExpressionType.Modulo            :
				case ExpressionType.Multiply          :
				case ExpressionType.MultiplyChecked   :
				case ExpressionType.NotEqual          :
				case ExpressionType.Or                :
				case ExpressionType.OrElse            :
				case ExpressionType.Power             :
				case ExpressionType.RightShift        :
				case ExpressionType.Subtract          :
				case ExpressionType.SubtractChecked   :
					return
						((BinaryExpression)expr1).Method == ((BinaryExpression)expr2).Method                      &&
						((BinaryExpression)expr1).Conversion.EqualsTo(((BinaryExpression)expr2).Conversion, info) &&
						((BinaryExpression)expr1).Left.EqualsTo(((BinaryExpression)expr2).Left, info)             &&
						((BinaryExpression)expr1).Right.EqualsTo(((BinaryExpression)expr2).Right, info);

				case ExpressionType.ArrayLength   :
				case ExpressionType.Convert       :
				case ExpressionType.ConvertChecked:
				case ExpressionType.Negate        :
				case ExpressionType.NegateChecked :
				case ExpressionType.Not           :
				case ExpressionType.Quote         :
				case ExpressionType.TypeAs        :
				case ExpressionType.UnaryPlus     :
					return
						((UnaryExpression)expr1).Method == ((UnaryExpression)expr2).Method &&
						((UnaryExpression)expr1).Operand.EqualsTo(((UnaryExpression)expr2).Operand, info);

				case ExpressionType.Conditional:
					return
						((ConditionalExpression)expr1).Test.EqualsTo(((ConditionalExpression)expr2).Test, info)     &&
						((ConditionalExpression)expr1).IfTrue.EqualsTo(((ConditionalExpression)expr2).IfTrue, info) &&
						((ConditionalExpression)expr1).IfFalse.EqualsTo(((ConditionalExpression)expr2).IfFalse, info);

				case ExpressionType.Call          : return EqualsToX((MethodCallExpression)expr1, (MethodCallExpression        )expr2, info);
				case ExpressionType.Constant      : return EqualsToX((ConstantExpression  )expr1, (ConstantExpression          )expr2, info);
				case ExpressionType.Invoke        : return EqualsToX((InvocationExpression)expr1, (InvocationExpression        )expr2, info);
				case ExpressionType.Lambda        : return EqualsToX((LambdaExpression    )expr1, (LambdaExpression            )expr2, info);
				case ExpressionType.ListInit      : return EqualsToX((ListInitExpression  )expr1, (ListInitExpression          )expr2, info);
				case ExpressionType.MemberAccess  : return EqualsToX((MemberExpression    )expr1, (MemberExpression            )expr2, info);
				case ExpressionType.MemberInit    : return EqualsToX((MemberInitExpression)expr1, (MemberInitExpression        )expr2, info);
				case ExpressionType.New           : return EqualsToX((NewExpression       )expr1, (NewExpression               )expr2, info);
				case ExpressionType.NewArrayBounds:
				case ExpressionType.NewArrayInit  : return EqualsToX((NewArrayExpression  )expr1, (NewArrayExpression          )expr2, info);
				case ExpressionType.Default       : return true;
				case ExpressionType.Parameter     : return ((ParameterExpression          )expr1).Name == ((ParameterExpression)expr2).Name;

				case ExpressionType.TypeIs:
					return
						((TypeBinaryExpression)expr1).TypeOperand == ((TypeBinaryExpression)expr2).TypeOperand &&
						((TypeBinaryExpression)expr1).Expression.EqualsTo(((TypeBinaryExpression)expr2).Expression, info);

				case ExpressionType.Block:
					return EqualsToX((BlockExpression)expr1, (BlockExpression)expr2, info);

				case ChangeTypeExpression.ChangeTypeType:
					return
						((ChangeTypeExpression)expr1).Type == ((ChangeTypeExpression)expr2).Type &&
						((ChangeTypeExpression)expr1).Expression.EqualsTo(((ChangeTypeExpression)expr2).Expression, info);

				case ExpressionType.Extension:
					return EqualsExtensions(expr1, expr2);

				default:
					throw new NotImplementedException($"Unhandled expression type: {expr1.NodeType}");
			}
		}

		static bool EqualsExtensions(Expression expr1, Expression expr2)
		{
			return expr1.Equals(expr2);
		}

		static bool EqualsToX(BlockExpression expr1, BlockExpression expr2, EqualsToInfo info)
		{
			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			for (var i = 0; i < expr1.Variables.Count; i++)
				if (!expr1.Variables[i].EqualsTo(expr2.Variables[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewArrayExpression expr1, NewArrayExpression expr2, EqualsToInfo info)
		{
			if (expr1.Expressions.Count != expr2.Expressions.Count)
				return false;

			for (var i = 0; i < expr1.Expressions.Count; i++)
				if (!expr1.Expressions[i].EqualsTo(expr2.Expressions[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(NewExpression expr1, NewExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count)
				return false;

			if (expr1.Members == null && expr2.Members != null)
				return false;

			if (expr1.Members != null && expr2.Members == null)
				return false;

			if (expr1.Constructor != expr2.Constructor)
				return false;

			if (expr1.Members != null)
			{
				if (expr1.Members.Count != expr2.Members!.Count)
					return false;

				for (var i = 0; i < expr1.Members.Count; i++)
					if (expr1.Members[i] != expr2.Members[i])
						return false;
			}

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(MemberInitExpression expr1, MemberInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Bindings.Count != expr2.Bindings.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			for (var i = 0; i < expr1.Bindings.Count; i++)
			{
				var b1 = expr1.Bindings[i];
				var b2 = expr2.Bindings[i];

				if (!CompareBindings(b1, b2, info))
					return false;
			}

			return true;
		}

		static bool CompareBindings(MemberBinding? b1, MemberBinding? b2, EqualsToInfo info)
		{
			if (b1 == b2)
				return true;

			if (b1 == null || b2 == null || b1.BindingType != b2.BindingType || b1.Member != b2.Member)
				return false;

			switch (b1.BindingType)
			{
				case MemberBindingType.Assignment:
					return ((MemberAssignment)b1).Expression.EqualsTo(((MemberAssignment)b2).Expression, info);

				case MemberBindingType.ListBinding:
					var ml1 = (MemberListBinding)b1;
					var ml2 = (MemberListBinding)b2;

					if (ml1.Initializers.Count != ml2.Initializers.Count)
						return false;

					for (var i = 0; i < ml1.Initializers.Count; i++)
					{
						var ei1 = ml1.Initializers[i];
						var ei2 = ml2.Initializers[i];

						if (ei1.AddMethod != ei2.AddMethod || ei1.Arguments.Count != ei2.Arguments.Count)
							return false;

						for (var j = 0; j < ei1.Arguments.Count; j++)
							if (!ei1.Arguments[j].EqualsTo(ei2.Arguments[j], info))
								return false;
					}

					break;

				case MemberBindingType.MemberBinding:
					var mm1 = (MemberMemberBinding)b1;
					var mm2 = (MemberMemberBinding)b2;

					if (mm1.Bindings.Count != mm2.Bindings.Count)
						return false;

					for (var i = 0; i < mm1.Bindings.Count; i++)
						if (!CompareBindings(mm1.Bindings[i], mm2.Bindings[i], info))
							return false;

					break;
			}

			return true;
		}

		static bool EqualsToX(MemberExpression expr1, MemberExpression expr2, EqualsToInfo info)
		{
			if (expr1.Member == expr2.Member)
			{
				return expr1.Expression.EqualsTo(expr2.Expression, info);
			}

			return false;
		}

		static bool EqualsToX(ListInitExpression expr1, ListInitExpression expr2, EqualsToInfo info)
		{
			if (expr1.Initializers.Count != expr2.Initializers.Count || !expr1.NewExpression.EqualsTo(expr2.NewExpression, info))
				return false;

			for (var i = 0; i < expr1.Initializers.Count; i++)
			{
				var i1 = expr1.Initializers[i];
				var i2 = expr2.Initializers[i];

				if (i1.Arguments.Count != i2.Arguments.Count || i1.AddMethod != i2.AddMethod)
					return false;

				for (var j = 0; j < i1.Arguments.Count; j++)
					if (!i1.Arguments[j].EqualsTo(i2.Arguments[j], info))
						return false;
			}

			return true;
		}

		static bool EqualsToX(LambdaExpression expr1, LambdaExpression expr2, EqualsToInfo info)
		{
			if (ReferenceEquals(expr1, expr2))
				return true;

			if (expr1.Parameters.Count != expr2.Parameters.Count || !expr1.Body.EqualsTo(expr2.Body, info))
				return false;

			for (var i = 0; i < expr1.Parameters.Count; i++)
				if (!expr1.Parameters[i].EqualsTo(expr2.Parameters[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(InvocationExpression expr1, InvocationExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || !expr1.Expression.EqualsTo(expr2.Expression, info))
				return false;

			for (var i = 0; i < expr1.Arguments.Count; i++)
				if (!expr1.Arguments[i].EqualsTo(expr2.Arguments[i], info))
					return false;

			return true;
		}

		static bool EqualsToX(ConstantExpression expr1, ConstantExpression expr2, EqualsToInfo info)
		{
			if (expr1.Value == null && expr2.Value == null)
				return true;

			var result = Equals(expr1.Value, expr2.Value);

			return result;
		}

		static bool EqualsToX(MethodCallExpression expr1, MethodCallExpression expr2, EqualsToInfo info)
		{
			if (expr1.Arguments.Count != expr2.Arguments.Count || expr1.Method != expr2.Method)
				return false;

			if (!expr1.Object.EqualsTo(expr2.Object, info))
				return false;

			var dependedParameters = SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(expr1.Method);

			if (dependedParameters == null)
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					if (!DefaultCompareArguments(expr1.Arguments[i], expr2.Arguments[i], info))
						return false;
				}
			}
			else
			{
				for (var i = 0; i < expr1.Arguments.Count; i++)
				{
					var dependentAttribute = dependedParameters[i];
					var arg1 = expr1.Arguments[i];
					var arg2 = expr2.Arguments[i];

					if (dependentAttribute != null)
					{
						if (arg1 is not ConstantPlaceholderExpression)
						{
							if (!dependentAttribute.ExpressionsEqual(info, arg1, arg2, static (info, e1, e2) => e1.EqualsTo(e2, info)))
								return false;
						}
					}
					else
					{
						if (!DefaultCompareArguments(arg1, arg2, info))
							return false;
					}
				}

			}

			return true;
		}

		static bool DefaultCompareArguments(Expression arg1, Expression arg2, EqualsToInfo info)
		{
			if (typeof(Sql.IQueryableContainer).IsSameOrParentOf(arg1.Type))
			{
				if (arg1.NodeType == ExpressionType.Constant && arg2.NodeType == ExpressionType.Constant)
				{
					var query1 = (arg1.EvaluateExpression<Sql.IQueryableContainer>()!).Query;
					var query2 = (arg2.EvaluateExpression<Sql.IQueryableContainer>()!).Query;
					return EqualsTo(query1.Expression, query2.Expression, info);
				}
			}

			return arg1.EqualsTo(arg2, info);
		}
	}
}
