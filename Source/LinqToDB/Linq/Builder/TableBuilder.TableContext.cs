using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

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
		public class TableContext : IBuildContext
		{
			#region Properties

#if DEBUG
			public string SqlQueryText => SelectQuery == null ? "" : SelectQuery.SqlText;
			public string Path => this.GetPath();
			public int    ContextId    { get; private set; }
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

			public bool IsSubQuery { get; }

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType)
			{
				Builder          = builder;
				Parent           = buildInfo.Parent;
				Expression       = buildInfo.Expression;
				SelectQuery      = buildInfo.SelectQuery;
				AssociationsToSubQueries = buildInfo.AssociationsAsSubQueries;
				IsSubQuery               = buildInfo.IsSubQuery;

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
				IsSubQuery               = buildInfo.IsSubQuery;

				OriginalType     = table.ObjectType;
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
				IsSubQuery  = false;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo)
			{
				Builder     = builder;
				Parent      = buildInfo.Parent;
				Expression  = buildInfo.Expression;
				SelectQuery = buildInfo.SelectQuery;
				AssociationsToSubQueries = buildInfo.AssociationsAsSubQueries;
				IsSubQuery               = buildInfo.IsSubQuery;

				var mc   = (MethodCallExpression)Expression;
				var attr = mc.Method.GetTableFunctionAttribute(builder.MappingSchema)!;

				if (!typeof(IQueryable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return IQueryable<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
				SqlTable         = new SqlTable(builder.MappingSchema, ObjectType);
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);

				SelectQuery.From.Table(SqlTable);

				attr.SetTable((context: this, builder), builder.DataContext.CreateSqlProvider(), Builder.MappingSchema, SqlTable, mc, static (context, a, _) => context.builder.ConvertToSql(context.context, a));

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
#if DEBUG
				ContextId = Builder.GenerateContextId();
#endif

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

			public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			#endregion

			#region BuildExpression

			public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			#endregion

			#region ConvertToSql

			public virtual SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				throw new NotImplementedException();
								}

			#endregion

			#region ConvertToIndex

			public virtual SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
									{
				throw new NotImplementedException();
			}

			public Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.AssociationRoot))
					return path;

				if (SequenceHelper.IsSameContext(path, this))
				{
					// trying to access Queryable variant
					if (path.Type != ObjectType && flags.HasFlag(ProjectFlags.Expression))
						return new SqlEagerLoadExpression(this, path, Builder.GetSequenceExpression(this));

					return Builder.BuildFullEntityExpression(this, ObjectType, flags);
				}

				if (path is not MemberExpression member)
					return Builder.CreateSqlError(this, path);

				var sql = GetField(member, member.GetLevel(Builder.MappingSchema), false);
				if (sql == null)
					return path;

				var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path);

				return placeholder;
			}

			#endregion

			#region IsExpression

			public virtual IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				throw new NotImplementedException(); 
			}

			#endregion

			#region GetContext

			public IBuildContext GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (!buildInfo.CreateSubQuery)
					return this;

				var expr = Builder.GetSequenceExpression(this);
				var context = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

				return context;
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
				throw new NotImplementedException(); 
			}

			#endregion

			#region SetAlias

			public void SetAlias(string? alias)
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
										var newField = SqlTable[fieldName];
										if (newField == null)
										{
											newField = new SqlField(
												new ColumnDescriptor(
													Builder.MappingSchema,
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
