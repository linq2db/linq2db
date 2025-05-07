using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	sealed class EnumerableContext : BuildContextBase
	{
		public override Expression?    Expression    { get; }
		public override MappingSchema  MappingSchema => Builder.MappingSchema;
		public          SqlValuesTable Table         { get; }

		public override bool AutomaticAssociations => false;

		public EnumerableContext(TranslationModifier translationModifier, ExpressionBuilder builder, ISqlExpression source, SelectQuery query, Type elementType)
			: base(translationModifier, builder, elementType, query)
		{
			Table = new SqlValuesTable(source);

			SelectQuery.From.Table(Table);
		}

		EnumerableContext(TranslationModifier translationModifier, ExpressionBuilder builder, Expression expression, SelectQuery query, SqlValuesTable table, Type elementType)
			: base(translationModifier, builder, elementType, query)
		{
			Parent     = null;
			Table      = table;
			Expression = expression;
		}

		static ConstructorInfo _sqlValueconstructor =
			MemberHelper.ConstructorOf(() => new SqlValue(new DbDataType(typeof(object)), null));

		List<(Expression path, ColumnDescriptor? descriptor, SqlPlaceholderExpression placeholder)> _fieldsMap = new ();

		static string? GenerateFieldName(Expression expr)
		{
			var     current   = expr;
			string? fieldName = null;
			while (current is MemberExpression memberExpression)
			{
				if (fieldName != null)
					fieldName = memberExpression.Member.Name + "_" + fieldName;
				else
					fieldName = memberExpression.Member.Name;
				current = memberExpression.Expression;
			}

			return fieldName;
		}

		SqlPlaceholderExpression? BuildFieldPlaceholder(MemberExpression memberExpression, ProjectFlags flags)
		{
			if (memberExpression.Expression == null)
				return null;

			var currentDescriptor = Builder.CurrentDescriptor;

			SqlPlaceholderExpression? foundPlaceholder = null;

			foreach (var (path, descriptor, placeholder) in _fieldsMap)
			{
				if (descriptor == currentDescriptor && (ExpressionEqualityComparer.Instance.Equals(path, memberExpression)))
				{
					foundPlaceholder = placeholder;
					break;
				}
			}

			if (foundPlaceholder != null)
			{
				return foundPlaceholder;
			}

			var fieldName = GenerateFieldName(memberExpression);
			if (fieldName == null)
			{
				fieldName = "field1";
			}

			Utils.MakeUniqueNames([fieldName], _fieldsMap.Select(x => ((SqlField)x.placeholder.Sql).Name), x => x, (e, v, s) => fieldName = v);

			var entityDescriptor = MappingSchema.GetEntityDescriptor(memberExpression.Expression.Type, Builder.DataOptions.ConnectionOptions.OnEntityDescriptorCreated);
			var entityColumnDescriptor = entityDescriptor.FindColumnDescriptor(memberExpression.Member);

			var dbDataType = currentDescriptor?.GetDbDataType(true) ?? entityColumnDescriptor?.GetDbDataType(true) ?? MappingSchema.GetDbDataType(memberExpression.Type);

			var valueGetter = BuildValueGetter(entityColumnDescriptor, memberExpression, currentDescriptor, dbDataType);
			if (valueGetter == null)
			{
				return null;
			}

			var field = new SqlField(dbDataType, fieldName, true);
			var fieldPlaceholder = ExpressionBuilder.CreatePlaceholder(this, field, memberExpression);


			_fieldsMap.Add((memberExpression, currentDescriptor, fieldPlaceholder));

			Table.Add(field, valueGetter);

			return fieldPlaceholder;
		}

		static Expression BuildNullPropagationGetter(ParameterExpression objectVariable, ParameterExpression resultVariable, List<MemberInfo> members, int memberIndex, Func<Expression, Expression> finalizer)
		{
			var member  = members[memberIndex];
			var objType = member.DeclaringType!;

			var memberAccessExpr = Expression.MakeMemberAccess(Expression.Convert(objectVariable, objType), member);

			Expression thenValue;

			if (memberIndex == members.Count - 1)
			{
				thenValue = Expression.Assign(resultVariable, finalizer(memberAccessExpr));
			}
			else
			{
				var local = new ExpressionGenerator();
				local.Assign(objectVariable, memberAccessExpr);
				local.AddExpression(BuildNullPropagationGetter(objectVariable, resultVariable, members, memberIndex + 1, finalizer));
				thenValue = local.Build();
			}

			return Expression.IfThen(Expression.TypeIs(objectVariable, objType), thenValue);

			// It is what should be generated
			/*
			 ISqlExpression? result = null;
			 objVariable = obj;
			 if (objVariable is objType1)
			 {
				 objVariable = ((objType1)objVariable).Member1;

			    if (objVariable is objType2)
                {
					 var memberValue = ((objType2)objVariable).Member2;
			         result = new SqlValue(dbDataType, finalizer(memberValue))
				}
			 }
			 */
		}

		Func<object, ISqlExpression>? BuildValueGetter(ColumnDescriptor? column, MemberExpression me, ColumnDescriptor? typeDescriptor, DbDataType dbDataType)
		{
			var generator = new ExpressionGenerator();
			var objParam  = Expression.Parameter(typeof(object), "obj");

			var resultVariable = generator.AssignToVariable(Expression.Constant(null, typeof(ISqlExpression)), "result");
			var objectVariable = generator.DeclareVariable(objParam.Type, "currentObj");
			generator.Assign(objectVariable, objParam);

			var contextExpression = (Expression)me;
			List<MemberInfo> members = [];
			while (contextExpression?.UnwrapConvert() is MemberExpression memberExpression)
			{
				if (memberExpression.Member.DeclaringType == null)
					return null;
				members.Add(memberExpression.Member);
				contextExpression = memberExpression.Expression;
			}

			if (contextExpression?.UnwrapConvert() is not ContextRefExpression)
			{
				return null;
			}

			members.Reverse();

			var descriptor = column ?? typeDescriptor;

			var ifThenExpression = BuildNullPropagationGetter(objectVariable, resultVariable, members, 0, accessor =>
			{
				if (descriptor != null) 
					accessor = descriptor.ApplyConversions(accessor, dbDataType, true);

				return Expression.New(_sqlValueconstructor,
					Expression.Constant(dbDataType),
					Expression.Convert(accessor, typeof(object)));
			});

			generator.AddExpression(ifThenExpression);

			generator.IfThen(Expression.Equal(resultVariable, Expression.Constant(null, resultVariable.Type)), Expression.Assign(resultVariable, Expression.New(_sqlValueconstructor,
				Expression.Constant(dbDataType),
				Expression.Constant(null, typeof(object)))));

			generator.AddExpression(resultVariable);

			var body = generator.Build();

			var getterLambda = Expression.Lambda<Func<object, ISqlExpression>>(body, objParam);
			var getterFunc   = getterLambda.CompileExpression();

			return getterFunc;
		}

		public override Expression MakeExpression(Expression path, ProjectFlags flags)
		{
			if (flags.IsRoot() || flags.IsMemberRoot())
				return path;

			if (SequenceHelper.IsSameContext(path, this))
			{
				if (flags.IsTable() || flags.IsAssociationRoot() || flags.IsTraverse() || flags.IsAggregationRoot() || flags.IsExtractProjection())
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

				var result = Builder.BuildFullEntityExpression(MappingSchema, path, ElementType, flags);

				return result;
			}

			if (path is not MemberExpression member)
				return ExpressionBuilder.CreateSqlError(path);

			var placeholder = BuildFieldPlaceholder(member, flags);
			if (placeholder == null)
				return ExpressionBuilder.CreateSqlError(path);

			return placeholder;
		}

		public override IBuildContext Clone(CloningContext context)
		{
			return new EnumerableContext(TranslationModifier, Builder, Expression!, context.CloneElement(SelectQuery),
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
