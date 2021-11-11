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
			//TODO: refactor TableContext to base class ObjectContext

			var expr   = BuildExpression(null, 0, false);
			var mapper = Builder.BuildMapper<T>(expr);

			QueryRunner.SetRunQuery(query, mapper);
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

			var sqlInfos  = ConvertToIndex(expression, level, ConvertFlags.All);
			if (Parent != null)
				for (var i = 0; i < sqlInfos.Length; i++)
				{
					var info  = sqlInfos[i];
					var index = ConvertToParentIndex(info.Index, Parent);
					sqlInfos[i] = info.WithIndex(index);
				}

			var indexes = sqlInfos.Select(info => Tuple.Create(info.Index, QueryHelper.ExtractField(info.Sql))).ToArray();

			var expr = BuildTableExpression(!Builder.IsBlockDisable, _elementType, indexes);

			return expr;
		}

		#region TableContext code almost not changed. TODO: Remove after implementing base ObjectContext

		class ColumnInfo
		{
			public bool       IsComplex;
			public string     Name       = null!;
			public Expression Expression = null!;
		}

		static bool IsRecord(Attribute[] attrs, out int sequence)
		{
			sequence = -1;
			var compilationMappingAttr = attrs.FirstOrDefault(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CompilationMappingAttribute");
			var cliMutableAttr         = attrs.FirstOrDefault(attr => attr.GetType().FullName == "Microsoft.FSharp.Core.CLIMutableAttribute");

			if (compilationMappingAttr != null)
			{
				// https://github.com/dotnet/fsharp/blob/1fcb351bb98fe361c7e70172ea51b5e6a4b52ee0/src/fsharp/FSharp.Core/prim-types.fsi
				// ObjectType = 3
				if (Convert.ToInt32(((dynamic)compilationMappingAttr).SourceConstructFlags) == 3)
					return false;

				sequence = ((dynamic)compilationMappingAttr).SequenceNumber;
			}

			return compilationMappingAttr != null && cliMutableAttr == null;
		}
					
		IEnumerable<(string Name, Expression? Expr)> GetExpressions(TypeAccessor typeAccessor, bool isRecordType, List<ColumnInfo> columns)
		{
			var members = isRecordType ?
				typeAccessor.Members.Select(m =>
					{
						if (IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(typeAccessor.Type, m.MemberInfo), out var sequence))
							return new { m, sequence };
						return null;
					})
					.Where(_ => _ != null).OrderBy(_ => _!.sequence).Select(_ => _!.m) :
				typeAccessor.Members;

			/*
			var loadWith      = GetLoadWith();
			var loadWithItems = loadWith == null ? new List<AssociationHelper.LoadWithItem>() : AssociationHelper.GetLoadWith(loadWith);
			*/

			foreach (var member in members)
			{
				var column = columns.FirstOrDefault(c => !c.IsComplex && c.Name == member.Name);

				if (column != null)
				{
					yield return (member.Name, column.Expression);
				}
				else
				{
					var assocAttr = Builder.MappingSchema.GetAttributes<AssociationAttribute>(typeAccessor.Type, member.MemberInfo).FirstOrDefault();
					var isAssociation = assocAttr != null;

					if (isAssociation)
					{
						/*var loadWithItem = loadWithItems.FirstOrDefault(_ => MemberInfoEqualityComparer.Default.Equals(_.Info.MemberInfo, member.MemberInfo));
						if (loadWithItem != null)
						{
							var ma = Expression.MakeMemberAccess(Expression.Constant(null, typeAccessor.Type), member.MemberInfo);
							yield return (member.Name, BuildExpression(ma, 1, false));
						}*/
					}
					else
					{
						var name = member.Name + '.';
						var cols = columns.Where(c => c.IsComplex && c.Name.StartsWith(name)).ToList();

						if (cols.Count == 0)
						{
							yield return (member.Name, null);
						}
						else
						{
							foreach (var col in cols)
							{
								col.Name      = col.Name.Substring(name.Length);
								col.IsComplex = col.Name.Contains(".");
							}

							var typeAcc  = TypeAccessor.GetAccessor(member.Type);
							var isRecord = IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(member.Type), out _);

							var exprs = GetExpressions(typeAcc, isRecord, cols).ToList();

							if (isRecord || !HasDefaultConstructor(member.Type))
							{
								var expr = BuildFromParametrizedConstructor(member.Type, exprs);

								yield return (member.Name, expr);
							}
							else
							{
								var expr = Expression.MemberInit(
									Expression.New(member.Type),
									from m in typeAcc.Members.Zip(exprs, (m,e) => new { m, e })
									where m.e.Expr != null
									select (MemberBinding)Expression.Bind(m.m.MemberInfo, m.e.Expr));

								yield return (member.Name, expr);
							}
						}
					}
				}
			}
		}

		bool IsAnonymous(Type type)
		{
			if (!type.IsPublic     &&
			    type.IsGenericType &&
			    (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal) ||
			     type.Name.StartsWith("VB$AnonymousType",   StringComparison.Ordinal)))
			{
				return Builder.MappingSchema.GetAttribute<CompilerGeneratedAttribute>(type) != null;
			}

			return false;
		}			
			
		bool HasDefaultConstructor(Type type)
		{
			var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

			foreach (var constructor in constructors)
			{
				if (constructor.GetParameters().Length == 0)
					return true;
			}

			return constructors.Length == 0;
		}

		ConstructorInfo SelectParametrizedConstructor(Type objectType)
		{
			var constructors = objectType.GetConstructors();

			if (constructors.Length == 0)
			{
				constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);

				if (constructors.Length == 0)
					throw new InvalidOperationException($"Type '{objectType.Name}' has no constructors.");
			}

			if (constructors.Length > 1)
				throw new InvalidOperationException($"Type '{objectType.Name}' has ambiguous constructors.");

			return constructors[0];
		}

		Expression BuildFromParametrizedConstructor(Type objectType,
			IList<(string Name, Expression? Expr)> expressions)
		{
			var ctor = SelectParametrizedConstructor(objectType);

			var parameters = ctor.GetParameters();
			var argFound   = false;

			var args = new Expression?[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				var param = parameters[i];
				var memberExpr =
					expressions.FirstOrDefault(m => m.Name == param.Name).Expr ??
					expressions.FirstOrDefault(m => m.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
						.Expr;

				var arg = memberExpr;
				argFound = argFound || arg != null;

				arg ??= new DefaultValueExpression(Builder.MappingSchema, param.ParameterType);

				args[i] = arg;
			}

			if (!argFound)
			{
				throw new InvalidOperationException($"Type '{objectType.Name}' has no suitable constructor.");
			}

			var expr = Expression.New(ctor, args);

			return expr;
		}

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index, bool isRecord)
		{
			var exprs = GetExpressions(entityDescriptor.TypeAccessor, isRecord,
				(
					from idx in index
					where idx.Item1 >= 0 && idx.Item2 != null
					from cd in entityDescriptor.Columns.Where(c => c.ColumnName == idx.Item2.PhysicalName)
					select new ColumnInfo
					{
						IsComplex  = cd.MemberAccessor.IsComplex,
						Name       = cd.MemberName,
						Expression = new ConvertFromDataReaderExpression(cd.MemberType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)
					}
				).ToList()).ToList();

			return BuildFromParametrizedConstructor(objectType, exprs);
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index)
		{
			var members =
			(
				from idx in index
				where idx.Item1 >= 0 && idx.Item2 != null
				from cd in entityDescriptor.Columns.Where(c => c.ColumnName == idx.Item2.PhysicalName)
				where
					(cd.Storage != null                                   ||
					 !(cd.MemberAccessor.MemberInfo is PropertyInfo info) ||
					 info.GetSetMethod(true) != null)
				select new
				{
					Column = cd,
					Expr   = new ConvertFromDataReaderExpression(cd.StorageType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)
				}
			).ToList();


			var initExpr = Expression.MemberInit(Expression.New(objectType),
				members
					// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
					.Where(m => !m.Column.MemberAccessor.IsComplex)
					.Select(m => (MemberBinding)Expression.Bind(m.Column.StorageInfo, m.Expr))
			);

			var        hasComplex = members.Count > initExpr.Bindings.Count;
			Expression expr       = initExpr;

			/*var loadWith = GetLoadWith();

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
			}*/

			return expr;
		}

		ParameterExpression? _variable;

		Expression BuildTableExpression(bool buildBlock, Type objectType, Tuple<int, SqlField?>[] index)
		{
			if (buildBlock && _variable != null)
				return _variable;

			var entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(objectType);

			// choosing type that can be instantiated
			/*if ((objectType.IsInterface || objectType.IsAbstract) && !(ObjectType.IsInterface || ObjectType.IsAbstract))
			{
				objectType = ObjectType;
			}*/

			var expr =
				IsRecord(Builder.MappingSchema.GetAttributes<Attribute>(objectType), out var _) ?
					BuildRecordConstructor (entityDescriptor, objectType, index, true) :
					IsAnonymous(objectType) || !HasDefaultConstructor(objectType) ?
						BuildRecordConstructor (entityDescriptor, objectType, index, false) :
						BuildDefaultConstructor(entityDescriptor, objectType, index);

			/*expr = BuildCalculatedColumns(entityDescriptor, expr);
			expr = ProcessExpression(expr);
			expr = NotifyEntityCreated(expr);*/

			if (!buildBlock)
				return expr;

			return _variable = Builder.BuildVariable(expr);
		}

		#endregion

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

		public SqlInfo MakeSql(Expression path, ProjectFlags flags)
		{
			throw new NotImplementedException();
		}

		public SqlInfo MakeColumn(Expression path, SqlInfo sqlInfo, string? alias)
		{
			throw new NotImplementedException();
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
			return new SqlSelectStatement(SelectQuery);
		}

		public void CompleteColumns()
		{
		}
	}
}
