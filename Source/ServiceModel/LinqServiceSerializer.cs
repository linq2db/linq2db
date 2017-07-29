﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LinqToDB.ServiceModel
{
	using Common;
	using Extensions;
	using Mapping;
	using SqlQuery;

	static class LinqServiceSerializer
	{
		#region Public Members

		public static string Serialize(SelectQuery query, SqlParameter[] parameters, List<string> queryHints)
		{
			return new QuerySerializer().Serialize(query, parameters, queryHints);
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
				switch (type.GetTypeCodeEx())
				{
					case TypeCode.Decimal  : return ((decimal) value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.Double   : return ((double)  value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.Single   : return ((float)   value).ToString(CultureInfo.InvariantCulture);
					case TypeCode.DateTime : return ((DateTime)value).ToString("o");
				}

				if (type == typeof(DateTimeOffset))
					return ((DateTimeOffset)value).ToString("o");

				return Converter.ChangeTypeTo<string>(value);
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
								Append(elementType, null);
							else
								Append(val.GetType(), val); //Append(ConvertToString(val.GetType(), val));
							cnt++;
						}

					Builder.Insert(len, cnt.ToString(CultureInfo.CurrentCulture));
				}
			}

			protected void Append(int value)
			{
				Builder.Append(' ').Append(value);
			}

			protected void Append(int? value)
			{
				Builder.Append(' ').Append(value.HasValue ? '1' : '0');

				if (value.HasValue)
					Builder.Append(value.Value);
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

						Append(Configuration.LinqService.SerializeAssemblyQualifiedName ? type.AssemblyQualifiedName : type.FullName);
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

			protected int? ReadNullableInt()
			{
				Get(' ');

				if (Get('0'))
					return null;

				Get('1');

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

			protected Type ReadType()
			{
				var idx = ReadInt();
				object type;
				if (!Dic.TryGetValue(idx, out type))
				{
					Pos++;
					var typecode = ReadInt();
					Pos++;

					switch (typecode)
					{
						case TypeIndex     : type = ResolveType(ReadString());  break;
						case TypeArrayIndex: type = GetArrayType(Read<Type>()); break;

						default:
							throw new SerializationException("TypeIndex or TypeArrayIndex ({0} or {1}) expected, but was {2}".Args(TypeIndex, TypeArrayIndex, typecode));
					}

					
					Dic.Add(idx, type);

					NextLine();

					var idx2 = ReadInt();
					if (idx2 != idx)
						throw new SerializationException("Wrong type reading, expected index is {0} but was {1}".Args(idx, idx2));
				}

				return (Type) type;
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

					for (var i = 0; i < count.Value; i++)
					{
						var elementType = deserializer.ReadType();
						arr[i] = (T) deserializer.ReadValue(elementType);
					}

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

				switch (type.GetTypeCodeEx())
				{
					case TypeCode.Decimal  : return decimal. Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.Double   : return double.  Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.Single   : return float.   Parse(str, CultureInfo.InvariantCulture);
					case TypeCode.DateTime : return DateTime.ParseExact(str, "o", CultureInfo.InvariantCulture);
				}

				if (type == typeof(DateTimeOffset))
					return DateTimeOffset.ParseExact(str, "o", CultureInfo.InvariantCulture);

				return Converter.ChangeType(str, type);
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

#if !SILVERLIGHT && !NETFX_CORE

					type = LinqService.TypeResolver(str);

#endif

					if (type == null)
					{
						if (Configuration.LinqService.ThrowUnresolvedTypeException)
							throw new LinqToDBException("Type '{0}' cannot be resolved. Use LinqService.TypeResolver to resolve unknown types.".Args(str));

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
			public string Serialize(SelectQuery query, SqlParameter[] parameters, List<string> queryHints)
			{
				var queryHintCount = queryHints == null ? 0 : queryHints.Count;

				Builder.AppendLine(queryHintCount.ToString());

				if (queryHintCount > 0)
					foreach (var hint in queryHints)
						Builder.AppendLine(hint);

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
							Append(elem.CanBeNull);
							Append(elem.IsPrimaryKey);
							Append(elem.PrimaryKeyOrder);
							Append(elem.IsIdentity);
							Append(elem.IsUpdatable);
							Append(elem.IsInsertable);
							Append((int)elem.DataType);
							Append(elem.DbType);
							Append(elem.Length);
							Append(elem.Precision);
							Append(elem.Scale);
							Append(elem.CreateFormat);

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var elem = (SqlFunction)e;

							Append(elem.SystemType);
							Append(elem.Name);
							Append(elem.IsAggregate);
							Append(elem.Precedence);
							Append(elem.Parameters);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var elem = (SqlParameter)e;

							Append(elem.Name);
							Append(elem.IsQueryParameter);
							Append((int)elem.DataType);
							Append(elem.DbSize);
							Append(elem.LikeStart);
							Append(elem.LikeEnd);
							Append(elem.ReplaceLike);

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

							Append((int)elem.DataType);
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

							if (elem.SequenceAttributes.IsNullOrEmpty())
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
							var elem = (SelectQuery.Predicate.Expr)e;

							Append(elem.Expr1);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.NotExprPredicate :
						{
							var elem = (SelectQuery.Predicate.NotExpr)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var elem = (SelectQuery.Predicate.ExprExpr)e;

							Append(elem.Expr1);
							Append((int)elem.Operator);
							Append(elem.Expr2);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var elem = (SelectQuery.Predicate.Like)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Escape);

							break;
						}

					case QueryElementType.BetweenPredicate :
						{
							var elem = (SelectQuery.Predicate.Between)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Expr3);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var elem = (SelectQuery.Predicate.IsNull)e;

							Append(elem.Expr1);
							Append(elem.IsNot);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var elem = (SelectQuery.Predicate.InSubQuery)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.SubQuery);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var elem = (SelectQuery.Predicate.InList)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Values);

							break;
						}

					case QueryElementType.FuncLikePredicate :
						{
							var elem = (SelectQuery.Predicate.FuncLike)e;
							Append(elem.Function);
							break;
						}

					case QueryElementType.SqlQuery :
						{
							var elem = (SelectQuery)e;

							Append(elem.SourceID);
							Append((int)elem.QueryType);
							Append(elem.From);

							var appendInsert      = false;
							var appendUpdate      = false;
							var appendDelete      = false;
							var appendSelect      = false;
							var appendCreateTable = false;

							switch (elem.QueryType)
							{
								case QueryType.InsertOrUpdate :
									appendUpdate = true;
									appendInsert = true;
									break;

								case QueryType.Update         :
									appendUpdate = true;
									appendSelect = elem.Select.SkipValue != null || elem.Select.TakeValue != null;
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

								case QueryType.CreateTable    :
									appendCreateTable = true;
									break;

								default                       :
									appendSelect = true;
									break;
							}

							Append(appendInsert);      if (appendInsert)      Append(elem.Insert);
							Append(appendUpdate);      if (appendUpdate)      Append(elem.Update);
							Append(appendDelete);      if (appendDelete)      Append(elem.Delete);
							Append(appendSelect);      if (appendSelect)      Append(elem.Select);
							Append(appendCreateTable); if (appendCreateTable) Append(elem.CreateTable);

							Append(elem.Where);
							Append(elem.GroupBy);
							Append(elem.Having);
							Append(elem.OrderBy);
							Append(elem.ParentSelect == null ? 0 : elem.ParentSelect.SourceID);
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
							var elem = (SelectQuery.Column) e;

							Append(elem.Parent.SourceID);
							Append(elem.Expression);
							Append(elem._alias);

							break;
						}

					case QueryElementType.SearchCondition :
						{
							Append(((SelectQuery.SearchCondition)e).Conditions);
							break;
						}

					case QueryElementType.Condition :
						{
							var elem = (SelectQuery.Condition)e;

							Append(elem.IsNot);
							Append(elem.Predicate);
							Append(elem.IsOr);

							break;
						}

					case QueryElementType.TableSource :
						{
							var elem = (SelectQuery.TableSource)e;

							Append(elem.Source);
							Append(elem._alias);
							Append(elem.Joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var elem = (SelectQuery.JoinedTable)e;

							Append((int)elem.JoinType);
							Append(elem.Table);
							Append(elem.IsWeak);
							Append(elem.Condition);

							break;
						}

					case QueryElementType.SelectClause :
						{
							var elem = (SelectQuery.SelectClause)e;

							Append(elem.IsDistinct);
							Append(elem.SkipValue);
							Append(elem.TakeValue);
							Append(elem.Columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var elem = (SelectQuery.InsertClause)e;

							Append(elem.Items);
							Append(elem.Into);
							Append(elem.WithIdentity);

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var elem = (SelectQuery.UpdateClause)e;

							Append(elem.Items);
							Append(elem.Keys);
							Append(elem.Table);

							break;
						}

					case QueryElementType.DeleteClause :
						{
							Append(((SelectQuery.DeleteClause)e).Table);
							break;
						}

					case QueryElementType.SetExpression :
						{
							var elem = (SelectQuery.SetExpression)e;

							Append(elem.Column);
							Append(elem.Expression);

							break;
						}

					case QueryElementType.CreateTableStatement :
						{
							var elem = (SelectQuery.CreateTableStatement)e;

							Append(elem.Table);
							Append(elem.IsDrop);
							Append(elem.StatementHeader);
							Append(elem.StatementFooter);
							Append((int)elem.DefaulNullable);

							break;
						}

					case QueryElementType.FromClause    : Append(((SelectQuery.FromClause)   e).Tables);          break;
					case QueryElementType.WhereClause   : Append(((SelectQuery.WhereClause)  e).SearchCondition); break;
					case QueryElementType.GroupByClause : Append(((SelectQuery.GroupByClause)e).Items);           break;
					case QueryElementType.OrderByClause : Append(((SelectQuery.OrderByClause)e).Items);           break;

					case QueryElementType.OrderByItem :
						{
							var elem = (SelectQuery.OrderByItem)e;

							Append(elem.Expression);
							Append(elem.IsDescending);

							break;
						}

					case QueryElementType.Union :
						{
							var elem = (SelectQuery.Union)e;

							Append(elem.SelectQuery);
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
			SelectQuery    _query;
			SqlParameter[] _parameters;

			readonly Dictionary<int,SelectQuery> _queries = new Dictionary<int,SelectQuery>();
			readonly List<Action>                _actions = new List<Action>();

			public LinqServiceQuery Deserialize(string str)
			{
				Str = str;

				List<string> queryHints = null;

				var queryHintCount = ReadInt();

				NextLine();

				if (queryHintCount > 0)
				{
					queryHints = new List<string>();

					for (var i = 0; i < queryHintCount; i++)
					{
						var pos = Pos;

						while (Pos < Str.Length && Peek() != '\n' && Peek() != '\r')
							Pos++;

						queryHints.Add(Str.Substring(pos, Pos - pos));

						NextLine();
					}
				}

				while (Parse()) {}

				foreach (var action in _actions)
					action();

				return new LinqServiceQuery { Query = _query, Parameters = _parameters, QueryHints = queryHints };
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
							var dataType         = ReadInt();
							var dbType           = ReadString();
							var length           = ReadNullableInt();
							var precision        = ReadNullableInt();
							var scale            = ReadNullableInt();
							var createFormat     = ReadString();

							obj = new SqlField
							{
								SystemType      = systemType,
								Name            = name,
								PhysicalName    = physicalName,
								CanBeNull       = nullable,
								IsPrimaryKey    = isPrimaryKey,
								PrimaryKeyOrder = primaryKeyOrder,
								IsIdentity      = isIdentity,
								IsInsertable    = isInsertable,
								IsUpdatable     = isUpdatable,
								DataType        = (DataType)dataType,
								DbType          = dbType,
								Length          = length,
								Precision       = precision,
								Scale           = scale,
								CreateFormat    = createFormat,
							};

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var systemType  = Read<Type>();
							var name        = ReadString();
							var isAggregate = ReadBool();
							var precedence  = ReadInt();
							var parameters  = ReadArray<ISqlExpression>();

							obj = new SqlFunction(systemType, name, isAggregate, precedence, parameters);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var name             = ReadString();
							var isQueryParameter = ReadBool();
							var dbType           = (DataType)ReadInt();
							var dbSize           = ReadInt();
							var likeStart        = ReadString();
							var likeEnd          = ReadString();
							var replaceLike      = ReadBool();

							var systemType       = Read<Type>();
							var value            = ReadValue(systemType);

							obj = new SqlParameter(systemType, name, value)
							{
								IsQueryParameter = isQueryParameter,
								DataType         = dbType,
								DbSize           = dbSize,
								LikeStart        = likeStart,
								LikeEnd          = likeEnd,
								ReplaceLike      = replaceLike,
							};

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
							var dbType     = (DataType)ReadInt();
							var systemType = Read<Type>();
							var length     = ReadNullableInt();
							var precision  = ReadNullableInt();
							var scale      = ReadNullableInt();

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

							obj = new SelectQuery.Predicate.Expr(expr1, precedence);

							break;
						}

					case QueryElementType.NotExprPredicate :
						{
							var expr1      = Read<ISqlExpression>();
							var isNot      = ReadBool();
							var precedence = ReadInt();

							obj = new SelectQuery.Predicate.NotExpr(expr1, isNot, precedence);

							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var expr1     = Read<ISqlExpression>();
							var @operator = (SelectQuery.Predicate.Operator)ReadInt();
							var expr2     = Read<ISqlExpression>();

							obj = new SelectQuery.Predicate.ExprExpr(expr1, @operator, expr2);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var expr1  = Read<ISqlExpression>();
							var isNot  = ReadBool();
							var expr2  = Read<ISqlExpression>();
							var escape = Read<ISqlExpression>();

							obj = new SelectQuery.Predicate.Like(expr1, isNot, expr2, escape);

							break;
						}

					case QueryElementType.BetweenPredicate :
						{
							var expr1 = Read<ISqlExpression>();
							var isNot = ReadBool();
							var expr2 = Read<ISqlExpression>();
							var expr3 = Read<ISqlExpression>();

							obj = new SelectQuery.Predicate.Between(expr1, isNot, expr2, expr3);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var expr1 = Read<ISqlExpression>();
							var isNot = ReadBool();

							obj = new SelectQuery.Predicate.IsNull(expr1, isNot);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var expr1    = Read<ISqlExpression>();
							var isNot    = ReadBool();
							var subQuery = Read<SelectQuery>();

							obj = new SelectQuery.Predicate.InSubQuery(expr1, isNot, subQuery);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var expr1  = Read<ISqlExpression>();
							var isNot  = ReadBool();
							var values = ReadList<ISqlExpression>();

							obj = new SelectQuery.Predicate.InList(expr1, isNot, values);

							break;
						}

					case QueryElementType.FuncLikePredicate :
						{
							var func = Read<SqlFunction>();
							obj = new SelectQuery.Predicate.FuncLike(func);
							break;
						}

					case QueryElementType.SqlQuery :
						{
							var sid                = ReadInt();
							var queryType          = (QueryType)ReadInt();
							var from               = Read<SelectQuery.FromClause>();
							var readInsert         = ReadBool();
							var insert             = readInsert ? Read<SelectQuery.InsertClause>() : null;
							var readUpdate         = ReadBool();
							var update             = readUpdate ? Read<SelectQuery.UpdateClause>() : null;
							var readDelete         = ReadBool();
							var delete             = readDelete ? Read<SelectQuery.DeleteClause>() : null;
							var readSelect         = ReadBool();
							var select             = readSelect ? Read<SelectQuery.SelectClause>() : new SelectQuery.SelectClause(null);
							var readCreateTable    = ReadBool();
							var createTable        = readCreateTable ?
								Read<SelectQuery.CreateTableStatement>() :
								new SelectQuery.CreateTableStatement();
							var where              = Read<SelectQuery.WhereClause>();
							var groupBy            = Read<SelectQuery.GroupByClause>();
							var having             = Read<SelectQuery.WhereClause>();
							var orderBy            = Read<SelectQuery.OrderByClause>();
							var parentSql          = ReadInt();
							var parameterDependent = ReadBool();
							var unions             = ReadArray<SelectQuery.Union>();
							var parameters         = ReadArray<SqlParameter>();

							var query = _query = new SelectQuery(sid) { QueryType = queryType };

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
								createTable,
								parameterDependent,
								parameters.ToList());

							_queries.Add(sid, _query);

							if (parentSql != 0)
								_actions.Add(() =>
								{
									SelectQuery selectQuery;
									if (_queries.TryGetValue(parentSql, out selectQuery))
										query.ParentSelect = selectQuery;
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

							var col = new SelectQuery.Column(null, expression, alias);

							_actions.Add(() => { col.Parent = _queries[sid]; return; });

							obj = col;

							break;
						}

					case QueryElementType.SearchCondition :
						obj = new SelectQuery.SearchCondition(ReadArray<SelectQuery.Condition>());
						break;

					case QueryElementType.Condition :
						obj = new SelectQuery.Condition(ReadBool(), Read<ISqlPredicate>(), ReadBool());
						break;

					case QueryElementType.TableSource :
						{
							var source = Read<ISqlTableSource>();
							var alias  = ReadString();
							var joins  = ReadArray<SelectQuery.JoinedTable>();

							obj = new SelectQuery.TableSource(source, alias, joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var joinType  = (SelectQuery.JoinType)ReadInt();
							var table     = Read<SelectQuery.TableSource>();
							var isWeak    = ReadBool();
							var condition = Read<SelectQuery.SearchCondition>();

							obj = new SelectQuery.JoinedTable(joinType, table, isWeak, condition);

							break;
						}

					case QueryElementType.SelectClause :
						{
							var isDistinct = ReadBool();
							var skipValue  = Read<ISqlExpression>();
							var takeValue  = Read<ISqlExpression>();
							var columns    = ReadArray<SelectQuery.Column>();

							obj = new SelectQuery.SelectClause(isDistinct, takeValue, skipValue, columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var items = ReadArray<SelectQuery.SetExpression>();
							var into  = Read<SqlTable>();
							var wid   = ReadBool();

							var c = new SelectQuery.InsertClause { Into = into, WithIdentity = wid };

							c.Items.AddRange(items);
							obj = c;

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var items = ReadArray<SelectQuery.SetExpression>();
							var keys  = ReadArray<SelectQuery.SetExpression>();
							var table = Read<SqlTable>();

							var c = new SelectQuery.UpdateClause { Table = table };

							c.Items.AddRange(items);
							c.Keys. AddRange(keys);
							obj = c;

							break;
						}

					case QueryElementType.DeleteClause :
						{
							var table = Read<SqlTable>();
							obj = new SelectQuery.DeleteClause { Table = table };
							break;
						}

					case QueryElementType.CreateTableStatement :
						{
							var table           = Read<SqlTable>();
							var isDrop          = ReadBool();
							var statementHeader = ReadString();
							var statementFooter = ReadString();
							var defaultNullable = (DefaulNullable)ReadInt();

							obj = new SelectQuery.CreateTableStatement
							{
								Table           = table,
								IsDrop          = isDrop,
								StatementHeader = statementHeader,
								StatementFooter = statementFooter,
								DefaulNullable  = defaultNullable,
							};

							break;
						}

					case QueryElementType.SetExpression : obj = new SelectQuery.SetExpression(Read     <ISqlExpression>(), Read<ISqlExpression>()); break;
					case QueryElementType.FromClause    : obj = new SelectQuery.FromClause   (ReadArray<SelectQuery.TableSource>());                break;
					case QueryElementType.WhereClause   : obj = new SelectQuery.WhereClause  (Read     <SelectQuery.SearchCondition>());            break;
					case QueryElementType.GroupByClause : obj = new SelectQuery.GroupByClause(ReadArray<ISqlExpression>());                         break;
					case QueryElementType.OrderByClause : obj = new SelectQuery.OrderByClause(ReadArray<SelectQuery.OrderByItem>());                break;

					case QueryElementType.OrderByItem :
						{
							var expression   = Read<ISqlExpression>();
							var isDescending = ReadBool();

							obj = new SelectQuery.OrderByItem(expression, isDescending);

							break;
						}

					case QueryElementType.Union :
						{
							var sqlQuery = Read<SelectQuery>();
							var isAll    = ReadBool();

							obj = new SelectQuery.Union(sqlQuery, isAll);

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
					Append(Configuration.LinqService.SerializeAssemblyQualifiedName ? type.AssemblyQualifiedName : type.FullName);
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
