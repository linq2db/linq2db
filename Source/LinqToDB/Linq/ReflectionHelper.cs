using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if NET6_0_OR_GREATER
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(LinqToDB.Linq.ReflectionHelper))]
[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(LinqToDB.Linq.ReflectionHelper.IndexExpressor<>))]
#endif

namespace LinqToDB.Linq
{
	using LinqToDB.Expressions;

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

			private static void ClearCache(Type[]? updatedTypes) // Hot Reload Compatibility
			{
				Item = IndexerExpressor(c => c[0]!);
			}
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

		#region Hot Reload Compatibility
		private static void ClearCache(Type[]? updatedTypes) // Hot Reload Compatibility
		{
			Binary.Conversion				= Binary.PropertyOf(e => e.Conversion);
			Binary.Left						= Binary.PropertyOf(e => e.Left);
			Binary.Right					= Binary.PropertyOf(e => e.Right);

			Unary.Operand					= Unary.PropertyOf(e => e.Operand);

			LambdaExpr.Body					= LambdaExpr.PropertyOf(e => e.Body);
			LambdaExpr.Parameters			= LambdaExpr.PropertyOf(e => e.Parameters);

			Constant.Value					= Constant.PropertyOf(e => e.Value);

			QueryableInt.Expression			= QueryableInt.PropertyOf(e => e.Expression);

			MethodCall.Object				= MethodCall.PropertyOf(e => e.Object);
			MethodCall.Arguments			= MethodCall.PropertyOf(e => e.Arguments);

			Conditional.Test				= Conditional.PropertyOf(e => e.Test);
			Conditional.IfTrue				= Conditional.PropertyOf(e => e.IfTrue);
			Conditional.IfFalse				= Conditional.PropertyOf(e => e.IfFalse);

			Invocation.Expression			= Invocation.PropertyOf(e => e.Expression);
			Invocation.Arguments			= Invocation.PropertyOf(e => e.Arguments);

			ListInit.NewExpression			= ListInit.PropertyOf(e => e.NewExpression);
			ListInit.Initializers			= ListInit.PropertyOf(e => e.Initializers);

			ElementInit.Arguments			= ElementInit.PropertyOf(e => e.Arguments);

			Member.Expression				= Member.PropertyOf(e => e.Expression);

			MemberInit.NewExpression		= MemberInit.PropertyOf(e => e.NewExpression);
			MemberInit.Bindings				= MemberInit.PropertyOf(e => e.Bindings);

			New.Arguments					= New.PropertyOf(e => e.Arguments);

			NewArray.Expressions			= NewArray.PropertyOf(e => e.Expressions);

			TypeBinary.Expression			= TypeBinary.PropertyOf(e => e.Expression);

			MemberAssignmentBind.Expression	= MemberAssignmentBind.PropertyOf(e => e.Expression);

			MemberListBind.Initializers		= MemberListBind.PropertyOf(e => e.Initializers);

			MemberMemberBind.Bindings		= MemberMemberBind.PropertyOf(e => e.Bindings);

			Block.Expressions				= Block.PropertyOf(e => e.Expressions);
			Block.Variables					= Block.PropertyOf(e => e.Variables);

			ExprItem						= IndexExpressor<Expression>.Item;
			ParamItem						= IndexExpressor<ParameterExpression>.Item;
			ElemItem						= IndexExpressor<ElementInit>.Item;

			DataReader.GetValue				= DataReader.MethodOf(rd => rd.GetValue(0));
			DataReader.IsDBNull				= DataReader.MethodOf(rd => rd.IsDBNull(0));

			Functions.String.Like21			= Functions.String.MethodOf(s => Sql.Like(s, ""));
			Functions.String.Like22			= Functions.String.MethodOf(s => Sql.Like(s, "", ' '));
#if !NET45
			Functions.FormattableString.GetArguments 
											= Functions.FormattableString.MethodOf(s => s.GetArgument(0));
#endif
		}
		#endregion
	}
}
