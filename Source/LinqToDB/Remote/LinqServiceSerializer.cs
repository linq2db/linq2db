using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace LinqToDB.Remote
{
	using Common;
	using Extensions;
	using Mapping;
	using SqlQuery;

	static class LinqServiceSerializer
	{
		#region Public Members

		public static string Serialize(
			MappingSchema                serializationSchema,
			SqlStatement                 statement,
			IReadOnlyParameterValues?    parameterValues,
			IReadOnlyCollection<string>? queryHints,
			DataOptions                  dataOptions)
		{
			return new QuerySerializer(serializationSchema).Serialize(statement, parameterValues, queryHints, dataOptions);
		}

		public static LinqServiceQuery Deserialize(MappingSchema serializationSchema, MappingSchema contextSchema, DataOptions options, string str)
		{
			return new QueryDeserializer(serializationSchema, contextSchema, options).Deserialize(str);
		}

		public static string Serialize(MappingSchema serializationSchema, LinqServiceResult result)
		{
			return new ResultSerializer(serializationSchema).Serialize(result);
		}

		public static LinqServiceResult DeserializeResult(MappingSchema serializationSchema, MappingSchema contextSchema, DataOptions options, string str)
		{
			return new ResultDeserializer(serializationSchema, contextSchema, options).DeserializeResult(str);
		}

		public static string Serialize(MappingSchema serializationSchema, string[] data)
		{
			return new StringArraySerializer(serializationSchema).Serialize(data);
		}

		public static string[] DeserializeStringArray(MappingSchema serializationSchema, MappingSchema contextSchema, DataOptions options, string str)
		{
			return new StringArrayDeserializer(serializationSchema, contextSchema, options).Deserialize(str);
		}

		#endregion

		#region SerializerBase

		const int TypeIndex      = -2;
		const int TypeArrayIndex = -3;

		class SerializerBase
		{
			readonly MappingSchema _mappingSchema;

			protected readonly StringBuilder             Builder        = new ();
			protected readonly Dictionary<object,int>    ObjectIndices  = new (Utils.ObjectReferenceEqualityComparer<object>.Default);
			protected readonly Dictionary<object,string> DelayedObjects = new (Utils.ObjectReferenceEqualityComparer<object>.Default);
			protected int                                Index;

			protected readonly Dictionary<SqlValuesTable, IReadOnlyList<ISqlExpression[]>> EnumerableData = new ();

			protected SerializerBase(MappingSchema serializationMappingSchema)
			{
				_mappingSchema = serializationMappingSchema;
			}

			protected void Append(Type type, object? value, bool withType = true)
			{
				if (withType)
					Append(type);

				// TODO: should we preserve DBNull is AST?
				if (value == null || value is DBNull)
					Append((string?)null);
				else if (!type.IsArray)
				{
					Append(SerializationConverter.Serialize(_mappingSchema, value));
				}
				else
				{
					var elementType = type.GetElementType()!;

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
						foreach (object? val in (IEnumerable)value)
						{
							if (val == null)
								Append(elementType, null);
							else
								Append(val.GetType(), val);
							cnt++;
						}

					Builder.Insert(len, cnt.ToString(CultureInfo.InvariantCulture));
				}
			}

			protected void Append(int value)
			{
				Builder.Append(CultureInfo.InvariantCulture, $" {value}");
			}

			protected void AppendStringList(ICollection<string> strings)
			{
				Append(strings.Count);

				foreach (var str in strings)
					Append(str);
			}

			protected void Append(int? value)
			{
				Builder.Append(' ').Append(value.HasValue ? '1' : '0');

				if (value.HasValue)
					Builder.Append(CultureInfo.InvariantCulture, $"{value.Value}");
			}

			protected void Append(Type? value)
			{
				// don't move space to format, as GetType is not get-only method...
				Builder.Append(' ');
				Builder.Append(CultureInfo.InvariantCulture, $"{GetType(value)}");
			}

			protected void Append(bool value)
			{
				Builder.Append(' ').Append(value ? '1' : '0');
			}

			protected void Append(bool? value)
			{
				Builder.Append(' ').Append(value.HasValue ? '1' : '0');

				if (value.HasValue)
					Builder.Append(value.Value ? '1' : '0');
			}

			protected void Append(DbDataType type)
			{
				Append(type.SystemType);
				Append((int)type.DataType);
				Append(type.DbType);
				Append(type.Length);
				Append(type.Precision);
				Append(type.Scale);
			}

			protected void Append(SqlObjectName name)
			{
				Append(name.Name);
				Append(name.Server);
				Append(name.Database);
				Append(name.Schema);
				Append(name.Package);
			}

			protected void Append(IQueryElement? element)
			{
				Builder.Append(CultureInfo.InvariantCulture, $" {(element == null ? 0 : ObjectIndices[element])}");
			}

			protected void AppendDelayed(IQueryElement? element)
			{
				Builder.Append(' ');

				if (element == null)
					Builder.Append('0');
				else
				{
					if (ObjectIndices.TryGetValue(element, out var idx))
						Builder.Append(idx.ToString(NumberFormatInfo.InvariantInfo));
					else
					{
						if (!DelayedObjects.TryGetValue(element, out var id))
							DelayedObjects.Add(element, id = Guid.NewGuid().ToString("B"));

						Builder.Append(id);
					}
				}
			}

			protected void Append(string? str)
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
					Builder.Append(CultureInfo.InvariantCulture, $"{str.Length}:{str}");
				}
			}

			protected int GetType(Type? type)
			{
				if (type == null)
					return 0;

				if (!ObjectIndices.TryGetValue(type, out var idx))
				{
					if (type.IsArray)
					{
						var elementType = GetType(type.GetElementType());

						ObjectIndices.Add(type, idx = ++Index);

						Builder.Append(CultureInfo.InvariantCulture, $"{idx} {TypeArrayIndex} {elementType}");
					}
					else
					{
						ObjectIndices.Add(type, idx = ++Index);

						Builder.Append(CultureInfo.InvariantCulture, $"{idx} {TypeIndex}");

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
			protected readonly DataOptions   _options;
			protected readonly MappingSchema _contextSchema;
			protected readonly MappingSchema _serializationSchema;
			protected readonly Dictionary<int,object>         ObjectIndices  = new ();
			protected readonly Dictionary<int,Action<object>> DelayedObjects = new ();

			protected string Str = null!;
			protected int    Pos;

			public DataOptions Options => _options;

			protected DeserializerBase(MappingSchema serializationMappingSchema, MappingSchema contextMappingSchema, DataOptions options)
			{
				_serializationSchema = serializationMappingSchema;
				_contextSchema       = contextMappingSchema;
				_options             = options;
			}

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

			protected DbDataType ReadDbDataType()
			{
				var systemType = ReadType()!;
				var dataType   = (DataType)ReadInt();
				var dbType     = ReadString();
				var length     = ReadNullableInt();
				var precision  = ReadNullableInt();
				var scale      = ReadNullableInt();

				return new DbDataType(systemType, dataType, dbType, length, precision, scale);
			}

			protected SqlObjectName ReadObjectName()
			{
				return new (ReadString()!, ReadString(), ReadString(), ReadString(), ReadString());
			}

			protected DbDataType? ReadDbDataTypeNullable()
			{
				if (ReadBool())
					return ReadDbDataType();
				return null;
			}

			protected int ReadInt()
			{
				Get(' ');

				var minus = Get('-');
				var value = 0;

				for (var c = Peek(); char.IsDigit(c); c = Next())
					value = value * 10 + (c - '0');

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
					value = value * 10 + (c - '0');

				return minus ? -value : value;
			}

			protected int? ReadCount()
			{
				Get(' ');

				if (Get('-'))
					return null;

				var value = 0;

				for (var c = Peek(); char.IsDigit(c); c = Next())
					value = value * 10 + (c - '0');

				return value;
			}

			protected string? ReadString()
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

			protected List<string> ReadStringList()
			{
				var size = ReadInt();
				var list = new List<string>(size);

				for (var i = 0; i < size; i++)
					list.Add(ReadString()!);

				return list;
			}

			protected bool ReadBool()
			{
				Get(' ');

				var value = Peek() == '1';

				Pos++;

				return value;
			}

			protected bool? ReadNullableBool()
			{
				Get(' ');

				if (Get('0'))
					return null;

				Get('1');

				var value = Peek() == '1';

				Pos++;

				return value;
			}

			protected T? Read<T>()
				where T : class
			{
				var idx = ReadInt();
				return idx == 0 ? null : (T)ObjectIndices[idx];
			}

			protected void ReadDelayedObject(Action<object?> action)
			{
				var idx = ReadInt();

				if (idx == 0)
					action(null);
				else
				{
					if (ObjectIndices.TryGetValue(idx, out var obj))
						action(obj);
					else
						if (DelayedObjects.TryGetValue(idx, out var a))
							DelayedObjects[idx] = o => { a(o); action(o); };
						else
							DelayedObjects[idx] = action;
				}
			}

			protected Type? ReadType()
			{
				var idx = ReadInt();

				if (idx == 0)
					return null;

				if (!ObjectIndices.TryGetValue(idx, out var type))
				{
					Pos++;
					var typecode = ReadInt();
					Pos++;

					type = typecode switch
					{
						TypeIndex      => ResolveType(ReadString())!,
						TypeArrayIndex => GetArrayType(Read<Type>()!),
						_              => throw new SerializationException(
							FormattableString.Invariant($"TypeIndex or TypeArrayIndex ({TypeIndex} or {TypeArrayIndex}) expected, but was {typecode}")),
					};
					ObjectIndices.Add(idx, type);

					NextLine();

					var idx2 = ReadInt();
					if (idx2 != idx)
						throw new SerializationException(FormattableString.Invariant($"Wrong type reading, expected index is {idx} but was {idx2}"));
				}

				return (Type?) type;
			}

			protected T[]? ReadArray<T>()
				where T : class
			{
				var count = ReadCount();

				if (count == null)
					return null;

				var items = new T[count.Value];

				for (var i = 0; i < count; i++)
					items[i] = Read<T>()!;

				return items;
			}

			protected List<T>? ReadList<T>()
				where T : class
			{
				var count = ReadCount();

				if (count == null)
					return null;

				var items = new List<T>(count.Value);

				for (var i = 0; i < count; i++)
					items.Add(Read<T>()!);

				return items;
			}

			protected void NextLine()
			{
				while (Pos < Str.Length && (Peek() == '\n' || Peek() == '\r'))
					Pos++;
			}

			interface IDeserializerHelper
			{
				object? GetArray(DeserializerBase deserializer);
			}

			sealed class DeserializerHelper<T> : IDeserializerHelper
			{
				public object? GetArray(DeserializerBase deserializer)
				{
					var count = deserializer.ReadCount();

					if (count == null)
						return null;

					var arr   = new T[count.Value];

					for (var i = 0; i < count.Value; i++)
					{
						var elementType = deserializer.ReadType();
						arr[i] = (T) deserializer.ReadValue(elementType)!;
					}

					return arr;
				}
			}

			static readonly Dictionary<Type,Func<DeserializerBase,object?>> _arrayDeserializers = new ();

			protected object? ReadValue(Type? type)
			{
				if (type == null)
					return ReadString();

				if (type.IsArray)
				{
					var elem = type.GetElementType()!;

					Func<DeserializerBase,object?>? deserializer;

					lock (_arrayDeserializers)
					{
						if (!_arrayDeserializers.TryGetValue(elem, out deserializer))
						{
							var helper = (IDeserializerHelper)Activator.CreateInstance(typeof(DeserializerHelper<>).MakeGenericType(elem))!;
							_arrayDeserializers.Add(elem, deserializer = helper.GetArray);
						}
					}

					return deserializer(this);
				}

				return SerializationConverter.Deserialize(_serializationSchema, type, ReadString());

			}

			protected readonly List<string> UnresolvedTypes = new ();

			protected Type? ResolveType(string? str)
			{
				if (str == null)
					return null;

				Type? type = Type.GetType(str, false);

				if (type == null)
				{
					if (str == "System.Data.Linq.Binary")
						return typeof(System.Data.Linq.Binary);

					try
					{
						foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
						{
							type = assembly.GetType(str);
							if (type != null)
								break;
						}
					}
					catch
					{
						// ignore errors
					}

					if (type == null)
					{
						type = LinqService.TypeResolver(str);
						if (type == null)
						{
							if (Configuration.LinqService.ThrowUnresolvedTypeException)
								throw new LinqToDBException(
									$"Type '{str}' cannot be resolved. Use LinqService.TypeResolver to resolve unknown types.");

							UnresolvedTypes.Add(str);

							Debug.WriteLine(
								$"Type '{str}' cannot be resolved. Use LinqService.TypeResolver to resolve unknown types.",
								"LinqServiceSerializer");
						}
					}
				}

				return type;
			}
		}

		#endregion

		#region QuerySerializer

		sealed class QuerySerializer : SerializerBase
		{
			public QuerySerializer(MappingSchema serializationMappingSchema)
				: base(serializationMappingSchema)
			{
			}

			class QuerySerializationVisitor : QueryElementVisitor
			{
				readonly QuerySerializer   _serializer;
				readonly EvaluationContext _evaluationContext;

				public QuerySerializationVisitor(QuerySerializer serializer, EvaluationContext evaluationContext) : base(VisitMode.ReadOnly)
				{
					_serializer        = serializer;
					_evaluationContext = evaluationContext;
				}

				[return: NotNullIfNotNull(nameof(element))]
				public override IQueryElement? Visit(IQueryElement? element)
				{
					if (element == null)
						return null;

					base.Visit(element);

					RegisterInSerializer(element);

					return element;
				}

				protected override ISqlExpression VisitSqlColumnExpression(SqlColumn column, ISqlExpression expression)
				{
					base.VisitSqlColumnExpression(column, expression);

					RegisterInSerializer(column);

					return expression;
				}

				protected override IQueryElement VisitSqlTable(SqlTable element)
				{
					RegisterInSerializer(element.All);
					VisitElements(element.Fields, VisitMode.ReadOnly);
					return base.VisitSqlTable(element);
				}

				protected override IQueryElement VisitSqlCteTable(SqlCteTable element)
				{
					RegisterInSerializer(element.All);
					VisitElements(element.Fields, VisitMode.ReadOnly);
					return base.VisitSqlCteTable(element);
				}

				protected override IQueryElement VisitSqlRawSqlTable(SqlRawSqlTable element)
				{
					RegisterInSerializer(element.All);
					VisitElements(element.Fields, VisitMode.ReadOnly);
					return base.VisitSqlRawSqlTable(element);
				}

				protected override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
				{
					VisitElements(element.SourceFields, VisitMode.ReadOnly);
					return base.VisitSqlTableLikeSource(element);
				}

				protected override IQueryElement VisitSqlValuesTable(SqlValuesTable element)
				{
					VisitElements(element.Fields, VisitMode.ReadOnly);
					VisitListOfArrays(element.Rows, VisitMode.ReadOnly);
					return element;
				}

				void RegisterInSerializer(IQueryElement element)
				{
					if (!_serializer.ObjectIndices.ContainsKey(element))
						_serializer.Visit(element, _evaluationContext);
				}
			}

			void SerializeOptions(DataOptions options)
			{
				Append(options.LinqOptions.PreferExistsForScalar);
				Append(options.SqlOptions.EnableConstantExpressionInOrderBy);
				Append(options.SqlOptions.GenerateFinalAliases);
			}

			public string Serialize(
				SqlStatement                 statement,
				IReadOnlyParameterValues?    parameterValues,
				IReadOnlyCollection<string>? queryHints,
				DataOptions                  options)
			{
				// Add QueryHints.
				//
				var queryHintCount = queryHints?.Count ?? 0;

				Builder.AppendLine(queryHintCount.ToString(NumberFormatInfo.InvariantInfo));

				if (queryHintCount > 0)
					foreach (var hint in queryHints!)
						Builder.AppendLine(hint);

				// Serialize options.
				//
				SerializeOptions(options);

				// Serialize statement.
				//
				var visitor = new QuerySerializationVisitor(this, new EvaluationContext(parameterValues));

				visitor.Visit(statement);

				if (DelayedObjects.Count > 0)
				{
					var    first = DelayedObjects.First();
					string displayName;
					if (first.Key is IQueryElement element)
						displayName = element.ToDebugString();
					else
						displayName = first.Key.GetType().Name;

					throw new LinqToDBException($"QuerySerializer error. Unknown object '{displayName}'.");
				}

				Builder.AppendLine();

				var str = Builder.ToString();

#if DEBUG
				Debug.WriteLine(str);
#endif

				return str;
			}

			void Visit(IQueryElement e, EvaluationContext evaluationContext)
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlValuesTable :
						{
							var table = (SqlValuesTable)e;

							var rows  = table.BuildRows(evaluationContext);
							EnumerableData.Add(table, rows);

							foreach (var row in rows)
							foreach (var item in row)
							{
								if (!ObjectIndices.ContainsKey(item))
									Visit(item, evaluationContext);
							}

							break;
						}

					case QueryElementType.SqlField :
						{
							var fld = (SqlField)e;

							if (fld.Type.SystemType != null)
								GetType(fld.Type.SystemType);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var p = (SqlParameter)e;

							var pValue = p.GetParameterValue(evaluationContext.ParameterValues);
							var v      = pValue.ProviderValue;
							var t      = v == null ? pValue.DbDataType.SystemType : v.GetType();

							if (v == null || t.IsArray || t == typeof(string) || !(v is IEnumerable))
							{
								GetType(t);
							}
							else
							{
								var elemType = t.GetItemType()!;
								GetType(GetArrayType(elemType));
							}

							break;
						}

					case QueryElementType.SqlFunction              : GetType(((SqlFunction)             e).SystemType)          ; break;
					case QueryElementType.SqlExpression            : GetType(((SqlExpression)           e).SystemType)          ; break;
					case QueryElementType.SqlNullabilityExpression : GetType(((SqlNullabilityExpression)e).SystemType)          ; break;
					case QueryElementType.SqlAnchor                : GetType(((SqlAnchor)e).SystemType)                         ; break;
					case QueryElementType.SqlBinaryExpression      : GetType(((SqlBinaryExpression)     e).SystemType)          ; break;
					case QueryElementType.SqlDataType              : GetType(((SqlDataType)             e).Type.SystemType)     ; break;
					case QueryElementType.SqlValue                 : GetType(((SqlValue)                e).ValueType.SystemType); break;
					case QueryElementType.SqlTable                 : GetType(((SqlTable)                e).ObjectType)          ; break;
					case QueryElementType.SqlCteTable              : GetType(((SqlCteTable)             e).ObjectType)          ; break;
					case QueryElementType.CteClause                : GetType(((CteClause)               e).ObjectType)          ; break;
					case QueryElementType.SqlRawSqlTable           : GetType(((SqlRawSqlTable)          e).ObjectType)          ; break;
				}

				ObjectIndices.Add(e, ++Index);

				Builder.Append(CultureInfo.InvariantCulture, $"{Index} {(int)e.ElementType}");

				if (DelayedObjects.Count > 0)
				{
					if (DelayedObjects.TryGetValue(e, out var id))
					{
						Builder.Replace(id, Index.ToString(NumberFormatInfo.InvariantInfo));
						DelayedObjects.Remove(e);
					}
				}

				switch (e.ElementType)
				{
					case QueryElementType.SqlField :
						{
							var elem = (SqlField)e;

							Append(elem.Type);
							Append(elem.Name);
							Append(elem.PhysicalName);
							Append(elem.CanBeNull);
							Append(elem.IsPrimaryKey);
							Append(elem.PrimaryKeyOrder);
							Append(elem.IsIdentity);
							Append(elem.IsUpdatable);
							Append(elem.IsInsertable);
							Append(elem.IsDynamic);
							Append(elem.CreateFormat);
							Append(elem.CreateOrder);

							AppendDelayed(elem.Table);

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var elem = (SqlFunction)e;

							Append(elem.SystemType);
							Append(elem.Name);
							Append(elem.IsAggregate);
							Append(elem.IsPure);
							Append(elem.Precedence);
							Append(elem.Parameters);
							Append((int)elem.NullabilityType);
							Append(elem.CanBeNullNullable);
							Append(elem.DoNotOptimize);

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var elem = (SqlParameter)e;
							var paramValue = elem.GetParameterValue(evaluationContext.ParameterValues);

							Append(elem.Name);
							Append(elem.IsQueryParameter);
							Append(elem.NeedsCast);
							Append(paramValue.DbDataType);

							var value = paramValue.ProviderValue;
							var type  = value == null ? paramValue.DbDataType.SystemType : value.GetType();

							if (value == null || type.IsEnum || type.IsArray || type == typeof(string) || !(value is IEnumerable))
							{
								Append(type, value);
							}
							else
							{
								var elemType = type.GetItemType()!;

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
							Append((int)elem.Flags);
							Append((int)elem.NullabilityType);
							Append(elem.CanBeNullNullable);
							Append(elem.Parameters);

							break;
						}

					case QueryElementType.SqlNullabilityExpression :
					{
						var elem = (SqlNullabilityExpression)e;
						Append(elem.SqlExpression);
						Append(elem.CanBeNull);

						break;
					}

					case QueryElementType.SqlAnchor :
					{
						var elem = (SqlAnchor)e;
						Append(elem.SqlExpression);
						Append((int)elem.AnchorKind);

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

							Append(elem.ValueType);
							var type  = elem.Value?.GetType() ?? elem.ValueType.SystemType;
							Append(type, elem.Value);

							break;
						}

					case QueryElementType.SqlDataType :
						{
							var elem = (SqlDataType)e;

							Append(elem.Type);

							break;
						}

					case QueryElementType.SqlTable :
						{
							var elem = (SqlTable)e;

							Append(elem.SourceID);
							Append(elem.Expression);
							Append(elem.Alias);
							Append(elem.TableName);
							Append(elem.ObjectType);
							Append(elem.ID);

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

							Append(ObjectIndices[elem.All]);
							Append(elem.Fields.Count);

							foreach (var field in elem.Fields)
								Append(ObjectIndices[field]);

							Append((int)elem.SqlTableType);

							if (elem.SqlTableType != SqlTableType.Table)
							{
								if (elem.TableArguments == null)
									Append(0);
								else
								{
									Append(elem.TableArguments.Length);

									foreach (var expr in elem.TableArguments)
										Append(ObjectIndices[expr]);
								}
							}

							Append((int)elem.TableOptions);

							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.SqlCteTable :
						{
							var elem = (SqlCteTable)e;

							Append(elem.SourceID);
							Append(elem.Alias);

							AppendDelayed(elem.Cte);

							Append(ObjectIndices[elem.All]);
							Append(elem.Fields.Count);

							foreach (var field in elem.Fields)
								Append(ObjectIndices[field]);

							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.SqlRawSqlTable :
						{
							var elem = (SqlRawSqlTable)e;

							Append(elem.SourceID);
							Append(elem.Alias);
							Append(elem.ObjectType);

							Append(ObjectIndices[elem.All]);
							Append(elem.Fields.Count);

							foreach (var field in elem.Fields)
								Append(ObjectIndices[field]);

							Append(elem.SQL);

							if (elem.Parameters == null)
								Append(0);
							else
							{
								Append(elem.Parameters.Length);

								foreach (var expr in elem.Parameters)
									Append(ObjectIndices[expr]);
							}

							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.ExprPredicate :
						{
							var elem = (SqlPredicate.Expr)e;

							Append(elem.Expr1);
							Append(elem.Precedence);

							break;
						}

					case QueryElementType.NotPredicate :
						{
							var elem = (SqlPredicate.Not)e;

							Append(elem.Predicate);

							break;
						}

					case QueryElementType.TruePredicate :
						{
							break;
						}

					case QueryElementType.FalsePredicate :
						{
							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var elem = (SqlPredicate.ExprExpr)e;

							Append(elem.Expr1);
							Append((int)elem.Operator);
							Append(elem.Expr2);
							Append(elem.WithNull == null ? 3 : elem.WithNull.Value ? 1 : 0);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var elem = (SqlPredicate.Like)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Escape);
							Append(elem.FunctionName);
							break;
						}

					case QueryElementType.SearchStringPredicate:
					{
						var elem = (SqlPredicate.SearchString)e;

						Append(elem.Expr1);
						Append(elem.IsNot);
						Append(elem.Expr2);
						Append((int)elem.Kind);
						Append(elem.CaseSensitive);
						break;
					}

					case QueryElementType.BetweenPredicate :
						{
							var elem = (SqlPredicate.Between)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);
							Append(elem.Expr3);

							break;
						}

					case QueryElementType.IsTruePredicate :
						{
							var elem = (SqlPredicate.IsTrue)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.TrueValue);
							Append(elem.FalseValue);
							Append(elem.WithNull == null ? 3 : elem.WithNull.Value ? 1 : 0);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var elem = (SqlPredicate.IsNull)e;

							Append(elem.Expr1);
							Append(elem.IsNot);

							break;
						}

					case QueryElementType.IsDistinctPredicate :
						{
							var elem = (SqlPredicate.IsDistinct)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Expr2);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var elem = (SqlPredicate.InSubQuery)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.SubQuery);
							Append(elem.DoNotConvert);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var elem = (SqlPredicate.InList)e;

							Append(elem.Expr1);
							Append(elem.IsNot);
							Append(elem.Values);
							Append(elem.WithNull == null ? 3 : elem.WithNull.Value ? 1 : 0);

							break;
						}

					case QueryElementType.ExistsPredicate:
					{
						var elem = (SqlPredicate.Exists)e;

						Append(elem.IsNot);
						Append(elem.SubQuery);

						break;
					}

					case QueryElementType.SqlQuery :
						{
							var elem = (SelectQuery)e;

							Append(elem.SourceID);
							Append(elem.From);

							Append(elem.Select);

							Append(elem.Where);
							Append(elem.GroupBy);
							Append(elem.Having);
							Append(elem.OrderBy);
							Append(elem.IsParameterDependent);
							Append(elem.QueryName);
							Append(elem.DoNotSetAliases);

							if (!elem.HasSetOperators)
								Builder.Append(" -");
							else
								Append(elem.SetOperators);

							if (ObjectIndices.TryGetValue(elem.All, out var index))
								Append(index);
							else
								Builder.Append(" -");

							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.Column :
						{
							var elem = (SqlColumn) e;

							Append(elem.Parent!.SourceID);
							Append(elem.Expression);
							Append(elem.RawAlias);

							break;
						}

					case QueryElementType.SearchCondition :
						{
							Append(((SqlSearchCondition)e).IsOr);
							Append(((SqlSearchCondition)e).Predicates);
							break;
						}

					case QueryElementType.TableSource :
						{
							var elem = (SqlTableSource)e;

							Append(elem.Source);
							Append(elem.RawAlias);
							Append(elem.Joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var elem = (SqlJoinedTable)e;

							Append((int)elem.JoinType);
							Append(elem.Table);
							Append(elem.IsWeak);
							Append(elem.Condition);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.SelectClause :
						{
							var elem = (SqlSelectClause)e;

							Append(elem.IsDistinct);
							Append(elem.SkipValue);
							Append(elem.TakeValue);
							Append((int?)elem.TakeHints);

							Append(elem.Columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var elem = (SqlInsertClause)e;

							Append(elem.Items);
							Append(elem.Into);
							Append(elem.WithIdentity);

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var elem = (SqlUpdateClause)e;

							Append(elem.Items);
							Append(elem.Keys);
							Append(elem.Table);
							Append(elem.TableSource);
							Append(elem.HasComparison);

							break;
						}

					case QueryElementType.WithClause :
						{
							var elem = (SqlWithClause)e;

							Append(elem.Clauses);

							break;
						}

					case QueryElementType.CteClause:
						{
							var elem = (CteClause)e;

							Append(elem.Name);
							Append(elem.Body);
							Append(elem.ObjectType);
							Append(elem.Fields);
							Append(elem.IsRecursive);

							break;
						}

					case QueryElementType.SelectStatement :
						{
							var elem = (SqlSelectStatement)e;
							Append(elem.Tag);
							Append(elem.With);
							Append(elem.SelectQuery);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.InsertStatement :
						{
							var elem = (SqlInsertStatement)e;
							Append(elem.Tag);
							Append(elem.With);
							Append(elem.Insert);
							Append(elem.SelectQuery);
							Append(elem.Output);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.InsertOrUpdateStatement :
						{
							var elem = (SqlInsertOrUpdateStatement)e;
							Append(elem.Tag);
							Append(elem.With);
							Append(elem.Insert);
							Append(elem.Update);
							Append(elem.SelectQuery);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.UpdateStatement :
						{
							var elem = (SqlUpdateStatement)e;
							Append(elem.Tag);
							Append(elem.With);
							Append(elem.SelectQuery);
							Append(elem.Update);
							Append(elem.Output);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.DeleteStatement :
						{
							var elem = (SqlDeleteStatement)e;

							Append(elem.Tag);
							Append(elem.With);
							Append(elem.Table);
							Append(elem.Output);
							Append(elem.Top);
							Append(elem.SelectQuery);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.SetExpression :
						{
							var elem = (SqlSetExpression)e;
							Append(elem.Column);
							Append(elem.Expression);

							break;
						}

					case QueryElementType.CreateTableStatement :
						{
							var elem = (SqlCreateTableStatement)e;

							Append(elem.Tag);
							Append(elem.Table);
							Append(elem.StatementHeader);
							Append(elem.StatementFooter);
							Append((int)elem.DefaultNullable);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.DropTableStatement :
					{
						var elem = (SqlDropTableStatement)e;

						Append(elem.Tag);
						Append(elem.Table);
						Append(elem.SqlQueryExtensions);

						break;
					}

					case QueryElementType.TruncateTableStatement :
					{
						var elem = (SqlTruncateTableStatement)e;

						Append(elem.Tag);
						Append(elem.Table);
						Append(elem.ResetIdentity);
						Append(elem.SqlQueryExtensions);

						break;
					}

					case QueryElementType.FromClause    : Append(((SqlFromClause)   e).Tables);          break;
					case QueryElementType.WhereClause   : Append(((SqlWhereClause)  e).SearchCondition); break;
					case QueryElementType.HavingClause  : Append(((SqlHavingClause) e).SearchCondition); break;
					case QueryElementType.GroupByClause :
						{
							Append((int)((SqlGroupByClause)e).GroupingType);
							Append(((SqlGroupByClause)e).Items);
							break;
						}

					case QueryElementType.GroupingSet   : Append(((SqlGroupingSet)e).Items);             break;
					case QueryElementType.OrderByClause : Append(((SqlOrderByClause)e).Items);           break;

					case QueryElementType.OrderByItem :
						{
							var elem = (SqlOrderByItem)e;

							Append(elem.Expression);
							Append(elem.IsDescending);
							Append(elem.IsPositioned);

							break;
						}

					case QueryElementType.SetOperator :
						{
							var elem = (SqlSetOperator)e;

							Append(elem.SelectQuery);
							Append((int)elem.Operation);

							break;
						}

					case QueryElementType.SqlTableLikeSource:
						{
							var elem = (SqlTableLikeSource)e;

							Append(elem.SourceID);
							Append(elem.SourceEnumerable);
							Append(elem.SourceQuery);
							Append(elem.SourceFields);

							break;
						}

					case QueryElementType.MergeOperationClause:
						{
							var elem = (SqlMergeOperationClause)e;

							Append((int)elem.OperationType);
							Append(elem.Where);
							Append(elem.WhereDelete);
							Append(elem.Items);

							break;
						}

					case QueryElementType.MergeStatement:
						{
							var elem = (SqlMergeStatement)e;

							Append(elem.Tag);
							Append(elem.With);
							Append(elem.Hint);
							Append(elem.Target);
							Append(elem.Source);
							Append(elem.On);
							Append(elem.Operations);
							Append(elem.Output);
							Append(elem.SqlQueryExtensions);

						break;
						}

					case QueryElementType.MultiInsertStatement:
						{
							var elem = (SqlMultiInsertStatement)e;

							Append((int)elem.InsertType);
							Append(elem.Source);
							Append(elem.Inserts);
							Append(elem.SqlQueryExtensions);

							break;
						}

					case QueryElementType.ConditionalInsertClause:
						{
							var elem = (SqlConditionalInsertClause)e;

							Append(elem.When);
							Append(elem.Insert);

							break;
						}

					case QueryElementType.SqlValuesTable:
						{
							var elem = (SqlValuesTable)e;
							var rows = EnumerableData[elem];

							Append(elem.Fields);
							Append(rows.Count);
							foreach (var row in rows)
								Append(row);

							break;
						}

					case QueryElementType.SqlAliasPlaceholder:
						{
							break;
						}

					case QueryElementType.SqlRow:
						{
							var elem = (SqlRowExpression)e;

							Append(elem.Values);

							break;
						}

					case QueryElementType.OutputClause:
						{
							var elem = (SqlOutputClause)e;

							Append(elem.OutputTable);

							if (elem.HasOutputItems)
								Append(elem.OutputItems);
							else
								Builder.Append(" -");

							Append(elem.OutputColumns);

							break;
						}

					case QueryElementType.Comment:
					{
						var elem = (SqlComment)e;
						AppendStringList(elem.Lines);
						break;
					}

					case QueryElementType.SqlQueryExtension:
					{
						var ext = (SqlQueryExtension)e;
						Append(ext.Configuration);
						Append((int)ext.Scope);
						Append(ext.BuilderType);
						Append(ext.Arguments.Count);

						foreach (var argument in ext.Arguments)
						{
							Append(argument.Key);
							Append(argument.Value);
						}

						break;
					}

					case QueryElementType.SqlCast:
					{
						var elem = (SqlCastExpression)e;
						Append(elem.ToType);
						Append(elem.Expression);
						Append(elem.FromType);
						Append(elem.IsMandatory);
						break;
					}

					case QueryElementType.SqlCondition:
					{
						var elem = (SqlConditionExpression)e;
						Append(elem.Condition);
						Append(elem.TrueValue);
						Append(elem.FalseValue);
						break;
					}

					case QueryElementType.SqlCase:
					{
						var elem = (SqlCaseExpression)e;

						Append(elem.Type);
						Append(elem.Cases.Count);

						foreach (var caseItem in elem.Cases)
						{
							Append(caseItem.Condition);
							Append(caseItem.ResultExpression);
						}

						Append(elem.ElseExpression);

						break;
					}

					case QueryElementType.CompareTo:
					{
						var elem = (SqlCompareToExpression)e;

						Append(elem.Expression1);
						Append(elem.Expression2);

						break;
					}

					case QueryElementType.SqlCoalesce:
					{
						var elem = (SqlCoalesceExpression)e;

						Append(elem.Expressions);
						break;
					}

					default:
						throw new InvalidOperationException($"Serialize not implemented for element {e.ElementType}");
				}

				Builder.AppendLine();
			}

			void Append<T>(ICollection<T>? exprs)
				where T : IQueryElement
			{
				if (exprs == null)
					Builder.Append(" -");
				else
				{
					Append(exprs.Count);

					foreach (var e in exprs)
						Append(ObjectIndices[e]);
				}
			}
		}

		#endregion

		#region QueryDeserializer

		public sealed class QueryDeserializer : DeserializerBase
		{
			SqlStatement   _statement  = null!;

			readonly Dictionary<int,SelectQuery> _queries = new ();
			readonly List<Action>                _actions = new ();

			public QueryDeserializer(MappingSchema serializationMappingSchema, MappingSchema contextSchema, DataOptions options)
				: base(serializationMappingSchema, contextSchema, options)
			{
			}

			DataOptions DeserializeOptions(DataOptions options)
			{
				options = options
					.WithOptions<LinqOptions>(lo => lo.WithPreferExistsForScalar(ReadBool()))
					.WithOptions<SqlOptions>(so =>
						so.WithEnableConstantExpressionInOrderBy(ReadBool())
							.WithGenerateFinalAliases(ReadBool()));

				return options;
			}

			public LinqServiceQuery Deserialize(string str)
			{
				Str = str;

				// Deserialize QueryHints.
				//
				List<string>? queryHints = null;

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

				// Deserialize options.
				//
				var options = DeserializeOptions(Options);

				NextLine();

				// Deserialize statement.
				//
				while (Parse()) {}

				foreach (var action in _actions)
					action();

				return new LinqServiceQuery { Statement = _statement, QueryHints = queryHints, DataOptions = options };
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
					case (QueryElementType)TypeIndex      : obj = ResolveType(ReadString());                break;
					case (QueryElementType)TypeArrayIndex : obj = GetArrayType(ReadType()!);              break;

					case QueryElementType.SqlField :
						{
							var dbDataType       = ReadDbDataType();
							var name             = ReadString()!;
							var physicalName     = ReadString()!;
							var nullable         = ReadBool();
							var isPrimaryKey     = ReadBool();
							var primaryKeyOrder  = ReadInt();
							var isIdentity       = ReadBool();
							var isUpdatable      = ReadBool();
							var isInsertable     = ReadBool();
							var isDynamic        = ReadBool();
							var createFormat     = ReadString();
							var createOrder      = ReadNullableInt();

							SqlField field;
							obj = field = new SqlField(name, physicalName)
							{
								Type            = dbDataType,
								CanBeNull       = nullable,
								IsPrimaryKey    = isPrimaryKey,
								PrimaryKeyOrder = primaryKeyOrder,
								IsIdentity      = isIdentity,
								IsUpdatable     = isUpdatable,
								IsInsertable    = isInsertable,
								IsDynamic       = isDynamic,
								CreateFormat    = createFormat,
								CreateOrder     = createOrder
							};

							ReadDelayedObject(table =>
							{
								field.Table = table as ISqlTableSource;
								if (table is SqlTable sqlTable && sqlTable.ObjectType != null)
								{
									var ed = _contextSchema.GetEntityDescriptor(sqlTable.ObjectType, _options.ConnectionOptions.OnEntityDescriptorCreated);
									field.ColumnDescriptor = ed[field.Name]!;
								}
							});

							break;
						}

					case QueryElementType.SqlFunction :
						{
							var systemType    = ReadType()!;
							var name          = ReadString()!;
							var isAggregate   = ReadBool();
							var isPure        = ReadBool();
							var precedence    = ReadInt();
							var parameters    = ReadArray<ISqlExpression>()!;
							var nullability   = (ParametersNullabilityType)ReadInt();
							var canBeNull     = ReadNullableBool();
							var doNotOptimize = ReadBool();

							obj = new SqlFunction(systemType, name, isAggregate, isPure, precedence, nullability, canBeNull, parameters)
							{
								DoNotOptimize = doNotOptimize
							};

							break;
						}

					case QueryElementType.SqlParameter :
						{
							var name             = ReadString();
							var isQueryParameter = ReadBool();
							var needCast         = ReadBool();
							var dbDataType       = ReadDbDataType();

							var value            = ReadValue(ReadType()!);

							obj = new SqlParameter(dbDataType, name, value)
							{
								IsQueryParameter = isQueryParameter,
								NeedsCast = needCast,
							};

							break;
						}

					case QueryElementType.SqlExpression :
						{
							var systemType  = ReadType();
							var expr        = ReadString()!;
							var precedence  = ReadInt();
							var flags       = (SqlFlags)ReadInt();
							var nullability = (ParametersNullabilityType)ReadInt();
							var canBeNull   = ReadNullableBool();
							var parameters  = ReadArray<ISqlExpression>()!;

							obj = new SqlExpression(systemType, expr, precedence, flags, nullability, canBeNull, parameters);

							break;
						}

					case QueryElementType.SqlNullabilityExpression :
					{
						var sqlExpression = Read<ISqlExpression>()!;
						var isNullable = ReadBool();

						obj = new SqlNullabilityExpression(sqlExpression, isNullable);

						break;
					}

					case QueryElementType.SqlAnchor :
					{
						var sqlExpression = Read<ISqlExpression>()!;
						var kind          = (SqlAnchor.AnchorKindEnum)ReadInt();

						obj = new SqlAnchor(sqlExpression, kind);

						break;
					}

					case QueryElementType.SqlBinaryExpression :
						{
							var systemType = ReadType()!;
							var expr1      = Read<ISqlExpression>()!;
							var operation  = ReadString()!;
							var expr2      = Read<ISqlExpression>()!;
							var precedence = ReadInt();

							obj = new SqlBinaryExpression(systemType, expr1, operation, expr2, precedence);

							break;
						}

					case QueryElementType.SqlValue :
						{
							var dbDataType    = ReadDbDataType();
							var value         = ReadValue(ReadType()!);

							obj = new SqlValue(dbDataType, value);

							break;
						}

					case QueryElementType.SqlDataType :
						{
							var dbDataType = ReadDbDataType();

							obj = new SqlDataType(dbDataType);

							break;
						}

					case QueryElementType.SqlTable :
						{
							var sourceID           = ReadInt();
							var expression         = ReadString();
							var alias              = ReadString()!;
							var tableName          = ReadObjectName();
							var objectType         = ReadType()!;
							var tableID            = ReadString();
							var sequenceAttributes = null as SequenceNameAttribute[];

							var count = ReadCount();

							if (count != null)
							{
								sequenceAttributes = new SequenceNameAttribute[count.Value];

								for (var i = 0; i < count.Value; i++)
									sequenceAttributes[i] = new SequenceNameAttribute(ReadString()!, ReadString()!);
							}

							var all        = Read<SqlField>()!;
							var fields     = ReadArray<SqlField>()!;
							var flds       = new SqlField[fields.Length + 1];

							flds[0] = all;
							Array.Copy(fields, 0, flds, 1, fields.Length);

							var sqlTableType = (SqlTableType)ReadInt();
							var tableArgs    = sqlTableType == SqlTableType.Table ? null : ReadArray<ISqlExpression>();
							var tableOptions = (TableOptions)ReadInt();

							var extensions = ReadList<SqlQueryExtension>();

							obj = new SqlTable(
								sourceID, expression, alias, tableName, objectType, sequenceAttributes, flds,
								sqlTableType, tableArgs, tableOptions, tableID)
							{
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.SqlCteTable :
						{
							var sourceID  = ReadInt();
							var alias     = ReadString()!;

							//CteClause cte       = Read<CteClause>();
							SqlCteTable? cteTable = null;
							CteClause?   cte      = null;
							var isDelayed = true;

							ReadDelayedObject(o =>
							{
								cte = (CteClause)o!;

								if (cteTable == null)
									isDelayed = false;
								else
									cteTable.SetDelayedCteObject(cte);
							});

							var all        = Read<SqlField>()!;
							var fields     = ReadArray<SqlField>()!;
							var extensions = ReadList<SqlQueryExtension>();

							var flds   = new SqlField[fields.Length + 1];

							flds[0] = all;
							Array.Copy(fields, 0, flds, 1, fields.Length);

							cteTable = isDelayed ?
								new SqlCteTable(sourceID, alias, flds) :
								new SqlCteTable(sourceID, alias, flds, cte!);

							cteTable.SqlQueryExtensions = extensions;

							obj = cteTable;

							break;
						}

					case QueryElementType.SqlRawSqlTable :
						{
							var sourceID           = ReadInt();
							var alias              = ReadString()!;
							var objectType         = ReadType()!;

							var all    = Read<SqlField>()!;
							var fields = ReadArray<SqlField>()!;
							var flds   = new SqlField[fields.Length + 1];

							flds[0] = all;
							Array.Copy(fields, 0, flds, 1, fields.Length);

							var sql        = ReadString()!;
							var parameters = ReadArray<ISqlExpression>()!;
							var extensions = ReadList<SqlQueryExtension>();

							obj = new SqlRawSqlTable(sourceID, alias, objectType, flds, sql, parameters)
							{
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.ExprPredicate :
						{
							var expr1      = Read<ISqlExpression>()!;
							var precedence = ReadInt();

							obj = new SqlPredicate.Expr(expr1, precedence);

							break;
						}

					case QueryElementType.NotPredicate :
						{
							var predicate = Read<ISqlPredicate>()!;

							obj = new SqlPredicate.Not(predicate);

							break;
						}

					case QueryElementType.TruePredicate :
						{
							obj = SqlPredicate.True;

							break;
						}

					case QueryElementType.FalsePredicate :
						{
							obj = SqlPredicate.False;

							break;
						}

					case QueryElementType.ExprExprPredicate :
						{
							var expr1     = Read<ISqlExpression>()!;
							var @operator = (SqlPredicate.Operator)ReadInt();
							var expr2     = Read<ISqlExpression>()!;
							var withNull  = ReadInt();

							obj = new SqlPredicate.ExprExpr(expr1, @operator, expr2, withNull == 3 ? null : withNull == 1);

							break;
						}

					case QueryElementType.LikePredicate :
						{
							var expr1  = Read<ISqlExpression>()!;
							var isNot  = ReadBool();
							var expr2  = Read<ISqlExpression>()!;
							var escape = Read<ISqlExpression>();
							var fun    = ReadString();
							obj = new SqlPredicate.Like(expr1, isNot, expr2, escape, fun);

							break;
						}

					case QueryElementType.SearchStringPredicate:
					{
						var expr1         = Read<ISqlExpression>()!;
						var isNot         = ReadBool();
						var expr2         = Read<ISqlExpression>()!;
						var kind          = (SqlPredicate.SearchString.SearchKind)ReadInt();
						var caseSensitive = Read<ISqlExpression>()!;
						obj = new SqlPredicate.SearchString(expr1, isNot, expr2, kind, caseSensitive);

						break;
					}

					case QueryElementType.BetweenPredicate :
						{
							var expr1 = Read<ISqlExpression>()!;
							var isNot = ReadBool();
							var expr2 = Read<ISqlExpression>()!;
							var expr3 = Read<ISqlExpression>()!;

							obj = new SqlPredicate.Between(expr1, isNot, expr2, expr3);

							break;
						}

					case QueryElementType.IsTruePredicate :
						{
							var expr1 = Read<ISqlExpression>()!;
							var isNot = ReadBool();
							var trueValue  = Read<ISqlExpression>()!;
							var falseValue = Read<ISqlExpression>()!;
							var withNull   = ReadInt();

							obj = new SqlPredicate.IsTrue(expr1, trueValue, falseValue, withNull == 3 ? null : withNull == 1, isNot);

							break;
						}

					case QueryElementType.IsNullPredicate :
						{
							var expr1 = Read<ISqlExpression>()!;
							var isNot = ReadBool();

							obj = new SqlPredicate.IsNull(expr1, isNot);

							break;
						}

					case QueryElementType.IsDistinctPredicate :
						{
							var expr1 = Read<ISqlExpression>()!;
							var isNot = ReadBool();
							var expr2 = Read<ISqlExpression>()!;

							obj = new SqlPredicate.IsDistinct(expr1, isNot, expr2);

							break;
						}

					case QueryElementType.InSubQueryPredicate :
						{
							var expr1        = Read<ISqlExpression>()!;
							var isNot        = ReadBool();
							var subQuery     = Read<SelectQuery>()!;
							var doNotConvert = ReadBool();

							obj = new SqlPredicate.InSubQuery(expr1, isNot, subQuery, doNotConvert);

							break;
						}

					case QueryElementType.InListPredicate :
						{
							var expr1  = Read<ISqlExpression>()!;
							var isNot  = ReadBool();
							var values = ReadList<ISqlExpression>()!;
							var withNull = ReadInt();

							obj = new SqlPredicate.InList(expr1, withNull == 3 ? null : withNull == 1, isNot, values);

							break;
						}

					case QueryElementType.ExistsPredicate:
					{
						var isNot    = ReadBool();
						var subQuery = Read<SelectQuery>()!;

						obj = new SqlPredicate.Exists(isNot, subQuery);

						break;
					}

					case QueryElementType.SqlQuery :
						{
							var sid                = ReadInt();
							var from               = Read<SqlFromClause>()!;
							var select             = Read<SqlSelectClause>()!;
							var where              = Read<SqlWhereClause>()!;
							var groupBy            = Read<SqlGroupByClause>()!;
							var having             = Read<SqlHavingClause>()!;
							var orderBy            = Read<SqlOrderByClause>()!;
							var parameterDependent = ReadBool();
							var queryName          = ReadString();
							var doNotSetAliases    = ReadBool();
							var unions             = ReadArray<SqlSetOperator>();

							var query = new SelectQuery(sid);

							query.Init(
								select,
								from,
								where,
								groupBy,
								having,
								orderBy,
								unions?.ToList(),
								null,
								parameterDependent,
								queryName,
								doNotSetAliases);

							_queries.Add(sid, query);

							query.All = Read<SqlField>()!;

							query.SqlQueryExtensions = ReadList<SqlQueryExtension>();

							obj = query;

							break;
						}

					case QueryElementType.Column :
						{
							var sid        = ReadInt();
							var expression = Read<ISqlExpression>()!;
							var alias      = ReadString();

							var col = new SqlColumn(null, expression, alias);

							_actions.Add(() => { col.Parent = _queries[sid]; });

							obj = col;

							break;
						}

					case QueryElementType.SearchCondition :
						obj = new SqlSearchCondition(ReadBool(), ReadArray<ISqlPredicate>()!);
						break;

					case QueryElementType.TableSource :
						{
							var source = Read<ISqlTableSource>()!;
							var alias  = ReadString();
							var joins  = ReadArray<SqlJoinedTable>();

							obj = new SqlTableSource(source, alias, joins);

							break;
						}

					case QueryElementType.JoinedTable :
						{
							var joinType   = (JoinType)ReadInt();
							var table      = Read<SqlTableSource>()!;
							var isWeak     = ReadBool();
							var condition  = Read<SqlSearchCondition>()!;
							var extensions = ReadList<SqlQueryExtension>();

							obj = new SqlJoinedTable(joinType, table, isWeak, condition)
							{
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.SelectClause :
						{
							var isDistinct = ReadBool();
							var skipValue  = Read<ISqlExpression>()!;
							var takeValue  = Read<ISqlExpression>()!;
							var takeHints  = (TakeHints?)ReadNullableInt();
							var columns    = ReadArray<SqlColumn>()!;

							obj = new SqlSelectClause(isDistinct, takeValue, takeHints, skipValue, columns);

							break;
						}

					case QueryElementType.InsertClause :
						{
							var items = ReadArray<SqlSetExpression>();
							var into  = Read<SqlTable>();
							var wid   = ReadBool();

							var c = new SqlInsertClause { Into = into, WithIdentity = wid };

							if(items != null)
								c.Items.AddRange(items);
							obj = c;

							break;
						}

					case QueryElementType.UpdateClause :
						{
							var items         = ReadArray<SqlSetExpression>();
							var keys          = ReadArray<SqlSetExpression>();
							var table         = Read<SqlTable>();
							var tableSource   = Read<SqlTableSource>();
							var hasComparison = ReadBool();

							var c = new SqlUpdateClause { Table = table, TableSource = tableSource, HasComparison = hasComparison };

							if (items != null)
								c.Items.AddRange(items);
							if (keys != null)
								c.Keys.AddRange(keys);
							obj = c;

							break;
						}

					case QueryElementType.WithClause :
						{
							var items = ReadArray<CteClause>();

							var c = new SqlWithClause();
							if (items != null)
								c.Clauses.AddRange(items);

							obj = c;

							break;
						}

					case QueryElementType.CteClause:
						{
							var name        = ReadString()!;
							var body        = Read<SelectQuery>();
							var objectType  = ReadType()!;
							var fields      = ReadArray<SqlField>()!;
							var isRecursive = ReadBool();

							var c = new CteClause(body, fields, objectType, isRecursive, name);

							obj = c;

							break;
						}

					case QueryElementType.SelectStatement :
						{
							var tag         = Read<SqlComment>();
							var with        = Read<SqlWithClause>();
							var selectQuery = Read<SelectQuery>()!;
							var extensions  = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlSelectStatement(selectQuery)
							{
								With = with,
								Tag  = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.InsertStatement :
						{
							var tag         = Read<SqlComment>();
							var with        = Read<SqlWithClause>();
							var insert      = Read<SqlInsertClause>()!;
							var selectQuery = Read<SelectQuery>()!;
							var output      = Read<SqlOutputClause>();
							var extensions  = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlInsertStatement(selectQuery)
							{
								Insert             = insert,
								Output             = output,
								With               = with,
								Tag                = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.UpdateStatement :
						{
							var tag         = Read<SqlComment>();
							var with        = Read<SqlWithClause>();
							var selectQuery = Read<SelectQuery>()!;
							var update      = Read<SqlUpdateClause>()!;
							var output      = Read<SqlOutputClause>()!;
							var extensions  = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlUpdateStatement(selectQuery)
							{
								Update             = update,
								Output             = output,
								With               = with,
								Tag                = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.InsertOrUpdateStatement :
						{
							var tag         = Read<SqlComment>();
							var with        = Read<SqlWithClause>();
							var insert      = Read<SqlInsertClause>()!;
							var update      = Read<SqlUpdateClause>()!;
							var selectQuery = Read<SelectQuery>();
							var extensions  = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlInsertOrUpdateStatement(selectQuery)
							{
								Insert             = insert,
								Update             = update,
								With               = with,
								Tag                = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.DeleteStatement :
						{
							var tag         = Read<SqlComment>();
							var with        = Read<SqlWithClause>();
							var table       = Read<SqlTable>();
							var output      = Read<SqlOutputClause>();
							var top         = Read<ISqlExpression>()!;
							var selectQuery = Read<SelectQuery>();
							var extensions  = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlDeleteStatement
							{
								Table       = table,
								Output      = output,
								Top         = top,
								SelectQuery = selectQuery,
								With        = with,
								Tag         = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.CreateTableStatement :
						{
							var tag             = Read<SqlComment>();
							var table           = Read<SqlTable>()!;
							var statementHeader = ReadString();
							var statementFooter = ReadString();
							var defaultNullable = (DefaultNullable)ReadInt();
							var extensions      = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlCreateTableStatement(table)
							{
								StatementHeader    = statementHeader,
								StatementFooter    = statementFooter,
								DefaultNullable    = defaultNullable,
								Tag                = tag,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.DropTableStatement :
					{
						var tag        = Read<SqlComment>();
						var table      = Read<SqlTable>()!;
						var extensions = ReadList<SqlQueryExtension>();

						obj = _statement = new SqlDropTableStatement(table)
						{
							Tag                = tag,
							SqlQueryExtensions = extensions
						};

						break;
					}

					case QueryElementType.TruncateTableStatement :
					{
						var tag        = Read<SqlComment>();
						var table      = Read<SqlTable>();
						var reset      = ReadBool();
						var extensions = ReadList<SqlQueryExtension>();

						obj = _statement = new SqlTruncateTableStatement
						{
							Table              = table,
							ResetIdentity      = reset,
							Tag                = tag,
							SqlQueryExtensions = extensions
						};

						break;
					}

					case QueryElementType.SetExpression :
					{
						var column     = Read<ISqlExpression>();
						var expression = Read<ISqlExpression>();

						obj = new SqlSetExpression(column!, expression);

						break;
					}
					case QueryElementType.FromClause    : obj = new SqlFromClause   (ReadArray<SqlTableSource>()!);                break;
					case QueryElementType.WhereClause   : obj = new SqlWhereClause  (Read     <SqlSearchCondition>()!);            break;
					case QueryElementType.HavingClause  : obj = new SqlHavingClause (Read     <SqlSearchCondition>()!);            break;
					case QueryElementType.GroupByClause : obj = new SqlGroupByClause((GroupingType)ReadInt(), ReadArray<ISqlExpression>()!); break;
					case QueryElementType.GroupingSet   : obj = new SqlGroupingSet  (ReadArray<ISqlExpression>()!);                          break;
					case QueryElementType.OrderByClause : obj = new SqlOrderByClause(ReadArray<SqlOrderByItem>()!);                break;

					case QueryElementType.OrderByItem :
						{
							var expression   = Read<ISqlExpression>()!;
							var isDescending = ReadBool();
							var isPositioned = ReadBool();

							obj = new SqlOrderByItem(expression, isDescending, isPositioned);

							break;
						}

					case QueryElementType.SetOperator :
						{
							var sqlQuery     = Read<SelectQuery>()!;
							var setOperation = (SetOperation)ReadInt();

							obj = new SqlSetOperator(sqlQuery, setOperation);

							break;
						}

					case QueryElementType.SqlTableLikeSource:
						{
							var sourceID         = ReadInt();
							var enumerableSource = Read<SqlValuesTable>()!;
							var querySource      = Read<SelectQuery>()!;
							var fields           = ReadArray<SqlField>()!;

							obj = new SqlTableLikeSource(sourceID, enumerableSource, querySource, fields);

							break;
						}

					case QueryElementType.MergeOperationClause:
						{
							var operationType = (MergeOperationType)ReadInt();
							var where         = Read<SqlSearchCondition>()!;
							var whereDelete   = Read<SqlSearchCondition>()!;
							var items         = ReadArray<SqlSetExpression>()!;

							obj = new SqlMergeOperationClause(operationType, where, whereDelete, items);

							break;
						}

					case QueryElementType.MergeStatement:
						{
							var tag        = Read<SqlComment>();
							var with       = Read<SqlWithClause>();
							var hint       = ReadString();
							var target     = Read<SqlTableSource>()!;
							var source     = Read<SqlTableLikeSource>()!;
							var on         = Read<SqlSearchCondition>()!;
							var operations = ReadArray<SqlMergeOperationClause>()!;
							var output     = Read<SqlOutputClause>();
							var extensions = ReadList<SqlQueryExtension>();

							obj = _statement = new SqlMergeStatement(with, hint, target, source, on, operations)
							{
								Tag    = tag,
								Output = output,
								SqlQueryExtensions = extensions
							};

							break;
						}

					case QueryElementType.MultiInsertStatement :
						{
							var insertType   = (MultiInsertType)ReadInt();
							var source       = Read<SqlTableLikeSource>()!;
							var inserts      = ReadList<SqlConditionalInsertClause>()!;
							var extensions   = ReadList<SqlQueryExtension>();

							obj = _statement =
								new SqlMultiInsertStatement(insertType, source, inserts)
								{
									SqlQueryExtensions = extensions
								};

							break;
						}

						case QueryElementType.ConditionalInsertClause:
						{
							var where  = Read<SqlSearchCondition>();
							var insert = Read<SqlInsertClause>()!;

							obj = new SqlConditionalInsertClause(insert, where);

							break;
						}

					case QueryElementType.SqlValuesTable:
						{
							var fields    = ReadArray<SqlField>()!;

							var rowsCount = ReadInt();
							var rows      = new List<ISqlExpression[]>(rowsCount);

							for (var i = 0; i < rowsCount; i++)
								rows.Add(ReadArray<ISqlExpression>()!);

							obj = new SqlValuesTable(fields, null, rows);

							break;
						}

					case QueryElementType.SqlAliasPlaceholder :
						{
							obj = SqlAliasPlaceholder.Instance;
							break;
						}

					case QueryElementType.SqlRow:
						{
							var values = ReadArray<ISqlExpression>()!;
							obj        = new SqlRowExpression(values);
							break;
						}

					case QueryElementType.OutputClause:
						{
							var output   = Read<SqlTable>();
							var items    = ReadArray<SqlSetExpression>()!;
							var columns  = ReadList<ISqlExpression>();

							var c = new SqlOutputClause()
							{
								OutputTable   = output,
								OutputColumns = columns
							};

							if (items is { Length : > 0 })
								c.OutputItems.AddRange(items);

							obj = c;

							break;
						}

					case QueryElementType.Comment:
						{
							obj = new SqlComment(ReadStringList());
							break;
						}

					case QueryElementType.SqlQueryExtension:
						{
							var configuration = ReadString();
							var scope         = (Sql.QueryExtensionScope)ReadInt();
							var builderType   = ReadType();
							var arguments     = new Dictionary<string,ISqlExpression>();

							var cnt = ReadInt();

							for (var j = 0; j < cnt; j++)
							{
								var key   = ReadString();
								var value = Read<ISqlExpression>();

								arguments.Add(key!, value!);
							}

							obj = new SqlQueryExtension()
							{
								Configuration = configuration,
								Scope         = scope,
								BuilderType   = builderType,
								Arguments     = arguments
							};
							break;
						}

					case QueryElementType.SqlCast:
					{
						var dataType   = ReadDbDataType();
						var expression = Read<ISqlExpression>();
						var fromType   = Read<SqlDataType>();
						var mandatory  = ReadBool();

						obj = new SqlCastExpression(expression!, dataType!, fromType, mandatory);

						break;
					}

					case QueryElementType.SqlCondition:
					{
						var condition = Read<ISqlPredicate>();
						var trueValue = Read<ISqlExpression>();
						var falseValue = Read<ISqlExpression>();

						obj = new SqlConditionExpression(condition!, trueValue!, falseValue!);

						break;
					}

					case QueryElementType.SqlCase:
					{
						var dbDataType = ReadDbDataType();
						var casesCount = ReadInt();

						var cases = new List<SqlCaseExpression.CaseItem>(casesCount);

						for (int i = 0; i < casesCount; i++)
						{
							var condition = Read<ISqlPredicate>();
							var resultExpression = Read<ISqlExpression>();

							cases.Add(new SqlCaseExpression.CaseItem(condition!, resultExpression!));
						}

						var elseExpression = Read<ISqlExpression>();

						obj = new SqlCaseExpression(dbDataType, cases, elseExpression);

						break;
					}

					case QueryElementType.CompareTo:
					{
						var expr1 = Read<ISqlExpression>();
						var expr2 = Read<ISqlExpression>();

						obj = new SqlCompareToExpression(expr1!, expr2!);

						break;
					}

					case QueryElementType.SqlCoalesce:
					{
						var expressions = ReadArray<ISqlExpression>()!;

						obj = new SqlCoalesceExpression(expressions);

						break;
					}

					default:
						throw new InvalidOperationException($"Parse not implemented for element {(QueryElementType)type}");
				}

				ObjectIndices.Add(idx, obj!);

				if (DelayedObjects.Count > 0 && DelayedObjects.TryGetValue(idx, out var action))
				{
					action(obj!);
					DelayedObjects.Remove(idx);
				}

				return true;
			}
		}

		#endregion

		#region ResultSerializer

		sealed class ResultSerializer : SerializerBase
		{
			public ResultSerializer(MappingSchema serializationMappingSchema)
				: base(serializationMappingSchema)
			{
			}

			public string Serialize(LinqServiceResult result)
			{
				Append(result.FieldCount);
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

				foreach (var data in result.Data)
				{
					foreach (var str in data)
						Append(str);

					Builder.AppendLine();
				}

				return Builder.ToString();
			}
		}

		#endregion

		#region ResultDeserializer

		sealed class ResultDeserializer : DeserializerBase
		{
			public ResultDeserializer(MappingSchema serializationMappingSchema, MappingSchema contextSchema, DataOptions options)
				: base(serializationMappingSchema, contextSchema, options)
			{
			}

			public LinqServiceResult DeserializeResult(string str)
			{
				Str = str;

				var fieldCount  = ReadInt();

				var result = new LinqServiceResult
				{
					FieldCount   = fieldCount,
					RowCount     = ReadInt(),
					QueryID      = new Guid(ReadString()!),
					FieldNames   = new string[fieldCount],
					FieldTypes   = new Type  [fieldCount],
					Data         = new List<string[]>(),
				};

				NextLine();

				for (var i = 0; i < fieldCount;  i++) { result.FieldNames  [i] = ReadString()!;              NextLine(); }
				for (var i = 0; i < fieldCount;  i++) { result.FieldTypes  [i] = ResolveType(ReadString())!; NextLine(); }

				for (var n = 0; n < result.RowCount; n++)
				{
					var data = new string[fieldCount];

					for (var i = 0; i < fieldCount; i++)
								data[i] = ReadString()!;

					result.Data.Add(data);

					NextLine();
				}

				return result;
			}
		}

		#endregion

		#region StringArraySerializer

		sealed class StringArraySerializer : SerializerBase
		{
			public StringArraySerializer(MappingSchema serializationMappingSchema)
				: base(serializationMappingSchema)
			{
			}

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

		sealed class StringArrayDeserializer : DeserializerBase
		{
			public StringArrayDeserializer(MappingSchema serializationMappingSchema, MappingSchema contextSchema, DataOptions options)
				: base(serializationMappingSchema, contextSchema, options)
			{
			}

			public string[] Deserialize(string str)
			{
				Str = str;

				var data = new string[ReadInt()];

				for (var i = 0; i < data.Length; i++)
					data[i] = ReadString()!;

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

		sealed class ArrayHelper<T> : IArrayHelper
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

		static readonly Dictionary<Type,Type>                _arrayTypes      = new ();
		static readonly Dictionary<Type,Func<object,object>> _arrayConverters = new ();

		static Type GetArrayType(Type elementType)
		{
			Type? arrayType;

			lock (_arrayTypes)
			{
				if (!_arrayTypes.TryGetValue(elementType, out arrayType))
				{
					var helper = (IArrayHelper)Activator.CreateInstance(typeof(ArrayHelper<>).MakeGenericType(elementType))!;
					_arrayTypes.Add(elementType, arrayType = helper.GetArrayType());
				}
			}

			return arrayType;
		}

		static object ConvertIEnumerableToArray(object list, Type elementType)
		{
			Func<object,object>? converter;

			lock (_arrayConverters)
			{
				if (!_arrayConverters.TryGetValue(elementType, out converter))
				{
					var helper = (IArrayHelper)Activator.CreateInstance(typeof(ArrayHelper<>).MakeGenericType(elementType))!;
					_arrayConverters.Add(elementType, converter = helper.ConvertToArray);
				}
			}

			return converter(list);
		}

		#endregion
	}
}
