using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Reflection;

namespace LinqToDB.Internal.Linq.Builder
{
	//TODO: review
	sealed class EnumerableContext : BuildContextBase
	{
		readonly EntityDescriptor _entityDescriptor;
		readonly bool _fieldsDefined;

		public override Expression?    Expression    { get; }
		public override MappingSchema  MappingSchema => Builder.MappingSchema;
		public          SqlValuesTable Table         { get; }

		public EnumerableContext(TranslationModifier translationModifier, ExpressionBuilder builder, BuildInfo buildInfo, SelectQuery query, Type elementType)
			: base(translationModifier, builder, elementType, query)
		{
			Parent            = buildInfo.Parent;
			_entityDescriptor = MappingSchema.GetEntityDescriptor(elementType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

			Table      = BuildValuesTable(buildInfo.Expression, out _fieldsDefined);
			Expression = buildInfo.Expression;

			SelectQuery.From.Table(Table);
		}

		EnumerableContext(TranslationModifier translationModifier, ExpressionBuilder builder, MappingSchema mappingSchema, Expression expression, SelectQuery query, SqlValuesTable table, Type elementType)
			: base(translationModifier, builder, elementType, query)
		{
			Parent            = null;
			_entityDescriptor = mappingSchema.GetEntityDescriptor(elementType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
			Table             = table;
			Expression        = expression;
		}

		SqlValuesTable BuildValuesTable(Expression expr, out bool fieldsDefined)
		{
			if (expr.NodeType == ExpressionType.NewArrayInit)
			{
				fieldsDefined = true;
				return BuildValuesTableFromArray((NewArrayExpression)expr);
			}

			var param = Builder.ParametersContext.BuildParameter(this, expr, null,
				buildParameterType : ParametersContext.BuildParameterType.InPredicate);

			if (param == null)
			{
				throw new InvalidOperationException($"Expression '{expr}' not translated to parameter.");
			}

			fieldsDefined = false;
			return new SqlValuesTable(param);
		}

		SqlValuesTable BuildValuesTableFromArray(NewArrayExpression arrayExpression)
		{
			if (MappingSchema.IsScalarType(ElementType))
			{
				var rows        = arrayExpression.Expressions.Select(e => new[] {Builder.ConvertToSql(Parent, e, false)}).ToList();
				var contextRef  = new ContextRefExpression(ElementType, this);
				var specialProp = SequenceHelper.CreateSpecialProperty(contextRef, ElementType, "item");
				var field       = new SqlField(Table, "item") { Type = new DbDataType(ElementType)};

				return new SqlValuesTable(new[] { field }, new[] { specialProp.Member }, rows);
			}

			var knownMembers = new HashSet<MemberInfo>();

			foreach (var row in arrayExpression.Expressions)
			{
				var sqlExpr = Builder.BuildSqlExpression(this, row);

				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(MappingSchema, members, sqlExpr);

				knownMembers.AddRange(members.Keys);
			}

			var ed = MappingSchema.GetEntityDescriptor(ElementType, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);

			var builtRows = new List<ISqlExpression[]>(arrayExpression.Expressions.Count);

			var columnsInfo = knownMembers
				.Select(m => (Member: m, Column: ed.Columns.FirstOrDefault(c => c.MemberInfo == m)))
				.ToList();

			foreach (var row in arrayExpression.Expressions)
			{
				var members = new Dictionary<MemberInfo, Expression>();
				Builder.ProcessProjection(MappingSchema, members, row);

				var rowValues = new ISqlExpression[columnsInfo.Count];

				var idx = 0;
				foreach (var (member, column) in columnsInfo)
				{
					ISqlExpression sql;
					using var      savedDescriptor = Builder.UsingColumnDescriptor(column);
					if (members.TryGetValue(member, out var accessExpr))
					{
						sql = Builder.ConvertToSql(Parent, accessExpr, false);
					}
					else
					{
						var nullValue = Expression.Constant(MappingSchema.GetDefaultValue(ElementType), ElementType);
						sql = Builder.ConvertToSql(Parent, nullValue, false);
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
					: new SqlField(new DbDataType(member.GetMemberType()), FormattableString.Invariant($"item{index + 1}"), true);
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
			if (SequenceHelper.IsSpecialProperty(path, ElementType, "item"))
			{
				var newField = BuildField(null, path, Builder.CurrentDescriptor);
				return newField;
			}

			foreach (var column in _entityDescriptor.Columns)
			{
				if (!column.MemberInfo.EqualsTo(path.Member, ElementType))
				{
					continue;
				}

				var newField = BuildField(column, path, null);
				return newField;
			}

			return null;
		}

		SqlField BuildField(ColumnDescriptor? column, MemberExpression me, ColumnDescriptor? typeDescriptor)
		{
			var memberName = me.Member.Name;
			if (!Table.FieldsLookup!.TryGetValue(me.Member, out var newField))
			{
				var getter = column?.GetDbParamLambda();
				if (getter == null)
				{
					var thisParam = Expression.Parameter(me.Type, memberName);
					getter = Expression.Lambda(thisParam, thisParam);
				}

				var dbDataType = (column ?? typeDescriptor)?.GetDbDataType(true) ?? ColumnDescriptor.CalculateDbDataType(MappingSchema, me.Type);

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
						Expression.Constant(dbDataType),
						Expression.Convert(getter.Body, typeof(object))));
				}

				var toType = ElementType;
				if (typeDescriptor != null)
				{
					toType = dbDataType.SystemType;
				}

				var param = Expression.Parameter(typeof(object), "e");

				var body = generator.Build();
				body = body.Replace(getter.Parameters[0], Expression.Convert(param, toType));

				var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, param);
				var getterFunc   = getterLambda.CompileExpression();

				Table.Add(newField = new SqlField(dbDataType, memberName, column?.CanBeNull ?? true), me.Member, getterFunc);
			}

			return newField;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsRoot() || flags.IsTable() || flags.IsAssociationRoot() || flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsExtractProjection() || flags.IsMemberRoot())
					return path;

				if (MappingSchema.IsScalarType(ElementType) || Builder.CurrentDescriptor != null)
				{
					var dbType = Builder.CurrentDescriptor?.GetDbDataType(true) ?? MappingSchema.GetDbDataType(ElementType);
					if (dbType.DataType != DataType.Undefined || ElementType.IsEnum)
					{
						if (path.Type != ElementType)
						{
							path = ((ContextRefExpression)path).WithType(ElementType);
						}

						var specialProp = SequenceHelper.CreateSpecialProperty(path, ElementType, "item");
						return specialProp;
					}
				}

				if (Table.FieldsLookup == null)
					throw new InvalidOperationException("Enumerable fields are not defined.");

				Expression result;
				if (_fieldsDefined)
				{
					var membersOrdered =
						from f in Table.Fields
						join fm in Table.FieldsLookup on f equals fm.Value
						select fm.Key;

					result = Builder.BuildEntityExpression(MappingSchema, path, ElementType, membersOrdered.ToList());

				}
				else
				{
					result = Builder.BuildFullEntityExpression(MappingSchema, path, ElementType, flags);
				}

				return result;
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(path);

			var sql = GetField(member);
			if (sql == null)
				return ExpressionBuilder.CreateSqlError(path);

			var placeholder = ExpressionBuilder.CreatePlaceholder(this, sql, path);

			return placeholder;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new EnumerableContext(TranslationModifier, Builder, MappingSchema, Expression!, context.CloneElement(SelectQuery),
				context.CloneElement(Table), ElementType);
		}

		public override void SetRunQuery<T>(Query<T> query, Expression expr)
		{
			var mapper = Builder.BuildMapper<T>(SelectQuery, expr);

			QueryRunner.SetRunQuery(query, mapper);
		}

		public override void SetAlias(string? alias)
		{
			if (SelectQuery.Select.Columns.Count == 1)
			{
				var sqlColumn = SelectQuery.Select.Columns[0];
				if (sqlColumn.RawAlias == null)
					sqlColumn.Alias = alias;
			}
		}

		public override SqlStatement GetResultStatement()
		{
			return new SqlSelectStatement(SelectQuery);
		}
	}
}
