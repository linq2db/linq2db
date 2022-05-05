using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Reflection;

	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	class EnumerableContext : IBuildContext
	{
		readonly Type _elementType;

#if DEBUG
		public string?               SqlQueryText { get; }
		public string  Path          => this.GetPath();
		public int     ContextId     { get; }
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
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
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
				foreach (var (member, column) in columnsInfo)
				{
					ISqlExpression sql;
					if (members.TryGetValue(member, out var accessExpr))
					{
						sql = Builder.ConvertToSql(Parent, accessExpr, columnDescriptor: column);
					}
					else
					{
						var nullValue = Expression.Constant(Builder.MappingSchema.GetDefaultValue(_elementType), _elementType);
						sql = Builder.ConvertToSql(Parent, nullValue, columnDescriptor: column);
					}

					rowValues[idx] = sql;
					++idx;
				}

				builtRows.Add(rowValues);
			}

			var fields = new SqlField[columnsInfo.Count];

			for (var index = 0; index < columnsInfo.Count; index++)
			{
				var (member, column) = columnsInfo[index];
				var field            = column != null
					? new SqlField(column)
					: new SqlField(member.GetMemberType(), "item" + (index + 1), true);
				fields[index]        = field;
			}

			return new SqlValuesTable(fields, columnsInfo.Select(ci => ci.Member).ToArray(), builtRows);
		}

		public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
		{
			var expr = Builder.FinalizeProjection(this,
				Builder.MakeExpression(new ContextRefExpression(typeof(T), this), ProjectFlags.Expression));

			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
		{
			throw new NotImplementedException();
		}

		public SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
		{
			throw new NotImplementedException();
		}

		private static ConstructorInfo _parameterConstructor =
			MemberHelper.ConstructorOf(() => new SqlParameter(new DbDataType(typeof(object)), "", null));

		private static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new SqlValue(new DbDataType(typeof(object)), null));

		private SqlField? GetField(MemberExpression path)
		{
			foreach (var column in _entityDescriptor.Columns)
			{
				if (column.MemberInfo.EqualsTo(path.Member, _elementType))
				{
					var newField = BuildField(column);

					return newField;
				}
			}

			return null;
		}

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
					generator.AddExpression(
						Expression.New(
							_parameterConstructor,
							Expression.Property(variable, Methods.LinqToDB.DataParameter.DbDataType),
							Expression.Constant(memberName),
							Expression.Property(variable, Methods.LinqToDB.DataParameter.Value)
						));
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
			throw new NotImplementedException();
		}

		public Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.HasFlag(ProjectFlags.Root))
					return path;

				// trying to access Queryable variant
				if (path.Type != _elementType && flags.HasFlag(ProjectFlags.Expression))
					return new SqlEagerLoadExpression(this, path, Builder.GetSequenceExpression(this));

				return Builder.BuildEntityExpression(this, _elementType, flags);
			}

			if (path is not MemberExpression member)
				return Builder.CreateSqlError(this, path);

			var sql = GetField(member);
			if (sql == null)
				return Builder.CreateSqlError(this, path);

			var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path);

			return placeholder;
		}

		public IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
		{
			throw new NotImplementedException();
		}

		public IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
		{
			return null;
		}

		public int ConvertToParentIndex(int index, IBuildContext context)
		{
			throw new NotImplementedException();
		}

		public void SetAlias(string? alias)
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
			return new SqlSelectStatement(SelectQuery);
		}

		public void CompleteColumns()
		{
		}
	}
}
