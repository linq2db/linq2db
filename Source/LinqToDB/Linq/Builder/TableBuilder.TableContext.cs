using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Interceptors;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using Tools;

	partial class TableBuilder
	{
		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		public class TableContext : IBuildContext
		{
			#region Properties

#if DEBUG
			public string _sqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
			public string Path => this.GetPath();
#endif

			public ExpressionBuilder      Builder     { get; }
			public Expression?            Expression  { get; }

			public SelectQuery            SelectQuery { get; set; }
			public SqlStatement?          Statement   { get; set; }

			public List<LoadWithInfo[]>?  LoadWith    { get; set; }

			public virtual IBuildContext? Parent      { get; set; }
			public bool                   IsScalar    { get; set; }

			public Type             OriginalType     = null!;
			public Type             ObjectType       = null!;
			public EntityDescriptor EntityDescriptor = null!;
			public SqlTable         SqlTable         = null!;

			internal bool           ForceLeftJoinAssociations { get; set; }

			public bool             AssociationsToSubQueries { get; set; }

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType)
			{
				Builder          = builder;
				Parent           = buildInfo.Parent;
				Expression       = buildInfo.Expression;
				SelectQuery      = buildInfo.SelectQuery;
				AssociationsToSubQueries = buildInfo.AssociationsAsSubQueries;

				OriginalType     = originalType;
				ObjectType       = GetObjectType();
				SqlTable         = new SqlTable(builder.MappingSchema, ObjectType);
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, SqlTable table)
			{
				Builder          = builder;
				Parent           = buildInfo.Parent;
				Expression       = buildInfo.Expression;
				SelectQuery      = buildInfo.SelectQuery;
				AssociationsToSubQueries = buildInfo.AssociationsAsSubQueries;

				OriginalType     = table.ObjectType!;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			internal TableContext(ExpressionBuilder builder, SelectQuery selectQuery, SqlTable table)
			{
				Builder          = builder;
				Parent           = null;
				Expression       = null;
				SelectQuery      = selectQuery;

				OriginalType     = table.ObjectType!;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			protected TableContext(ExpressionBuilder builder, SelectQuery selectQuery)
			{
				Builder     = builder;
				SelectQuery = selectQuery;
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo)
			{
				Builder     = builder;
				Parent      = buildInfo.Parent;
				Expression  = buildInfo.Expression;
				SelectQuery = buildInfo.SelectQuery;
				AssociationsToSubQueries = buildInfo.AssociationsAsSubQueries;

				var mc   = (MethodCallExpression)Expression;
				var attr = builder.GetTableFunctionAttribute(mc.Method)!;

				if (!typeof(ITable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return Table<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
				SqlTable         = new SqlTable(builder.MappingSchema, ObjectType);
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				SelectQuery.From.Table(SqlTable);

				attr.SetTable(builder.DataContext.CreateSqlProvider(), Builder.MappingSchema, SqlTable, mc, (a, _) => builder.ConvertToSql(this, a));

				Init(true);
			}

			protected Type GetObjectType()
			{
				for (var type = OriginalType.BaseType; type != null && type != typeof(object); type = type.BaseType)
				{
					var mapping = Builder.MappingSchema.GetEntityDescriptor(type).InheritanceMapping;

					if (mapping.Count > 0)
						return type;
				}

				return OriginalType;
			}

			public List<InheritanceMapping> InheritanceMapping = null!;

			protected void Init(bool applyFilters)
			{
				Builder.Contexts.Add(this);

				InheritanceMapping = EntityDescriptor.InheritanceMapping;

				// Original table is a parent.
				//
				if (applyFilters && ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);

					if (predicate.GetType() != typeof(SqlPredicate.Expr))
						SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));
				}
			}

			#endregion

			#region BuildQuery

			static object DefaultInheritanceMappingException(object value, Type type)
			{
				throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
			}

			Dictionary<MemberInfo, Expression>? _loadWithCache;

			void SetLoadWithBindings(Type objectType, ParameterExpression parentObject, List<Expression> exprs)
			{
				var loadWith = GetLoadWith();

				if (loadWith == null)
					return;

				var members = AssociationHelper.GetLoadWith(loadWith);

				foreach (var member in members)
				{
					if (member.Info.MemberInfo.DeclaringType!.IsAssignableFrom(objectType))
					{
						var ma   = Expression.MakeMemberAccess(new ContextRefExpression(objectType, this), member.Info.MemberInfo);
						var attr = Builder.MappingSchema.GetAttribute<AssociationAttribute>(member.Info.MemberInfo.ReflectedType!, member.Info.MemberInfo);

						if (_loadWithCache == null || !_loadWithCache.TryGetValue(member.Info.MemberInfo, out var ex))
						{
							if (Builder.AssociationPath == null)
								Builder.AssociationPath = new Stack<Tuple<AccessorMember, IBuildContext, List<LoadWithInfo[]>?>>();

							Builder.AssociationPath.Push(Tuple.Create(new AccessorMember(ma), (IBuildContext)this, (List<LoadWithInfo[]>?)loadWith));

							ex = BuildExpression(ma, 1, parentObject);
							if (_loadWithCache == null)
								_loadWithCache = new Dictionary<MemberInfo, Expression>();
							_loadWithCache.Add(member.Info.MemberInfo, ex);

							_ = Builder.AssociationPath.Pop();
						}

						if (member.Info.MemberInfo.IsDynamicColumnPropertyEx())
						{
							var typeAcc = TypeAccessor.GetAccessor(member.Info.MemberInfo.ReflectedType!);
							var setter  = new MemberAccessor(typeAcc, member.Info.MemberInfo, EntityDescriptor).SetterExpression;

							exprs.Add(Expression.Invoke(setter, parentObject, ex));
						}
						else
						{
							exprs.Add(Expression.Assign(
								attr?.Storage != null
									? ExpressionHelper.PropertyOrField(parentObject, attr.Storage)
									: Expression.MakeMemberAccess(parentObject, member.Info.MemberInfo),
								ex));
						}
					}
				}
			}


			static bool IsRecord(Attribute[] attrs, out int sequence)
			{
				sequence = -1;
				var compilationMappingAttr = attrs.FirstOrDefault(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute");
				var cliMutableAttr         = attrs.FirstOrDefault(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CLIMutableAttribute");

				if (compilationMappingAttr != null)
				{
					// https://github.com/dotnet/fsharp/blob/1fcb351bb98fe361c7e70172ea51b5e6a4b52ee0/src/fsharp/FSharp.Core/prim-types.fsi
					// ObjectType = 3
					if (System.Convert.ToInt32(((dynamic)compilationMappingAttr).SourceConstructFlags) == 3)
						return false;

					sequence = ((dynamic)compilationMappingAttr).SequenceNumber;
				}

				return compilationMappingAttr != null && cliMutableAttr == null;
			}

			bool IsAnonymous(Type type)
			{
				if (!type.IsPublic &&
					 type.IsGenericType &&
					(type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) ||
					 type.Name.StartsWith("VB$AnonymousType",   StringComparison.Ordinal)))
				{
					return Builder.MappingSchema.GetAttribute<CompilerGeneratedAttribute>(type) != null;
				}

				return false;
			}

			ParameterExpression? _variable;

			Expression BuildTableExpression(bool buildBlock, Type objectType, Tuple<int, SqlField?>[] index)
			{
				if (buildBlock && _variable != null)
					return _variable;

				var entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(objectType);

				// choosing type that can be instantiated
				if ((objectType.IsInterface || objectType.IsAbstract) && !(ObjectType.IsInterface || ObjectType.IsAbstract))
				{
					objectType = ObjectType;
				}

				var expr =
					IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(objectType), out var _) ?
						BuildRecordConstructor (entityDescriptor, objectType, index, true) :
					IsAnonymous(objectType) ?
						BuildRecordConstructor (entityDescriptor, objectType, index, false) :
						BuildDefaultConstructor(entityDescriptor, objectType, index);

				expr = BuildCalculatedColumns(entityDescriptor, expr);
				expr = ProcessExpression(expr);
				expr = NotifyEntityCreated(expr);

				if (!buildBlock)
					return expr;

				return _variable = Builder.BuildVariable(expr);
			}

			[UsedImplicitly]
			static object OnEntityCreated(IDataContext context, object entity)
			{
				DataContextEventData? args = null;
				foreach (var interceptor in context.GetInterceptors<IDataContextInterceptor>())
				{
					args   = args ?? new DataContextEventData(context);
					entity = interceptor.EntityCreated(args.Value, entity);
				}

				return entity;
			}

			Expression NotifyEntityCreated(Expression expr)
			{
				expr =
					Expression.Convert(
						Expression.Call(
							MemberHelper.MethodOf(() => OnEntityCreated(null!, null!)),
							ExpressionBuilder.DataContextParam,
							expr),
						expr.Type);

				return expr;
			}

			Expression BuildCalculatedColumns(EntityDescriptor entityDescriptor, Expression expr)
			{
				if (!entityDescriptor.HasCalculatedMembers)
					return expr;

				var isBlockDisable = Builder.IsBlockDisable;

				Builder.IsBlockDisable = true;

				var variable    = Expression.Variable(expr.Type, expr.Type.ToString());
				var expressions = new List<Expression>
				{
					Expression.Assign(variable, expr)
				};

				foreach (var member in entityDescriptor.CalculatedMembers!)
				{
					var accessExpression    = Expression.MakeMemberAccess(variable, member.MemberInfo);
					var convertedExpression = Builder.ConvertExpressionTree(accessExpression);
					var selectorLambda      = Expression.Lambda(convertedExpression, variable);

					var context    = new SelectContext(Parent, selectorLambda, this);
					var expression = context.BuildExpression(null, 0, false);

					expressions.Add(Expression.Assign(accessExpression, expression));
				}

				expressions.Add(variable);

				Builder.IsBlockDisable = isBlockDisable;

				return Expression.Block(new[] { variable }, expressions);
			}

			Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index)
			{
				var members =
				(
					from idx in index
					where idx.Item1 >= 0 && idx.Item2 != null
					from cd in entityDescriptor.Columns.Where(c => c.ColumnName == idx.Item2.PhysicalName)
					where
						(cd.Storage != null ||
						 !(cd.MemberAccessor.MemberInfo is PropertyInfo info) ||
						 info.GetSetMethod(true) != null)
					select new
					{
						Column = cd,
						Expr   = new ConvertFromDataReaderExpression(cd.StorageType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)
					}
				).ToList();

				var initExpr = Expression.MemberInit(
					Expression.New(objectType),
						members
							// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
							.Where (m => !m.Column.MemberAccessor.IsComplex)
							.Select(m => (MemberBinding)Expression.Bind(m.Column.StorageInfo, m.Expr)));

				Expression expr = initExpr;

				var hasComplex = members.Count > initExpr.Bindings.Count;
				var loadWith   = GetLoadWith();

				if (hasComplex || loadWith != null)
				{
					var obj   = Expression.Variable(expr.Type);
					var exprs = new List<Expression> { Expression.Assign(obj, expr) };

					if (hasComplex)
					{
						exprs.AddRange(
							members.Where(m => m.Column.MemberAccessor.IsComplex).Select(m =>
								m.Column.MemberAccessor.SetterExpression!.GetBody(obj, m.Expr)));
					}

					if (loadWith != null)
					{
						SetLoadWithBindings(objectType, obj, exprs);
					}

					exprs.Add(obj);

					expr = Expression.Block(new[] { obj }, exprs);
				}

				return expr;
			}

			class ColumnInfo
			{
				public bool       IsComplex;
				public string     Name = null!;
				public Expression Expression = null!;
			}

			IEnumerable<Expression?> GetExpressions(TypeAccessor typeAccessor, bool isRecordType, List<ColumnInfo> columns)
			{
				var members = isRecordType ?
					typeAccessor.Members.Select(m =>
					{
						if (IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(typeAccessor.Type, m.MemberInfo), out var sequence))
							return new { m, sequence };
						return null;
					})
					.Where(_ => _ != null).OrderBy(_ => _!.sequence).Select(_ => _!.m) :
					typeAccessor.Members;

				var loadWith      = GetLoadWith();
				var loadWithItems = loadWith == null ? new List<AssociationHelper.LoadWithItem>() : AssociationHelper.GetLoadWith(loadWith);

				foreach (var member in members)
				{
					var column = columns.FirstOrDefault(c => !c.IsComplex && c.Name == member.Name);

					if (column != null)
					{
						yield return column.Expression;
					}
					else
					{
						var assocAttr = Builder.MappingSchema.GetAttributes<AssociationAttribute>(typeAccessor.Type, member.MemberInfo).FirstOrDefault();
						var isAssociation = assocAttr != null;

						if (isAssociation)
						{
							var loadWithItem = loadWithItems.FirstOrDefault(_ => _.Info.MemberInfo == member.MemberInfo);
							if (loadWithItem != null)
							{
								var ma = Expression.MakeMemberAccess(Expression.Constant(null, typeAccessor.Type), member.MemberInfo);
								yield return BuildExpression(ma, 1, false);
							}
						}
						else
						{
							var name = member.Name + '.';
							var cols = columns.Where(c => c.IsComplex && c.Name.StartsWith(name)).ToList();

							if (cols.Count == 0)
							{
								yield return null;
							}
							else
							{
								foreach (var col in cols)
								{
									col.Name      = col.Name.Substring(name.Length);
									col.IsComplex = col.Name.Contains(".");
								}

								var typeAcc  = TypeAccessor.GetAccessor(member.Type);
								var isRecord = IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(member.Type), out var _);

								var exprs = GetExpressions(typeAcc, isRecord, cols).ToList();

								if (isRecord)
								{
									var ctor      = member.Type.GetConstructors().Single();
									var ctorParms = ctor.GetParameters();

									var parms =
										(
										from p in ctorParms.Select((p, i) => new { p, i })
										join e in exprs.Select((e, i) => new { e, i }) on p.i equals e.i into j
											from e in j.DefaultIfEmpty()
											select
											e?.e ?? Expression.Constant(p.p.DefaultValue ?? Builder.MappingSchema.GetDefaultValue(p.p.ParameterType), p.p.ParameterType)
										).ToList();

									yield return Expression.New(ctor, parms);
								}
								else
								{
									var expr = Expression.MemberInit(
										Expression.New(member.Type),
										from m in typeAcc.Members.Zip(exprs, (m,e) => new { m, e })
										where m.e != null
										select (MemberBinding)Expression.Bind(m.m.MemberInfo, m.e));

									yield return expr;
								}
							}
						}
					}
				}
			}

			Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index, bool isRecord)
			{
				var ctor = objectType.GetConstructors().Single();

				var exprs = GetExpressions(entityDescriptor.TypeAccessor, isRecord,
					(
						from idx in index
						where idx.Item1 >= 0 && idx.Item2 != null
						from cd in entityDescriptor.Columns.Where(c => c.ColumnName == idx.Item2.PhysicalName)
						select new ColumnInfo
						{
							IsComplex  = cd.MemberAccessor.IsComplex,
							Name       = cd.MemberName,
							Expression = new ConvertFromDataReaderExpression(cd.MemberType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)
						}
					).ToList()).ToList();

				var parms =
				(
					from p in ctor.GetParameters().Select((p,i) => new { p, i })
					join e in exprs.Select((e,i) => new { e, i }) on p.i equals e.i into j
					from e in j.DefaultIfEmpty()
					select e?.e ?? Expression.Constant(Builder.MappingSchema.GetDefaultValue(p.p.ParameterType), p.p.ParameterType)
				).ToList();

				var expr = Expression.New(ctor, parms);

				return expr;
			}

			protected virtual Expression ProcessExpression(Expression expression)
			{
				return expression;
			}

			Tuple<int, SqlField?>[] BuildIndex(Tuple<int, SqlField?>[] index, Type objectType)
			{
				var names = new Dictionary<string,int>();
				var n     = 0;
				var ed    = Builder.MappingSchema.GetEntityDescriptor(objectType);

				foreach (var cd in ed.Columns)
					if (cd.MemberAccessor.TypeAccessor.Type == ed.TypeAccessor.Type)
						names.Add(cd.MemberName, n++);

				var q =
					from r in SqlTable.Fields.Select((f,i) => new { f, i })
					where names.ContainsKey(r.f.Name)
					orderby names[r.f.Name]
					select index[r.i];

				return q.ToArray();
			}

			protected virtual Expression BuildQuery(Type tableType, TableContext tableContext, ParameterExpression? parentObject)
			{
				SqlInfo[] info;

				if (IsScalarSet())
				{
					info = ConvertToIndex(null, 0, ConvertFlags.All);
					if (info.Length != 1)
						throw new LinqToDBException($"Invalid scalar type processing for type '{tableType.Name}'.");
					var parentIndex = ConvertToParentIndex(info[0].Index, this);
					return Builder.BuildSql(tableType, parentIndex, info[0].Sql);
				}

				if (ObjectType == tableType)
				{
					info = ConvertToIndex(null, 0, ConvertFlags.All);
				}
				else
				{
					info = ConvertToSql(null, 0, ConvertFlags.All);

					var table = new SqlTable(Builder.MappingSchema, tableType);

					var matchedFields = new List<SqlInfo>();
					foreach (var field in table.Fields)
					{
						foreach (var sqlInfo in info)
						{
							var sqlField = (SqlField)sqlInfo.Sql;
							var found = sqlField.Name == field.Name;

							if (!found)
							{
								found = EntityDescriptor.Aliases != null &&
								        EntityDescriptor.Aliases.TryGetValue(field.Name, out var alias) &&
								        alias == sqlField.Name;
							}

							if (found)
							{
								matchedFields.Add(GetIndex(sqlInfo));
								break;
							}
						}
					}

					info = matchedFields.ToArray();
				}

				var index = info.Select(idx => Tuple.Create(ConvertToParentIndex(idx.Index, this), QueryHelper.GetUnderlyingField(idx.Sql))).ToArray();

				if (ObjectType != tableType || InheritanceMapping.Count == 0)
					return BuildTableExpression(!Builder.IsBlockDisable, tableType, index);

				Expression expr;

				var defaultMapping = InheritanceMapping.SingleOrDefault(m => m.IsDefault);

				if (defaultMapping != null)
				{
					expr = Expression.Convert(
						BuildTableExpression(false, defaultMapping.Type, BuildIndex(index, defaultMapping.Type)),
						ObjectType);
				}
				else
				{
					var exceptionMethod = MemberHelper.MethodOf(() => DefaultInheritanceMappingException(null!, null!));
					var field           = SqlTable[InheritanceMapping[0].DiscriminatorName] ?? throw new LinqException($"Field {InheritanceMapping[0].DiscriminatorName} not found in table {SqlTable}");
					var dindex          = ConvertToParentIndex(_indexes[field].Index, this);

					expr = Expression.Convert(
						Expression.Call(null, exceptionMethod,
							new ConvertFromDataReaderExpression(typeof(object), dindex, null, ExpressionBuilder.DataReaderParam),
							Expression.Constant(ObjectType)),
						ObjectType);
				}

				foreach (var mapping in InheritanceMapping.Select((m,i) => new { m, i }).Where(m => m.m != defaultMapping))
				{
					var field  = SqlTable[InheritanceMapping[mapping.i].DiscriminatorName] ?? throw new LinqException($"Field {InheritanceMapping[mapping.i].DiscriminatorName} not found in table {SqlTable}");
					var dindex = ConvertToParentIndex(_indexes[field].Index, this);

					Expression testExpr;

					var isNullExpr = Expression.Call(
						ExpressionBuilder.DataReaderParam,
						ReflectionHelper.DataReader.IsDBNull,
						Expression.Constant(dindex));

					if (mapping.m.Code == null)
					{
						testExpr = isNullExpr;
					}
					else
					{
						var codeType = mapping.m.Code.GetType();

						testExpr = ExpressionBuilder.Equal(
							Builder.MappingSchema,
							Builder.BuildSql(codeType, dindex, mapping.m.Discriminator.ValueConverter),
							Expression.Constant(mapping.m.Code));

						if (mapping.m.Discriminator.CanBeNull)
						{
							testExpr =
								Expression.AndAlso(
									Expression.Not(isNullExpr),
									testExpr);
						}
					}

					expr = Expression.Condition(
						testExpr,
						Expression.Convert(BuildTableExpression(false, mapping.m.Type, BuildIndex(index, mapping.m.Type)), ObjectType),
						expr);
				}

				return expr;
			}

			private bool IsScalarSet()
			{
				return IsScalar || Builder.MappingSchema.IsScalarType(OriginalType);
			}

			public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildQuery(typeof(T), this, null);
				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			#endregion

			#region BuildExpression

			public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				return BuildExpression(expression, level, null);
			}

			Expression BuildExpression(Expression? expression, int level, ParameterExpression? parentObject)
			{

				if (expression == null)
				{
					return BuildQuery(OriginalType, this, parentObject);
				}

				// Build table.
				//

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
				var descriptor      = GetAssociationDescriptor(levelExpression, out _);
				if (descriptor?.IsList == true)
				{
					if (Builder.GetRootObject(expression) is ContextRefExpression)
						return EagerLoading.GenerateAssociationExpression(Builder, this, expression, descriptor)!;

					return Builder.BuildMultipleQuery(this, expression, false);
				}

				var contextInfo = FindContextExpression(expression, level, false, false);

				if (contextInfo == null)
				{
					if (expression is MemberExpression memberExpression)
					{
						if (EntityDescriptor != null &&
							EntityDescriptor.TypeAccessor.Type == memberExpression.Member.DeclaringType)
						{
							return new DefaultValueExpression(Builder.MappingSchema, memberExpression.Type);
						}
					}

					throw new LinqException("'{0}' cannot be converted to SQL.", expression);
				}

				if (contextInfo.Field == null)
				{
					Expression expr;

					if (contextInfo.CurrentExpression == null)
						throw new InvalidOperationException("contextInfo.CurrentExpression is null");

					var maxLevel = contextInfo.CurrentExpression.GetLevel(Builder.MappingSchema);

					if (contextInfo.CurrentLevel + 1 > maxLevel)
						expr = contextInfo.Context.BuildExpression(null, 0, false);
					else
						expr = contextInfo.Context.BuildExpression(contextInfo.CurrentExpression, contextInfo.CurrentLevel + 1, false);

					return expr;
				}

				// Build field.
				//
				var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
				var idx  = ConvertToParentIndex(info.Index, null);

				return Builder.BuildSql(expression!, idx, info.Sql);
			}

			#endregion

			#region ConvertToSql

			public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.All   :
						{
							var contextInfo = FindContextExpression(expression, level, false, true)!;

							if (contextInfo.Field == null)
							{
								SqlInfo[] result;

								if (!IsScalarSet())
								{
									// Handling case with Associations
									//
									if (contextInfo.Context != this)
									{
										result = contextInfo.Context.ConvertToIndex(contextInfo.CurrentExpression, contextInfo.CurrentLevel, flags);
									}
									else

									{
										result = SqlTable.Fields
											.Where(field => !field.IsDynamic && !field.SkipOnEntityFetch)
											.Select(f =>
												f.ColumnDescriptor != null
													? new SqlInfo(f.ColumnDescriptor.MemberInfo, f)
													: new SqlInfo(f))
											.ToArray();
									}

								}
								else
								{
									result = new[]
									{
										new SqlInfo(SqlTable)
									};
								}

								return result;
							}
							break;
						}

					case ConvertFlags.Key   :
						{
							var contextInfo = FindContextExpression(expression, level, false, true)!;

							if (contextInfo.Field == null)
							{
								if (contextInfo.Context != this)
								{
									SqlInfo[] resultSql;
									var maxLevel = contextInfo.CurrentExpression!.GetLevel(Builder.MappingSchema);
									if (maxLevel == 0)
									{
										resultSql = contextInfo.Context.ConvertToSql(null, 0, flags);
									}
									else
									{
										resultSql = contextInfo.Context.ConvertToSql(contextInfo.CurrentExpression,
											contextInfo.CurrentLevel >= maxLevel
												? contextInfo.CurrentLevel
												: contextInfo.CurrentLevel + 1, flags);
									}

									if (contextInfo.AsSubquery)
									{
										resultSql = new[]
										{
											new SqlInfo(contextInfo.Context.SelectQuery, SelectQuery)
										};
									}

									return resultSql;
								}

								if (IsScalarSet())
								{
									var result = new[]
									{
										new SqlInfo(SqlTable)
									};
									return result;
								}

								var q =
									from field in SqlTable.Fields
									where field.IsPrimaryKey
									orderby field.PrimaryKeyOrder
									select new SqlInfo(field.ColumnDescriptor.MemberInfo, field, SelectQuery);

								var key = q.ToArray();

								return key.Length != 0 ? key : ConvertToSql(expression, level, ConvertFlags.All);
							}
							else
							{
								return new[]
								{
									// ???
									new SqlInfo(QueryHelper.GetUnderlyingField(contextInfo.Field)?.ColumnDescriptor.MemberInfo!, contextInfo.Field)
								};
							}
						}

					case ConvertFlags.Field :
						{
							var contextInfo = FindContextExpression(expression, level, false, false);
							if (contextInfo == null)
							{
								if (expression == null)
									throw new InvalidOperationException();

								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

								throw new LinqException($"Expression '{levelExpression}' is not a Field.");
							}

							if (contextInfo.Field != null)
								return new[]
								{
									// ???
									new SqlInfo(QueryHelper.GetUnderlyingField(contextInfo.Field)?.ColumnDescriptor.MemberInfo!, contextInfo.Field, SelectQuery)
								};

							if (contextInfo.CurrentExpression == null)
							{
								return new[]
								{
									new SqlInfo
									(
										SqlTable.All
									)
								};
							}

							SqlInfo[] resultSql;
							var maxLevel = contextInfo.CurrentExpression.GetLevel(Builder.MappingSchema);
							if (contextInfo.Context == this)
							{
								if (maxLevel == 0)
									resultSql = contextInfo.Context.ConvertToSql(null, 0, flags);
								else
									resultSql = contextInfo.Context.ConvertToSql(contextInfo.CurrentExpression,
										contextInfo.CurrentLevel + 1, flags);
							}
							else
							{
								if (maxLevel == 0)
									resultSql = contextInfo.Context.ConvertToIndex(null, 0, flags);
								else
									resultSql = contextInfo.Context.ConvertToIndex(contextInfo.CurrentExpression,
										contextInfo.CurrentLevel + 1, flags);

								if (contextInfo.AsSubquery)
								{
									resultSql = new[]
									{
										new SqlInfo(contextInfo.Context.SelectQuery, SelectQuery)
									};
								}
							}
							return resultSql;
						}
				}

				throw new NotImplementedException();
			}

			#endregion

			#region ConvertToIndex

			readonly Dictionary<ISqlExpression,SqlInfo> _indexes = new ();

			protected virtual SqlInfo GetIndex(SqlInfo expr)
			{
				if (_indexes.TryGetValue(expr.Sql, out var n))
					return n;

				int index;
				if (expr.Sql is SqlField field)
				{
					index = SelectQuery.Select.Add(field, field.Alias);
				}
				else
				{
					index = SelectQuery.Select.Add(expr.Sql);
				}

				var newExpr = new SqlInfo
				(
					expr.MemberChain,
					SelectQuery.Select.Columns[index],
					SelectQuery,
					index
				);

				_indexes.Add(expr.Sql, newExpr);

				return newExpr;
			}

			public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :

						var info = ConvertToSql(expression, level, flags);

						for (var i = 0; i < info.Length; i++)
							info[i] = GetIndex(info[i]);

						return info;
				}

				throw new NotImplementedException();
			}

			#endregion

			#region IsExpression

			public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				switch (requestFlag)
				{
					case RequestFor.Field      :
						{
							if (expression == null || expression.GetLevel(Builder.MappingSchema) == 0)
								return IsExpressionResult.False;

							var contextInfo = FindContextExpression(expression, level, false, false);
							if (contextInfo == null)
								return IsExpressionResult.False;

							if (contextInfo.Field != null)
								return IsExpressionResult.True;

							if (contextInfo.CurrentExpression == null
							    || contextInfo.CurrentExpression.GetLevel(Builder.MappingSchema) == contextInfo.CurrentLevel)
								return IsExpressionResult.False;

							return contextInfo.Context.IsExpression(contextInfo.CurrentExpression,
								contextInfo.CurrentLevel + 1, requestFlag);
						}

					case RequestFor.Table       :
					case RequestFor.Object      :
						{
							if (expression == null)
							{
								if (!IsScalarSet())
									return new IsExpressionResult(true, this);
								return IsExpressionResult.False;
							}

							var contextInfo = FindContextExpression(expression, level, false, false);
							if (contextInfo == null)
								return IsExpressionResult.False;

							if (contextInfo.Field != null)
								return IsExpressionResult.False;

							if (contextInfo.CurrentExpression == null
							    || contextInfo.CurrentExpression.GetLevel(Builder.MappingSchema) == contextInfo.CurrentLevel)
								return new IsExpressionResult(true, contextInfo.Context);

							return contextInfo.Context.IsExpression(contextInfo.CurrentExpression,
								contextInfo.CurrentLevel + 1, requestFlag);

						}

					case RequestFor.Expression :
						{
							if (expression == null)
								return IsExpressionResult.False;

							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess:
								case ExpressionType.Call:
									{
										var descriptor = GetAssociationDescriptor(levelExpression, out _);
										if (descriptor != null)
											return IsExpressionResult.False;

										var contextInfo = FindContextExpression(expression, level, false, false);
										return new IsExpressionResult(contextInfo?.Context == null);
									}
								case ExpressionType.Extension:
									{
										if (levelExpression is ContextRefExpression)
											return IsExpressionResult.False;
										break;
									}
								case ExpressionType.Parameter    :
									{
										if (IsExpression(expression, level, RequestFor.Object).Result)
											return IsExpressionResult.False;
										return IsExpressionResult.True;
									}
							}

							return IsExpressionResult.True;
						}

					case RequestFor.Association      :
						{
							if (expression == null)
								return IsExpressionResult.False;

							var result = IsExpression(expression, level, RequestFor.Table);
							if (!result.Result || !(result.Context is AssociationContext))
								return IsExpressionResult.False;

							return result;
						}
				}

				return IsExpressionResult.False;
			}

			#endregion

			#region GetContext

			Dictionary<AccessorMember, Tuple<IBuildContext, bool>>? _associationContexts;
			Dictionary<AccessorMember, IBuildContext>? _collectionAssociationContexts;


			public IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (expression == null)
				{
					if (buildInfo != null && buildInfo.IsSubQuery)
					{
						var table = new TableContext(
							Builder,
							new BuildInfo(Parent is SelectManyBuilder.SelectManyContext ? this : Parent, Expression!, buildInfo.SelectQuery),
							SqlTable.ObjectType!);

						return table;
					}

					return this;
				}

				if (buildInfo != null)
				{

					if (buildInfo.IsSubQuery)
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

						if (levelExpression == expression && expression.NodeType == ExpressionType.MemberAccess ||
						    expression.NodeType == ExpressionType.Call)
						{
							var tableLevel  = FindContextExpression(expression, level, true, true)!;

							if (tableLevel.Descriptor?.IsList == true)
							{
								Expression ma;
								switch (buildInfo.Expression)
								{
									case MemberExpression me:
										{
											ma = me.Expression;
											break;
										}
									case MethodCallExpression mc:
										{
											ma = mc.Method.IsStatic ? mc.Arguments[0] : mc.Object;
											break;
										}

									default:
										ma = Builder.GetRootObject(buildInfo.Expression);
										break;

								}

								var elementType     = tableLevel.Descriptor.GetElementType(Builder.MappingSchema);
								var parentExactType = ma.Type;

								var queryMethod = AssociationHelper.CreateAssociationQueryLambda(
									Builder, new AccessorMember(expression), tableLevel.Descriptor, OriginalType, parentExactType, elementType,
									false, false, GetLoadWith(), out _);
								;
								var expr   = queryMethod.GetBody(ma);

								buildInfo.IsAssociationBuilt = true;

								return Builder.BuildSequence(new BuildInfo(buildInfo, expr));
							}
						}
						else
						{
							var tableLevel  = FindContextExpression(expression, level, true, true)!;

							var result = tableLevel.Context.GetContext(tableLevel.CurrentExpression, tableLevel.CurrentLevel + 1, buildInfo);
							if (result == null)
								throw new LinqException($"Can not build association for expression '{tableLevel.CurrentExpression}'");
							return result;
						}
					}
				}

				throw new InvalidOperationException();
			}

			public virtual SqlStatement GetResultStatement()
			{
				return Statement ??= new SqlSelectStatement(SelectQuery);
			}

			public void CompleteColumns()
			{
			}

			#endregion

			#region ConvertToParentIndex

			public virtual int ConvertToParentIndex(int index, IBuildContext? context)
			{
				if (context != null && context != this && context.SelectQuery != SelectQuery)
				{
					var sqlInfo = new SqlInfo(context.SelectQuery.Select.Columns[index], context.SelectQuery, index);
					sqlInfo = GetIndex(sqlInfo);
					index = sqlInfo.Index;
				}

				return Parent?.ConvertToParentIndex(index, this) ?? index;
			}

			#endregion

			#region SetAlias

			public void SetAlias(string alias)
			{
				if (alias == null || SqlTable == null)
					return;

				if (!alias.Contains('<'))
					if (SqlTable.Alias == null)
						SqlTable.Alias = alias;
			}

			#endregion

			#region GetSubQuery

			public ISqlExpression? GetSubQuery(IBuildContext context)
			{
				return null;
			}

			#endregion

			#region Helpers

			protected List<LoadWithInfo[]>? GetLoadWith()
			{
				return LoadWith;
			}

			protected ISqlExpression? GetField(Expression expression, int level, bool throwException)
			{
				expression = expression.SkipPathThrough();

				if (expression.NodeType == ExpressionType.MemberAccess)
				{
					var memberExpression = (MemberExpression)expression;

					if (EntityDescriptor.Aliases != null)
					{
						if (EntityDescriptor.Aliases.TryGetValue(memberExpression.Member.Name, out var aliasName))
						{
							var alias = EntityDescriptor[aliasName!];

							if (alias == null)
							{
								foreach (var column in EntityDescriptor.Columns)
								{
									if (column.MemberInfo.EqualsTo(memberExpression.Member, SqlTable.ObjectType))
									{
										expression = memberExpression = ExpressionHelper.PropertyOrField(
											Expression.Convert(memberExpression.Expression, column.MemberInfo.DeclaringType), column.MemberName);
										break;
									}
								}
							}
							else
							{
								var expr = memberExpression.Expression;

								if (alias.MemberInfo.DeclaringType != memberExpression.Member.DeclaringType)
									expr = Expression.Convert(memberExpression.Expression, alias.MemberInfo.DeclaringType);

								expression = memberExpression = ExpressionHelper.PropertyOrField(expr, alias.MemberName);
							}
						}
					}

					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						if (levelExpression != expression)
						{
							var levelMember = (MemberExpression)levelExpression;

							if (memberExpression.Member.IsNullableValueMember() && memberExpression.Expression == levelExpression)
								memberExpression = levelMember;
							else
							{
								var sameType =
									levelMember.Member.ReflectedType == SqlTable.ObjectType ||
									levelMember.Member.DeclaringType     == SqlTable.ObjectType;

								if (!sameType)
								{
									var mi = SqlTable.ObjectType!.GetInstanceMemberEx(levelMember.Member.Name);
									sameType = mi.Any(_ => _.DeclaringType == levelMember.Member.DeclaringType);
								}

								if (sameType || InheritanceMapping.Count > 0)
								{
									string? pathName = null;
									foreach (var field in SqlTable.Fields)
									{
										var name = levelMember.Member.Name;
										if (field.Name.IndexOf('.') >= 0)
										{
											if (pathName == null)
											{
												var suffix = string.Empty;
												for (var ex = (MemberExpression)expression;
													ex != levelMember;
													ex = (MemberExpression)ex.Expression)
												{
													suffix = string.IsNullOrEmpty(suffix)
														? ex.Member.Name
														: ex.Member.Name + "." + suffix;
												}

												pathName = !string.IsNullOrEmpty(suffix) ? name + "." + suffix : name;
											}

											if (field.Name == pathName)
												return field;
										}
										else if (field.Name == name)
											return field;
									}
								}
							}
						}

						if (levelExpression == memberExpression)
						{
							foreach (var field in SqlTable.Fields)
							{
								if (field.ColumnDescriptor.MemberInfo.EqualsTo(memberExpression.Member, SqlTable.ObjectType))
								{
									if (field.ColumnDescriptor.MemberAccessor.IsComplex
										&& !field.ColumnDescriptor.MemberAccessor.MemberInfo.IsDynamicColumnPropertyEx())
									{
										var name = memberExpression.Member.Name;
										var me   = memberExpression;

										if (me.Expression is MemberExpression)
										{
											while (me.Expression is MemberExpression me1)
											{
												me   = me1;
												name = me.Member.Name + '.' + name;
											}

											var fld = SqlTable[name];

											if (fld != null)
												return fld;
										}
									}
									else
									{
										return field;
									}
								}

								if (InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
									foreach (var mapping in InheritanceMapping)
										foreach (var mm in Builder.MappingSchema.GetEntityDescriptor(mapping.Type).Columns)
											if (mm.MemberAccessor.MemberInfo.EqualsTo(memberExpression.Member))
												return field;

								if (memberExpression.Member.IsDynamicColumnPropertyEx())
								{
									var fieldName = memberExpression.Member.Name;

									// do not add association columns
									if (EntityDescriptor.Associations.All(a => a.MemberInfo != memberExpression.Member))
									{
										var newField = SqlTable[fieldName];
										if (newField == null)
										{
											newField = new SqlField(new ColumnDescriptor(
												Builder.MappingSchema,
												new ColumnAttribute(fieldName),
												new MemberAccessor(EntityDescriptor.TypeAccessor, memberExpression.Member, EntityDescriptor)))
												{
													IsDynamic        = true,
												};

											SqlTable.Add(newField);
										}

										return newField;
									}
								}
							}

							if (throwException &&
								EntityDescriptor != null &&
								EntityDescriptor.TypeAccessor.Type == memberExpression.Member.DeclaringType)
							{
								throw new LinqException("Member '{0}.{1}' is not a table column.",
									memberExpression.Member.DeclaringType.Name, memberExpression.Member.Name);
							}
						}
					}
				}

				return null;
			}

			class ContextInfo
			{
				public ContextInfo(IBuildContext context, ISqlExpression? field, Expression? currentExpression, int currentLevel)
				{
					Context           = context;
					Field             = field;
					CurrentExpression = currentExpression;
					CurrentLevel      = currentLevel;
				}

				public ContextInfo(IBuildContext context, Expression? currentExpression, int currentLevel):
					this(context, null, currentExpression, currentLevel)
				{
				}

				public IBuildContext          Context;
				public Expression?            CurrentExpression;
				public ISqlExpression?        Field;
				public int                    CurrentLevel;
				public bool                   AsSubquery;
				public AssociationDescriptor? Descriptor;
			}

			ContextInfo? FindContextExpression(Expression? expression, int level, bool forceInner, bool throwExceptionForNull)
			{
				if (expression == null)
				{
					return new ContextInfo(this, null, 0);
				}

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				switch (levelExpression.NodeType)
				{
					case ExpressionType.Parameter    :
						{
							return new ContextInfo(this, expression, level);
						}
					case ExpressionType.MemberAccess:
						{
							var field = GetField(expression, level, false);
							if (field != null)
							{
								return new ContextInfo(this, field, expression, level);
							}

							goto case ExpressionType.Call;
						}
					case ExpressionType.Call         :
						{
							var descriptor = GetAssociationDescriptor(levelExpression, out var accessorMember);
							if (descriptor != null && accessorMember != null)
							{
								var isOuter = descriptor.CanBeNull || ForceLeftJoinAssociations;
								IBuildContext? associatedContext;

								if (!descriptor.IsList && !AssociationsToSubQueries)
								{
									if (_associationContexts == null ||
									    !_associationContexts.TryGetValue(accessorMember, out var foundInfo))
									{

										if (forceInner)
											isOuter = false;
										else if (!isOuter)
										{
											var ctx = Parent;
											while (ctx is SubQueryContext)
											{
												ctx = ctx.Parent;
											}

											if (ctx is DefaultIfEmptyBuilder.DefaultIfEmptyContext)
												isOuter = true;
										}

										var newExpression = expression.Replace(levelExpression,
											new ContextRefExpression(levelExpression.Type, this));

										associatedContext = AssociationHelper.BuildAssociationInline(Builder,
											new BuildInfo(Parent, newExpression, SelectQuery), this, accessorMember, descriptor,
											!forceInner,
											ref isOuter);

										_associationContexts ??= new Dictionary<AccessorMember, Tuple<IBuildContext, bool>>();
										_associationContexts.Add(accessorMember, Tuple.Create(associatedContext, isOuter));
									}
									else
									{
										associatedContext = foundInfo.Item1;
									}

									var contextRef = new ContextRefExpression(levelExpression.Type, associatedContext);
									return new ContextInfo(associatedContext, expression.Replace(levelExpression, contextRef), 0) {Descriptor = descriptor};
								}
								else
								{
									if (AssociationsToSubQueries || _collectionAssociationContexts == null || !_collectionAssociationContexts.TryGetValue(accessorMember, out associatedContext))
									{
										var newExpression = expression.Replace(levelExpression,
											new ContextRefExpression(levelExpression.Type, this));

										associatedContext = AssociationHelper.BuildAssociationSelectMany(Builder,
											new BuildInfo(Parent, newExpression, new SelectQuery()),
											this, accessorMember, descriptor,
											ref isOuter);

										if (!AssociationsToSubQueries)
										{
											_collectionAssociationContexts ??= new Dictionary<AccessorMember, IBuildContext>();
											_collectionAssociationContexts.Add(accessorMember, associatedContext);
										}
										else
										{
											associatedContext.SelectQuery.ParentSelect = SelectQuery;
										}

									}
									var contextRef =
										new ContextRefExpression(levelExpression.Type, associatedContext);
									return new ContextInfo(associatedContext,
										expression.Replace(levelExpression, contextRef), 0)
									{
										Descriptor = descriptor,
										AsSubquery = AssociationsToSubQueries || descriptor.IsList
									};
								}

							}

							break;
						}
					default:
						{
							if (levelExpression is ContextRefExpression refExpression)
							{
								return new ContextInfo(refExpression.BuildContext, expression, level);
							}
							break;
						}
				}

				if (throwExceptionForNull)
					throw new LinqException($"Expression '{expression}' ({levelExpression}) is not a table.");

				return null;
			}

			AssociationDescriptor? GetAssociationDescriptor(Expression expression, out AccessorMember? memberInfo, bool onlyCurrent = true)
			{
				memberInfo = null;
				if (expression.NodeType.In(ExpressionType.MemberAccess, ExpressionType.Call))
					memberInfo = new AccessorMember(expression);

				if (memberInfo == null)
					return null;

				var descriptor = GetAssociationDescriptor(memberInfo, EntityDescriptor);
				if (descriptor == null && !onlyCurrent && memberInfo.MemberInfo.DeclaringType != ObjectType)
					descriptor = GetAssociationDescriptor(memberInfo, Builder.MappingSchema.GetEntityDescriptor(memberInfo.MemberInfo.DeclaringType!));

				return descriptor;
			}

			AssociationDescriptor? GetAssociationDescriptor(AccessorMember accessorMember, EntityDescriptor entityDescriptor)
			{
				AssociationDescriptor? descriptor = null;

				if (accessorMember.MemberInfo.MemberType == MemberTypes.Method)
				{
					var attribute = Builder.MappingSchema.GetAttribute<AssociationAttribute>(accessorMember.MemberInfo.DeclaringType!, accessorMember.MemberInfo, a => a.Configuration);

					if (attribute != null)
						descriptor = new AssociationDescriptor
						(
							entityDescriptor.ObjectType,
							accessorMember.MemberInfo,
							attribute.GetThisKeys(),
							attribute.GetOtherKeys(),
							attribute.ExpressionPredicate,
							attribute.Predicate,
							attribute.QueryExpressionMethod,
							attribute.QueryExpression,
							attribute.Storage,
							attribute.CanBeNull,
							attribute.AliasName
						);
				}
				else if (accessorMember.MemberInfo.MemberType == MemberTypes.Property || accessorMember.MemberInfo.MemberType == MemberTypes.Field)
				{
					var inheritance      =
					(
						from m in entityDescriptor.InheritanceMapping
						let om = Builder.MappingSchema.GetEntityDescriptor(m.Type)
						where om.Associations.Count > 0
						select om
					).ToList();

					descriptor = entityDescriptor.Associations
						.Concat(inheritance.SelectMany(om => om.Associations))
						.FirstOrDefault(a => a.MemberInfo.EqualsTo(accessorMember.MemberInfo));
				}

				return descriptor;
			}

			#endregion
		}
	}
}
