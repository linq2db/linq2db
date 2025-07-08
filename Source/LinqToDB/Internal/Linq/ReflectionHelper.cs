using System;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq
{
	sealed class ReflectionHelper
	{
		public abstract class Expressor<T>
		{
			protected static FieldInfo FieldOf(Expression<Func<T,object?>> func)
			{
				return MemberHelper.FieldOf(func);
			}

			protected static MethodInfo MethodOf(Expression<Func<T,object?>> func)
			{
				return MemberHelper.MethodOf(func);
			}

			protected static PropertyInfo PropertyOf(Expression<Func<T, object?>> func)
			{
				return MemberHelper.PropertyOf(func);
			}
		}

		public sealed class Binary : Expressor<BinaryExpression>
		{
			public static PropertyInfo Conversion = PropertyOf(e => e.Conversion);
			public static PropertyInfo Left       = PropertyOf(e => e.Left);
			public static PropertyInfo Right      = PropertyOf(e => e.Right);
		}

		public sealed class Unary : Expressor<UnaryExpression>
		{
			public static PropertyInfo Operand = PropertyOf(e => e.Operand);
		}

		public sealed class LambdaExpr : Expressor<LambdaExpression>
		{
			public static PropertyInfo Body       = PropertyOf(e => e.Body);
			public static PropertyInfo Parameters = PropertyOf(e => e.Parameters);
		}

		public sealed class Constant : Expressor<ConstantExpression>
		{
			public static PropertyInfo Value = PropertyOf(e => e.Value);
		}

		public sealed class QueryableInt : Expressor<IQueryable>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public sealed class MethodCall : Expressor<MethodCallExpression>
		{
			public static PropertyInfo Object    = PropertyOf(e => e.Object);
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public sealed class Conditional : Expressor<ConditionalExpression>
		{
			public static PropertyInfo Test    = PropertyOf(e => e.Test);
			public static PropertyInfo IfTrue  = PropertyOf(e => e.IfTrue);
			public static PropertyInfo IfFalse = PropertyOf(e => e.IfFalse);
		}

		public sealed class Invocation : Expressor<InvocationExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
			public static PropertyInfo Arguments  = PropertyOf(e => e.Arguments);
		}

		public sealed class ListInit : Expressor<ListInitExpression>
		{
			public static PropertyInfo NewExpression = PropertyOf(e => e.NewExpression);
			public static PropertyInfo Initializers  = PropertyOf(e => e.Initializers);
		}

		public sealed class ElementInit : Expressor<System.Linq.Expressions.ElementInit>
		{
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public sealed class Member : Expressor<MemberExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public sealed class MemberInit : Expressor<MemberInitExpression>
		{
			public static PropertyInfo NewExpression = PropertyOf(e => e.NewExpression);
			public static PropertyInfo Bindings      = PropertyOf(e => e.Bindings);
		}

		public sealed class New : Expressor<NewExpression>
		{
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public sealed class NewArray : Expressor<NewArrayExpression>
		{
			public static PropertyInfo Expressions = PropertyOf(e => e.Expressions);
		}

		public sealed class TypeBinary : Expressor<TypeBinaryExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public sealed class IndexExpressor<T>
		{
			public static MethodInfo IndexerExpressor(Expression<Func<ReadOnlyCollection<T>, object>> func)
			{
				return ((MethodCallExpression)((UnaryExpression)func.Body).Operand).Method;
			}

			public static MethodInfo Item = IndexerExpressor(c => c[0]!);
		}

		public sealed class MemberAssignmentBind : Expressor<MemberAssignment>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public sealed class MemberListBind : Expressor<MemberListBinding>
		{
			public static PropertyInfo Initializers = PropertyOf(e => e.Initializers);
		}

		public sealed class MemberMemberBind : Expressor<MemberMemberBinding>
		{
			public static PropertyInfo Bindings = PropertyOf(e => e.Bindings);
		}

		public sealed class Block : Expressor<BlockExpression>
		{
			public static PropertyInfo Expressions = PropertyOf(e => e.Expressions);
			public static PropertyInfo Variables   = PropertyOf(e => e.Variables);
		}

		public sealed class SqlGenericConstructor : Expressor<SqlGenericConstructorExpression>
		{
			public static PropertyInfo Assignments = PropertyOf(e => e.Assignments);
		}

		public sealed class SqlGenericConstructorAssignment : Expressor<SqlGenericConstructorExpression.Assignment>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public static MethodInfo ExprItem  = IndexExpressor<Expression>         .Item;
		public static MethodInfo ParamItem = IndexExpressor<ParameterExpression>.Item;
		public static MethodInfo ElemItem  = IndexExpressor<ElementInit>        .Item;

		public sealed class DataReader : Expressor<DbDataReader>
		{
			public static MethodInfo GetValue = MethodOf(rd => rd.GetValue(0));
			public static MethodInfo IsDBNull = MethodOf(rd => rd.IsDBNull(0));
		}
	}
}
