using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using Reflection;
	using SqlQuery;

	//TODO: review
	sealed class EnumerableContext : BuildContextBase
	{
		readonly EntityDescriptor _entityDescriptor;

		public override Expression?    Expression { get; }
		public          SqlValuesTable Table      { get; }

		public EnumerableContext(ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType)
			: base(builder, elementType, query)
		{
			Parent            = buildInfo.Parent;
            _entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(elementType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
			Table             = BuildValuesTable(buildInfo.Expression);
			Expression        = buildInfo.Expression;

			foreach (var field in Table.Fields)
			{
				SelectQuery.Select.AddNew(field);
			}

			if (!buildInfo.IsTest)
				SelectQuery.From.Table(Table);
		}

		SqlValuesTable BuildValuesTable(Expression expr)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
				return BuildValuesTableFromArray((NewArrayExpression)expr);

			return new SqlValuesTable(Builder.ConvertToSql(this, expr));
		}

		SqlValuesTable BuildValuesTableFromArray(NewArrayExpression arrayExpression)
		{
			if (Builder.MappingSchema.IsScalarType(ElementType))
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

			var ed = Builder.MappingSchema.GetEntityDescriptor(ElementType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

			var builtRows = new List<ISqlExpression[]>(arrayExpression.Expressions.Count);

			var columnsInfo = knownMembers.Select(m => (Member: m, Column: ed.Columns.FirstOrDefault(c => c.MemberInfo == m)))
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
						var nullValue = Expression.Constant(Builder.MappingSchema.GetDefaultValue(ElementType), ElementType);
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
					: new SqlField(new DbDataType(member.GetMemberType()), "item" + (index + 1), true);
				fields[index]        = field;
			}

			return new SqlValuesTable(fields, columnsInfo.Select(ci => ci.Member).ToArray(), builtRows);
		}

		static ConstructorInfo _parameterConstructor =
			MemberHelper.ConstructorOf(() => new SqlParameter(new DbDataType(typeof(object)), "", null));

		static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new SqlValue(new DbDataType(typeof(object)), null));

		SqlField? GetField(MemberExpression path)
		{
			foreach (var column in _entityDescriptor.Columns)
			{
				if (!column.MemberInfo.EqualsTo(path.Member, ElementType))
				{
					continue;
				}

				var newField = BuildField(column);

				return newField;
			}

			return null;
        }

		SqlField BuildField(ColumnDescriptor column)
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
				body = body.Replace(getter.Parameters[0], Expression.Convert(param, ElementType));

				var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
				var getterFunc   = getterLambda.Compile();

				Table.Add(newField = new SqlField(column), column.MemberInfo, getterFunc);
			}

			return newField;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.HasFlag(ProjectFlags.Root))
					return path;

				/*
				// trying to access Queryable variant
				if (path.Type != ElementType && flags.HasFlag(ProjectFlags.Expression))
					return new SqlEagerLoadExpression((ContextRefExpression)path, path, Builder.GetSequenceExpression(this));
					*/

				if (flags.HasFlag(ProjectFlags.Expression))
					return Expression!; // do nothing

				var result = Builder.BuildSqlExpression(this, Expression!, flags.SqlFlag());

				return result;
				//return Builder.BuildEntityExpression(this, _elementType, flags);
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(this, path);

			var sql = GetField(member);
			if (sql == null)
				return ExpressionBuilder.CreateSqlError(this, path);

			var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path);

			return placeholder;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			//TODO: Clone
			throw new NotImplementedException();
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
				SelectQuery.Select.Columns[0].Alias = alias;
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}
	}
}
