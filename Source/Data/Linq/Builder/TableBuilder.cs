using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Extensions;

namespace LinqToDB.Data.Linq.Builder
{
	using LinqToDB.Linq;
	using Data.Sql;
	using Mapping;
	using Reflection;
	using Reflection.Extension;

	class TableBuilder : ISequenceBuilder
	{
		#region TableBuilder

		int ISequenceBuilder.BuildCounter { get; set; }

		static T Find<T>(ExpressionBuilder builder, BuildInfo buildInfo, Func<int,IBuildContext,T> action)
		{
			var expression = buildInfo.Expression;

			switch (expression.NodeType)
			{
				case ExpressionType.Constant:
					{
						var c = (ConstantExpression)expression;
						if (c.Value is IQueryable)
							return action(1, null);

						break;
					}

				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expression;

						if (mc.Method.Name == "GetTable")
							if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Table<>))
								return action(2, null);

						var attr = builder.GetTableFunctionAttribute(mc.Method);

						if (attr != null)
							return action(5, null);

						break;
					}

				case ExpressionType.MemberAccess:

					if (expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(Table<>))
						return action(3, null);

					// Looking for association.
					//
					if (buildInfo.IsSubQuery && buildInfo.SqlQuery.From.Tables.Count == 0)
					{
						var ctx = builder.GetContext(buildInfo.Parent, expression);
						if (ctx != null)
							return action(4, ctx);
					}

					break;

				case ExpressionType.Parameter:
					{
						if (buildInfo.IsSubQuery && buildInfo.SqlQuery.From.Tables.Count == 0)
						{
							var ctx = builder.GetContext(buildInfo.Parent, expression);
							if (ctx != null)
								return action(4, ctx);
						}

						break;
					}
			}

			return action(0, null);
		}

		public bool CanBuild(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return Find(builder, buildInfo, (n,_) => n > 0);
		}

		public IBuildContext BuildSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return Find(builder, buildInfo, (n,ctx) =>
			{
				switch (n)
				{
					case 0 : return null;
					case 1 : return new TableContext(builder, buildInfo, ((IQueryable)((ConstantExpression)buildInfo.Expression).Value).ElementType);
					case 2 :
					case 3 : return new TableContext(builder, buildInfo, buildInfo.Expression.Type.GetGenericArguments()[0]);
					case 4 : return ctx.GetContext(buildInfo.Expression, 0, buildInfo);
					case 5 : return new TableContext(builder, buildInfo);
				}

				throw new InvalidOperationException();
			});
		}

		public SequenceConvertInfo Convert(ExpressionBuilder builder, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public bool IsSequence(ExpressionBuilder builder, BuildInfo buildInfo)
		{
			return true;
		}

		#endregion

		#region TableContext

		public class TableContext : IBuildContext
		{
			#region Properties

#if DEBUG
			public string _sqlQueryText { get { return SqlQuery == null ? "" : SqlQuery.SqlText; } }
#endif

			public ExpressionBuilder Builder    { get; private set; }
			public Expression        Expression { get; private set; }
			public SqlQuery          SqlQuery   { get; set; }

			public virtual IBuildContext Parent { get; set; }

			public    Type         OriginalType;
			public    Type         ObjectType;
			protected ObjectMapper ObjectMapper;
			public    SqlTable     SqlTable;

			#endregion

			#region Init

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo, Type originalType)
			{
				Builder      = builder;
				Parent       = buildInfo.Parent;
				Expression   = buildInfo.Expression;
				SqlQuery     = buildInfo.SqlQuery;

				OriginalType = originalType;
				ObjectType   = GetObjectType();
				SqlTable     = new SqlTable(builder.MappingSchema, ObjectType);
				ObjectMapper = Builder.MappingSchema.GetObjectMapper(ObjectType);

				SqlQuery.From.Table(SqlTable);

				Init();
			}

			protected TableContext(ExpressionBuilder builder, SqlQuery sqlQuery)
			{
				Builder  = builder;
				SqlQuery = sqlQuery;
			}

			public TableContext(ExpressionBuilder builder, BuildInfo buildInfo)
			{
				Builder    = builder;
				Parent     = buildInfo.Parent;
				Expression = buildInfo.Expression;
				SqlQuery   = buildInfo.SqlQuery;

				var mc   = (MethodCallExpression)Expression;
				var attr = builder.GetTableFunctionAttribute(mc.Method);

				if (!mc.Method.ReturnType.IsGenericType || mc.Method.ReturnType.GetGenericTypeDefinition() != typeof(Table<>))
					throw new LinqException("Table function has to return Table<T>.");

				OriginalType = mc.Method.ReturnType.GetGenericArguments()[0];
				ObjectType   = GetObjectType();
				SqlTable     = new SqlTable(builder.MappingSchema, ObjectType);
				ObjectMapper = Builder.MappingSchema.GetObjectMapper(ObjectType);

				SqlQuery.From.Table(SqlTable);

				var args = mc.Arguments.Select(a => builder.ConvertToSql(this, a));

				attr.SetTable(SqlTable, mc.Method, mc.Arguments, args);

				Init();
			}

			protected Type GetObjectType()
			{
				for (var type = OriginalType.BaseType; type != null && type != typeof(object); type = type.BaseType)
				{
					var extension = TypeExtension.GetTypeExtension(type, Builder.MappingSchema.Extensions);
					var mapping   = Builder.MappingSchema.MetadataProvider.GetInheritanceMapping(type, extension);

					if (mapping.Length > 0)
						return type;
				}

				return OriginalType;
			}

			public List<InheritanceMappingAttribute> InheritanceMapping;
			public List<string>                      InheritanceDiscriminators;

			protected void Init()
			{
				Builder.Contexts.Add(this);

				InheritanceMapping = ObjectMapper.InheritanceMapping;

				if (InheritanceMapping.Count > 0)
					InheritanceDiscriminators = GetInheritanceDiscriminators(Builder, SqlTable, ObjectType, InheritanceMapping);

				// Original table is a parent.
				//
				if (ObjectType != OriginalType)
				{
					var predicate = Builder.MakeIsPredicate(this, OriginalType);

					if (predicate.GetType() != typeof(SqlQuery.Predicate.Expr))
						SqlQuery.Where.SearchCondition.Conditions.Add(new SqlQuery.Condition(false, predicate));
				}
			}

			internal static List<string> GetInheritanceDiscriminators(
				ExpressionBuilder                 builder,
				SqlTable                          sqlTable,
				Type                              objectType,
				List<InheritanceMappingAttribute> inheritanceMapping)
			{
				var inheritanceDiscriminators = new List<string>(inheritanceMapping.Count);

				foreach (var mapping in inheritanceMapping)
				{
					string discriminator = null;

					foreach (MemberMapper mm in builder.MappingSchema.GetObjectMapper(mapping.Type))
					{
						if (mm.MapMemberInfo.SqlIgnore == false && !sqlTable.Fields.Any(f => f.Value.Name == mm.MemberName))
						{
							var field = new SqlField(mm.Type, mm.MemberName, mm.Name, mm.MapMemberInfo.Nullable, int.MinValue, null, mm);
							sqlTable.Fields.Add(field);
						}

						if (mm.MapMemberInfo.IsInheritanceDiscriminator)
							discriminator = mm.MapMemberInfo.MemberName;
					}

					inheritanceDiscriminators.Add(discriminator);
				}

				var dname = inheritanceDiscriminators.FirstOrDefault(s => s != null);

				if (dname == null)
					throw new LinqException("Inheritance Discriminator is not defined for the '{0}' hierarchy.", objectType);

				for (var i = 0; i < inheritanceDiscriminators.Count; i++)
					if (inheritanceDiscriminators[i] == null)
						inheritanceDiscriminators[i] = dname;

				return inheritanceDiscriminators;
			}

			#endregion

			#region BuildQuery

			class MappingData
			{
				public MappingSchema  MappingSchema;
				public ObjectMapper   ObjectMapper;
				public int[]          Index;
				public IValueMapper[] ValueMappers;
			}

			static object MapDataReaderToObject(IDataReader dataReader, MappingData data)
			{
				var source     = data.MappingSchema.CreateDataReaderMapper(dataReader);
				var destObject = data.ObjectMapper.CreateInstance();

				if (data.ValueMappers == null)
				{
					var mappers = new IValueMapper[data.Index.Length];

					for (var i = 0; i < data.Index.Length; i++)
					{
						var n = data.Index[i];

						if (n < 0)
							continue;

						if (!data.ObjectMapper.SupportsTypedValues(i))
						{
							mappers[i] = data.MappingSchema.DefaultValueMapper;
							continue;
						}

						var sourceType = source.           GetFieldType(n) ?? typeof(object);
						var destType   = data.ObjectMapper.GetFieldType(i) ?? typeof(object);

						IValueMapper t;

						if (sourceType == destType)
						{
							lock (data.MappingSchema.SameTypeMappers)
								if (!data.MappingSchema.SameTypeMappers.TryGetValue(sourceType, out t))
									data.MappingSchema.SameTypeMappers.Add(sourceType, t = data.MappingSchema.GetValueMapper(sourceType, destType));
						}
						else
						{
							var key = new KeyValuePair<Type,Type>(sourceType, destType);

							lock (data.MappingSchema.DifferentTypeMappers)
								if (!data.MappingSchema.DifferentTypeMappers.TryGetValue(key, out t))
									data.MappingSchema.DifferentTypeMappers.Add(key, t = data.MappingSchema.GetValueMapper(sourceType, destType));
						}

						mappers[i] = t;
					}

					data.ValueMappers = mappers;
				}

				var dest = data.ObjectMapper;
				var idx  = data.Index;
				var ms   = data.ValueMappers;

				for (var i = 0; i < idx.Length; i++)
				{
					var n = idx[i];

					if (n >= 0)
						ms[i].Map(source, dataReader, n, dest, destObject, i);
				}

				return destObject;
			}

			static object DefaultInheritanceMappingException(object value, Type type)
			{
				throw new LinqException("Inheritance mapping is not defined for discriminator value '{0}' in the '{1}' hierarchy.", value, type);
			}

			static readonly MethodInfo _mapperMethod = ReflectionHelper.Expressor<object>.MethodExpressor(_ => MapDataReaderToObject(null, null));

#if FW4 || SILVERLIGHT
			ParameterExpression _variable;
			static int _varIndex;
#endif

			Expression BuildTableExpression(bool buildBlock, Type objectType, int[] index)
			{
#if FW4 || SILVERLIGHT
				if (buildBlock && _variable != null)
					return _variable;
#endif

				var data = new MappingData
				{
					MappingSchema = Builder.MappingSchema,
					ObjectMapper  = Builder.MappingSchema.GetObjectMapper(objectType),
					Index         = index
				};

				Expression expr = Expression.Convert(
					Expression.Call(null, _mapperMethod,
						ExpressionBuilder.DataReaderParam,
						Expression.Constant(data)),
					objectType);

				expr = ProcessExpression(expr);

#if FW4 || SILVERLIGHT

				if (!buildBlock)
					return expr;

				Builder.BlockVariables.  Add(_variable = Expression.Variable(expr.Type, expr.Type.Name + _varIndex++));
				Builder.BlockExpressions.Add(Expression.Assign(_variable, expr));

				return _variable;

#else
				return expr;
#endif
			}

			protected virtual Expression ProcessExpression(Expression expression)
			{
				return expression;
			}

			int[] BuildIndex(int[] index, Type objectType)
			{
				var names = new Dictionary<string,int>();
				var n     = 0;

				foreach (MemberMapper mm in Builder.MappingSchema.GetObjectMapper(objectType))
					if (mm.MapMemberInfo.SqlIgnore == false)
						names.Add(mm.MemberName, n++);

				var q =
					from r in SqlTable.Fields.Values.Select((f,i) => new { f, i })
					where names.ContainsKey(r.f.Name)
					orderby names[r.f.Name]
					select index[r.i];

				return q.ToArray();
			}

			Expression BuildQuery(Type tableType)
			{
				SqlInfo[] info;

				if (ObjectType == tableType)
				{
					info = ConvertToIndex(null, 0, ConvertFlags.All);
				}
				else
				{
					info = ConvertToSql(null, 0, ConvertFlags.All);

					var table = new SqlTable(Builder.MappingSchema, tableType);
					var q     =
						from fld1 in table.Fields.Values.Select((f,i) => new { f, i })
						join fld2 in info on fld1.f.Name equals ((SqlField)fld2.Sql).Name
						orderby fld1.i
						select GetIndex(fld2);

					info = q.ToArray();
				}

				var index = info.Select(idx => ConvertToParentIndex(idx.Index, null)).ToArray();

				if (ObjectType != tableType || InheritanceMapping.Count == 0)
					return BuildTableExpression(!Builder.IsBlockDisable, tableType, index);

				Expression expr;

				var defaultMapping = InheritanceMapping.SingleOrDefault(m => m.IsDefault);

				if (defaultMapping != null)
				{
					expr = Expression.Convert(
						BuildTableExpression(false, defaultMapping.Type, BuildIndex(index, defaultMapping.Type)),
						ObjectType);
				}
				else
				{
					var exceptionMethod = ReflectionHelper.Expressor<object>.MethodExpressor(_ => DefaultInheritanceMappingException(null, null));
					var dindex          =
						(
							from f in SqlTable.Fields.Values
							where f.Name == InheritanceDiscriminators[0]
							select ConvertToParentIndex(_indexes[f].Index, null)
						).First();

					expr = Expression.Convert(
						Expression.Call(null, exceptionMethod,
							Expression.Call(
								ExpressionBuilder.DataReaderParam,
								ReflectionHelper.DataReader.GetValue,
								Expression.Constant(dindex)),
							Expression.Constant(ObjectType)),
						ObjectType);
				}

				foreach (var mapping in InheritanceMapping.Select((m,i) => new { m, i }).Where(m => m.m != defaultMapping))
				{
					var dindex =
						(
							from f in SqlTable.Fields.Values
							where f.Name == InheritanceDiscriminators[mapping.i]
							select ConvertToParentIndex(_indexes[f].Index, null)
						).First();

					Expression testExpr;

					if (mapping.m.Code == null)
					{
						testExpr = Expression.Call(
							ExpressionBuilder.DataReaderParam,
							ReflectionHelper.DataReader.IsDBNull,
							Expression.Constant(dindex));
					}
					else
					{
						var codeType = mapping.m.Code.GetType();

						testExpr = Expression.Equal(
							Expression.Constant(mapping.m.Code),
							Builder.BuildSql(codeType, dindex));
					}

					expr = Expression.Condition(
						testExpr,
						Expression.Convert(BuildTableExpression(false, mapping.m.Type, BuildIndex(index, mapping.m.Type)), ObjectType),
						expr);
				}

				return expr;
			}

			public void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				var expr   = BuildQuery(typeof(T));
				var mapper = Builder.BuildMapper<T>(expr);

				query.SetQuery(mapper.Compile());
			}

			#endregion

			#region BuildExpression

			public Expression BuildExpression(Expression expression, int level)
			{
				// Build table.
				//
				var table = FindTable(expression, level, false);

				if (table == null)
				{
					if (expression is MemberExpression)
					{
						var memberExpression = (MemberExpression)expression;

						if (ObjectMapper != null &&
							ObjectMapper.TypeAccessor.Type == memberExpression.Member.DeclaringType)
						{
							throw new LinqException("Member '{0}.{1}' is not a table column.",
								memberExpression.Member.Name, memberExpression.Member.Name);
						}
					}

					throw new InvalidOperationException();
				}

				if (table.Field == null)
					return table.Table.BuildQuery(table.Table.OriginalType);

				// Build field.
				//
				var info = ConvertToIndex(expression, level, ConvertFlags.Field).Single();
				var idx  = ConvertToParentIndex(info.Index, null);

				return Builder.BuildSql(expression.Type, idx);
			}

			#endregion

			#region ConvertToSql

			public SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.All   :
						{
							var table = FindTable(expression, level, false);

							if (table.Field == null)
								return table.Table.SqlTable.Fields.Values
									.Select(_ => new SqlInfo { Sql = _, Member = _.MemberMapper.MemberAccessor.MemberInfo })
									.ToArray();

							break;
						}

					case ConvertFlags.Key   :
						{
							var table = FindTable(expression, level, false);

							if (table.Field == null)
							{
								var q =
									from f in table.Table.SqlTable.Fields.Values
									where f.IsPrimaryKey
									orderby f.PrimaryKeyOrder
									select new SqlInfo { Sql = f, Member = f.MemberMapper.MemberAccessor.MemberInfo };

								var key = q.ToArray();

								return key.Length != 0 ? key : ConvertToSql(expression, level, ConvertFlags.All);
							}

							break;
						}

					case ConvertFlags.Field :
						{
							var table = FindTable(expression, level, true);

							if (table.Field != null)
								return new[]
								{
									new SqlInfo { Sql = table.Field, Member = table.Field.MemberMapper.MemberAccessor.MemberInfo }
								};

							break;
						}
				}

				throw new NotImplementedException();
			}

			#endregion

			#region ConvertToIndex

			readonly Dictionary<ISqlExpression,SqlInfo> _indexes = new Dictionary<ISqlExpression,SqlInfo>();

			protected SqlInfo GetIndex(SqlInfo expr)
			{
				SqlInfo n;

				if (_indexes.TryGetValue(expr.Sql, out n))
					return n;

				if (expr.Sql is SqlField)
				{
					var field = (SqlField)expr.Sql;
					expr.Index = SqlQuery.Select.Add(field, field.Alias);
				}
				else
				{
					expr.Index = SqlQuery.Select.Add(expr.Sql);
				}

				expr.Query = SqlQuery;

				_indexes.Add(expr.Sql, expr);

				return expr;
			}

			public SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				switch (flags)
				{
					case ConvertFlags.Field :
					case ConvertFlags.Key   :
					case ConvertFlags.All   :

						var info = ConvertToSql(expression, level, flags);

						for (var i = 0; i < info.Length; i++)
							info[i] = GetIndex(info[i]);

						return info;
				}

				throw new NotImplementedException();
			}

			#endregion

			#region IsExpression

			public IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFor)
			{
				switch (requestFor)
				{
					case RequestFor.Field      :
						{
							var table = FindTable(expression, level, false);
							return new IsExpressionResult(table != null && table.Field != null);
						}

					case RequestFor.Table       :
					case RequestFor.Object      :
						{
							var table = FindTable(expression, level, false);
							return new IsExpressionResult(
								table       != null &&
								table.Field == null &&
								(expression == null || expression.GetLevelExpression(table.Level) == expression));
						}

					case RequestFor.Expression :
						{
							if (expression == null)
								return IsExpressionResult.False;

							var levelExpression = expression.GetLevelExpression(level);

							switch (levelExpression.NodeType)
							{
								case ExpressionType.MemberAccess :
								case ExpressionType.Parameter    :
								case ExpressionType.Call         :

									var table = FindTable(expression, level, false);
									return new IsExpressionResult(table == null);
							}

							return IsExpressionResult.True;
						}

					case RequestFor.Association      :
						{
							if (ObjectMapper.Associations.Count > 0)
							{
								var table = FindTable(expression, level, false);
								var isat  =
									table       != null &&
									table.Table is AssociatedTableContext &&
									table.Field == null &&
									(expression == null || expression.GetLevelExpression(table.Level) == expression);

								return new IsExpressionResult(isat, isat ? table.Table : null);
							}

							return IsExpressionResult.False;
						}
				}

				return IsExpressionResult.False;
			}

			#endregion

			#region GetContext

			interface IAssociationHelper
			{
				Expression GetExpression(Expression parent, AssociatedTableContext association);
			}

			class AssociationHelper<T> : IAssociationHelper
				where T : class
			{
				public Expression GetExpression(Expression parent, AssociatedTableContext association)
				{
					Expression expr  = null;
					var        param = Expression.Parameter(typeof(T), "c");

					foreach (var cond in (association).ParentAssociationJoin.Condition.Conditions)
					{
						var p  = (SqlQuery.Predicate.ExprExpr)cond.Predicate;
						var e1 = Expression.MakeMemberAccess(parent, ((SqlField)p.Expr1).MemberMapper.MemberAccessor.MemberInfo);
						var e2 = Expression.MakeMemberAccess(param,  ((SqlField)p.Expr2).MemberMapper.MemberAccessor.MemberInfo) as Expression;

						while (e1.Type != e2.Type)
						{
							if (e1.Type.IsNullable())
							{
								e1 = Expression.PropertyOrField(e1, "Value");
								continue;
							}

							if (e2.Type.IsNullable())
							{
								e2 = Expression.PropertyOrField(e2, "Value");
								continue;
							}

							e2 = Expression.Convert(e2, e1.Type);
						}

						var ex = Expression.Equal(e1, e2);
							
						expr = expr == null ? ex : Expression.AndAlso(expr, ex);
					}

					var predicate = Expression.Lambda<Func<T,bool>>(expr, param);

					return association.Builder.DataContextInfo.DataContext.GetTable<T>().Where(predicate).Expression;
				}
			}

			public IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				if (expression == null)
				{
					if (buildInfo != null && buildInfo.IsSubQuery)
					{
						var table = new TableContext(
							Builder,
							new BuildInfo(Parent is SelectManyBuilder.SelectManyContext ? this : Parent, Expression, buildInfo.SqlQuery),
							SqlTable.ObjectType);

						return table;
					}

					return this;
				}

				if (ObjectMapper.Associations.Count > 0)
				{
					var levelExpression = expression.GetLevelExpression(level);

					if (buildInfo != null && buildInfo.IsSubQuery)
					{
						if (levelExpression == expression && expression.NodeType == ExpressionType.MemberAccess)
						{
							var association = (AssociatedTableContext)GetAssociation(expression, level).Table;

							if (association.IsList)
							{
								var ma     = (MemberExpression)buildInfo.Expression;
								var atype  = typeof(AssociationHelper<>).MakeGenericType(association.ObjectType);
								var helper = (IAssociationHelper)Activator.CreateInstance(atype);
								var expr   = helper.GetExpression(ma.Expression, association);

								buildInfo.IsAssociationBuilt = true;

								return Builder.BuildSequence(new BuildInfo(buildInfo, expr));
							}
						}
						else
						{
							var association = GetAssociation(levelExpression, level);
							((AssociatedTableContext)association.Table).ParentAssociationJoin.IsWeak = false;
							return association.Table.GetContext(expression, level + 1, buildInfo);
						}
					}
				}

				throw new InvalidOperationException();
			}

			#endregion

			#region ConvertToParentIndex

			public int ConvertToParentIndex(int index, IBuildContext context)
			{
				return Parent == null ? index : Parent.ConvertToParentIndex(index, this);
			}

			#endregion

			#region SetAlias

			public void SetAlias(string alias)
			{
				if (alias.Contains('<'))
					return;

				if (SqlTable.Alias == null)
					SqlTable.Alias = alias;
			}

			#endregion

			#region GetSubQuery

			public ISqlExpression GetSubQuery(IBuildContext context)
			{
				return null;
			}

			#endregion

			#region Helpers

			SqlField GetField(Expression expression, int level, bool throwException)
			{
				if (expression.NodeType == ExpressionType.MemberAccess)
				{
					var memberExpression = (MemberExpression)expression;
					var levelExpression  = expression.GetLevelExpression(level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						if (levelExpression != expression)
						{
							var levelMember = (MemberExpression)levelExpression;

							if (ReflectionExtensions.IsNullableValueMember(memberExpression.Member) && memberExpression.Expression == levelExpression)
								memberExpression = levelMember;
							else
							{
								var sameType =
									levelMember.Member.ReflectedType == SqlTable.ObjectType ||
									levelMember.Member.DeclaringType == SqlTable.ObjectType;

								if (!sameType)
								{
									var mi = SqlTable.ObjectType.GetMember(levelMember.Member.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
									sameType = mi.Any(_ => _.DeclaringType == levelMember.Member.DeclaringType);
								}

								if (sameType || InheritanceMapping.Count > 0)
								{
									foreach (var field in SqlTable.Fields.Values)
									{
										if (field.MemberMapper is MemberMapper.ComplexMapper)
										{
											var name = levelMember.Member.Name;

											for (var ex = (MemberExpression)expression; ex != levelMember; ex = (MemberExpression)ex.Expression)
												name += "." + ex.Member.Name;

											if (field.MemberMapper.MemberName == name)
												return field;
										}
									}
								}
							}
						}

						if (levelExpression == memberExpression)
						{
							foreach (var field in SqlTable.Fields.Values)
							{
								if (ReflectionExtensions.Equals(field.MemberMapper.MapMemberInfo.MemberAccessor.MemberInfo, memberExpression.Member))
								{
									if (field.MemberMapper is MemberMapper.ComplexMapper &&
										field.MemberMapper.MemberName.IndexOf('.') > 0)
									{
										var name = memberExpression.Member.Name;
										var me   = memberExpression;

										if (!(me.Expression is MemberExpression))
											return null;

										while (me.Expression is MemberExpression)
										{
											me   = (MemberExpression)me.Expression;
											name = me.Member.Name + '.' + name;
										}

										return SqlTable.Fields.Values.FirstOrDefault(f => f.MemberMapper.MemberName == name);
									}

									return field;
								}

								if (InheritanceMapping.Count > 0 && field.Name == memberExpression.Member.Name)
									foreach (var mapping in InheritanceMapping)
										foreach (MemberMapper mm in Builder.MappingSchema.GetObjectMapper(mapping.Type))
											if (ReflectionExtensions.Equals(mm.MapMemberInfo.MemberAccessor.MemberInfo, memberExpression.Member))
												return field;
							}

							if (throwException &&
								ObjectMapper != null &&
								ObjectMapper.TypeAccessor.Type == memberExpression.Member.DeclaringType)
							{
								throw new LinqException("Member '{0}.{1}' is not a table column.",
									memberExpression.Member.Name, memberExpression.Member.Name);
							}
						}
					}
				}

				return null;
			}

			[JetBrains.Annotations.NotNull]
			readonly Dictionary<MemberInfo,AssociatedTableContext> _associations =
				new Dictionary<MemberInfo,AssociatedTableContext>(new MemberInfoComparer());

			class TableLevel
			{
				public TableContext Table;
				public SqlField     Field;
				public int          Level;
			}

			TableLevel FindTable(Expression expression, int level, bool throwException)
			{
				if (expression == null)
					return new TableLevel { Table = this };

				var levelExpression = expression.GetLevelExpression(level);

				switch (levelExpression.NodeType)
				{
					case ExpressionType.MemberAccess :
					case ExpressionType.Parameter    :
						{
							var field = GetField(expression, level, throwException);

							if (field != null || (level == 0 && levelExpression == expression))
								return new TableLevel { Table = this, Field = field, Level = level };

							return GetAssociation(expression, level);
						}
				}

				return null;
			}

			TableLevel GetAssociation(Expression expression, int level)
			{
				var objectMapper    = ObjectMapper;
				var levelExpression = expression.GetLevelExpression(level);
				var inheritance     =
					(
						from m in InheritanceMapping
						let om = Builder.MappingSchema.GetObjectMapper(m.Type)
						where om.Associations.Count > 0
						select om
					).ToList();

				if (objectMapper.Associations.Count > 0 || inheritance.Count > 0)
				{
					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var memberExpression = (MemberExpression)levelExpression;

						AssociatedTableContext tableAssociation;

						if (!_associations.TryGetValue(memberExpression.Member, out tableAssociation))
						{
							var q =
								from a in objectMapper.Associations.Concat(inheritance.SelectMany(om => om.Associations))
								where ReflectionExtensions.Equals(a.MemberAccessor.MemberInfo, memberExpression.Member)
								select new AssociatedTableContext(Builder, this, a) { Parent = Parent };

							tableAssociation = q.FirstOrDefault();

							_associations.Add(memberExpression.Member, tableAssociation);
						}

						if (tableAssociation != null)
						{
							if (levelExpression == expression)
								return new TableLevel { Table = tableAssociation, Level = level };

							var al = tableAssociation.GetAssociation(expression, level + 1);

							if (al != null)
								return al;

							var field = tableAssociation.GetField(expression, level + 1, false);

							return new TableLevel { Table = tableAssociation, Field = field, Level = field == null ? level : level + 1 };
						}
					}
				}

				return null;
			}

			#endregion
		}

		#endregion

		#region AssociatedTableContext

		public class AssociatedTableContext : TableContext
		{
			public readonly TableContext          ParentAssociation;
			public readonly SqlQuery.JoinedTable  ParentAssociationJoin;
			public readonly Association           Association;
			public readonly bool                  IsList;

			public override IBuildContext Parent
			{
				get { return ParentAssociation.Parent; }
				set { }
			}

			public AssociatedTableContext(ExpressionBuilder builder, TableContext parent, Association association)
				: base(builder, parent.SqlQuery)
			{
				var type = ReflectionExtensions.GetMemberType(association.MemberAccessor.MemberInfo);
				var left = association.CanBeNull;

				if (ReflectionExtensions.IsSameOrParent(typeof(IEnumerable), type))
				{
					var etypes = ReflectionExtensions.GetGenericArguments(type, typeof(IEnumerable));
					type       = etypes != null && etypes.Length > 0 ? etypes[0] : ReflectionExtensions.GetListItemType(type);
					IsList     = true;
				}

				OriginalType = type;
				ObjectType   = GetObjectType();
				ObjectMapper = Builder.MappingSchema.GetObjectMapper(ObjectType);
				SqlTable     = new SqlTable(builder.MappingSchema, ObjectType);

				var psrc = parent.SqlQuery.From[parent.SqlTable];
				var join = left ? SqlTable.WeakLeftJoin() : IsList ? SqlTable.InnerJoin() : SqlTable.WeakInnerJoin();

				Association           = association;
				ParentAssociation     = parent;
				ParentAssociationJoin = join.JoinedTable;

				psrc.Joins.Add(join.JoinedTable);

				for (var i = 0; i < association.ThisKey.Length; i++)
				{
					SqlField field1;
					SqlField field2;

					if (!parent.SqlTable.Fields.TryGetValue(association.ThisKey[i], out field1))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.ThisKey[i], parent.ObjectType);

					if (!SqlTable.Fields.TryGetValue(association.OtherKey[i], out field2))
						throw new LinqException("Association key '{0}' not found for type '{1}.", association.OtherKey[i], ObjectType);

					join.Field(field1).Equal.Field(field2);
				}

				Init();
			}

			protected override Expression ProcessExpression(Expression expression)
			{
				var isLeft = false;

				for (
					var association = this;
					isLeft == false && association != null;
					association = association.ParentAssociation as AssociatedTableContext)
				{
					isLeft =
						association.ParentAssociationJoin.JoinType == SqlQuery.JoinType.Left ||
						association.ParentAssociationJoin.JoinType == SqlQuery.JoinType.OuterApply;
				}

				if (isLeft)
				{
					Expression cond = null;

					var keys = ConvertToIndex(null, 0, ConvertFlags.Key);

					foreach (var key in keys)
					{
						var index2  = ConvertToParentIndex(key.Index, null);

						Expression e = Expression.Call(
							ExpressionBuilder.DataReaderParam,
							ReflectionHelper.DataReader.IsDBNull,
							Expression.Constant(index2));

						cond = cond == null ? e : Expression.AndAlso(cond, e);
					}

					expression = Expression.Condition(cond, Expression.Constant(null, expression.Type), expression);
				}

				return expression;
			}
		}

		#endregion
	}
}
