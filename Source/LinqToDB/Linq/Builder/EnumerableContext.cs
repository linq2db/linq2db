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
				var rows = new List<ISqlExpression[]>();
				foreach (var e in arrayExpression.Expressions)
					rows.Add(new[] { Builder.ConvertToSql(Parent, e) });

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

			var columnsInfo = new List<(MemberInfo Member, ColumnDescriptor? Column)>(knownMembers.Count);
			foreach (var m in knownMembers)
			{
				ColumnDescriptor? ci = null;
				foreach (var c in ed.Columns)
				{
					if (c.MemberInfo == m)
					{
						ci = c;
						break;
					}
				}
				columnsInfo.Add((Member: m, Column: ci));
			}

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

			return new SqlValuesTable(fields, columnsInfo.Select(static ci => ci.Member).ToArray(), builtRows);
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

			var indexes = sqlInfos.Select(static info => Tuple.Create(info.Index, QueryHelper.ExtractField(info.Sql))).ToArray();

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

		IEnumerable<(string Name, Expression? Expr)> GetExpressions(TypeAccessor typeAccessor, RecordType recordType, List<ColumnInfo> columns)
		{
			IEnumerable<MemberAccessor> members = typeAccessor.Members;

			if (recordType == RecordType.FSharp)
			{
				var membersWithOrder = new List<(int sequence, MemberAccessor ma)>();
				foreach (var member in typeAccessor.Members)
				{
					var sequence = RecordsHelper.GetFSharpRecordMemberSequence(Builder.MappingSchema, typeAccessor.Type, member.MemberInfo);
					if (sequence != -1)
					{
						membersWithOrder.Add((sequence, member));
					}

					members = membersWithOrder.OrderBy(static _ => _!.sequence).Select(static _ => _.ma);
				}
			}

			/*
			var loadWith      = GetLoadWith();
			var loadWithItems = loadWith == null ? new List<AssociationHelper.LoadWithItem>() : AssociationHelper.GetLoadWith(loadWith);
			*/

			foreach (var member in members)
			{
				ColumnInfo? column = null;
				foreach (var c in columns)
				{
					if (!c.IsComplex && c.Name == member.Name)
					{
						column = c;
						break;
					}
				}

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
						var cols = new List<ColumnInfo>();
						foreach (var c in columns)
						{
							if (c.IsComplex && c.Name.StartsWith(name))
							{
								cols.Add(c);
							}
						}

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

							var typeAcc          = TypeAccessor.GetAccessor(member.Type);
							var memberRecordType = RecordsHelper.GetRecordType(Builder.MappingSchema, member.Type);

							var exprs = GetExpressions(typeAcc, memberRecordType, cols).ToList();

							if ((memberRecordType & RecordType.CallConstructorOnWrite) != 0)
							{
								var expr = BuildFromParametrizedConstructor(member.Type, exprs);

								yield return (member.Name, expr);
							}
							else
							{
								var bindings = new List<MemberBinding>();
								for (var i = 0; i < typeAcc.Members.Count && i < exprs.Count; i++)
								{
									if (exprs[i].Expr != null)
									{
										bindings.Add(Expression.Bind(typeAcc.Members[i].MemberInfo, exprs[i].Expr!));
									}
								}

								var expr = Expression.MemberInit(Expression.New(member.Type), bindings);

								yield return (member.Name, expr);
							}
						}
					}
				}
			}
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

			var args = new Expression[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				var param = parameters[i];
				Expression? memberExpr = null;
				foreach (var (Name, Expr) in expressions)
				{
					if (Name == param.Name)
					{
						memberExpr = Expr;
						break;
					}
				}
				if (memberExpr == null)
				{
					foreach (var (Name, Expr) in expressions)
					{
						if (Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase))
						{
							memberExpr = Expr;
							break;
						}
					}
				}

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

		Expression BuildRecordConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index, RecordType recordType)
		{
			var columns = new List<ColumnInfo>();
			foreach (var idx in index)
			{
				if (idx.Item1 >= 0 && idx.Item2 != null)
				{
					ColumnDescriptor? cd = null;
					foreach (var c in entityDescriptor.Columns)
					{
						if (c.ColumnName == idx.Item2.PhysicalName)
						{
							cd = c;
							break;
						}
					}

					if (cd != null)
					{
						columns.Add(new ColumnInfo
						{
							IsComplex  = cd.MemberAccessor.IsComplex,
							Name       = cd.MemberName,
							Expression = new ConvertFromDataReaderExpression(cd.MemberType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)
						});
					}
				}
			}

			var exprs = GetExpressions(entityDescriptor.TypeAccessor, recordType, columns).ToList();

			return BuildFromParametrizedConstructor(objectType, exprs);
		}

		Expression BuildDefaultConstructor(EntityDescriptor entityDescriptor, Type objectType, Tuple<int, SqlField?>[] index)
		{
			var members = new List<(ColumnDescriptor Column, ConvertFromDataReaderExpression Expr)>();
			foreach (var idx in index)
			{
				if (idx.Item1 >= 0 && idx.Item2 != null)
				{
					ColumnDescriptor? cd = null;
					foreach (var c in entityDescriptor.Columns)
					{
						if (c.ColumnName == idx.Item2.PhysicalName)
						{
							cd = c;
							break;
						}
					}

					if (cd != null
						&& (cd.Storage != null ||
							!(cd.MemberAccessor.MemberInfo is PropertyInfo info) ||
							info.GetSetMethod(true) != null))
					{
						members.Add((cd, new ConvertFromDataReaderExpression(cd.StorageType, idx.Item1, cd.ValueConverter, Builder.DataReaderLocal)));
					}
				}
			}

			var initExpr = Expression.MemberInit(Expression.New(objectType),
				members
					// IMPORTANT: refactoring this condition will affect hasComplex variable calculation below
					.Where(static m => !m.Column.MemberAccessor.IsComplex)
					.Select(static m => (MemberBinding)Expression.Bind(m.Column.StorageInfo, m.Expr))
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

			var recordType       = RecordsHelper.GetRecordType(Builder.MappingSchema, objectType);
			var entityDescriptor = Builder.MappingSchema.GetEntityDescriptor(objectType);

			// choosing type that can be instantiated
			/*if ((objectType.IsInterface || objectType.IsAbstract) && !(ObjectType.IsInterface || ObjectType.IsAbstract))
			{
				objectType = ObjectType;
			}*/

			var expr =
				recordType != RecordType.NotRecord ?
					BuildRecordConstructor (entityDescriptor, objectType, index, recordType) :
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
						var body = Expression.New(Methods.LinqToDB.Sql.SqlValueConstructor,
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
						sql = new SqlInfo[Table.Fields.Count];
						for (var i = 0; i < Table.Fields.Count; i++)
							sql[i] = new SqlInfo(Table.Fields[i].ColumnDescriptor.MemberInfo, Table.Fields[i], SelectQuery);
					}
					else
					{
						sql = new SqlInfo[_entityDescriptor.Columns.Count];
						for (var i = 0; i < _entityDescriptor.Columns.Count; i++)
							sql[i] = new SqlInfo(_entityDescriptor.Columns[i].MemberInfo, BuildField(_entityDescriptor.Columns[i]), SelectQuery);
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
							Methods.LinqToDB.Sql.SqlParameterConstructor,
							Expression.Property(variable, Methods.LinqToDB.DataParameter.DbDataType),
							Expression.Constant(memberName),
							Expression.Property(variable, Methods.LinqToDB.DataParameter.Value)
						));
				}
				else
				{
					generator.AddExpression(Expression.New(Methods.LinqToDB.Sql.SqlValueConstructor,
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
								foreach (var f in Table.Fields)
								{
									if (MemberInfoComparer.Instance.Equals(f.ColumnDescriptor?.MemberInfo, me.Member))
										return IsExpressionResult.True;
								}
							}
							else
							{
								foreach (var c in _entityDescriptor.Columns)
								{
									if (MemberInfoComparer.Instance.Equals(c.MemberInfo, me.Member))
										return IsExpressionResult.True;
								}
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
