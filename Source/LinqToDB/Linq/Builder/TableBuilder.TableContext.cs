using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	partial class TableBuilder
	{
		[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
		public class TableContext : BuildContextBase, ITableContext
		{
			#region Properties

			MappingSchema _mappingSchema;

			public override Expression?   Expression    { get; }
			public override MappingSchema MappingSchema => _mappingSchema;
			public override Type          ElementType   => ObjectType;

			public          Type             OriginalType;
			public          EntityDescriptor EntityDescriptor;

			public Type          ObjectType    { get; set; }
			public SqlTable      SqlTable      { get; set; }
			public LoadWithInfo  LoadWithRoot  { get; set; } = new();
			public MemberInfo[]? LoadWithPath  { get; set; }

			public bool IsSubQuery { get; }

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, MappingSchema mappingSchema, BuildInfo buildInfo, Type originalType) : base (builder, originalType, buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				SelectQuery    = buildInfo.SelectQuery;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				_mappingSchema = mappingSchema;

				OriginalType     = originalType;
				ObjectType       = GetObjectType();
				EntityDescriptor = mappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				SqlTable         = new SqlTable(EntityDescriptor);

				SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, MappingSchema mappingSchema, BuildInfo buildInfo, SqlTable table) : base (builder, table.ObjectType, buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				SelectQuery    = buildInfo.SelectQuery;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				_mappingSchema = mappingSchema;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			internal TableContext(ExpressionBuilder builder, MappingSchema mappingSchema, SelectQuery selectQuery, SqlTable table, bool isOptional) : base(builder, table.ObjectType, selectQuery)
			{
				Parent         = null;
				Expression     = null;
				IsSubQuery     = false;
				IsOptional     = isOptional;
				_mappingSchema = mappingSchema;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, MappingSchema mappingSchema, BuildInfo buildInfo) : base (builder, typeof(object), buildInfo.SelectQuery)
			{
				Parent         = buildInfo.Parent;
				Expression     = buildInfo.Expression;
				IsSubQuery     = buildInfo.IsSubQuery;
				IsOptional     = SequenceHelper.GetIsOptional(buildInfo);
				_mappingSchema = mappingSchema;

				var mc   = (MethodCallExpression)buildInfo.Expression;
				var attr = mc.Method.GetTableFunctionAttribute(mappingSchema);

				if (attr == null)
					throw new LinqException($"Method '{mc.Method}' has no '{nameof(Sql.TableFunctionAttribute)}'.");

				if (!typeof(IQueryable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return IQueryable<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
				EntityDescriptor = mappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				SqlTable         = new SqlTable(EntityDescriptor);

				SelectQuery.From.Table(SqlTable);

				attr.SetTable(builder.DataOptions, (context: this, builder), builder.DataContext.CreateSqlProvider(), mappingSchema, SqlTable, mc, static (context, a, _, inline) =>
				{
					if (context.builder.CanBeCompiled(a, false))
					{
						var param = context.builder.ParametersContext.BuildParameter(context.context, a, columnDescriptor : null, forceConstant : true, doNotCheckCompatibility : true);
						if (param != null)
						{
							if (inline == true)
							{
								param.SqlParameter.IsQueryParameter = false;
							}
							return new SqlPlaceholderExpression(null, param.SqlParameter, a);
						}
					}

					return context.builder.BuildSqlExpression(context.context, a);
				});

				builder.RegisterExtensionAccessors(mc);

				Init(true);
			}

			protected Type GetObjectType()
			{
				for (var type = OriginalType.BaseType; type != null && type != typeof(object); type = type.BaseType)
				{
					var mapping = MappingSchema.GetEntityDescriptor(type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated).InheritanceMapping;

					if (mapping.Count > 0)
						return type;
				}

				return OriginalType;
			}

			public IReadOnlyList<InheritanceMapping> InheritanceMapping = null!;

			protected void Init(bool applyFilters)
			{
				InheritanceMapping = EntityDescriptor.InheritanceMapping;

				// Original table is a parent.
				//
				if (applyFilters && ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);

					if (predicate.GetType() != typeof(SqlPredicate.Expr))
						SelectQuery.Where.EnsureConjunction().Add(predicate);
				}
			}

			#endregion

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsAssociationRoot() || flags.IsAggregationRoot() || flags.IsTraverse() || flags.IsExtractProjection() || flags.IsSubquery())
					return path;

				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsTable())
						return path;

					// Expand is initiated by Eager Loading but there is need to expand in case when we need comparison
					if (flags.IsExpand() && !flags.IsKeys())
						return path;

					if (flags.IsSubquery() && !(path.Type.IsSameOrParentOf(ElementType) || ElementType.IsSameOrParentOf(path.Type)))
					{
						var expr = Builder.GetSequenceExpression(this);
						if (expr == null)
							return path;
						return expr;
					}

					// Eager load case
					if (path.Type.IsEnumerableType(ElementType))
					{
						return path;
					}

					if (MappingSchema.IsScalarType(ElementType))
					{
						var tablePlaceholder =
							ExpressionBuilder.CreatePlaceholder(this, SqlTable, path, trackingPath : path);
						return tablePlaceholder;
					}

					Expression fullEntity = Builder.BuildFullEntityExpression(MappingSchema, path, ElementType, flags);
					// Entity can contain calculated columns which should be exposed
					fullEntity = Builder.ConvertExpressionTree(fullEntity);

					if (fullEntity.Type != path.Type)
						fullEntity = Expression.Convert(fullEntity, path.Type);

					return fullEntity;
				}

				Expression member;

				if (path is MemberExpression me)
					member = me;
				else
					return path;

				var sql = GetField(member, false);

				if (sql != null)
				{
					if (flags.IsTable())
					{
						if (path is MemberExpression memberExpression && SequenceHelper.IsSameContext(memberExpression.Expression, this))
						{
							return ((ContextRefExpression)memberExpression.Expression!).WithType(path.Type);
						}
						return path;
					}
				}

				if (sql == null)
				{
					if (flags.IsSqlOrExpression() && !me.IsAssociation(MappingSchema))
					{
						Expression fullEntity = Builder.BuildFullEntityExpression(MappingSchema, new ContextRefExpression(ElementType, this), ElementType, flags);

						var projected = Builder.Project(this, path, null, -1, flags, fullEntity, true);

						if (projected is not SqlErrorExpression)
							return projected;
					}

					return path;
				}

				if (flags.IsExtractProjection())
				{
					return path;
				}

				var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path, trackingPath : path);

				return placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TableContext(Builder, MappingSchema, context.CloneElement(SelectQuery), context.CloneElement(SqlTable), IsOptional);
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			public override bool IsOptional { get; }

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			#region SetAlias

			public override void SetAlias(string? alias)
			{
				if (alias == null)
					return;

				if (SqlTable.Alias != null)
					return;

				if (!alias.Contains('<'))
					SqlTable.Alias = alias;
			}

			#endregion

			#region Helpers

			protected ISqlExpression? GetField(Expression expression, bool throwException)
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
											Expression.Convert(memberExpression.Expression!, column.MemberInfo.DeclaringType!), column.MemberName);
										break;
									}
								}
							}
							else
							{
								var expr = memberExpression.Expression!;

								if (alias.MemberInfo.DeclaringType != memberExpression.Member.DeclaringType)
									expr = Expression.Convert(memberExpression.Expression!, alias.MemberInfo.DeclaringType!);

								expression = memberExpression = ExpressionHelper.PropertyOrField(expr, alias.MemberName);
							}
						}
					}

					var levelExpression = expression;

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
									levelMember.Member.DeclaringType == SqlTable.ObjectType;

								if (!sameType)
								{
									var members = SqlTable.ObjectType.GetInstanceMemberEx(levelMember.Member.Name);

									foreach (var mi in members)
									{
										if (mi.DeclaringType == levelMember.Member.DeclaringType)
										{
											sameType = true;
											break;
										}
									}
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
													ex = (MemberExpression)ex.Expression!)
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

										if (me.Expression.UnwrapConvert() is MemberExpression)
										{
											while (me.Expression.UnwrapConvert() is MemberExpression me1)
											{
												me   = me1;
												name = me.Member.Name + '.' + name;
											}

											var fld = SqlTable.FindFieldByMemberName(name);

											if (fld != null)
												return fld;
										}
									}
									else
									{
										if (SequenceHelper.IsSameContext(memberExpression.Expression.UnwrapConvert(), this))
											return field;
									}
								}

								if (InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
								{
									foreach (var mapping in InheritanceMapping)
									{
										foreach (var mm in MappingSchema.GetEntityDescriptor(mapping.Type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated).Columns)
										{
											if (mm.MemberAccessor.MemberInfo.EqualsTo(memberExpression.Member))
												return field;
										}
									}
								}

							}

							if (memberExpression.Member.IsDynamicColumnPropertyEx())
							{
								var fieldName = memberExpression.Member.Name;

								// do not add association columns
								var flag = true;

								foreach (var assoc in EntityDescriptor.Associations)
								{
									if (assoc.MemberInfo == memberExpression.Member)
									{
										flag = false;
										break;
									}
								}

								if (flag)
								{
									var newField = SqlTable.FindFieldByMemberName(fieldName);
									if (newField == null)
									{
										newField = new SqlField(
											new ColumnDescriptor(
												MappingSchema,
												EntityDescriptor,
												new ColumnAttribute(fieldName),
												new MemberAccessor(EntityDescriptor.TypeAccessor,
													memberExpression.Member, EntityDescriptor),
												InheritanceMapping.Count > 0)
										) { IsDynamic = true, };

										SqlTable.Add(newField);
									}

									return newField;
								}
							}

							if (throwException                             &&
							    EntityDescriptor                   != null &&
							    EntityDescriptor.TypeAccessor.Type == memberExpression.Member.DeclaringType)
							{
								throw new LinqException("Member '{0}.{1}' is not a table column.",
									memberExpression.Member.DeclaringType.Name, memberExpression.Member.Name);
							}
						}
					}
				}

				if (throwException)
				{
					throw new LinqException($"Member '{expression}' is not a table column.");
				}
				return null;
			}

			#endregion
		}
	}
}
