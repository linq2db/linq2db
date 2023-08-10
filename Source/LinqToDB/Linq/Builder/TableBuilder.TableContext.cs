using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
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

			public override Expression? Expression  { get; }
			public override Type        ElementType => ObjectType;

			public          Type             OriginalType;
			public          EntityDescriptor EntityDescriptor;

			public Type          ObjectType   { get; set; }
			public SqlTable      SqlTable     { get; set; }
			public LoadWithInfo  LoadWithRoot { get; set; } = new();
			public MemberInfo[]? LoadWithPath { get; set; }

			public bool IsSubQuery { get; }

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType) : base (builder, originalType, buildInfo.SelectQuery)
			{
				Parent      = buildInfo.Parent;
				Expression  = buildInfo.Expression;
				SelectQuery = buildInfo.SelectQuery;
				IsSubQuery  = buildInfo.IsSubQuery;

				OriginalType     = originalType;
				ObjectType       = GetObjectType();
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				SqlTable         = new SqlTable(EntityDescriptor);

				if (!buildInfo.IsTest)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, SqlTable table) : base (builder, table.ObjectType, buildInfo.SelectQuery)
			{
				Parent      = buildInfo.Parent;
				Expression  = buildInfo.Expression;
				SelectQuery = buildInfo.SelectQuery;
				IsSubQuery  = buildInfo.IsSubQuery;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			internal TableContext(ExpressionBuilder builder, SelectQuery selectQuery, SqlTable table) : base(builder, table.ObjectType, selectQuery)
			{
				Parent     = null;
				Expression = null;
				IsSubQuery = false;

				OriginalType     = table.ObjectType;
				ObjectType       = GetObjectType();
				SqlTable         = table;
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

				if (SqlTable.SqlTableType != SqlTableType.SystemTable)
					SelectQuery.From.Table(SqlTable);

				Init(true);
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo) : base (builder, typeof(object), buildInfo.SelectQuery)
			{
				Parent     = buildInfo.Parent;
				Expression = buildInfo.Expression;
				IsSubQuery = buildInfo.IsSubQuery;

				var mc   = (MethodCallExpression)buildInfo.Expression;
				var attr = mc.Method.GetTableFunctionAttribute(builder.MappingSchema)!;

				if (!typeof(IQueryable<>).IsSameOrParentOf(mc.Method.ReturnType))
					throw new LinqException("Table function has to return IQueryable<T>.");

				OriginalType     = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType       = GetObjectType();
				EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
				SqlTable         = new SqlTable(EntityDescriptor);

				SelectQuery.From.Table(SqlTable);

				attr.SetTable(builder.DataOptions, (context: this, builder), builder.DataContext.CreateSqlProvider(), Builder.MappingSchema, SqlTable, mc, static (context, a, _) => context.builder.ConvertToSql(context.context, a));

				Init(true);
			}

			protected Type GetObjectType()
			{
				for (var type = OriginalType.BaseType; type != null && type != typeof(object); type = type.BaseType)
				{
					var mapping = Builder.MappingSchema.GetEntityDescriptor(type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated).InheritanceMapping;

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
						SelectQuery.Where.SearchCondition.Conditions.Add(new SqlCondition(false, predicate));
				}
			}

			#endregion

			#region BuildQuery

			public virtual void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				throw new NotImplementedException();

				/*var expr = Builder.FinalizeProjection(this,
					Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

				var mapper = Builder.BuildMapper<T>(expr);

				QueryRunner.SetRunQuery(query, mapper);*/
			}

			#endregion

			#region BuildExpression

			public virtual Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				throw new NotImplementedException();
			}

			#endregion

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.IsRoot() || flags.IsAssociationRoot() || flags.IsExtractProjection() || flags.IsAggregationRoot())
					return path;

				if (SequenceHelper.IsSameContext(path, this))
				{
					if (flags.IsTable())
						return path;

					// Eager load case
					if (path.Type.IsEnumerableType(ElementType))
					{
						return path;
					}

					return Builder.BuildFullEntityExpression(this, path, path.Type, flags);
				}

				Expression member;

				if (path is MemberExpression me)
				{
					member = me;
				}
				else if (path is MethodCallExpression mc && mc.Method.IsSqlPropertyMethodEx())
				{
					var memberInfo = MemberHelper.GetMemberInfo(mc);
					var memberAccess = Expression.MakeMemberAccess(mc.Arguments[0], memberInfo);
					member = memberAccess;
				}
				else
					return path;

				var sql = GetField(member, false);

				if (sql != null)
				{
					if (flags.HasFlag(ProjectFlags.Table))
					{
						var root = Builder.GetRootContext(this, path, false);
						return root ?? path;
					}
				}

				if (sql == null)
				{
					var memberInfo = MemberHelper.GetMemberInfo(member);

					if (EntityDescriptor.HasCalculatedMembers)
					{
						var found = EntityDescriptor.CalculatedMembers?.FirstOrDefault(ma =>
							MemberInfoComparer.Instance.Equals(ma.MemberInfo, memberInfo));

						if (found != null)
						{
							return Builder.ExposeExpression(member);
						}
					}

					if (member is MemberExpression meCheck && SequenceHelper.IsSameContext(meCheck.Expression, this))
					{
						// It will help to do not crash when user uses Automapper and it tries to map non accessible fields
						//
						if (flags.IsExpression())
							return new DefaultValueExpression(Builder.MappingSchema, path.Type);
					}

					return path;
				}

				var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path, trackingPath: path);

				return placeholder;
			}

			public override IBuildContext Clone(CloningContext context)
			{
				return new TableContext(Builder, context.CloneElement(SelectQuery), context.CloneElement(SqlTable));
			}

			public override void SetRunQuery<T>(Query<T> query, Expression expr)
			{
				var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

				QueryRunner.SetRunQuery(query, mapper);
			}

			#region GetContext

			public override IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
			{
				if (!buildInfo.CreateSubQuery || buildInfo.IsTest)
					return this;

				var expr    = Builder.GetSequenceExpression(this);
				var context = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

				return context;
			}

			public override SqlStatement GetResultStatement()
			{
				return new SqlSelectStatement(SelectQuery);
			}

			#endregion

			#region SetAlias

			public override void SetAlias(string? alias)
			{
				if (alias == null)
					return;

				if (!alias.Contains('<'))
					SqlTable.Alias ??= alias;
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

										if (me.Expression is MemberExpression)
										{
											while (me.Expression is MemberExpression me1)
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
										if (SequenceHelper.IsSameContext(memberExpression.Expression, this))
											return field;
									}
								}

								if (InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
								{
									foreach (var mapping in InheritanceMapping)
									{
										foreach (var mm in Builder.MappingSchema.GetEntityDescriptor(mapping.Type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated).Columns)
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
