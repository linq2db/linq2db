using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using Common;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

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

			public ExpressionBuilder   Builder     { get; }
			public Expression?         Expression  { get; }

			public SelectQuery         SelectQuery { get; set; }
			public SqlStatement?       Statement   { get; set; }

			public List<Tuple<MemberInfo, Expression?>[]>? LoadWith    { get; set; }

			public virtual IBuildContext? Parent   { get; set; }

			public Type             OriginalType = null!;
			public Type             ObjectType = null!;
			public EntityDescriptor EntityDescriptor = null!;
			public SqlTable         SqlTable = null!;

			internal bool           ForceLeftJoinAssociations { get; set; }

			private readonly bool   _associationsToSubQueries;
			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType)
			{
				Builder          = builder;
				Parent           = buildInfo.Parent;
				Expression       = buildInfo.Expression;
				SelectQuery      = buildInfo.SelectQuery;
				_associationsToSubQueries = buildInfo.AssociationsAsSubQueries;

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
				_associationsToSubQueries = buildInfo.AssociationsAsSubQueries;

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
				_associationsToSubQueries = buildInfo.AssociationsAsSubQueries;

				var mc   = (MethodCallExpression)Expression;
				var attr = builder.GetTableFunctionAttribute(mc.Method)!;

				if (!typeof(ITable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return Table<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
				SqlTable         = new SqlTable(builder.MappingSchema, ObjectType);
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				SelectQuery.From.Table(SqlTable);

				var args = mc.Arguments.Select(a => builder.ConvertToSql(this, a));

				attr.SetTable(Builder.MappingSchema, SqlTable, mc.Method, mc.Arguments, args);

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
					if (member.MemberInfo.DeclaringType.IsAssignableFrom(objectType))
					{
						var ma = Expression.MakeMemberAccess(new ContextRefExpression(objectType, this), member.MemberInfo);

						// if (member.NextLoadWith.Count > 0)
						// {
						// 	var table = FindTable(ma, 1, false, true)!;
						// 	if (table.Table is TableContext tableContext)
						// 		tableContext.LoadWith = member.NextLoadWith;
						// }

						var attr = Builder.MappingSchema.GetAttribute<AssociationAttribute>(member.MemberInfo.ReflectedType, member.MemberInfo);


						if (_loadWithCache == null || !_loadWithCache.TryGetValue(member.MemberInfo, out var ex))
						{
							if (Builder.AssociationPath == null)
								Builder.AssociationPath = new Stack<Tuple<MemberInfo, IBuildContext>>();

							Builder.AssociationPath.Push(Tuple.Create(member.MemberInfo, (IBuildContext)this));

							ex = BuildExpression(ma, 1, parentObject);
							if (_loadWithCache == null)
								_loadWithCache = new Dictionary<MemberInfo, Expression>();
							_loadWithCache.Add(member.MemberInfo, ex);

							_ = Builder.AssociationPath.Pop();
						}

						if (member.MemberInfo.IsDynamicColumnPropertyEx())
						{
							var typeAcc = TypeAccessor.GetAccessor(member.MemberInfo.ReflectedType);
							var setter  = new MemberAccessor(typeAcc, member.MemberInfo, EntityDescriptor).SetterExpression;

							exprs.Add(Expression.Invoke(setter, parentObject, ex));
						}
						else
						{
							exprs.Add(Expression.Assign(
								attr?.Storage != null
									? ExpressionHelper.PropertyOrField(parentObject, attr.Storage)
									: Expression.MakeMemberAccess(parentObject, member.MemberInfo),
								ex));
						}
					}
				}
			}


			static bool IsRecord(Attribute[] attrs)
			{
				return attrs.Any(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute")
					&& attrs.All(attr => attr.GetType().FullName != "Microsoft.FSharp.Core.CLIMutableAttribute");
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

			Expression BuildTableExpression(bool buildBlock, Type objectType, int[] index)
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
					IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(objectType)) ?
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
				var onEntityCreated = context.OnEntityCreated;

				if (onEntityCreated != null)
				{
					var args = new EntityCreatedEventArgs
					{
						Entity      = entity,
						DataContext = context
					};

					onEntityCreated(args);

					return args.Entity;
				}

				return entity;
			}

			Expression NotifyEntityCreated(Expression expr)
			{
				if (Builder.DataContext is IEntityServices)
				{
//					var cex = Expression.Convert(ExpressionBuilder.DataContextParam, typeof(INotifyEntityCreated));
//
//					expr =
//						Expression.Convert(
//							Expression.Call(
//								cex,
//								MemberHelper.MethodOf((INotifyEntityCreated n) => n.EntityCreated(null)),
//								expr),
//							expr.Type);
					expr =
						Expression.Convert(
							Expression.Call(
								MemberHelper.MethodOf(() => OnEntityCreated(null!, null!)),
								ExpressionBuilder.DataContextParam,
								expr),
							expr.Type);
				}


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

					IBuildContext context;
					var buildInfo = new BuildInfo(this, selectorLambda.Body, new SelectQuery());

					if (Builder.IsSequence(buildInfo))
					{
						var expressionCtx = new ExpressionContext(Parent, this, selectorLambda);
						buildInfo         = new BuildInfo(expressionCtx, selectorLambda.Body, new SelectQuery());
						context           = Builder.BuildSequence(buildInfo);
						Builder.ReplaceParent(expressionCtx, this);
					}
					else
					{
						context = new SelectContext(Parent, selectorLambda, this);
					}

					var expression          = context.BuildExpression(null, 0, false);

					expressions.Add(Expression.Assign(accessExpression, expression));
				}

				expressions.Add(variable);

				Builder.IsBlockDisable = isBlockDisable;

				return Expression.Block(new[] { variable }, expressions);
			}

			Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType, int[] index)
			{
				var members =
				(
					from idx in index.Select((n,i) => new { n, i })
					where idx.n >= 0
					let   cd = entityDescriptor.Columns[idx.i]
					where
						cd.Storage != null ||
						!(cd.MemberAccessor.MemberInfo is PropertyInfo) ||
						((PropertyInfo)cd.MemberAccessor.MemberInfo).GetSetMethod(true) != null
					select new
					{
						Column = cd,
						Expr   = new ConvertFromDataReaderExpression(cd.StorageType, idx.n, Builder.DataReaderLocal)
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

			IEnumerable<Expression?>? GetExpressions(TypeAccessor typeAccessor, bool isRecordType, List<ColumnInfo> columns)
			{
				var members = isRecordType ?
					typeAccessor.Members.Where(m =>
						IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(typeAccessor.Type, m.MemberInfo))) :
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
							var loadWithItem = loadWithItems.FirstOrDefault(_ => _.MemberInfo == member.MemberInfo);
							if (loadWithItem != null)
							{
								var ma = Expression.MakeMemberAccess(Expression.Constant(null, typeAccessor.Type), member.MemberInfo);
								// if (loadWithItem.NextLoadWith.Count > 0)
								// {
								// 	var table = FindTable(ma, 1, false, true);
								// 	if (table!.Table is TableContext tableContext)
								// 		tableContext.LoadWith = loadWithItem.NextLoadWith;
								// }
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
								var isRecord = IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(member.Type));

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

			Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType, int[] index, bool isRecord)
			{
				var ctor = objectType.GetConstructors().Single();

				var exprs = GetExpressions(entityDescriptor.TypeAccessor, isRecord,
					(
						from idx in index.Select((n,i) => new { n, i })
						where idx.n >= 0
						let   cd = entityDescriptor.Columns[idx.i]
						select new ColumnInfo
						{
							IsComplex  = cd.MemberAccessor.IsComplex,
							Name       = cd.MemberName,
							Expression = new ConvertFromDataReaderExpression(cd.MemberType, idx.n, Builder.DataReaderLocal)
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

			int[] BuildIndex(int[] index, Type objectType)
			{
				var names = new Dictionary<string,int>();
				var n     = 0;
				var ed    = Builder.MappingSchema.GetEntityDescriptor(objectType);

				foreach (var cd in ed.Columns)
					if (cd.MemberAccessor.TypeAccessor.Type == ed.TypeAccessor.Type)
						names.Add(cd.MemberName, n++);

				var q =
					from r in SqlTable.Fields.Values.Select((f,i) => new { f, i })
					where names.ContainsKey(r.f.Name)
					orderby names[r.f.Name]
					select index[r.i];

				return q.ToArray();
			}

			protected virtual Expression BuildQuery(Type tableType, TableContext tableContext, ParameterExpression? parentObject)
			{
				SqlInfo[] info;

				var isScalar = IsScalarType(tableType);
				if (isScalar)
				{
					info = ConvertToIndex(null, 0, ConvertFlags.All);
					if (info.Length != 1)
						throw new LinqToDBException($"Invalid scalar type processing for type '{tableType.Name}'.");
					var parentIndex = ConvertToParentIndex(info[0].Index, this);
					return Builder.BuildSql(tableType, parentIndex);
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
					foreach (var field in table.Fields.Values)
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

				var index = info.Select(idx => ConvertToParentIndex(idx.Index, this)).ToArray();

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
					if (tableContext is AssociatedTableContext)
					{
						expr = Expression.Constant(null, ObjectType);
					}
					else
					{
					var exceptionMethod = MemberHelper.MethodOf(() => DefaultInheritanceMappingException(null!, null!));
					var dindex          =
						(
							from f in SqlTable.Fields.Values
							where f.Name == InheritanceMapping[0].DiscriminatorName
							select ConvertToParentIndex(_indexes[f].Index, this)
						).First();

					expr = Expression.Convert(
						Expression.Call(null, exceptionMethod,
							Expression.Call(
								ExpressionBuilder.DataReaderParam,
								ReflectionHelper.DataReader.GetValue,
								Expression.Constant(dindex)),
							Expression.Constant(ObjectType)),
						ObjectType);
				}
				}

				foreach (var mapping in InheritanceMapping.Select((m,i) => new { m, i }).Where(m => m.m != defaultMapping))
				{
					var dindex =
						(
							from f in SqlTable.Fields.Values
							where f.Name == InheritanceMapping[mapping.i].DiscriminatorName
							select ConvertToParentIndex(_indexes[f].Index, this)
						).First();

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
							Builder.BuildSql(codeType, dindex),
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

			private bool IsScalarType(Type tableType)
			{
				return tableType.IsArray || Builder.MappingSchema.IsScalarType(tableType);
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
					if (expression.GetRootObject(Builder.MappingSchema) is ContextRefExpression)
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

					var maxLevel = contextInfo.CurrentExpression.GetLevel(Builder.MappingSchema);
					 
					if (contextInfo.CurrentLevel + 1 > maxLevel)
						expr = contextInfo.Context.BuildExpression(null, 0, false);
					else
						expr = contextInfo.Context.BuildExpression(contextInfo.CurrentExpression, contextInfo.CurrentLevel + 1, false);

					if (expression is MemberExpression memberExpression)
					{
						// workaround for not mapped properties

						// if (table.Table is TableContext tableContext)
						// {
						// 	var orginalType = tableContext.OriginalType;
						// 	// if (table.Table is AssociatedTableContext association)
						// 	// {
						// 	// 	if (association.IsList)
						// 	// 		orginalType = typeof(IEnumerable<>).MakeGenericType(orginalType);
						// 	// }
						//
						// 	if (!orginalType.IsSameOrParentOf(memberExpression.Type))
						// 		expr = new DefaultValueExpression(Builder.MappingSchema, memberExpression.Type);
						//
						// 	if (expr == null)
						// 		expr = tableContext.BuildQuery(tableContext.OriginalType, tableContext, parentObject);
						//}
					}
					
					return expr;
				}

				// Build field.
				//
				var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
				var idx  = ConvertToParentIndex(info.Index, null);

				return Builder.BuildSql(expression!, idx);
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

								if (!IsScalarType(OriginalType))
								{
									// Handling case with Associations. Needs refactoring
									if (contextInfo.Context != this)
									{
										result = contextInfo.Context.ConvertToIndex(contextInfo.CurrentExpression, contextInfo.CurrentLevel, flags);
									}
									else
									
									{
										result = SqlTable.Fields.Values
											.Where(field => !field.IsDynamic)
											.Select(f =>
												f.ColumnDescriptor != null
													? new SqlInfo(f.ColumnDescriptor.MemberInfo) { Sql = f }
													: new SqlInfo { Sql = f })
											.ToArray();
									}

								}
								else
								{
									ISqlExpression sql = SqlTable;
									if (SqlTable is SqlRawSqlTable)
									{
										sql                  = SqlTable.All;
										((SqlField)sql).Type = ((SqlField)sql).Type?.WithSystemType(OriginalType) ?? new DbDataType(OriginalType);
									}

									result = new[]
									{
										new SqlInfo(Enumerable.Empty<MemberInfo>()) { Sql = sql }
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
									var maxLevel = contextInfo.CurrentExpression!.GetLevel(Builder.MappingSchema);
									if (maxLevel == 0)
									{
										return contextInfo.Context.ConvertToSql(null, 0, flags);
									}
									return contextInfo.Context.ConvertToSql(contextInfo.CurrentExpression,
										contextInfo.CurrentLevel >= maxLevel
											? contextInfo.CurrentLevel
											: contextInfo.CurrentLevel + 1, flags);
								}

								var q =
									from field in SqlTable.Fields.Values
									where field.IsPrimaryKey
									orderby field.PrimaryKeyOrder
									select new SqlInfo(field.ColumnDescriptor.MemberInfo)
									{
										Sql = field
									};

								var key = q.ToArray();

								return key.Length != 0 ? key : ConvertToSql(expression, level, ConvertFlags.All);
							}
							else
							{
								return new[]
								{
									// ???
									new SqlInfo(QueryHelper.GetUnderlyingField(contextInfo.Field)?.ColumnDescriptor.MemberInfo!) { Sql = contextInfo.Field }
								};
							}

							break;
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
									new SqlInfo(QueryHelper.GetUnderlyingField(contextInfo.Field)?.ColumnDescriptor.MemberInfo!) { Sql = contextInfo.Field, Query = SelectQuery }
								};

							if (contextInfo.CurrentExpression == null)
							{
								return new[]
									{
										new SqlInfo
										{
											Sql = IsScalarType(OriginalType)
												? (ISqlExpression)SqlTable
												: SqlTable.All
										}
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
							}
							return resultSql;
						}
				}

				throw new NotImplementedException();
			}

			#endregion

			#region ConvertToIndex

			readonly Dictionary<ISqlExpression,SqlInfo> _indexes = new Dictionary<ISqlExpression,SqlInfo>();

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

				var newExpr = new SqlInfo(expr.MemberChain)
				{
					Sql   = SelectQuery.Select.Columns[index],
					Query = SelectQuery,
					Index = index
				};
				
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

			public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFor)
			{
				switch (requestFor)
				{
					case RequestFor.Field      :
						{
							if (expression == null || expression.GetLevel(Builder.MappingSchema) == 0)
								return IsExpressionResult.False;

							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, 1);
							if (GetAssociationDescriptor(levelExpression, out _) != null)
								return IsExpressionResult.False;

							var contextInfo = FindContextExpression(expression, level, false, false);
							return new IsExpressionResult(contextInfo?.Field != null);
						}

					case RequestFor.Table       :
					case RequestFor.Object      :
						{
							if (expression == null)
								return new IsExpressionResult(true, this);

							var currentLevel = 1;
							while (true)
							{
								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, currentLevel);

								var descriptor = GetAssociationDescriptor(levelExpression, out _, false);
								if (descriptor == null)
									return IsExpressionResult.False;

								if (descriptor.IsList && levelExpression == expression)
									return IsExpressionResult.True;

								if (levelExpression == expression)
									return IsExpressionResult.True;

								++currentLevel;
							}

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

							var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

							return new IsExpressionResult(
								GetAssociationDescriptor(levelExpression, out _) != null);
						}
				}

				return IsExpressionResult.False;
			}

			#endregion

			#region GetContext

			Dictionary<MemberInfo, Tuple<IBuildContext, bool>>? _associationContexts;

			public virtual IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
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

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				if (levelExpression is ContextRefExpression contextRef)
					return contextRef.BuildContext;

				IBuildContext sequence;

				if (levelExpression == expression && expression.NodeType == ExpressionType.MemberAccess ||
				    expression.NodeType == ExpressionType.Call)
				{
					var descriptor = GetAssociationDescriptor(levelExpression, out _) ?? throw new InvalidOperationException();

					if (descriptor.IsList)
					{
						var ma     = expression.NodeType == ExpressionType.MemberAccess
							? ((MemberExpression)buildInfo.Expression).Expression
							: expression.NodeType == ExpressionType.Call
								? ((MethodCallExpression)buildInfo.Expression).Arguments[0]
								: buildInfo.Expression.GetRootObject(Builder.MappingSchema);

						// var ma = expression.GetLevelExpression(Builder.MappingSchema, level - 1);

						var elementType     = descriptor.GetElementType(Builder.MappingSchema);
						var parentExactType = ma.Type;

						var isOuter = false;
						var queryMethod = AssociationHelper.CreateAssociationQueryLambda(
							Builder, descriptor, ObjectType, parentExactType, elementType,
							false, isOuter, LoadWith, out isOuter);

						var expr   = queryMethod.GetBody(ma);

						buildInfo.IsAssociationBuilt = true;

						// if (association.ParentAssociationJoin != null && (tableLevel.IsNew || buildInfo.CopyTable))
						// 	association.ParentAssociationJoin.IsWeak = true;

						sequence = Builder.BuildSequence(new BuildInfo(buildInfo, expr));
						return sequence;
					}
				}
				else
				{
					var descriptor = GetAssociationDescriptor(levelExpression, out var memberInto);
					if (descriptor == null)
						throw new LinqException($"Can not find association or expression {levelExpression}.");

					if (_associationContexts != null && _associationContexts.ContainsKey(memberInto))
					{
						// there already association defined, so start from that point
						var tableLevel = FindContextExpression(expression, level, false, true)!;

						var expr = tableLevel.CurrentExpression;

						if (buildInfo.CreateSubQuery)
						{
							var newBuildInfo = new BuildInfo(buildInfo, expr);
							sequence = tableLevel.Context.GetContext(tableLevel.CurrentExpression,
								tableLevel.CurrentLevel + 1, newBuildInfo);
						}
						else
						{
							var newBuildInfo = new BuildInfo(this, expr, new SelectQuery());
							sequence = tableLevel.Context.GetContext(tableLevel.CurrentExpression,
								tableLevel.CurrentLevel + 1, newBuildInfo);
							if (newBuildInfo.IsAssociationBuilt)
							{
								var tableSource = tableLevel.Context.SelectQuery.From.Tables.First();
								var join = new SqlFromClause.Join(JoinType.CrossApply, sequence.SelectQuery,
									null, false, null);

								tableSource.Joins.Add(join.JoinedTable);
							}
						}
					}
					else
					{
						// build SelectMany chain

						var newRootContext = this;

						var levelRoot = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
						var currentRoot = new ContextRefExpression(levelRoot.Type, newRootContext);
						var newExpression = expression.Replace(levelRoot, currentRoot);


						var currentDescriptor = descriptor;
						Expression currentQuery = currentRoot;
						var currentParentOriginal = ObjectType;
						var currentLoadWith = LoadWith;

						do
						{
							var elementType = currentDescriptor.GetElementType(Builder.MappingSchema);
							var parentExactType = currentDescriptor.GetParentElementType();

							var isOuter = false;
							var queryMethod = AssociationHelper.CreateAssociationQueryLambda(
								Builder, currentDescriptor, currentParentOriginal, parentExactType, elementType,
								false, isOuter, currentLoadWith, out isOuter);

							if (currentQuery == currentRoot)
							{
								currentQuery = queryMethod.GetBody(currentRoot);
							}
							else
							{
								var selectManyMethod =
									Methods.Queryable.SelectManyProjection.MakeGenericMethod(
										currentParentOriginal,
										elementType,
										elementType);

								var childQueryLambda = Expression.Lambda(
									EagerLoading.EnsureEnumerable(queryMethod.Body, Builder.MappingSchema),
									queryMethod.Parameters);

								var parentParam = Expression.Parameter(currentParentOriginal, "master");
								var detailParam = Expression.Parameter(elementType, "detail");
								var resultLambda = Expression.Lambda(detailParam, parentParam, detailParam);

								currentQuery = Expression.Call(selectManyMethod, currentQuery,
									childQueryLambda, resultLambda);
							}

							if (levelExpression == newExpression)
								break;

							++level;
							levelExpression = newExpression.GetLevelExpression(Builder.MappingSchema, level);

							if (currentLoadWith != null)
							{
								currentLoadWith = AssociationHelper.GetLoadWith(currentLoadWith)
									.FirstOrDefault(li => li.MemberInfo == memberInto)
									?.NextLoadWith;
							}

							currentDescriptor = GetAssociationDescriptor(levelExpression, out memberInto, false);

							if (currentDescriptor == null)
								throw new LinqToDBException($"Can not find association for {levelExpression}");

							currentParentOriginal = elementType;

						} while (true);

						var subContext = Builder.BuildSequence(new BuildInfo(buildInfo, currentQuery));

						buildInfo.IsAssociationBuilt = true;
						sequence = subContext;
					}

					return sequence;
				}

				throw new InvalidOperationException();
			}

			public virtual IBuildContext? GetContextOld(Expression? expression, int level, BuildInfo buildInfo)
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

				if (buildInfo != null && buildInfo.CreateSubQuery)
				{
					var root = expression.GetRootObject(Builder.MappingSchema);
					if (root == expression)
						return this;

					// var newRootContext = GetContext(null, 0, buildInfo);
					var newRootContext = this;

					var levelRoot     = expression.GetLevelExpression(Builder.MappingSchema, level - 1);
					var currentRoot   = new ContextRefExpression(levelRoot.Type, newRootContext);
					var newExpression = expression.Replace(levelRoot, currentRoot);

					var levelExpression = newExpression.GetLevelExpression(Builder.MappingSchema, level);
					var descriptor = GetAssociationDescriptor(levelExpression, out var memberInto);
					if (descriptor != null)
					{
						var currentDescriptor     = descriptor;
						Expression currentQuery   = currentRoot;
						var currentParentOriginal = ObjectType;

						do
						{
							var elementType     = currentDescriptor.GetElementType(Builder.MappingSchema);
							var parentExactType = currentDescriptor.GetParentElementType();

							var isOuter = false;
							var queryMethod = AssociationHelper.CreateAssociationQueryLambda(
								Builder, currentDescriptor, currentParentOriginal, parentExactType, elementType,
								false, isOuter, LoadWith, out isOuter);

							if (currentQuery == currentRoot)
							{
								currentQuery = queryMethod.GetBody(currentRoot);
							}
							else
							{
								var selectManyMethod =
									Methods.Queryable.SelectManyProjection.MakeGenericMethod(
										currentParentOriginal,
										elementType,
										elementType);

								var childQueryLambda = Expression.Lambda(
									EagerLoading.EnsureEnumerable(queryMethod.Body, Builder.MappingSchema),
									queryMethod.Parameters);

								var parentParam  = Expression.Parameter(currentParentOriginal, "master");
								var detailParam  = Expression.Parameter(elementType, "detail");
								var resultLambda = Expression.Lambda(detailParam, parentParam, detailParam);

								currentQuery = Expression.Call(selectManyMethod, currentQuery,
									childQueryLambda, resultLambda);
							}

							if (levelExpression == newExpression)
								break;

							++level;
							levelExpression = newExpression.GetLevelExpression(Builder.MappingSchema, level);

							currentDescriptor = GetAssociationDescriptor(levelExpression, out memberInto, false);

							if (currentDescriptor == null)
								throw new LinqToDBException($"Can not find association for {levelExpression}");

							currentParentOriginal = elementType;

						} while (true);

						var subContext = Builder.BuildSequence(new BuildInfo(buildInfo, currentQuery) {});

						buildInfo.IsAssociationBuilt = true;

						return subContext;
					}
				}

				if (buildInfo != null && buildInfo.IsSubQuery)
				{
					var root = expression.GetRootObject(Builder.MappingSchema);
					if (root == expression)
						return this;

					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
					var context = FindContextExpression(expression, level, false, true)!;
					if (expression != levelExpression)
					{
						var result = context.Context.GetContext(context.CurrentExpression, context.CurrentLevel + 1,
							buildInfo);
						return result;
					}

					return context.Context;
				}

				throw new InvalidOperationException();
			}

			public virtual SqlStatement GetResultStatement()
			{
				return Statement ?? (Statement = new SqlSelectStatement(SelectQuery));
			}

			#endregion

			#region ConvertToParentIndex

			public virtual int ConvertToParentIndex(int index, IBuildContext? context)
			{
				if (context != null && context != this && context.SelectQuery != SelectQuery)
				{
					var sqlInfo = new SqlInfo() {Index = index, Query = context.SelectQuery, Sql = context.SelectQuery.Select.Columns[index]};
					sqlInfo = GetIndex(sqlInfo);
					index = sqlInfo.Index;
					// index = SelectQuery.Select.Add(context.SelectQuery.Select.Columns[index]);
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

			protected internal virtual List<Tuple<MemberInfo, Expression?>[]>? GetLoadWith()
			{
				return LoadWith;
			}

			protected virtual ISqlExpression? GetField(Expression expression, int level, bool throwException)
			{
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
									foreach (var field in SqlTable.Fields.Values)
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
							foreach (var field in SqlTable.Fields.Values)
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
											while (me.Expression is MemberExpression)
											{
												me = (MemberExpression)me.Expression;
												name = me.Member.Name + '.' + name;
											}

											var fld = SqlTable.Fields.Values.FirstOrDefault(f => f.Name == name);

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
										if (!SqlTable.Fields.TryGetValue(fieldName, out var newField))
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

			readonly Dictionary<MemberInfo,AssociatedTableContext> _associations =
				new Dictionary<MemberInfo,AssociatedTableContext>(new MemberInfoComparer());

			class TableLevel
			{
				public IBuildContext   Table = null!;
				public ISqlExpression? Field;
				public int             Level;
				public bool            IsNew;
			}


			bool IsTable(Expression? expression)
			{
				var ctx = Builder.GetContext(this, expression);
				return ctx == this;
			}


			class ContextInfo
			{
				public ContextInfo(IBuildContext context, ISqlExpression? field, Expression? currentExpression, int currentLevel)
				{
					Context = context;
					Field = field;
					CurrentExpression = currentExpression;
					CurrentLevel = currentLevel;
				}

				public ContextInfo(IBuildContext context, Expression? currentExpression, int currentLevel): 
					this(context, null, currentExpression, currentLevel)
				{
				}

				public IBuildContext Context;
				public Expression? CurrentExpression;
				public ISqlExpression? Field;
				public int CurrentLevel;
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
							var descriptor = GetAssociationDescriptor(levelExpression, out var memberInto);
							if (descriptor != null)
							{
								var isOuter = descriptor.CanBeNull;
								IBuildContext? associatedContext;
								if (!descriptor.IsList)
								{
									Tuple<IBuildContext, bool>? foundInfo = null;
									if (_associationContexts == null ||
									    !_associationContexts.TryGetValue(memberInto, out foundInfo))
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
											new BuildInfo(Parent, newExpression, SelectQuery), this, descriptor,
											!forceInner,
											ref isOuter);

										_associationContexts ??= new Dictionary<MemberInfo, Tuple<IBuildContext, bool>>();
										_associationContexts.Add(memberInto, Tuple.Create(associatedContext, isOuter));
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
									var newExpression = expression.Replace(levelExpression,
										new ContextRefExpression(levelExpression.Type, this));

									associatedContext = AssociationHelper.BuildAssociationInline(Builder,
										new BuildInfo(Parent, newExpression, SelectQuery), this, descriptor,
										false,
										ref isOuter);

									var contextRef = new ContextRefExpression(levelExpression.Type, associatedContext);
									return new ContextInfo(associatedContext, expression.Replace(levelExpression, contextRef), 0) {Descriptor = descriptor};
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

			AssociationDescriptor? GetAssociationDescriptor(Expression expression, out MemberInfo memberInfo, bool onlyCurrent = true)
			{
				memberInfo = null;
				if (expression.NodeType == ExpressionType.MemberAccess)
					memberInfo = ((MemberExpression)expression).Member;
				else if (expression.NodeType == ExpressionType.Call)
					memberInfo = ((MethodCallExpression)expression).Method;

				if (memberInfo == null)
					return null;

				var descriptor = GetAssociationDescriptor(memberInfo, EntityDescriptor);
				if (descriptor == null && !onlyCurrent && memberInfo.DeclaringType != ObjectType)
					descriptor = GetAssociationDescriptor(memberInfo, Builder.MappingSchema.GetEntityDescriptor(memberInfo.DeclaringType));

				return descriptor;
			}

			AssociationDescriptor? GetAssociationDescriptor(MemberInfo memberInfo, EntityDescriptor entityDescriptor)
			{
				AssociationDescriptor? descriptor = null;
				
				if (memberInfo.MemberType == MemberTypes.Method)
				{
					var attribute = Builder.MappingSchema.GetAttribute<AssociationAttribute>(memberInfo.DeclaringType, memberInfo, a => a.Configuration);

					if (attribute != null)
						descriptor = new AssociationDescriptor
						(
							entityDescriptor.ObjectType,
							memberInfo,
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
				else if (memberInfo.MemberType == MemberTypes.Property || memberInfo.MemberType == MemberTypes.Field)
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
						.FirstOrDefault(a => a.MemberInfo.EqualsTo(memberInfo));
				}

				return descriptor;
			}

			TableLevel? GetAssociation(Expression expression, int level, bool isSubquery)
			{
				var objectMapper    = EntityDescriptor;
				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);
				var inheritance     =
					(
						from m in objectMapper.InheritanceMapping
						let om = Builder.MappingSchema.GetEntityDescriptor(m.Type)
						where om.Associations.Count > 0
						select om
					).ToList();

				AssociatedTableContext? tableAssociation = null;
				var isNew = false;

				if (levelExpression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression) levelExpression;
					var aa = Builder.MappingSchema.GetAttribute<AssociationAttribute>(mc.Method.DeclaringType, mc.Method, a => a.Configuration);

					if (aa != null)
						tableAssociation = new AssociatedTableContext(
							Builder,
							this,
							new AssociationDescriptor(
								EntityDescriptor.ObjectType,
								mc.Method,
								aa.GetThisKeys(),
								aa.GetOtherKeys(),
								aa.ExpressionPredicate,
								aa.Predicate,
								aa.QueryExpressionMethod,
								aa.QueryExpression,
								aa.Storage,
								aa.CanBeNull,
								aa.AliasName),
							ForceLeftJoinAssociations,
							_associationsToSubQueries || isSubquery);

					isNew = true;
				}

				if (tableAssociation == null && levelExpression.NodeType == ExpressionType.MemberAccess && objectMapper.Associations.Count > 0 || inheritance.Count > 0)
				{

					var memberExpression = (MemberExpression)levelExpression;

					// subquery association shouldn't be cached, because different assciation navigation pathes
					// should produce different subqueries
					if (_associationsToSubQueries || isSubquery || !_associations.TryGetValue(memberExpression.Member, out tableAssociation))
					{
						var q =
							from a in objectMapper.Associations.Concat(inheritance.SelectMany(om => om.Associations))
							where a.MemberInfo.EqualsTo(memberExpression.Member)
							select new AssociatedTableContext(Builder, this, a, ForceLeftJoinAssociations, _associationsToSubQueries || isSubquery);

						tableAssociation = q.FirstOrDefault();

						isNew = true;

						if (!_associationsToSubQueries && !isSubquery && !tableAssociation.IsList)
							_associations.Add(memberExpression.Member, tableAssociation);
					}
				}

				if (tableAssociation != null)
				{
					if (_associationsToSubQueries || isSubquery)
					{
						ISqlExpression field = null;
						if (levelExpression != expression)
						{
							tableAssociation.IsSubQuery = false;
							var associationRef = new ContextRefExpression(levelExpression.Type, tableAssociation.InnerContext);
							var expr = expression.Transform(e => e == levelExpression ? associationRef : e);
							var info = new BuildInfo((IBuildContext?)this, expr, tableAssociation.SelectQuery);
							var sequence = Builder.BuildSequence(info);
							return new TableLevel{Table = sequence};
						}
						// {
						// 	if (Builder.Is(buildInfo))
						// 	{
						// 		var associationRef = new ContextRefExpression(levelExpression.Type, tableAssociation);
						// 		var expr = expression.Transform(e => e == levelExpression ? associationRef : e);
						// 		var info = new BuildInfo((IBuildContext?)null, expr, tableAssociation.SelectQuery);
						// 		field = Builder.BuildSequence(info).SelectQuery;
						// 	}
						// 	else
						// {
						// 	var associationRef = new ContextRefExpression(levelExpression.Type, tableAssociation);
						// 	var expr = expression.Transform(e => e == levelExpression ? associationRef : e);
						// 	var info = new BuildInfo((IBuildContext?)null, expr, tableAssociation.InnerContext.SelectQuery);
						// 	var sequence = Builder.BuildSequence(info).SelectQuery;
						// }
						//}
						
						// tableAssociation.SelectQuery.Select.Columns.Clear();
						//
						// if (field != null)
						// 	tableAssociation.SelectQuery.Select.Add(field);
						field = tableAssociation.SelectQuery;
					
						return new TableLevel { Table = tableAssociation, Field = field, Level = field == null ? level : level + 1, IsNew = isNew };
					}

					return new TableLevel { Table = tableAssociation, Level = level, IsNew = isNew };

					//
					// var al = tableAssociation.GetAssociation(expression, level + 1, buildInfo);
					//
					// if (al != null)
					// 	return al;
					//
					// var field = tableAssociation.GetField(expression, level + 1, false);
					//
					// if (_associationsToSubQueries)
					// {
					// 	tableAssociation.SelectQuery.Select.Columns.Clear();
					// 	if (field != null)
					// 		tableAssociation.SelectQuery.Select.Add(field);
					// 	field = tableAssociation.SelectQuery;
					// }
					//
					// return new TableLevel { Table = tableAssociation, Field = field, Level = field == null ? level : level + 1, IsNew = isNew };
				}

				return null;
			}

			#endregion
		}
	}
}
