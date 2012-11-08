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

		internal class QueryCtx : Expressor<QueryContext>
		{
			public static FieldInfo Counter = FieldOf(ctx => ctx.Counter);
		}

		public class MapSchema : Expressor<Mapping.MappingSchemaOld>
		{
			public static MethodInfo MapValueToEnum = MethodOf(m => m.MapValueToEnum   (null, null));
			public static MethodInfo ChangeType     = MethodOf(m => m.ConvertChangeType(null, null));

			public static Dictionary<Type,MethodInfo> Converters = new Dictionary<Type,MethodInfo>
			{
				// Primitive Types
				//
				{ typeof(SByte),           MethodOf(m => m.ConvertToSByte                 (null)) },
				{ typeof(Int16),           MethodOf(m => m.ConvertToInt16                 (null)) },
				{ typeof(Int32),           MethodOf(m => m.ConvertToInt32                 (null)) },
				{ typeof(Int64),           MethodOf(m => m.ConvertToInt64                 (null)) },
				{ typeof(Byte),            MethodOf(m => m.ConvertToByte                  (null)) },
				{ typeof(UInt16),          MethodOf(m => m.ConvertToUInt16                (null)) },
				{ typeof(UInt32),          MethodOf(m => m.ConvertToUInt32                (null)) },
				{ typeof(UInt64),          MethodOf(m => m.ConvertToUInt64                (null)) },
				{ typeof(Char),            MethodOf(m => m.ConvertToChar                  (null)) },
				{ typeof(Single),          MethodOf(m => m.ConvertToSingle                (null)) },
				{ typeof(Double),          MethodOf(m => m.ConvertToDouble                (null)) },
				{ typeof(Boolean),         MethodOf(m => m.ConvertToBoolean               (null)) },

				// Simple Types
				//
				{ typeof(String),          MethodOf(m => m.ConvertToString                (null)) },
				{ typeof(DateTime),        MethodOf(m => m.ConvertToDateTime              (null)) },
				{ typeof(TimeSpan),        MethodOf(m => m.ConvertToTimeSpan              (null)) },
				{ typeof(DateTimeOffset),  MethodOf(m => m.ConvertToDateTimeOffset        (null)) },
				{ typeof(Decimal),         MethodOf(m => m.ConvertToDecimal               (null)) },
				{ typeof(Guid),            MethodOf(m => m.ConvertToGuid                  (null)) },
				{ typeof(Stream),          MethodOf(m => m.ConvertToStream                (null)) },
#if !SILVERLIGHT
				{ typeof(XmlReader),       MethodOf(m => m.ConvertToXmlReader             (null)) },
				{ typeof(XmlDocument),     MethodOf(m => m.ConvertToXmlDocument           (null)) },
#endif
				{ typeof(Byte[]),          MethodOf(m => m.ConvertToByteArray             (null)) },
				{ typeof(System.Data.Linq.Binary),      MethodOf(m => m.ConvertToLinqBinary            (null)) },
				{ typeof(Char[]),          MethodOf(m => m.ConvertToCharArray             (null)) },

				// Nullable Types
				//
				{ typeof(SByte?),          MethodOf(m => m.ConvertToNullableSByte         (null)) },
				{ typeof(Int16?),          MethodOf(m => m.ConvertToNullableInt16         (null)) },
				{ typeof(Int32?),          MethodOf(m => m.ConvertToNullableInt32         (null)) },
				{ typeof(Int64?),          MethodOf(m => m.ConvertToNullableInt64         (null)) },
				{ typeof(Byte?),           MethodOf(m => m.ConvertToNullableByte          (null)) },
				{ typeof(UInt16?),         MethodOf(m => m.ConvertToNullableUInt16        (null)) },
				{ typeof(UInt32?),         MethodOf(m => m.ConvertToNullableUInt32        (null)) },
				{ typeof(UInt64?),         MethodOf(m => m.ConvertToNullableUInt64        (null)) },
				{ typeof(Char?),           MethodOf(m => m.ConvertToNullableChar          (null)) },
				{ typeof(Double?),         MethodOf(m => m.ConvertToNullableDouble        (null)) },
				{ typeof(Single?),         MethodOf(m => m.ConvertToNullableSingle        (null)) },
				{ typeof(Boolean?),        MethodOf(m => m.ConvertToNullableBoolean       (null)) },
				{ typeof(DateTime?),       MethodOf(m => m.ConvertToNullableDateTime      (null)) },
				{ typeof(TimeSpan?),       MethodOf(m => m.ConvertToNullableTimeSpan      (null)) },
				{ typeof(DateTimeOffset?), MethodOf(m => m.ConvertToNullableDateTimeOffset(null)) },
				{ typeof(Decimal?),        MethodOf(m => m.ConvertToNullableDecimal       (null)) },
				{ typeof(Guid?),           MethodOf(m => m.ConvertToNullableGuid          (null)) },

#if !SILVERLIGHT

				// SqlTypes
				//
				{ typeof(SqlByte),         MethodOf(m => m.ConvertToSqlByte               (null)) },
				{ typeof(SqlInt16),        MethodOf(m => m.ConvertToSqlInt16              (null)) },
				{ typeof(SqlInt32),        MethodOf(m => m.ConvertToSqlInt32              (null)) },
				{ typeof(SqlInt64),        MethodOf(m => m.ConvertToSqlInt64              (null)) },
				{ typeof(SqlSingle),       MethodOf(m => m.ConvertToSqlSingle             (null)) },
				{ typeof(SqlBoolean),      MethodOf(m => m.ConvertToSqlBoolean            (null)) },
				{ typeof(SqlDouble),       MethodOf(m => m.ConvertToSqlDouble             (null)) },
				{ typeof(SqlDateTime),     MethodOf(m => m.ConvertToSqlDateTime           (null)) },
				{ typeof(SqlDecimal),      MethodOf(m => m.ConvertToSqlDecimal            (null)) },
				{ typeof(SqlMoney),        MethodOf(m => m.ConvertToSqlMoney              (null)) },
				{ typeof(SqlString),       MethodOf(m => m.ConvertToSqlString             (null)) },
				{ typeof(SqlBinary),       MethodOf(m => m.ConvertToSqlBinary             (null)) },
				{ typeof(SqlGuid),         MethodOf(m => m.ConvertToSqlGuid               (null)) },
				{ typeof(SqlBytes),        MethodOf(m => m.ConvertToSqlBytes              (null)) },
				{ typeof(SqlChars),        MethodOf(m => m.ConvertToSqlChars              (null)) },
				{ typeof(SqlXml),          MethodOf(m => m.ConvertToSqlXml                (null)) },

#endif
			};
		}

		public class Functions
		{
			public class String : Expressor<string>
			{
#if !SILVERLIGHT
				public static MethodInfo Like11     = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", ""));
				public static MethodInfo Like12     = MethodOf(s => System.Data.Linq.SqlClient.SqlMethods.Like("", "", ' '));
#endif

				public static MethodInfo Like21     = MethodOf(s => Sql.Like(s, ""));
				public static MethodInfo Like22     = MethodOf(s => Sql.Like(s, "", ' '));
			}
		}
	}
}
