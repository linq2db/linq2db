using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq
{
	class ReflectionHelper
	{
		public class Expressor<T>
		{
			public static FieldInfo FieldOf(Expression<Func<T,object?>> func)
			{
				return MemberHelper.FieldOf(func);
			}

			public static MethodInfo MethodOf(Expression<Func<T,object?>> func)
			{
				return MemberHelper.MethodOf(func);
			}

			public static PropertyInfo PropertyOf(Expression<Func<T, object?>> func)
			{
				return MemberHelper.PropertyOf(func);
			}
		}

		public class Binary : Expressor<BinaryExpression>
		{
			public static PropertyInfo Conversion = PropertyOf(e => e.Conversion);
			public static PropertyInfo Left       = PropertyOf(e => e.Left);
			public static PropertyInfo Right      = PropertyOf(e => e.Right);
		}

		public class Unary : Expressor<UnaryExpression>
		{
			public static PropertyInfo Operand = PropertyOf(e => e.Operand);
		}

		public class LambdaExpr : Expressor<LambdaExpression>
		{
			public static PropertyInfo Body       = PropertyOf(e => e.Body);
			public static PropertyInfo Parameters = PropertyOf(e => e.Parameters);
		}

		public class Constant : Expressor<ConstantExpression>
		{
			public static PropertyInfo Value = PropertyOf(e => e.Value);
		}

		public class QueryableInt : Expressor<IQueryable>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public class MethodCall : Expressor<MethodCallExpression>
		{
			public static PropertyInfo Object    = PropertyOf(e => e.Object);
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public class Conditional : Expressor<ConditionalExpression>
		{
			public static PropertyInfo Test    = PropertyOf(e => e.Test);
			public static PropertyInfo IfTrue  = PropertyOf(e => e.IfTrue);
			public static PropertyInfo IfFalse = PropertyOf(e => e.IfFalse);
		}

		public class Invocation : Expressor<InvocationExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
			public static PropertyInfo Arguments  = PropertyOf(e => e.Arguments);
		}

		public class ListInit : Expressor<ListInitExpression>
		{
			public static PropertyInfo NewExpression = PropertyOf(e => e.NewExpression);
			public static PropertyInfo Initializers  = PropertyOf(e => e.Initializers);
		}

		public class ElementInit : Expressor<System.Linq.Expressions.ElementInit>
		{
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public class Member : Expressor<MemberExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public class MemberInit : Expressor<MemberInitExpression>
		{
			public static PropertyInfo NewExpression = PropertyOf(e => e.NewExpression);
			public static PropertyInfo Bindings      = PropertyOf(e => e.Bindings);
		}

		public class New : Expressor<NewExpression>
		{
			public static PropertyInfo Arguments = PropertyOf(e => e.Arguments);
		}

		public class NewArray : Expressor<NewArrayExpression>
		{
			public static PropertyInfo Expressions = PropertyOf(e => e.Expressions);
		}

		public class TypeBinary : Expressor<TypeBinaryExpression>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public class IndexExpressor<T>
		{
			public static MethodInfo IndexerExpressor(Expression<Func<ReadOnlyCollection<T>, object>> func)
			{
				return ((MethodCallExpression)((UnaryExpression)func.Body).Operand).Method;
			}

			public static MethodInfo Item = IndexerExpressor(c => c[0]!);
		}

		public class MemberAssignmentBind : Expressor<MemberAssignment>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public class MemberListBind : Expressor<MemberListBinding>
		{
			public static PropertyInfo Initializers = PropertyOf(e => e.Initializers);
		}

		public class MemberMemberBind : Expressor<MemberMemberBinding>
		{
			public static PropertyInfo Bindings = PropertyOf(e => e.Bindings);
		}

		public class Block : Expressor<BlockExpression>
		{
			public static PropertyInfo Expressions = PropertyOf(e => e.Expressions);
			public static PropertyInfo Variables   = PropertyOf(e => e.Variables);
		}

		public class ContextConstruction : Expressor<ContextConstructionExpression>
		{
			public static PropertyInfo InnerExpression = PropertyOf(e => e.InnerExpression);
		}

		public class SqlGenericConstructor : Expressor<SqlGenericConstructorExpression>
		{
			public static PropertyInfo Assignments = PropertyOf(e => e.Assignments);
		}

		public class SqlGenericConstructorAssignment : Expressor<SqlGenericConstructorExpression.Assignment>
		{
			public static PropertyInfo Expression = PropertyOf(e => e.Expression);
		}

		public static MethodInfo ExprItem  = IndexExpressor<Expression>         .Item;
		public static MethodInfo ParamItem = IndexExpressor<ParameterExpression>.Item;
		public static MethodInfo ElemItem  = IndexExpressor<ElementInit>        .Item;

		public class DataReader : Expressor<DbDataReader>
		{
			public static MethodInfo GetValue = MethodOf(rd => rd.GetValue(0));
			public static MethodInfo IsDBNull = MethodOf(rd => rd.IsDBNull(0));
		}

		public class Functions
		{
			public class String : Expressor<string>
			{
#if NETFRAMEWORK
				public static MethodInfo Like11 = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", ""));
				public static MethodInfo Like12 = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", "", ' '));
#endif

				public static MethodInfo Like21 = MethodOf(s => Sql.Like(s, ""));
				public static MethodInfo Like22 = MethodOf(s => Sql.Like(s, "", ' '));
			}

#if !NET45
			public class FormattableString : Expressor<System.FormattableString>
			{
				public static MethodInfo GetArguments = MethodOf(s => s.GetArgument(0));
			}
#endif
		}
	}
}
