using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class EnumerableContext : IBuildContext
	{
		readonly Type _elementType;

#if DEBUG
		public string?               _sqlQueryText { get; }
		public string                Path          => this.GetPath();
#endif
		public  ExpressionBuilder    Builder       { get; }
		public  Expression           Expression    { get; }
		public  SelectQuery          SelectQuery   { get; set; }
		public  SqlStatement?        Statement     { get; set; }
		public  IBuildContext?       Parent        { get; set; }

		private readonly EntityDescriptor _entityDescriptor;

		public SqlValuesTable Table { get; }

		public EnumerableContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType)
		{
			Parent            = buildInfo.Parent;
			Builder           = builder;
			Expression        = buildInfo.Expression;
			SelectQuery       = query;
			_elementType      = elementType;
			_entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(elementType);
			Table             = BuildValuesTable();

			foreach (var field in Table.Fields)
			{
				SelectQuery.Select.AddNew(field);
			}

			SelectQuery.From.Table(Table);
		}

		SqlValuesTable BuildValuesTable()
		{
			if (Expression.NodeType == ExpressionType.NewArrayInit)
				return BuildValuesTableFromArray((NewArrayExpression)Expression);

			return new SqlValuesTable(Builder.ConvertToSql(Parent, Expression));
		}

		SqlValuesTable BuildValuesTableFromArray(NewArrayExpression arrayExpression)
		{
			if (Builder.MappingSchema.IsScalarType(_elementType))
			{
				var rows  = arrayExpression.Expressions.Select(e => new[] {Builder.ConvertToSql(Parent, e)}).ToList();
				var field = new SqlField(Table, "item");
				return new SqlValuesTable(new[] { field }, null, rows);
			}


			var knownMembers = new HashSet<MemberInfo>();

			foreach (var row in arrayExpression.Expressions)
			{
				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(members, row);

				knownMembers.AddRange(members.Keys);
			}

			var ed = Builder.MappingSchema.GetEntityDescriptor(_elementType);

			var builtRows = new List<ISqlExpression[]>(arrayExpression.Expressions.Count);

			var columnsInfo = knownMembers.Select(m => (Member: m, Column: ed.Columns.Find(c => c.MemberInfo == m)))
				.ToList();

			foreach (var row in arrayExpression.Expressions)
			{
				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(members, row);

				var rowValues = new ISqlExpression[columnsInfo.Count];

				var idx = 0;
				foreach (var info in columnsInfo)
				{
					ISqlExpression sql;
					if (members.TryGetValue(info.Member, out var accessExpr))
					{
						sql = Builder.ConvertToSql(Parent, accessExpr, columnDescriptor: info.Column);
					}
					else
					{
						var nullValue = Expression.Constant(Builder.MappingSchema.GetDefaultValue(_elementType), _elementType);
						sql = Builder.ConvertToSql(Parent, nullValue, columnDescriptor: info.Column);
					}

					rowValues[idx] = sql;
					++idx;
				}

				builtRows.Add(rowValues);
			}

			var fields = new SqlField[columnsInfo.Count];

			for (var index = 0; index < columnsInfo.Count; index++)
			{
				var info  = columnsInfo[index];
				var field = info.Column != null
					? new SqlField(info.Column)
					: new SqlField(info.Member.GetMemberType(), "item" + (index + 1), true);
				fields[index] = field;
			}

			return new SqlValuesTable(fields, columnsInfo.Select(ci => ci.Member).ToArray(), builtRows);
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			throw new NotImplementedException();
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			if (Builder.MappingSchema.IsScalarType(_elementType))
			{
				var info  = ConvertToIndex(expression, level, ConvertFlags.Field)[0];
				var index = info.Index;
				if (Parent != null)
					index = ConvertToParentIndex(index, Parent);


				return Builder.BuildSql(_elementType, index, info.Sql);
			}

			throw new NotImplementedException("Projection of in-memory collections is not implemented");
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			if (expression == null)
			{
				SqlInfo[] sql;
				if (Builder.MappingSchema.IsScalarType(_elementType))
				{
					SqlField field;
					if (Table.Fields.Count > 0)
					{
						field = Table.Fields[0];
					}
					else
					{
						field = new SqlField(_elementType, "item", true);
						var param = Expression.Parameter(typeof(object), "record");
						var body = Expression.New(_sqlValueconstructor,
							Expression.Constant(new DbDataType(_elementType,
								ColumnDescriptor.CalculateDataType(Builder.MappingSchema, _elementType))),
							param);

						var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
						var getterFunc   = getterLambda.Compile();
						Table.Add(field, null, getterFunc);
					}

					sql = new[] { new SqlInfo(field, SelectQuery) };
				}
				else
				{
					if (Table.Rows != null)
					{
						sql = Table.Fields.Select(f => new SqlInfo(f.ColumnDescriptor.MemberInfo, f, SelectQuery)).ToArray();
					}
					else
					{
						sql = _entityDescriptor.Columns
							.Select(c => new SqlInfo(c.MemberInfo, BuildField(c), SelectQuery)).ToArray();
					}

					if (sql.Length == 0)
						throw new LinqToDBException($"Entity of type '{_elementType.Name}' as no defined columns.");
				}

				return sql;
			}

			switch (flags)
			{
				case ConvertFlags.Field:
				{
					if (expression.NodeType == ExpressionType.MemberAccess)
					{
						var memberExpression = (MemberExpression)expression;

						foreach (var column in _entityDescriptor.Columns)
						{
							if (column.MemberInfo.EqualsTo(memberExpression.Member, _elementType))
							{
								var newField = BuildField(column);

								return new[]
								{
									new SqlInfo(column.MemberInfo, newField, SelectQuery)
								};
							}
						}
					}

					break;
				}
			}

			throw new NotImplementedException();
		}

		private static ConstructorInfo _parameterConstructor =
			MemberHelper.ConstructorOf(() => new SqlParameter(new DbDataType(typeof(object)), "", null));

		private static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new SqlValue(new DbDataType(typeof(object)), null));

		private SqlField BuildField(ColumnDescriptor column)
		{
			var memberName = column.MemberName;
			if (!Table.FieldsLookup!.TryGetValue(column.MemberInfo, out var newField))
			{
				var getter = column.GetDbParamLambda();

				var generator = new ExpressionGenerator();
				if (typeof(DataParameter).IsSameOrParentOf(getter.Body.Type))
				{
					
					var variable  = generator.AssignToVariable(getter.Body);
					generator.AddExpression(Expression.New(_parameterConstructor,
						Expression.Property(variable, nameof(DataParameter.DataType),
							Expression.Constant(memberName),
							Expression.Property(variable, nameof(DataParameter.Value))
						)));
				}
				else
				{
					generator.AddExpression(Expression.New(_sqlValueconstructor,
						Expression.Constant(column.GetDbDataType(true)),
						Expression.Convert(getter.Body, typeof(object))));
				}

				var param = Expression.Parameter(typeof(object), "e");

				var body = generator.Build();
				body = body.Replace(getter.Parameters[0], Expression.Convert(param, _elementType));

				var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
				var getterFunc   = getterLambda.Compile();

				Table.Add(newField = new SqlField(column), column.MemberInfo, getterFunc);
			}

			return newField;
		}

		public SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
		{
			var sql = ConvertToSql(expression, level, flags);

			for (var i = 0; i < sql.Length; i++)
			{
				var info = sql[i];
				var idx  = info.Query!.Select.Add(info.Sql);

				sql[i] = info.WithIndex(idx).WithSql(info.Query!.Select.Columns[idx]);
			}

			return sql;
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			if (expression == null)
			{
				switch (requestFlag)
				{
					case RequestFor.Expression:
					case RequestFor.Field: return IsExpressionResult.GetResult(Builder.MappingSchema.IsScalarType(_elementType));
					case RequestFor.Object:
						return IsExpressionResult.GetResult(!Builder.MappingSchema.IsScalarType(_elementType));
				}
			}
			else
			{
				switch (requestFlag)
				{
					case RequestFor.Expression:
					case RequestFor.Field:
					{
						if (Builder.MappingSchema.IsScalarType(_elementType))
						{
							return IsExpressionResult.True;
						}

						if (expression is MemberExpression me)
						{
							if (Table.Rows != null)
							{
								if (Table.Fields.Any(f =>
										MemberInfoComparer.Instance.Equals(f.ColumnDescriptor?.MemberInfo, me.Member)))
								{
									return IsExpressionResult.True;
								}
							}
							else
							if (_entityDescriptor.Columns.Any(c =>
									MemberInfoComparer.Instance.Equals(c.MemberInfo, me.Member)))
							{
								return IsExpressionResult.True;
							}
						}

						break;
					}
				}
			}

			return IsExpressionResult.False;
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			throw new NotImplementedException();
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			if (Parent == null)
				return index;

			return Parent.ConvertToParentIndex(index, this);
		}

		public void SetAlias(string alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
				SelectQuery.Select.Columns[0].Alias = alias;
		}

		public ISqlExpression GetSubQuery(IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public SqlStatement GetResultStatement()
		{
			throw new NotImplementedException();
		}

		public void CompleteColumns()
		{
		}
	}
}
