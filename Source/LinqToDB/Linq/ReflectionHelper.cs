using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq
{
	using LinqToDB.Expressions;

	class ReflectionHelper
	{
		public class Expressor<T>
		{
			public static FieldInfo FieldOf(Expression<Func<T,object>> func)
			{
				return MemberHelper.FieldOf(func);
			}

			public static MethodInfo MethodOf(Expression<Func<T,object>> func)
			{
				return MemberHelper.MethodOf(func);
			}
		}

		public class Binary : Expressor<BinaryExpression>
		{
			public static MethodInfo Conversion = MethodOf(e => e.Conversion);
			public static MethodInfo Left       = MethodOf(e => e.Left);
			public static MethodInfo Right      = MethodOf(e => e.Right);
		}

		public class Unary : Expressor<UnaryExpression>
		{
			public static MethodInfo Operand = MethodOf(e => e.Operand);
		}

		public class LambdaExpr : Expressor<LambdaExpression>
		{
			public static MethodInfo Body       = MethodOf(e => e.Body);
			public static MethodInfo Parameters = MethodOf(e => e.Parameters);
		}

		public class Constant : Expressor<ConstantExpression>
		{
			public static MethodInfo Value = MethodOf(e => e.Value);
		}

		public class QueryableInt : Expressor<IQueryable>
		{
			public static MethodInfo Expression = MethodOf(e => e.Expression);
		}

		public class MethodCall : Expressor<MethodCallExpression>
		{
			public static MethodInfo Object    = MethodOf(e => e.Object);
			public static MethodInfo Arguments = MethodOf(e => e.Arguments);
		}

		public class Conditional : Expressor<ConditionalExpression>
		{
			public static MethodInfo Test    = MethodOf(e => e.Test);
			public static MethodInfo IfTrue  = MethodOf(e => e.IfTrue);
			public static MethodInfo IfFalse = MethodOf(e => e.IfFalse);
		}

		public class Invocation : Expressor<InvocationExpression>
		{
			public static MethodInfo Expression = MethodOf(e => e.Expression);
			public static MethodInfo Arguments  = MethodOf(e => e.Arguments);
		}

		public class ListInit : Expressor<ListInitExpression>
		{
			public static MethodInfo NewExpression = MethodOf(e => e.NewExpression);
			public static MethodInfo Initializers  = MethodOf(e => e.Initializers);
		}

		public class ElementInit : Expressor<System.Linq.Expressions.ElementInit>
		{
			public static MethodInfo Arguments = MethodOf(e => e.Arguments);
		}

		public class Member : Expressor<MemberExpression>
		{
			public static MethodInfo Expression = MethodOf(e => e.Expression);
		}

		public class MemberInit : Expressor<MemberInitExpression>
		{
			public static MethodInfo NewExpression = MethodOf(e => e.NewExpression);
			public static MethodInfo Bindings      = MethodOf(e => e.Bindings);
		}

		public class New : Expressor<NewExpression>
		{
			public static MethodInfo Arguments = MethodOf(e => e.Arguments);
		}

		public class NewArray : Expressor<NewArrayExpression>
		{
			public static MethodInfo Expressions = MethodOf(e => e.Expressions);
		}

		public class TypeBinary : Expressor<TypeBinaryExpression>
		{
			public static MethodInfo Expression = MethodOf(e => e.Expression);
		}

		public class IndexExpressor<T>
		{
			public static MethodInfo IndexerExpressor(Expression<Func<ReadOnlyCollection<T>, object>> func)
			{
				return ((MethodCallExpression)((UnaryExpression)func.Body).Operand).Method;
			}

			public static MethodInfo Item = IndexerExpressor(c => c[0]);
		}

		public class MemberAssignmentBind : Expressor<MemberAssignment>
		{
			public static MethodInfo Expression = MethodOf(e => e.Expression);
		}

		public class MemberListBind : Expressor<MemberListBinding>
		{
			public static MethodInfo Initializers = MethodOf(e => e.Initializers);
		}

		public class MemberMemberBind : Expressor<MemberMemberBinding>
		{
			public static MethodInfo Bindings = MethodOf(e => e.Bindings);
		}

		public class Block : Expressor<BlockExpression>
		{
			public static MethodInfo Expressions = MethodOf(e => e.Expressions);
			public static MethodInfo Variables   = MethodOf(e => e.Variables);
		}

		public static MethodInfo ExprItem  = IndexExpressor<Expression>         .Item;
		public static MethodInfo ParamItem = IndexExpressor<ParameterExpression>.Item;
		public static MethodInfo ElemItem  = IndexExpressor<ElementInit>        .Item;

		public class DataReader : Expressor<IDataReader>
		{
			public static MethodInfo GetValue = MethodOf(rd => rd.GetValue(0));
			public static MethodInfo IsDBNull = MethodOf(rd => rd.IsDBNull(0));
		}

		public class Functions
		{
			public class String : Expressor<string>
			{
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
				public static MethodInfo Like11 = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", ""));
				public static MethodInfo Like12 = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", "", ' '));
#endif

				public static MethodInfo Like21 = MethodOf(s => Sql.Like(s, ""));
				public static MethodInfo Like22 = MethodOf(s => Sql.Like(s, "", ' '));
			}
		}
	}
}
