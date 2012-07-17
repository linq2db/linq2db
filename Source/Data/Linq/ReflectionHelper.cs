using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;

using LinqBinary = System.Data.Linq.Binary;

namespace LinqToDB.Data.Linq
{
	public class ReflectionHelper
	{
		public class Expressor<T>
		{
			public static FieldInfo FieldExpressor(Expression<Func<T,object>> func)
			{
				return (FieldInfo)((MemberExpression)((UnaryExpression)func.Body).Operand).Member;
			}

			public static MethodInfo PropertyExpressor(Expression<Func<T,object>> func)
			{
				return ((PropertyInfo)((MemberExpression)func.Body).Member).GetGetMethod();
			}

			public static MethodInfo MethodExpressor(Expression<Func<T,object>> func)
			{
				var ex = func.Body;

				if (ex is UnaryExpression)
					ex = ((UnaryExpression)ex).Operand;

				//if (ex is MemberExpression)
				//	return ((PropertyInfo)((MemberExpression)ex).Member).GetGetMethod();

				return ((MethodCallExpression)ex).Method;
			}
		}

		public static MemberInfo MemeberInfo(LambdaExpression func)
		{
			var ex = func.Body;

			if (ex is UnaryExpression)
				ex = ((UnaryExpression)ex).Operand;

			return
				ex is MemberExpression     ? ((MemberExpression)    ex).Member :
				ex is MethodCallExpression ? ((MethodCallExpression)ex).Method :
				                 (MemberInfo)((NewExpression)       ex).Constructor;
		}

		public class Binary : Expressor<BinaryExpression>
		{
			public static MethodInfo Conversion = PropertyExpressor(e => e.Conversion);
			public static MethodInfo Left       = PropertyExpressor(e => e.Left);
			public static MethodInfo Right      = PropertyExpressor(e => e.Right);
		}

		public class Unary : Expressor<UnaryExpression>
		{
			public static MethodInfo Operand = PropertyExpressor(e => e.Operand);
		}

		public class LambdaExpr : Expressor<LambdaExpression>
		{
			public void Foo()
			{
				Expression<Func<LambdaExpression, object>> a = e => e.Body;
				a.ToString();
			}

			public static MethodInfo Body       = PropertyExpressor(e => e.Body);
			public static MethodInfo Parameters = PropertyExpressor(e => e.Parameters);
		}

		public class Constant : Expressor<ConstantExpression>
		{
			public static MethodInfo Value = PropertyExpressor(e => e.Value);
		}

		public class QueryableInt : Expressor<IQueryable>
		{
			public static MethodInfo Expression = PropertyExpressor(e => e.Expression);
		}

		public class MethodCall : Expressor<MethodCallExpression>
		{
			public static MethodInfo Object    = PropertyExpressor(e => e.Object);
			public static MethodInfo Arguments = PropertyExpressor(e => e.Arguments);
		}

		public class Conditional : Expressor<ConditionalExpression>
		{
			public static MethodInfo Test    = PropertyExpressor(e => e.Test);
			public static MethodInfo IfTrue  = PropertyExpressor(e => e.IfTrue);
			public static MethodInfo IfFalse = PropertyExpressor(e => e.IfFalse);
		}

		public class Invocation : Expressor<InvocationExpression>
		{
			public static MethodInfo Expression = PropertyExpressor(e => e.Expression);
			public static MethodInfo Arguments  = PropertyExpressor(e => e.Arguments);
		}

		public class ListInit : Expressor<ListInitExpression>
		{
			public static MethodInfo NewExpression = PropertyExpressor(e => e.NewExpression);
			public static MethodInfo Initializers  = PropertyExpressor(e => e.Initializers);
		}

		public class ElementInit : Expressor<System.Linq.Expressions.ElementInit>
		{
			public static MethodInfo Arguments = PropertyExpressor(e => e.Arguments);
		}

		public class Member : Expressor<MemberExpression>
		{
			public static MethodInfo Expression = PropertyExpressor(e => e.Expression);
		}

		public class MemberInit : Expressor<MemberInitExpression>
		{
			public static MethodInfo NewExpression = PropertyExpressor(e => e.NewExpression);
			public static MethodInfo Bindings      = PropertyExpressor(e => e.Bindings);
		}

		public class New : Expressor<NewExpression>
		{
			public static MethodInfo Arguments = PropertyExpressor(e => e.Arguments);
		}

		public class NewArray : Expressor<NewArrayExpression>
		{
			public static MethodInfo Expressions = PropertyExpressor(e => e.Expressions);
		}

		public class TypeBinary : Expressor<TypeBinaryExpression>
		{
			public static MethodInfo Expression = PropertyExpressor(e => e.Expression);
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
			public static MethodInfo Expression = PropertyExpressor(e => e.Expression);
		}

		public class MemberListBind : Expressor<MemberListBinding>
		{
			public static MethodInfo Initializers = PropertyExpressor(e => e.Initializers);
		}

		public class MemberMemberBind : Expressor<MemberMemberBinding>
		{
			public static MethodInfo Bindings = PropertyExpressor(e => e.Bindings);
		}

#if FW4 || SILVERLIGHT

		public class Block : Expressor<BlockExpression>
		{
			public static MethodInfo Expressions = PropertyExpressor(e => e.Expressions);
			public static MethodInfo Variables   = PropertyExpressor(e => e.Variables);
		}

#endif

		public static MethodInfo ExprItem  = IndexExpressor<Expression>         .Item;
		public static MethodInfo ParamItem = IndexExpressor<ParameterExpression>.Item;
		public static MethodInfo ElemItem  = IndexExpressor<ElementInit>        .Item;

		public class DataReader : Expressor<IDataReader>
		{
			public static MethodInfo GetValue = MethodExpressor(rd => rd.GetValue(0));
			public static MethodInfo IsDBNull = MethodExpressor(rd => rd.IsDBNull(0));
		}

		internal class QueryCtx : Expressor<QueryContext>
		{
			public static FieldInfo Counter = FieldExpressor(ctx => ctx.Counter);
		}

		public class MapSchema : Expressor<Mapping.MappingSchemaOld>
		{
			public static MethodInfo MapValueToEnum = MethodExpressor(m => m.MapValueToEnum   (null, null));
			public static MethodInfo ChangeType     = MethodExpressor(m => m.ConvertChangeType(null, null));

			public static Dictionary<Type,MethodInfo> Converters = new Dictionary<Type,MethodInfo>
			{
				// Primitive Types
				//
				{ typeof(SByte),           MethodExpressor(m => m.ConvertToSByte                 (null)) },
				{ typeof(Int16),           MethodExpressor(m => m.ConvertToInt16                 (null)) },
				{ typeof(Int32),           MethodExpressor(m => m.ConvertToInt32                 (null)) },
				{ typeof(Int64),           MethodExpressor(m => m.ConvertToInt64                 (null)) },
				{ typeof(Byte),            MethodExpressor(m => m.ConvertToByte                  (null)) },
				{ typeof(UInt16),          MethodExpressor(m => m.ConvertToUInt16                (null)) },
				{ typeof(UInt32),          MethodExpressor(m => m.ConvertToUInt32                (null)) },
				{ typeof(UInt64),          MethodExpressor(m => m.ConvertToUInt64                (null)) },
				{ typeof(Char),            MethodExpressor(m => m.ConvertToChar                  (null)) },
				{ typeof(Single),          MethodExpressor(m => m.ConvertToSingle                (null)) },
				{ typeof(Double),          MethodExpressor(m => m.ConvertToDouble                (null)) },
				{ typeof(Boolean),         MethodExpressor(m => m.ConvertToBoolean               (null)) },

				// Simple Types
				//
				{ typeof(String),          MethodExpressor(m => m.ConvertToString                (null)) },
				{ typeof(DateTime),        MethodExpressor(m => m.ConvertToDateTime              (null)) },
				{ typeof(TimeSpan),        MethodExpressor(m => m.ConvertToTimeSpan              (null)) },
				{ typeof(DateTimeOffset),  MethodExpressor(m => m.ConvertToDateTimeOffset        (null)) },
				{ typeof(Decimal),         MethodExpressor(m => m.ConvertToDecimal               (null)) },
				{ typeof(Guid),            MethodExpressor(m => m.ConvertToGuid                  (null)) },
				{ typeof(Stream),          MethodExpressor(m => m.ConvertToStream                (null)) },
#if !SILVERLIGHT
				{ typeof(XmlReader),       MethodExpressor(m => m.ConvertToXmlReader             (null)) },
				{ typeof(XmlDocument),     MethodExpressor(m => m.ConvertToXmlDocument           (null)) },
#endif
				{ typeof(Byte[]),          MethodExpressor(m => m.ConvertToByteArray             (null)) },
				{ typeof(LinqBinary),      MethodExpressor(m => m.ConvertToLinqBinary            (null)) },
				{ typeof(Char[]),          MethodExpressor(m => m.ConvertToCharArray             (null)) },

				// Nullable Types
				//
				{ typeof(SByte?),          MethodExpressor(m => m.ConvertToNullableSByte         (null)) },
				{ typeof(Int16?),          MethodExpressor(m => m.ConvertToNullableInt16         (null)) },
				{ typeof(Int32?),          MethodExpressor(m => m.ConvertToNullableInt32         (null)) },
				{ typeof(Int64?),          MethodExpressor(m => m.ConvertToNullableInt64         (null)) },
				{ typeof(Byte?),           MethodExpressor(m => m.ConvertToNullableByte          (null)) },
				{ typeof(UInt16?),         MethodExpressor(m => m.ConvertToNullableUInt16        (null)) },
				{ typeof(UInt32?),         MethodExpressor(m => m.ConvertToNullableUInt32        (null)) },
				{ typeof(UInt64?),         MethodExpressor(m => m.ConvertToNullableUInt64        (null)) },
				{ typeof(Char?),           MethodExpressor(m => m.ConvertToNullableChar          (null)) },
				{ typeof(Double?),         MethodExpressor(m => m.ConvertToNullableDouble        (null)) },
				{ typeof(Single?),         MethodExpressor(m => m.ConvertToNullableSingle        (null)) },
				{ typeof(Boolean?),        MethodExpressor(m => m.ConvertToNullableBoolean       (null)) },
				{ typeof(DateTime?),       MethodExpressor(m => m.ConvertToNullableDateTime      (null)) },
				{ typeof(TimeSpan?),       MethodExpressor(m => m.ConvertToNullableTimeSpan      (null)) },
				{ typeof(DateTimeOffset?), MethodExpressor(m => m.ConvertToNullableDateTimeOffset(null)) },
				{ typeof(Decimal?),        MethodExpressor(m => m.ConvertToNullableDecimal       (null)) },
				{ typeof(Guid?),           MethodExpressor(m => m.ConvertToNullableGuid          (null)) },

#if !SILVERLIGHT

				// SqlTypes
				//
				{ typeof(SqlByte),         MethodExpressor(m => m.ConvertToSqlByte               (null)) },
				{ typeof(SqlInt16),        MethodExpressor(m => m.ConvertToSqlInt16              (null)) },
				{ typeof(SqlInt32),        MethodExpressor(m => m.ConvertToSqlInt32              (null)) },
				{ typeof(SqlInt64),        MethodExpressor(m => m.ConvertToSqlInt64              (null)) },
				{ typeof(SqlSingle),       MethodExpressor(m => m.ConvertToSqlSingle             (null)) },
				{ typeof(SqlBoolean),      MethodExpressor(m => m.ConvertToSqlBoolean            (null)) },
				{ typeof(SqlDouble),       MethodExpressor(m => m.ConvertToSqlDouble             (null)) },
				{ typeof(SqlDateTime),     MethodExpressor(m => m.ConvertToSqlDateTime           (null)) },
				{ typeof(SqlDecimal),      MethodExpressor(m => m.ConvertToSqlDecimal            (null)) },
				{ typeof(SqlMoney),        MethodExpressor(m => m.ConvertToSqlMoney              (null)) },
				{ typeof(SqlString),       MethodExpressor(m => m.ConvertToSqlString             (null)) },
				{ typeof(SqlBinary),       MethodExpressor(m => m.ConvertToSqlBinary             (null)) },
				{ typeof(SqlGuid),         MethodExpressor(m => m.ConvertToSqlGuid               (null)) },
				{ typeof(SqlBytes),        MethodExpressor(m => m.ConvertToSqlBytes              (null)) },
				{ typeof(SqlChars),        MethodExpressor(m => m.ConvertToSqlChars              (null)) },
				{ typeof(SqlXml),          MethodExpressor(m => m.ConvertToSqlXml                (null)) },

#endif
			};
		}

		public class Functions
		{
			public class String : Expressor<string>
			{
				//public static MethodInfo Contains   = MethodExpressor(s => s.Contains(""));
				//public static MethodInfo StartsWith = MethodExpressor(s => s.StartsWith(""));
				//public static MethodInfo EndsWith   = MethodExpressor(s => s.EndsWith(""));

#if !SILVERLIGHT
				public static MethodInfo Like11     = MethodExpressor(s => System.Data.Linq.SqlClient.SqlMethods.Like("", ""));
				public static MethodInfo Like12     = MethodExpressor(s => System.Data.Linq.SqlClient.SqlMethods.Like("", "", ' '));
#endif

				public static MethodInfo Like21     = MethodExpressor(s => Sql.Like(s, ""));
				public static MethodInfo Like22     = MethodExpressor(s => Sql.Like(s, "", ' '));
			}
		}
	}
}
