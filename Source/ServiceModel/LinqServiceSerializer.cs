using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Extensions;
	using Mapping;
	using SqlBuilder;

	static class LinqServiceSerializer
	{
		#region Public Members

		public static string Serialize(SqlQuery query, SqlParameter[] parameters)
		{
			return new QuerySerializer().Serialize(query, parameters);
		}

		public static LinqServiceQuery Deserialize(string str)
		{
			return new QueryDeserializer().Deserialize(str);
		}

		public static string Serialize(LinqServiceResult result)
		{
			return new ResultSerializer().Serialize(result);
		}

		public static LinqServiceResult DeserializeResult(string str)
		{
			return new ResultDeserializer().DeserializeResult(str);
		}

		public static string Serialize(string[] data)
		{
			return new StringArraySerializer().Serialize(data);
		}

		public static string[] DeserializeStringArray(string str)
		{
			return new StringArrayDeserializer().Deserialize(str);
		}

		#endregion

		#region SerializerBase

		const int ParamIndex     = -1;
		const int TypeIndex      = -2;
		const int TypeArrayIndex = -3;

		class SerializerBase
		{
			protected readonly StringBuilder          Builder = new StringBuilder();
			protected readonly Dictionary<object,int> Dic     = new Dictionary<object,int>();
			protected int                             Index;

			static string ConvertToString(Type type, object value)
			{
				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Decimal  : return ((decimal) value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.Double   : return ((double)  value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.Single   : return ((float)   value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.DateTime : return ((DateTime)value).ToString("o");
				}

				if (type == typeof(DateTimeOffset))
					return ((DateTimeOffset)value).ToString("o");

				return Common.Converter.ChangeTypeTo<string>(value);
			}

			protected void Append(Type type, object value)
			{
				Append(type);

				if (value == null)
					Append((string)null);
				else if (!type.IsArray)
				{
					Append(ConvertToString(type, value));
				}
				else
				{
					var elementType = type.GetElementType();

					Builder.Append(' ');

					var len = Builder.Length;
					var cnt = 0;

					if (elementType.IsArray)
						foreach (var val in (IEnumerable)value)
						{
							Append(elementType, val);
							cnt++;
						}
					else
						foreach (var val in (IEnumerable)value)
						{
							if (val == null)
								Append((string)null);
							else
								Append(ConvertToString(val.GetType(), val));
							cnt++;
						}

					Builder.Insert(len, cnt.ToString(CultureInfo.CurrentCulture));
				}
			}

			protected void Append(int value)
			{
				Builder.Append(' ').Append(value);
			}

			protected void Append(Type value)
			{
				Builder.Append(' ').Append(value == null ? 0 : GetType(value));
			}

			protected void Append(bool value)
			{
				Builder.Append(' ').Append(value ? '1' : '0');
			}

			protected void Append(IQueryElement element)
			{
				Builder.Append(' ').Append(element == null ? 0 : Dic[element]);
			}

			protected void Append(string str)
			{
				Builder.Append(' ');

				if (str == null)
				{
					Builder.Append('-');
				}
				else if (str.Length == 0)
				{
					Builder.Append('0');
				}
				else
				{
					Builder
						.Append(str.Length)
						.Append(':')
						.Append(str);
				}
			}

			protected int GetType(Type type)
			{
				if (type == null)
					return 0;

				int idx;

				if (!Dic.TryGetValue(type, out idx))
				{
					if (type.IsArray)
					{
						var elementType = GetType(type.GetElementType());

						Dic.Add(type, idx = ++Index);

						Builder
							.Append(idx)
							.Append(' ')
							.Append(TypeArrayIndex)
							.Append(' ')
							.Append(elementType);
					}
					else
					{
						Dic.Add(type, idx = ++Index);

						Builder
							.Append(idx)
							.Append(' ')
							.Append(TypeIndex);

						Append(type.FullName);
					}

					Builder.AppendLine();
				}

				return idx;
			}
		}

		#endregion

		#region DeserializerBase

		public class DeserializerBase
		{
			protected readonly Dictionary<int,object> Dic = new Dictionary<int,object>();

			protected string Str;
			protected int    Pos;

			protected char Peek()
			{
				return Str[Pos];
			}

			char Next()
			{
				return Str[++Pos];
			}

			protected bool Get(char c)
			{
				if (Peek() == c)
				{
					Pos++;
					return true;
				}

				return false;
			}

			protected int ReadInt()
			{
				Get(' ');

				var minus = Get('-');
				var value = 0;

				for (var c = Peek(); char.IsDigit(c); c = Next())
					value = value * 10 + ((int)c - '0');

				return minus ? -value : value;
			}

			protected int? ReadCount()
			{
				Get(' ');

				if (Get('-'))
					return null;

				var value = 0;

				for (var c = Peek(); char.IsDigit(c); c = Next())
					value = value * 10 + ((int)c - '0');

				return value;
			}

			protected string ReadString()
			{
				Get(' ');

				var c = Peek();

				if (c == '-')
				{
					Pos++;
					return null;
				}

				if (c == '0')
				{
					Pos++;
					return string.Empty;
				}

				var len   = ReadInt();
				var value = Str.Substring(++Pos, len);

				Pos += len;

				return value;
			}

			protected bool ReadBool()
			{
				Get(' ');

				var value = Peek() == '1';

				Pos++;

				return value;
			}

			protected T Read<T>()
				where T : class
			{
				var idx = ReadInt();
				return idx == 0 ? null : (T)Dic[idx];
			}

			protected T[] ReadArray<T>()
				where T : class
			{
				var count = ReadCount();

				if (count == null)
					return null;

				var items = new T[count.Value];

				for (var i = 0; i < count; i++)
					items[i] = Read<T>();

				return items;
			}

			protected List<T> ReadList<T>()
				where T : class
			{
				var count = ReadCount();

				if (count == null)
					return null;

				var items = new List<T>(count.Value);

				for (var i = 0; i < count; i++)
					items.Add(Read<T>());

				return items;
			}

			protected void NextLine()
			{
				while (Pos < Str.Length && (Peek() == '\n' || Peek() == '\r'))
					Pos++;
			}

			interface IDeserializerHelper
			{
				object GetArray(DeserializerBase deserializer);
			}

			class DeserializerHelper<T> : IDeserializerHelper
			{
				public object GetArray(DeserializerBase deserializer)
				{
					var count = deserializer.ReadCount();

					if (count == null)
						return null;

					var arr   = new T[count.Value];
					var type  = typeof(T);

					for (var i = 0; i < count.Value; i++)
						arr[i] = (T)deserializer.ReadValue(type);

					return arr;
				}
			}

			static readonly Dictionary<Type,Func<DeserializerBase,object>> _arrayDeserializers =
				new Dictionary<Type,Func<DeserializerBase,object>>();

			protected object ReadValue(Type type)
			{
				if (type == null)
					return ReadString();

				if (type.IsArray)
				{
					var elem = type.GetElementType();

					Func<DeserializerBase,object> deserializer;

					lock (_arrayDeserializers)
					{
						if (!_arrayDeserializers.TryGetValue(elem, out deserializer))
						{
							var helper = (IDeserializerHelper)Activator.CreateInstance(typeof(DeserializerHelper<>).MakeGenericType(elem));
							_arrayDeserializers.Add(elem, deserializer = helper.GetArray);
						}
					}

					return deserializer(this);
				}

				var str = ReadString();

				if (str == null)
					return null;

				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Decimal  : return decimal. Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.Double   : return double.  Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.Single   : return float.   Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.DateTime : return DateTime.ParseExact(str, "o", CultureInfo.InvariantCulture);
				}

				if (type == typeof(DateTimeOffset))
					return DateTimeOffset.ParseExact(str, "o", CultureInfo.InvariantCulture);

				return Common.Converter.ChangeType(str, type);
			}

			protected readonly List<string> UnresolvedTypes = new List<string>();

			protected Type ResolveType(string str)
			{
				if (str == null)
					return null;

				var type = Type.GetType(str, false);

				if (type == null)
				{
					if (str == "System.Data.Linq.Binary")
						return typeof(System.Data.Linq.Binary);

#if !SILVERLIGHT

					type = LinqService.TypeResolver(str);

#endif

					if (type == null)
					{
						UnresolvedTypes.Add(str);

						Debug.WriteLine(
							"Type '{0}' cannot be resolved. Use LinqService.TypeResolver to resolve unknown types.".Args(str),
							"LinqServiceSerializer");
					}
				}

				return type;
			}
		}

		#endregion

		#region QuerySerializer

		class QuerySerializer : SerializerBase
		{
			public string Serialize(SqlQuery query, SqlParameter[] parameters)
			{
				var visitor = new QueryVisitor();

				visitor.Visit(query, Visit);

				foreach (var parameter in parameters)
					if (!Dic.ContainsKey(parameter))
						Visit(parameter);

				Builder
					.Append(++Index)
					.Append(' ')
					.Append(ParamIndex);

				Append(parameters.Length);

				foreach (var parameter in parameters)
					Append(parameter);

				Builder.AppendLine();

				return Builder.ToString();
			}

			void Visit(IQueryElement e)
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var fld = (SqlField)e;

							if (fld != fld.Table.All)
								GetType(fld.SystemType);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var p = (SqlParameter)e;
							var v = p.Value;
							var t = v == null ? p.SystemType : v.GetType();

							if (v == null || t.IsArray || t == typeof(string) || !(v is IEnumerable))
							{
								GetType(t);
							}
							else
							{
								var elemType = t.GetItemType();
								GetType(GetArrayType(elemType));
							}

							//if (p.EnumTypes != null)
							//	foreach (var type in p.EnumTypes)
							//		GetType(type);

							break;
						}

					case QueryElementType.SqlFunction         : GetType(((SqlFunction)        e).SystemType); break;
					case QueryElementType.SqlExpression       : GetType(((SqlExpression)      e).SystemType); break;
					case QueryElementType.SqlBinaryExpression : GetType(((SqlBinaryExpression)e).SystemType); break;
					case QueryElementType.SqlDataType         : GetType(((SqlDataType)        e).Type);       break;
					case QueryElementType.SqlValue            : GetType(((SqlValue)           e).SystemType); break;
					case QueryElementType.SqlTable            : GetType(((SqlTable)           e).ObjectType); break;
				}

				Dic.Add(e, ++Index);

				Builder
					.Append(Index)
					.Append(' ')
					.Append((int)e.ElementType);

				switch (e.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var elem = (SqlField)e;

							Append(elem.SystemType);
							Append(elem.Name);
							Append(elem.PhysicalName);
							Append(elem.Nullable);
							Append(elem.IsPrimaryKey);
							Append(elem.PrimaryKeyOrder);
							Append(elem.IsIdentity);
							Append(elem.IsUpdatable);
							Append(elem.IsInsertable);

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var elem = (SqlFunction)e;

							Append(elem.SystemType);
							Append(elem.Name);
							Append(elem.Precedence);
							Append(elem.Parameters);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var elem = (SqlParameter)e;

							Append(elem.Name);
							Append(elem.IsQueryParameter);
							Append((int)elem.DbType);
							Append(elem.DbSize);
							Append(elem.LikeStart);
							Append(elem.LikeEnd);

							var value = elem.LikeStart != null ? elem.RawValue : elem.Value;
							var type  = value == null ? elem.SystemType : value.GetType();

							if (value == null || type.IsArray || type == typeof(string) || !(value is IEnumerable))
							{
								Append(type, value);
							}
							else
							{
								var elemType = type.GetItemType();

								value = ConvertIEnumerableToArray(value, elemType);

								Append(GetArrayType(elemType), value);
							}

							break;
						}

					case QueryElementType.SqlExpression :
						{
							var elem = (SqlExpression)e;

							Append(elem.SystemType);
							Append(elem.Expr);
							Append(elem.Precedence);
							Append(elem.Parameters);

							break;
						}

					case QueryElementType.SqlBinaryExpression :
						{
							var elem = (SqlBinaryExpression)e;

							Append(elem.SystemType);
							Append(elem.Expr1);
							Append(elem.Operation);
							Append(elem.Expr2);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.SqlValue :
						{
							var elem = (SqlValue)e;
							Append(elem.SystemType, elem.Value);
							break;
						}

					case QueryElementType.SqlDataType :
						{
							var elem = (SqlDataType)e;

							Append((int)elem.SqlDbType);
							Append(elem.Type);
							Append(elem.Length);
							Append(elem.Precision);
							Append(elem.Scale);

							break;
						}

					case QueryElementType.SqlTable :
						{
							var elem = (SqlTable)e;

							Append(elem.SourceID);
							Append(elem.Name);
							Append(elem.Alias);
							Append(elem.Database);
							Append(elem.Owner);
							Append(elem.PhysicalName);
							Append(elem.ObjectType);

							if (elem.SequenceAttributes == null)
								Builder.Append(" -");
							else
							{
								Append(elem.SequenceAttributes.Length);

								foreach (var a in elem.SequenceAttributes)
								{
									Append(a.Configuration);
									Append(a.SequenceName);
								}
							}

							Append(Dic[elem.All]);
							Append(elem.Fields.Count);

							foreach (var field in elem.Fields)
								Append(Dic[field.Value]);

							Append((int)elem.SqlTableType);

							if (elem.SqlTableType != SqlTableType.Table)
							{
								if (elem.TableArguments == null)
									Append(0);
								else
								{
									Append(elem.TableArguments.Length);

									foreach (var expr in elem.TableArguments)
										Append(Dic[expr]);
								}
							}

							break;
						}

					case QueryElementType.ExprPredicate :
						{
							var elem = (SqlQuery.Predicate.Expr)e;

							Append(elem.Expr1);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.NotExprPredicate :
						{
							var elem = (SqlQuery.Predicate.NotExpr)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var elem = (SqlQuery.Predicate.ExprExpr)e;

							Append(elem.Expr1);
							Append((int)elem.Operator);
							Append(elem.Expr2);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var elem = (SqlQuery.Predicate.Like)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Escape);

							break;
						}

					case QueryElementType.BetweenPredicate :
						{
							var elem = (SqlQuery.Predicate.Between)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Expr3);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var elem = (SqlQuery.Predicate.IsNull)e;

							Append(elem.Expr1);
							Append(elem.IsNot);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var elem = (SqlQuery.Predicate.InSubQuery)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.SubQuery);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var elem = (SqlQuery.Predicate.InList)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Values);

							break;
						}

					case QueryElementType.FuncLikePredicate :
						{
							var elem = (SqlQuery.Predicate.FuncLike)e;
							Append(elem.Function);
							break;
						}

					case QueryElementType.SqlQuery :
						{
							var elem = (SqlQuery)e;

							Append(elem.SourceID);
							Append((int)elem.QueryType);
							Append(elem.From);

							var appendInsert = false;
							var appendUpdate = false;
							var appendDelete = false;
							var appendSelect = false;

							switch (elem.QueryType)
							{
								case QueryType.InsertOrUpdate :
									appendUpdate = true;
									appendInsert = true;
									break;

								case QueryType.Update         :
									appendUpdate = true;
									break;

								case QueryType.Delete         :
									appendDelete = true;
									appendSelect = true;
									break;

								case QueryType.Insert         :
									appendInsert = true;
									if (elem.From.Tables.Count != 0)
										appendSelect = true;
									break;

								default                       :
									appendSelect = true;
									break;
							}

							Append(appendInsert); if (appendInsert) Append(elem.Insert);
							Append(appendUpdate); if (appendUpdate) Append(elem.Update);
							Append(appendDelete); if (appendDelete) Append(elem.Delete);
							Append(appendSelect); if (appendSelect) Append(elem.Select);

							Append(elem.Where);
							Append(elem.GroupBy);
							Append(elem.Having);
							Append(elem.OrderBy);
							Append(elem.ParentSql == null ? 0 : elem.ParentSql.SourceID);
							Append(elem.IsParameterDependent);

							if (!elem.HasUnion)
								Builder.Append(" -");
							else
								Append(elem.Unions);

							Append(elem.Parameters);

							if (Dic.ContainsKey(elem.All))
								Append(Dic[elem.All]);
							else
								Builder.Append(" -");

							break;
						}

					case QueryElementType.Column :
						{
							var elem = (SqlQuery.Column) e;

							Append(elem.Parent.SourceID);
							Append(elem.Expression);
							Append(elem._alias);

							break;
						}

					case QueryElementType.SearchCondition :
							Append(((SqlQuery.SearchCondition)e).Conditions);
							break;

					case QueryElementType.Condition :
						{
							var elem = (SqlQuery.Condition)e;

							Append(elem.IsNot);
							Append(elem.Predicate);
							Append(elem.IsOr);

							break;
						}

					case QueryElementType.TableSource :
						{
							var elem = (SqlQuery.TableSource)e;

							Append(elem.Source);
							Append(elem._alias);
							Append(elem.Joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var elem = (SqlQuery.JoinedTable)e;

							Append((int)elem.JoinType);
							Append(elem.Table);
							Append(elem.IsWeak);
							Append(elem.Condition);

							break;
						}

					case QueryElementType.SelectClause :
						{
							var elem = (SqlQuery.SelectClause)e;

							Append(elem.IsDistinct);
							Append(elem.SkipValue);
							Append(elem.TakeValue);
							Append(elem.Columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var elem = (SqlQuery.InsertClause)e;

							Append(elem.Items);
							Append(elem.Into);
							Append(elem.WithIdentity);

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var elem = (SqlQuery.UpdateClause)e;

							Append(elem.Items);
							Append(elem.Keys);
							Append(elem.Table);

							break;
						}

					case QueryElementType.DeleteClause :
						{
							var elem = (SqlQuery.DeleteClause)e;
							Append(elem.Table);
							break;
						}

					case QueryElementType.SetExpression :
						{
							var elem = (SqlQuery.SetExpression)e;

							Append(elem.Column);
							Append(elem.Expression);

							break;
						}

					case QueryElementType.FromClause    : Append(((SqlQuery.FromClause)   e).Tables);          break;
					case QueryElementType.WhereClause   : Append(((SqlQuery.WhereClause)  e).SearchCondition); break;
					case QueryElementType.GroupByClause : Append(((SqlQuery.GroupByClause)e).Items);           break;
					case QueryElementType.OrderByClause : Append(((SqlQuery.OrderByClause)e).Items);           break;

					case QueryElementType.OrderByItem :
						{
							var elem = (SqlQuery.OrderByItem)e;

							Append(elem.Expression);
							Append(elem.IsDescending);

							break;
						}

					case QueryElementType.Union :
						{
							var elem = (SqlQuery.Union)e;

							Append(elem.SqlQuery);
							Append(elem.IsAll);

							break;
						}
				}

				Builder.AppendLine();
			}

			void Append<T>(ICollection<T> exprs)
				where T : IQueryElement
			{
				if (exprs == null)
					Builder.Append(" -");
				else
				{
					Append(exprs.Count);

					foreach (var e in exprs)
						Append(Dic[e]);
				}
			}
		}

		#endregion

		#region QueryDeserializer

		public class QueryDeserializer : DeserializerBase
		{
			SqlQuery       _query;
			SqlParameter[] _parameters;

			readonly Dictionary<int,SqlQuery> _queries = new Dictionary<int,SqlQuery>();
			readonly List<Action>             _actions = new List<Action>();

			public LinqServiceQuery Deserialize(string str)
			{
				Str = str;

				while (Parse()) {}

				foreach (var action in _actions)
					action();

				return new LinqServiceQuery { Query = _query, Parameters = _parameters };
			}

			bool Parse()
			{
				NextLine();

				if (Pos >= Str.Length)
					return false;

				var obj  = null as object;
				var idx  = ReadInt(); Pos++;
				var type = ReadInt(); Pos++;

				switch ((QueryElementType)type)
				{
					case (QueryElementType)ParamIndex     : obj = _parameters = ReadArray<SqlParameter>(); break;
					case (QueryElementType)TypeIndex      : obj = ResolveType(ReadString());               break;
					case (QueryElementType)TypeArrayIndex : obj = GetArrayType(Read<Type>());              break;

					case QueryElementType.SqlField :
						{
							var systemType       = Read<Type>();
							var name             = ReadString();
							var physicalName     = ReadString();
							var nullable         = ReadBool();
							var isPrimaryKey     = ReadBool();
							var primaryKeyOrder  = ReadInt();
							var isIdentity       = ReadBool();
							var isUpdatable      = ReadBool();
							var isInsertable     = ReadBool();

							obj = new SqlField
							{
								SystemType      = systemType,
								Name            = name,
								PhysicalName    = physicalName,
								Nullable        = nullable,
								IsPrimaryKey    = isPrimaryKey,
								PrimaryKeyOrder = primaryKeyOrder,
								IsIdentity      = isIdentity,
								IsInsertable    = isInsertable,
								IsUpdatable     = isUpdatable,
							};

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var systemType = Read<Type>();
							var name       = ReadString();
							var precedence = ReadInt();
							var parameters = ReadArray<ISqlExpression>();

							obj = new SqlFunction(systemType, name, precedence, parameters);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var name             = ReadString();
							var isQueryParameter = ReadBool();
							var dbType           = (DbType)ReadInt();
							var dbSize           = ReadInt();
							var likeStart = ReadString();
							var likeEnd   = ReadString();

							var systemType       = Read<Type>();
							var value            = ReadValue(systemType);
							//var enumTypes        = ReadList<Type>();
							//var takeValues       = null as List<int>;

							/*
							var count = ReadCount();

							if (count != null)
							{
								takeValues = new List<int>(count.Value);

								for (var i = 0; i < count; i++)
									takeValues.Add(ReadInt());
							}
							*/

							obj = new SqlParameter(systemType, name, value)
							{
								IsQueryParameter = isQueryParameter,
								DbType           = dbType,
								DbSize           = dbSize,
								//EnumTypes        = enumTypes,
								//TakeValues       = takeValues,
								LikeStart        = likeStart,
								LikeEnd          = likeEnd,
							};

							/*
							if (enumTypes != null && UnresolvedTypes.Count > 0)
								foreach (var et in enumTypes)
									if (et == null)
										throw new LinqException(
											"Query cannot be deserialized. The possible reason is that the deserializer could not resolve the following types: {0}. Use LinqService.TypeResolver to resolve types.",
											string.Join(", ", UnresolvedTypes.Select(_ => "'" + _ + "'").ToArray()));
							*/

							break;
						}

					case QueryElementType.SqlExpression :
						{
							var systemType = Read<Type>();
							var expr       = ReadString();
							var precedence = ReadInt();
							var parameters = ReadArray<ISqlExpression>();

							obj = new SqlExpression(systemType, expr, precedence, parameters);

							break;
						}

					case QueryElementType.SqlBinaryExpression :
						{
							var systemType = Read<Type>();
							var expr1      = Read<ISqlExpression>();
							var operation  = ReadString();
							var expr2      = Read<ISqlExpression>();
							var precedence = ReadInt();

							obj = new SqlBinaryExpression(systemType, expr1, operation, expr2, precedence);

							break;
						}

					case QueryElementType.SqlValue :
						{
							var systemType = Read<Type>();
							var value      = ReadValue(systemType);

							obj = new SqlValue(systemType, value);

							break;
						}

					case QueryElementType.SqlDataType :
						{
							var dbType     = (SqlDbType)ReadInt();
							var systemType = Read<Type>();
							var length     = ReadInt();
							var precision  = ReadInt();
							var scale      = ReadInt();

							obj = new SqlDataType(dbType, systemType, length, precision, scale);

							break;
						}

					case QueryElementType.SqlTable :
						{
							var sourceID           = ReadInt();
							var name               = ReadString();
							var alias              = ReadString();
							var database           = ReadString();
							var owner              = ReadString();
							var physicalName       = ReadString();
							var objectType         = Read<Type>();
							var sequenceAttributes = null as SequenceNameAttribute[];

							var count = ReadCount();

							if (count != null)
							{
								sequenceAttributes = new SequenceNameAttribute[count.Value];

								for (var i = 0; i < count.Value; i++)
									sequenceAttributes[i] = new SequenceNameAttribute(ReadString(), ReadString());
							}

							var all    = Read<SqlField>();
							var fields = ReadArray<SqlField>();
							var flds   = new SqlField[fields.Length + 1];

							flds[0] = all;
							Array.Copy(fields, 0, flds, 1, fields.Length);

							var sqlTableType = (SqlTableType)ReadInt();
							var tableArgs    = sqlTableType == SqlTableType.Table ? null : ReadArray<ISqlExpression>();

							obj = new SqlTable(
								sourceID, name, alias, database, owner, physicalName, objectType, sequenceAttributes, flds,
								sqlTableType, tableArgs);

							break;
						}

					case QueryElementType.ExprPredicate :
						{
							var expr1      = Read<ISqlExpression>();
							var precedence = ReadInt();

							obj = new SqlQuery.Predicate.Expr(expr1, precedence);

							break;
						}

					case QueryElementType.NotExprPredicate :
						{
							var expr1      = Read<ISqlExpression>();
							var isNot      = ReadBool();
							var precedence = ReadInt();

							obj = new SqlQuery.Predicate.NotExpr(expr1, isNot, precedence);

							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var expr1     = Read<ISqlExpression>();
							var @operator = (SqlQuery.Predicate.Operator)ReadInt();
							var expr2     = Read<ISqlExpression>();

							obj = new SqlQuery.Predicate.ExprExpr(expr1, @operator, expr2);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var expr1  = Read<ISqlExpression>();
							var isNot  = ReadBool();
							var expr2  = Read<ISqlExpression>();
							var escape = Read<ISqlExpression>();

							obj = new SqlQuery.Predicate.Like(expr1, isNot, expr2, escape);

							break;
						}

					case QueryElementType.BetweenPredicate :
						{
							var expr1 = Read<ISqlExpression>();
							var isNot = ReadBool();
							var expr2 = Read<ISqlExpression>();
							var expr3 = Read<ISqlExpression>();

							obj = new SqlQuery.Predicate.Between(expr1, isNot, expr2, expr3);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var expr1 = Read<ISqlExpression>();
							var isNot = ReadBool();

							obj = new SqlQuery.Predicate.IsNull(expr1, isNot);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var expr1    = Read<ISqlExpression>();
							var isNot    = ReadBool();
							var subQuery = Read<SqlQuery>();

							obj = new SqlQuery.Predicate.InSubQuery(expr1, isNot, subQuery);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var expr1  = Read<ISqlExpression>();
							var isNot  = ReadBool();
							var values = ReadList<ISqlExpression>();

							obj = new SqlQuery.Predicate.InList(expr1, isNot, values);

							break;
						}

					case QueryElementType.FuncLikePredicate :
						{
							var func = Read<SqlFunction>();
							obj = new SqlQuery.Predicate.FuncLike(func);
							break;
						}

					case QueryElementType.SqlQuery :
						{
							var sid                = ReadInt();
							var queryType          = (QueryType)ReadInt();
							var from               = Read<SqlQuery.FromClause>();
							var readInsert         = ReadBool();
							var insert             = readInsert ? Read<SqlQuery.InsertClause>() : null;
							var readUpdate         = ReadBool();
							var update             = readUpdate ? Read<SqlQuery.UpdateClause>() : null;
							var readDelete         = ReadBool();
							var delete             = readDelete ? Read<SqlQuery.DeleteClause>() : null;
							var readSelect         = ReadBool();
							var select             = readSelect ? Read<SqlQuery.SelectClause>() : new SqlQuery.SelectClause(null);
							var where              = Read<SqlQuery.WhereClause>();
							var groupBy            = Read<SqlQuery.GroupByClause>();
							var having             = Read<SqlQuery.WhereClause>();
							var orderBy            = Read<SqlQuery.OrderByClause>();
							var parentSql          = ReadInt();
							var parameterDependent = ReadBool();
							var unions             = ReadArray<SqlQuery.Union>();
							var parameters         = ReadArray<SqlParameter>();

							var query = _query = new SqlQuery(sid) { QueryType = queryType };

							query.Init(
								insert,
								update,
								delete,
								select,
								from,
								where,
								groupBy,
								having,
								orderBy,
								unions == null ? null : unions.ToList(),
								null,
								parameterDependent,
								parameters.ToList());

							_queries.Add(sid, _query);

							if (parentSql != 0)
								_actions.Add(() =>
								{
									SqlQuery sql;
									if (_queries.TryGetValue(parentSql, out sql))
										query.ParentSql = sql;
								});

							query.All = Read<SqlField>();

							obj = query;

							break;
						}

					case QueryElementType.Column :
						{
							var sid        = ReadInt();
							var expression = Read<ISqlExpression>();
							var alias      = ReadString();

							var col = new SqlQuery.Column(null, expression, alias);

							_actions.Add(() => { col.Parent = _queries[sid]; return; });

							obj = col;

							break;
						}

					case QueryElementType.SearchCondition :
						obj = new SqlQuery.SearchCondition(ReadArray<SqlQuery.Condition>());
						break;

					case QueryElementType.Condition :
						obj = new SqlQuery.Condition(ReadBool(), Read<ISqlPredicate>(), ReadBool());
						break;

					case QueryElementType.TableSource :
						{
							var source = Read<ISqlTableSource>();
							var alias  = ReadString();
							var joins  = ReadArray<SqlQuery.JoinedTable>();

							obj = new SqlQuery.TableSource(source, alias, joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var joinType  = (SqlQuery.JoinType)ReadInt();
							var table     = Read<SqlQuery.TableSource>();
							var isWeak    = ReadBool();
							var condition = Read<SqlQuery.SearchCondition>();

							obj = new SqlQuery.JoinedTable(joinType, table, isWeak, condition);

							break;
						}

					case QueryElementType.SelectClause :
						{
							var isDistinct = ReadBool();
							var skipValue  = Read<ISqlExpression>();
							var takeValue  = Read<ISqlExpression>();
							var columns    = ReadArray<SqlQuery.Column>();

							obj = new SqlQuery.SelectClause(isDistinct, takeValue, skipValue, columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var items = ReadArray<SqlQuery.SetExpression>();
							var into  = Read<SqlTable>();
							var wid   = ReadBool();

							var c = new SqlQuery.InsertClause { Into = into, WithIdentity = wid };

							c.Items.AddRange(items);
							obj = c;

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var items = ReadArray<SqlQuery.SetExpression>();
							var keys  = ReadArray<SqlQuery.SetExpression>();
							var table = Read<SqlTable>();
							//var wid   = ReadBool();

							var c = new SqlQuery.UpdateClause { Table = table };

							c.Items.AddRange(items);
							c.Keys. AddRange(keys);
							obj = c;

							break;
						}

					case QueryElementType.DeleteClause :
						{
							var table = Read<SqlTable>();
							obj = new SqlQuery.DeleteClause { Table = table };
							break;
						}

					case QueryElementType.SetExpression : obj = new SqlQuery.SetExpression(Read<ISqlExpression>(), Read<ISqlExpression>()); break;
					case QueryElementType.FromClause    : obj = new SqlQuery.FromClause(ReadArray<SqlQuery.TableSource>());                 break;
					case QueryElementType.WhereClause   : obj = new SqlQuery.WhereClause(Read<SqlQuery.SearchCondition>());                 break;
					case QueryElementType.GroupByClause : obj = new SqlQuery.GroupByClause(ReadArray<ISqlExpression>());                    break;
					case QueryElementType.OrderByClause : obj = new SqlQuery.OrderByClause(ReadArray<SqlQuery.OrderByItem>());              break;

					case QueryElementType.OrderByItem :
						{
							var expression   = Read<ISqlExpression>();
							var isDescending = ReadBool();

							obj = new SqlQuery.OrderByItem(expression, isDescending);

							break;
						}

					case QueryElementType.Union :
						{
							var sqlQuery = Read<SqlQuery>();
							var isAll    = ReadBool();

							obj = new SqlQuery.Union(sqlQuery, isAll);

							break;
						}
				}

				Dic.Add(idx, obj);

				return true;
			}
		}

		#endregion

		#region ResultSerializer

		class ResultSerializer : SerializerBase
		{
			public string Serialize(LinqServiceResult result)
			{
				Append(result.FieldCount);
				Append(result.VaryingTypes.Length);
				Append(result.RowCount);
				Append(result.QueryID.ToString());

				Builder.AppendLine();

				foreach (var name in result.FieldNames)
				{
					Append(name);
					Builder.AppendLine();
				}

				foreach (var type in result.FieldTypes)
				{
					Append(type.FullName);
					Builder.AppendLine();
				}

				foreach (var type in result.VaryingTypes)
				{
					Append(type.FullName);
					Builder.AppendLine();
				}

				foreach (var data in result.Data)
				{
					foreach (var str in data)
					{
						if (result.VaryingTypes.Length > 0 && !string.IsNullOrEmpty(str) && str[0] == '\0')
						{
							Builder.Append('*');
							Append((int)str[1]);
							Append(str.Substring(2));
						}
						else
							Append(str);
					}

					Builder.AppendLine();
				}

				return Builder.ToString();
			}
		}

		#endregion

		#region ResultDeserializer

		class ResultDeserializer : DeserializerBase
		{
			public LinqServiceResult DeserializeResult(string str)
			{
				Str = str;

				var fieldCount  = ReadInt();
				var varTypesLen = ReadInt();

				var result = new LinqServiceResult
				{
					FieldCount   = fieldCount,
					RowCount     = ReadInt(),
					VaryingTypes = new Type[varTypesLen],
					QueryID      = new Guid(ReadString()),
					FieldNames   = new string[fieldCount],
					FieldTypes   = new Type  [fieldCount],
					Data         = new List<string[]>(),
				};

				NextLine();

				for (var i = 0; i < fieldCount;  i++) { result.FieldNames  [i] = ReadString();              NextLine(); }
				for (var i = 0; i < fieldCount;  i++) { result.FieldTypes  [i] = ResolveType(ReadString()); NextLine(); }
				for (var i = 0; i < varTypesLen; i++) { result.VaryingTypes[i] = ResolveType(ReadString()); NextLine(); }

				for (var n = 0; n < result.RowCount; n++)
				{
					var data = new string[fieldCount];

					for (var i = 0; i < fieldCount; i++)
					{
						if (varTypesLen > 0)
						{
							Get(' ');

							if (Get('*'))
							{
								var idx = ReadInt();
								data[i] = "\0" + (char)idx + ReadString();
							}
							else
								data[i] = ReadString();
						}
						else
							data[i] = ReadString();
					}

					result.Data.Add(data);

					NextLine();
				}

				return result;
			}
		}

		#endregion

		#region StringArraySerializer

		class StringArraySerializer : SerializerBase
		{
			public string Serialize(string[] data)
			{
				Append(data.Length);

				foreach (var str in data)
					Append(str);

				Builder.AppendLine();

				return Builder.ToString();
			}
		}

		#endregion

		#region StringArrayDeserializer

		class StringArrayDeserializer : DeserializerBase
		{
			public string[] Deserialize(string str)
			{
				Str = str;

				var data = new string[ReadInt()];

				for (var i = 0; i < data.Length; i++)
					data[i] = ReadString();

				return data;
			}
		}

		#endregion

		#region Helpers

		interface IArrayHelper
		{
			Type   GetArrayType();
			object ConvertToArray(object list);
		}

		class ArrayHelper<T> : IArrayHelper
		{
			public Type GetArrayType()
			{
				return typeof(T[]);
			}

			public object ConvertToArray(object list)
			{
				return ((IEnumerable<T>)list).ToArray();
			}
		}

		static readonly Dictionary<Type,Type>                _arrayTypes      = new Dictionary<Type,Type>();
		static readonly Dictionary<Type,Func<object,object>> _arrayConverters = new Dictionary<Type,Func<object,object>>();

		static Type GetArrayType(Type elementType)
		{
			Type arrayType;

			lock (_arrayTypes)
			{
				if (!_arrayTypes.TryGetValue(elementType, out arrayType))
				{
					var helper = (IArrayHelper)Activator.CreateInstance(typeof(ArrayHelper<>).MakeGenericType(elementType));
					_arrayTypes.Add(elementType, arrayType = helper.GetArrayType());
				}
			}

			return arrayType;
		}

		static object ConvertIEnumerableToArray(object list, Type elementType)
		{
			Func<object,object> converter;

			lock (_arrayConverters)
			{
				if (!_arrayConverters.TryGetValue(elementType, out converter))
				{
					var helper = (IArrayHelper)Activator.CreateInstance(typeof(ArrayHelper<>).MakeGenericType(elementType));
					_arrayConverters.Add(elementType, converter = helper.ConvertToArray);
				}
			}

			return converter(list);
		}

		#endregion
	}
}
