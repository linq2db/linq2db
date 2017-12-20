using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	internal class BaseRowsetBuilder : ISequenceBuilder
	{
		public int BuildCounter { get; set; }

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var mc = buildInfo.Expression as MethodCallExpression;
			return mc != null && mc.IsQueryable("AsCTE");
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			var cte = builder.BuildCte(buildInfo,
				() =>
				{
					var methodCall = (MethodCallExpression) buildInfo.Expression;
					var name       = methodCall.Arguments[1].EvaluateExpression() as string;
					var info       = new BuildInfo(buildInfo, methodCall.Arguments[0]);
					var sequence   = builder.BuildSequence(info);
					return new CteClause(info.SelectQuery, info.Expression.Type.GetGenericArgumentsEx()[0], name);

				}
			);

			var context = new CteContext(builder, buildInfo, cte);

			return context;
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}
	}

	internal class CteBuilder : BaseRowsetBuilder
	{
	}

	internal class CteContext : BaseRowsetContext
	{
		private readonly Dictionary<ISqlExpression, SqlInfo> _indexes = new Dictionary<ISqlExpression, SqlInfo>();

		public CteContext(
			ExpressionBuilder builder,
			BuildInfo buildInfo,
			[JetBrains.Annotations.NotNull] CteClause cteTable
		) : base(builder, buildInfo, new CteTable(builder.MappingSchema, cteTable))
		{
			if (cteTable == null) throw new ArgumentNullException(nameof(cteTable));
		}

		public string Name { get; set; }

		public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
		{
			switch (flags)
			{
				case ConvertFlags.All:
				{
					var table = FindRowset(expression, level, false, true);

					if (table.Field == null)
						return table.Table.SqlTable.Fields.Values
							.Select(f => new SqlInfo(f.ColumnDescriptor.MemberInfo) {Sql = f})
							.ToArray();

					break;
				}

				case ConvertFlags.Key:
				{
					var table = FindRowset(expression, level, false, true);

					if (table.Field == null)
					{
						var q =
							from f in table.Table.SqlTable.Fields.Values
							where f.IsPrimaryKey
							orderby f.PrimaryKeyOrder
							select new SqlInfo(f.ColumnDescriptor.MemberInfo) {Sql = f};

						var key = q.ToArray();

						return key.Length != 0 ? key : ConvertToSql(expression, level, ConvertFlags.All);
					}

					break;
				}

				case ConvertFlags.Field:
				{
					var table = FindRowset(expression, level, true, true);

					if (table.Field != null)
						return new[]
						{
							new SqlInfo(table.Field.ColumnDescriptor.MemberInfo) {Sql = table.Field}
						};

					if (expression == null)
						return new[]
						{
							new SqlInfo {Sql = table.Table.SqlTable.All}
						};

					break;
				}
			}

			throw new NotImplementedException();
		}

		protected SqlInfo GetIndex(SqlInfo expr)
		{
			if (_indexes.TryGetValue(expr.Sql, out var n))
				return n;

			if (expr.Sql is SqlField field)
				expr.Index = SelectQuery.Select.Add(field, field.Alias);
			else
				expr.Index = SelectQuery.Select.Add(expr.Sql);
			
			expr.Query = SelectQuery;

			_indexes.Add(expr.Sql, expr);

			return expr;
		}

		public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
		{
			switch (flags)
			{
				case ConvertFlags.Field:
				case ConvertFlags.Key:
				case ConvertFlags.All:

					var info = ConvertToSql(expression, level, flags);

					for (var i = 0; i < info.Length; i++)
						info[i] = GetIndex(info[i]);

					return info;
			}

			throw new NotImplementedException();
		}

		private SqlField GetField(Expression expression, int level, bool throwException)
		{
			if (expression.NodeType == ExpressionType.MemberAccess)
			{
				var memberExpression = (MemberExpression) expression;

				if (EntityDescriptor.Aliases != null)
					if (EntityDescriptor.Aliases.ContainsKey(memberExpression.Member.Name))
					{
						var alias = EntityDescriptor[memberExpression.Member.Name];

						if (alias == null)
						{
							foreach (var column in EntityDescriptor.Columns)
								if (column.MemberInfo.EqualsTo(memberExpression.Member, SqlTable.ObjectType))
								{
									expression = memberExpression = Expression.PropertyOrField(
										Expression.Convert(memberExpression.Expression, column.MemberInfo.DeclaringType), column.MemberName);
									break;
								}
						}
						else
						{
							var expr = memberExpression.Expression;

							if (alias.MemberInfo.DeclaringType != memberExpression.Member.DeclaringType)
								expr = Expression.Convert(memberExpression.Expression, alias.MemberInfo.DeclaringType);

							expression = memberExpression = Expression.PropertyOrField(expr, alias.MemberName);
						}
					}

				var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

				if (levelExpression.NodeType == ExpressionType.MemberAccess)
				{
					if (levelExpression != expression)
					{
						var levelMember = (MemberExpression) levelExpression;

						if (memberExpression.Member.IsNullableValueMember() && memberExpression.Expression == levelExpression)
						{
							memberExpression = levelMember;
						}
						else
						{
							var sameType =
								levelMember.Member.ReflectedTypeEx() == SqlTable.ObjectType ||
								levelMember.Member.DeclaringType == SqlTable.ObjectType;

							if (!sameType)
							{
								var mi = SqlTable.ObjectType.GetInstanceMemberEx(levelMember.Member.Name);
								sameType = mi.Any(_ => _.DeclaringType == levelMember.Member.DeclaringType);
							}

							if (sameType /* || InheritanceMapping.Count > 0 */)
								foreach (var field in SqlTable.Fields.Values)
								{
									var name = levelMember.Member.Name;
									if (field.Name.IndexOf('.') >= 0)
									{
										for (var ex = (MemberExpression) expression; ex != levelMember; ex = (MemberExpression) ex.Expression)
											name += "." + ex.Member.Name;

										if (field.Name == name)
											return field;
									}
									else if (field.Name == name)
									{
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
									if (field.ColumnDescriptor.MemberAccessor.IsComplex)
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

								if (InheritanceMapping != null && InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
									foreach (var mapping in InheritanceMapping)
										foreach (var mm in Builder.MappingSchema.GetEntityDescriptor(mapping.Type).Columns)
											if (mm.MemberAccessor.MemberInfo.EqualsTo(memberExpression.Member))
												return field;

							}

						if (throwException &&
						    EntityDescriptor != null &&
						    EntityDescriptor.TypeAccessor.Type == memberExpression.Member.DeclaringType)
							throw new LinqException("Member '{0}.{1}' is not a table column.",
								memberExpression.Member.DeclaringType.Name, memberExpression.Member.Name);
					}
				}
			}

			return null;
		}

		RowsetLevel GetAssociation(Expression expression, int level)
		{
			return null;
		}

		private RowsetLevel FindRowset(Expression expression, int level, bool throwException, bool throwExceptionForNull)
		{
			if (expression == null)
				return new RowsetLevel {Table = this};

			var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

			RowsetLevel result = null;

			switch (levelExpression.NodeType)
			{
				case ExpressionType.MemberAccess:
				case ExpressionType.Parameter:
				{
					var field = GetField(expression, level, throwException);

					if (field != null || level == 0 && levelExpression == expression)
						return new RowsetLevel {Table = this, Field = field, Level = level};

					goto case ExpressionType.Call;
				}
				case ExpressionType.Call:
					result = GetAssociation(expression, level);
					break;
			}

			if (throwExceptionForNull && result == null)
				throw new LinqException($"Expression '{expression}' ({levelExpression}) is not a table.");

			return result;
		}


		public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
		{
			switch (requestFlag)
			{
				case RequestFor.Field:
				{
					var table = FindRowset(expression, level, false, false);
					return new IsExpressionResult(table?.Field != null);
				}

				case RequestFor.Table:
				case RequestFor.Object:
				{
					var table = FindRowset(expression, level, false, false);
					var isTable =
						table != null &&
						table.Field == null &&
						(expression == null || expression.GetLevelExpression(Builder.MappingSchema, table.Level) == expression);

					return new IsExpressionResult(isTable, isTable ? table.Table : null);
				}

				case RequestFor.Expression:
				{
					if (expression == null)
						return IsExpressionResult.False;

					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

					switch (levelExpression.NodeType)
					{
						case ExpressionType.MemberAccess:
						case ExpressionType.Parameter:
						case ExpressionType.Call:

							var table = FindRowset(expression, level, false, false);
							return new IsExpressionResult(table == null);
					}

					return IsExpressionResult.True;
				}

				case RequestFor.Association:
				{
					if (EntityDescriptor.Associations.Count > 0)
					{
						var table = FindRowset(expression, level, false, false);
						var isat =
							table?.Table is TableBuilder.AssociatedTableContext &&
							table.Field == null &&
							(expression == null || expression.GetLevelExpression(Builder.MappingSchema, table.Level) == expression);

						return new IsExpressionResult(isat, isat ? table.Table : null);
					}

					return IsExpressionResult.False;
				}
			}

			return IsExpressionResult.False;
		}

		public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
		{
			throw new NotImplementedException();
		}

		public override int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public override void SetAlias(string alias)
		{
			throw new NotImplementedException();
		}

		public override ISqlExpression GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public override SqlStatement GetResultStatement()
		{
			throw new NotImplementedException();
		}

		private class RowsetLevel
		{
			public SqlField Field;
			public bool IsNew;
			public int Level;
			public BaseRowsetContext Table;
		}
	}

	internal abstract class BaseRowsetContext : IBuildContext
	{
		public EntityDescriptor EntityDescriptor;
		public Type ObjectType;
		public Type OriginalType;
		public CteTable SqlTable;
		public List<InheritanceMapping> InheritanceMapping;

		public BaseRowsetContext(ExpressionBuilder builder, BuildInfo buildInfo, CteTable cteTable)
		{
			Builder = builder;
			Parent = buildInfo.Parent;
			Expression = buildInfo.Expression;

			ObjectType = cteTable.ObjectType;
			SqlTable = cteTable;
			EntityDescriptor = Builder.MappingSchema.GetEntityDescriptor(ObjectType);
			InheritanceMapping = EntityDescriptor.InheritanceMapping;

			SelectQuery = new SelectQuery();
			SelectQuery.From.Table(SqlTable);

			Init();
		}

		public string _sqlQueryText { get; }
		public ExpressionBuilder Builder { get; }
		public Expression Expression { get; }
		public SelectQuery SelectQuery { get; set; }
		public SqlStatement Statement { get; set; }
		public IBuildContext Parent { get; set; }
		public abstract void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter);
		public abstract Expression BuildExpression(Expression expression, int level, bool enforceServerSide);
		public abstract SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags);
		public abstract SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags);
		public abstract IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag);
		public abstract IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo);
		public abstract int ConvertToParentIndex(int index, IBuildContext context);
		public abstract void SetAlias(string alias);
		public abstract ISqlExpression GetSubQuery(IBuildContext context);
		public abstract SqlStatement GetResultStatement();

		protected virtual void Init()
		{
//			SelectQuery.From.Table(SqlTable);
		}

		protected virtual Type GetObjectType()
		{
			return OriginalType;
		}
	}
}
