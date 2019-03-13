using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using JetBrains.Annotations;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Linq.Builder;
using LinqToDB.Linq.Parser;
using LinqToDB.Linq.Parser.Clauses;
using LinqToDB.Mapping;
using LinqToDB.SqlProvider;
using LinqToDB.SqlQuery;
using LinqToDB.Reflection;

namespace LinqToDB.Linq.Generator
{
	public class QueryGenerator
	{
		public ModelTranslator Translator { get; }
		public IDataContext DataContext { get; }
		public MappingSchema MappingSchema => DataContext.MappingSchema;

		private readonly Dictionary<IQuerySource, QuerySourceRegistry> _registeredTableSources = new Dictionary<IQuerySource, QuerySourceRegistry>();
		private readonly Dictionary<MemberExpression, MemberTransformationInfo> _memberTransformations = CreateTransformationDictionary();
		private Dictionary<IQuerySource, SetRegistration> _setSelectors = new Dictionary<IQuerySource, SetRegistration>();

		static Dictionary<MemberExpression, MemberTransformationInfo> CreateTransformationDictionary()
		{
			return new Dictionary<MemberExpression, MemberTransformationInfo>(new ExpressionEqualityComparer());
		}

		class SetRegistration
		{
			public Dictionary<MemberExpression, MemberTransformationInfo> Sequence1Transformations { get; } = CreateTransformationDictionary();
			public Dictionary<MemberExpression, MemberTransformationInfo> Sequence2Transformations { get; } = CreateTransformationDictionary();
			public Expression Selector { get; set; }
		}


		SqlTableSource GenerateTableSource(SelectQuery selectQuery, SqlTableSource current, IQuerySource querySource, out bool shouldAdd)
		{
			ISqlTableSource source;
			shouldAdd = false;
			switch (querySource)
			{
				case TableSource ts:
					{
						source = new SqlTable(MappingSchema, querySource.ItemType);
						shouldAdd = true;
						break;
					}
				case JoinClause jc:
					{
						shouldAdd = true;
						var joinType = JoinType.Inner;
						var ts = GenerateTableSource(selectQuery, current, jc.Inner, out _);
						RegisterTableSource(selectQuery, jc.Inner, ts);

						var condition = CorrectEquality(jc.Condition);
						var searchCondition = new SqlSearchCondition();
						BuildSearchCondition(selectQuery, condition, searchCondition.Conditions);
						current.Joins.Add(new SqlJoinedTable(joinType, ts, false, searchCondition));

						return ts;
						break;
					}
				case SelectClause sc:
					{
						source = new SelectQuery();
						shouldAdd = true;
						break;
					}
				case BaseSetClause setClause:
					{
						var seq1Query = new SelectQuery();
						GenerateQueryInternal(seq1Query, setClause.Sequence1, null);
						var seq1QuerySource = setClause.Sequence1.GetQuerySource();

						// provide mandatory registration
						if (!_registeredTableSources.ContainsKey(seq1QuerySource))
							RegisterTableSource(seq1Query, seq1QuerySource, null);

						var seq2Query = new SelectQuery();
						GenerateQueryInternal(seq2Query, setClause.Sequence2, null);
						RegisterTableSource(seq2Query, setClause.Sequence2.GetQuerySource(), null);

						seq1Query.Unions.Add(new SqlUnion(seq2Query, !setClause.AllFieldsRequired));

						shouldAdd = true;
						source = seq1Query;
						break;
					}
				default:
					throw new NotImplementedException($"Can not create TableSource for '{querySource.GetType().Name}'");
			}

			return new SqlTableSource(source, querySource.ItemName);
		}

		private void RegisterTableSource(SelectQuery selectQuery, IQuerySource querySource, SqlTableSource tableSource)
		{
			_registeredTableSources.Add(querySource, new QuerySourceRegistry(querySource, selectQuery, tableSource, null, MappingSchema));
		}

		class QuerySourceRegistry
		{
			public QuerySourceRegistry(IQuerySource querySource, SelectQuery selectQuery, SqlTableSource tableSource, Func<QuerySourceRegistry, MemberInfo, ISqlExpression> sqlConverter, MappingSchema mappingSchema)
			{
				QuerySource = querySource;
				SelectQuery = selectQuery;
				TableSource = tableSource;
				MappingSchema = mappingSchema;
				SqlConverter = sqlConverter;
			}

			public IQuerySource QuerySource { get; }
			public SelectQuery SelectQuery { get; }
			public SqlTableSource TableSource { get; }
			public MappingSchema MappingSchema { get; }
			public Func<QuerySourceRegistry, MemberInfo, ISqlExpression> SqlConverter { get; }
		}

		void GenerateQueryInternal(SelectQuery selectQuery, Sequence sequence, List<IStreamedData> dataStream)
		{
			SqlTableSource current = null;
			bool isGrouping = false;
			foreach (var clause in sequence.Clauses)
			{
				if (clause is IQuerySource qs && !(clause is SelectClause))
				{
					current = GenerateTableSource(selectQuery, current, qs, out var shouldAdd);
					RegisterTableSource(selectQuery, qs, current);
					if (shouldAdd)
						selectQuery.From.Tables.Add(current);
				}

				switch (clause)
				{
					case Sequence seq:
						{
							var select = new SelectQuery();
							GenerateQueryInternal(select, seq, dataStream);

							current = new SqlTableSource(select, "");
							selectQuery.From.Tables.Add(current);
							break;
						}
					case SelectClause selectClause:
						{
							RegisterSelectorTransformation(selectQuery, current, selectClause, selectClause.Selector);
							break;
						}
					case WhereClause where:
						{
							var conditions = isGrouping
								? selectQuery.Having.SearchCondition.Conditions
								: selectQuery.Where.SearchCondition.Conditions;
							BuildSearchCondition(selectQuery, CorrectEquality(where.SearchExpression), conditions);
							break;
						}
					case BaseSetClause setClause:
						{
							var selector = CombineSetClause(setClause, selectQuery, current);
							RegisterSelectorTransformation(selectQuery, current, setClause, selector);
							if (setClause.AllFieldsRequired)
							{
								// simulate generation
//								GenerateProjection(selector, (e, sql) => e);
							}

							break;
						}
				}
			}

		}

		class MemberTransformationInfo
		{
			public MemberTransformationInfo(SelectQuery selectQuery, SqlTableSource tableSource, Expression transformation)
			{
				SelectQuery = selectQuery;
				TableSource = tableSource;
				Transformation = transformation;
			}

			public override string ToString()
			{
				return $" -> {Transformation}";
			}

			public SqlTableSource TableSource { get; }
			public SelectQuery SelectQuery { get; }
			public Expression Transformation { get; }
		}

		private void RegisterSelectorTransformation(SelectQuery selectQuery, SqlTableSource current, IQuerySource querySource,
			Expression selector)
		{
			RegisterSelectorTransformation(selectQuery, current, querySource, selector, _memberTransformations);
		}

		private void RegisterSelectorTransformation(SelectQuery selectQuery, SqlTableSource current, IQuerySource querySource,
			Expression selector, Dictionary<MemberExpression, MemberTransformationInfo> memberTransformations)
		{
			void RegisterLevel(Expression objExpression, Expression argument)
			{
				foreach (var mapping in GeneratorHelper.GetMemberMapping(argument, MappingSchema))
				{
					var ma = Expression.MakeMemberAccess(objExpression, mapping.Item1);
					memberTransformations.Add(ma, new MemberTransformationInfo(selectQuery, current, RemoveNullPropagation(mapping.Item2)));
					RegisterLevel(ma, mapping.Item2);
				}
			}

			var refExpression = Translator.GetSourceReference(querySource);
			RegisterLevel(refExpression, selector);
		}

		private MemberTransformationInfo GetMemberTransformation(MemberExpression memberExpression)
		{
			if (!_memberTransformations.TryGetValue(memberExpression, out var result))
			{
			};
			return result;
		}

		public QueryGenerator(ModelTranslator translator, [JetBrains.Annotations.NotNull] IDataContext dataContext)
		{
			Translator = translator;
			DataContext = dataContext ?? throw new ArgumentNullException(nameof(dataContext));
		}

		public Tuple<SqlStatement, Expression> GenerateStatement(Sequence sequence)
		{
			var selectQuery = new SelectQuery();
			GenerateQueryInternal(selectQuery, sequence, new List<IStreamedData>());
			var projection = GenerateProjection(sequence, (e, sqlExpr) =>
			{
				var idx = selectQuery.Select.Add(sqlExpr);
				return new ConvertFromDataReaderExpression(e.Type, idx, null, null);
			});
			return Tuple.Create((SqlStatement)new SqlSelectStatement(selectQuery), projection);
		}

		private Expression CorrectEquality(Expression expression)
		{
			var result = expression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Equal:
						{
							var binary = (BinaryExpression)e;
							if (binary.Left.Type.IsClassEx())
							{
								if (binary.Left.NodeType == ExpressionType.New)
								{
									var membersLeft  = GeneratorHelper.GetMemberMapping(binary.Left, MappingSchema);
									var membersRight = GeneratorHelper.GetMemberMapping(binary.Right, MappingSchema);

									var newComparison = membersLeft
										.Join(membersRight, l => l.Item1.Name, r => r.Item1.Name,
											(l, r) => Expression.Equal(CorrectEquality(l.Item2), CorrectEquality(r.Item2)))
										.Aggregate((i1, i2) => Expression.AndAlso(i1, i2));

									e = CorrectEquality(newComparison);
								}
								else
								{
									//TODO: add methods for IQuerySource to return needed projections
									throw new NotImplementedException();
								}
							}
							else
							{
								var leftSql = ConvertToSql(null, binary.Left);
								if (leftSql.CanBeNull)
								{
									var rightSql = ConvertToSql(null, binary.Right);
									if (rightSql.CanBeNull)
									{
										var newExpression =  Expression.OrElse(binary, Expression.AndAlso(
											Expression.Equal(CorrectEquality(binary.Left), new DefaultValueExpression(MappingSchema, binary.Left.Type)),
											Expression.Equal(CorrectEquality(binary.Right), new DefaultValueExpression(MappingSchema, binary.Right.Type))));

										e = newExpression;
									}
								}
							}

							break;
						}
				}

				return e;
			});

			return result;
		}

		private Expression GenerateProjection(Sequence sequence, Func<Expression, ISqlExpression, Expression> registerExpression)
		{
			Expression result = null;

			for (int i = sequence.Clauses.Count - 1; i >= 0; i--)
			{
				var clause = sequence.Clauses[i];
				if (clause is SelectClause selectClause)
				{
					result = GenerateProjection(selectClause.Selector, registerExpression); 
					break;
				}
				else if (clause is UnionClause union)
				{
					if (!_setSelectors.TryGetValue(union, out var registration))
						throw new LinqToDBException("Invalid registration for Set clause");
					result = GenerateProjection(registration.Selector.Reduce(), registerExpression);

					break;
				}
				else if (clause is TableSource table)
				{
					var reference = Translator.GetSourceReference(table);
					var ed = MappingSchema.GetEntityDescriptor(table.ItemType);
					//TODO:
					var ctor = table.ItemType.GetDefaultConstructorEx();
					var newExpression = Expression.New(ctor);
					var memberInit = Expression.MemberInit(newExpression,
						ed.Columns.Select(c => Expression.Bind(c.MemberInfo, Expression.MakeMemberAccess(reference, c.MemberInfo))));
					result = GenerateProjection(memberInit, registerExpression);
					break;
				}
			}

			if (result == null)
				throw new LinqToDBException("Sequence does not have SelectClause");

			return result;
		}

		private Expression GenerateSelector(Expression selector, Expression obj)
		{
			switch (selector.NodeType)
			{
				case ExpressionType.New:
					{
						var newExpression = (NewExpression)selector;

						var arguments = new List<Tuple<MemberInfo, Expression>>();
						for (var index = 0; index < newExpression.Arguments.Count; index++)
						{
							var argument = newExpression.Arguments[index];
							var member   = newExpression.Members[index];
							var ma       = Expression.MakeMemberAccess(obj, member);
							arguments.Add(Tuple.Create(member, GenerateSelector(argument, ma)));
						}

						return new UnifiedNewExpression(newExpression.Constructor, arguments);
					}

				case ExpressionType.MemberInit:
					{
						var memberInit = (MemberInitExpression)selector;
						var arguments = new List<Tuple<MemberInfo, Expression>>();

						foreach (var binding in memberInit.Bindings)
						{
							if (binding.BindingType != MemberBindingType.Assignment)
							{
								throw new NotImplementedException("Currently only Assignment binding is supported");
							}

							var assignment = (MemberAssignment)binding;

							var ma = Expression.MakeMemberAccess(obj, assignment.Member);
							arguments.Add(Tuple.Create(assignment.Member, ma.Type.IsClassEx() ? GenerateSelector(assignment.Expression, ma) : ma));
						}

						return new UnifiedNewExpression(memberInit.NewExpression.Constructor, arguments);
					}
				default: return obj;
			}
		}

		private Expression CombineSetClause(BaseSetClause setClause, SelectQuery selectQuery, SqlTableSource tableSource)
		{
			var setQuerySource = new SetQuerySource(setClause);
			var reference = Translator.GetSourceReference(setQuerySource);

			var projection1 = GenerateProjection(setClause.Sequence1, null);
			var projection2 = GenerateProjection(setClause.Sequence2, null);

			var querySource1 = setClause.Sequence1.GetQuerySource();
			var querySource2 = setClause.Sequence2.GetQuerySource();

			var sequence1Registration = GetTableSourceRegistry(querySource1);
			var sequence2Registration = GetTableSourceRegistry(querySource2);

			var s1 = GenerateSelector(projection1, reference);
			var s2 = GenerateSelector(projection2, reference);

			var setRegistration = new SetRegistration();

			RegisterSelectorTransformation(sequence1Registration.SelectQuery, sequence1Registration.TableSource, setQuerySource, projection1, setRegistration.Sequence1Transformations);
			RegisterSelectorTransformation(sequence2Registration.SelectQuery, sequence2Registration.TableSource, setQuerySource, projection2, setRegistration.Sequence2Transformations);

			var combineSelector = CombineSelector((UnifiedNewExpression)s1, (UnifiedNewExpression)s2);
			setRegistration.Selector = combineSelector.Reduce();
			_setSelectors.Add(setClause, setRegistration);

			RegisterTableSource(selectQuery, setQuerySource, tableSource);

			RegisterSelectorTransformation(selectQuery, current, setClause, selector);


			return setRegistration.Selector;
		}

		private UnifiedNewExpression CombineSelector(UnifiedNewExpression selector1, UnifiedNewExpression selector2)
		{
			var result = new UnifiedNewExpression(selector1.Constructor, Enumerable.Empty<Tuple<MemberInfo, Expression>>());

			void ProcessSelector(UnifiedNewExpression current, UnifiedNewExpression selector)
			{
				foreach (var member in selector.Members)
				{
					var found = current.Members.FirstOrDefault(m => m.Item1 == member.Item1);

					if (found == null)
					{
						var expr = member.Item2;
						if (expr is UnifiedNewExpression subNew)
							expr = new UnifiedNewExpression(subNew.Constructor, subNew.Members);
						current.AddMember(member.Item1, expr);
					}
					else
					{
						if (found.Item2 is UnifiedNewExpression subNew && member.Item2 is UnifiedNewExpression subCurrent)
							ProcessSelector(subCurrent, subNew);
					}
				}
			}
			ProcessSelector(result, selector1);
			ProcessSelector(result, selector2);

			return result;
		}

		private Expression GenerateProjection(Expression selector, Func<Expression, ISqlExpression, Expression> registerExpression)
		{
			var result = selector.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.New:
						{
							var newExpr = (NewExpression)e;
							if (newExpr.Members?.Count > 0)
							{
								var arguments = new List<Expression>();
								for (int i = 0; i < newExpr.Members.Count; i++)
								{
									var member = newExpr.Members[i];
									var argument = GenerateProjection(newExpr.Arguments[i], registerExpression);
									arguments.Add(argument);
								}

								return newExpr.Update(arguments);
							}
							break;
						}
					case ExpressionType.MemberInit:
						{
							var memberInit = (MemberInitExpression)e;
							var newExpression = (NewExpression)GenerateProjection(memberInit.NewExpression, registerExpression);
							var bindings = memberInit.Bindings.Where(b => b.BindingType == MemberBindingType.Assignment)
								.Select(b => Expression.Bind(b.Member, GenerateProjection(((MemberAssignment)b).Expression,
									registerExpression))).ToArray();
							return memberInit.Update(newExpression, bindings);
						}
					default:
						{
							// that means that we have to convert to reader
							if (registerExpression != null && !CanBeCompiled(e))
							{
								var sqlExpr = ConvertToSql(null, e);
								e = registerExpression(e, sqlExpr);
							}

							break;
						}
				}

				return e;
			});

			return result;
		}

		#region Public Members

		public readonly Expression            OriginalExpression;
		public readonly Expression            Expression;
		public readonly ParameterExpression[] CompiledParameters;
		private readonly List<IBuildContext>   Contexts = new List<IBuildContext>();

		public static readonly ParameterExpression QueryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public        readonly ParameterExpression DataReaderLocal;
		public static readonly ParameterExpression ParametersParam  = Expression.Parameter(typeof(object[]),     "ps");
		public static readonly ParameterExpression ExpressionParam  = Expression.Parameter(typeof(Expression),   "expr");

		#endregion

		readonly Query                             _query;
		private  bool                              _reorder;
		readonly Dictionary<Expression,Expression> _expressionAccessors;
		private  HashSet<Expression>               _subQueryExpressions;

		private readonly List<ParameterAccessor>   CurrentSqlParameters = new List<ParameterAccessor>();

		public readonly List<ParameterExpression>  BlockVariables       = new List<ParameterExpression>();
		public readonly List<Expression>           BlockExpressions     = new List<Expression>();
		public          bool                       IsBlockDisable;
		public          int                        VarIndex;

		readonly HashSet<Expression> _visitedExpressions;

		Sql.ExpressionAttribute GetExpressionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.ExpressionAttribute>(member.ReflectedTypeEx(), member, a => a.Configuration);
		}

		internal Sql.TableFunctionAttribute GetTableFunctionAttribute(MemberInfo member)
		{
			return MappingSchema.GetAttribute<Sql.TableFunctionAttribute>(member.ReflectedTypeEx(), member, a => a.Configuration);
		}

		public ISqlExpression Convert(SelectQuery selectQuery, ISqlExpression expr)
		{
			return DataContext.GetSqlOptimizer().ConvertExpression(expr);
		}

		public ISqlPredicate Convert(SelectQuery selectQuery, ISqlPredicate predicate)
		{
			return DataContext.GetSqlOptimizer().ConvertPredicate(selectQuery, predicate);
		}


		#region Search Condition Builder

		internal void BuildSearchCondition(SelectQuery context, Expression expression, List<SqlCondition> conditions)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.And     :
				case ExpressionType.AndAlso :
					{
						var e = (BinaryExpression)expression;

						BuildSearchCondition(context, e.Left,  conditions);
						BuildSearchCondition(context, e.Right, conditions);

						break;
					}

				case ExpressionType.Extension :
					{
						break;
					}

				case ExpressionType.Or     :
				case ExpressionType.OrElse :
					{
						var e           = (BinaryExpression)expression;
						var orCondition = new SqlSearchCondition();

						BuildSearchCondition(context, e.Left,  orCondition.Conditions);
						orCondition.Conditions[orCondition.Conditions.Count - 1].IsOr = true;
						BuildSearchCondition(context, e.Right, orCondition.Conditions);

						conditions.Add(new SqlCondition(false, orCondition));

						break;
					}

				default                    :
					var predicate = ConvertPredicate(context, expression);

					if (predicate is SqlPredicate.Expr ex)
					{
						var expr = ex.Expr1;

						if (expr.ElementType == QueryElementType.SearchCondition)
						{
							var sc = (SqlSearchCondition)expr;

							if (sc.Conditions.Count == 1)
							{
								conditions.Add(sc.Conditions[0]);
								break;
							}
						}
					}

					conditions.Add(new SqlCondition(false, predicate));

					break;
			}
		}

		static SqlCondition CheckIsNull(ISqlPredicate predicate, bool isNot, bool isNotExpression)
		{
			if (Common.Configuration.Linq.CompareNullsAsValues == false)
				return null;

			var inList = predicate as SqlPredicate.InList;

			// ili this will fail https://github.com/linq2db/linq2db/issues/909
			//
			//if (predicate is SelectQuery.SearchCondition)
			//{
			//	var sc = (SelectQuery.SearchCondition) predicate;

			//	inList = QueryVisitor
			//		.Find(sc, _ => _.ElementType == QueryElementType.InListPredicate) as SelectQuery.Predicate.InList;

			//	if (inList != null)
			//	{
			//		isNot = QueryVisitor.Find(sc, _ =>
			//		{
			//			var condition = _ as SelectQuery.Condition;
			//			return condition != null && condition.IsNot;
			//		}) != null;
			//	}
			//}

			if (predicate.CanBeNull && predicate is SqlPredicate.ExprExpr || inList != null)
			{
				var exprExpr = predicate as SqlPredicate.ExprExpr;

				if (exprExpr != null &&
					(
						exprExpr.Operator == SqlPredicate.Operator.NotEqual /*&& isNot == false*/ ||
						exprExpr.Operator == SqlPredicate.Operator.Equal    /*&& isNot == true */
					) ||
					inList != null && inList.IsNot || isNot)
				{
					var expr1 = exprExpr != null ? exprExpr.Expr1 : inList.Expr1;
					var expr2 = exprExpr?.Expr2;

					var nullValue1 =                 QueryVisitor.Find(expr1, _ => _ is IValueContainer);
					var nullValue2 = expr2 != null ? QueryVisitor.Find(expr2, _ => _ is IValueContainer) : null;

					var hasNullValue =
						   nullValue1 != null && ((IValueContainer) nullValue1).Value == null
						|| nullValue2 != null && ((IValueContainer) nullValue2).Value == null;

					if (!hasNullValue)
					{
						var expr1IsField =                  expr1.CanBeNull && QueryVisitor.Find(expr1, _ => _.ElementType == QueryElementType.SqlField) != null;
						var expr2IsField = expr2 != null && expr2.CanBeNull && QueryVisitor.Find(expr2, _ => _.ElementType == QueryElementType.SqlField) != null;

						var nullableField = expr1IsField
							? expr1
							: expr2IsField ? expr2 : null;

						if (nullableField != null)
						{
							var checkNullPredicate = new SqlPredicate.IsNull(nullableField, exprExpr != null && exprExpr.Operator == SqlPredicate.Operator.Equal);

							var predicateIsNot = isNot && inList == null;
							predicate = BasicSqlOptimizer.OptimizePredicate(predicate, ref predicateIsNot);

							if (predicate is SqlPredicate.ExprExpr ee &&
								(!ee.Expr1.CanBeNull || !ee.Expr2.CanBeNull) &&
								(
									ee.Operator != SqlPredicate.Operator.NotEqual && !isNot && !isNotExpression ||
									ee.Operator == SqlPredicate.Operator.NotEqual &&  isNot &&  isNotExpression
								))
							{
								return null;
							}

							var orCondition = new SqlSearchCondition(
								new SqlCondition(false,          checkNullPredicate),
								new SqlCondition(predicateIsNot, predicate));

							orCondition.Conditions[0].IsOr = exprExpr == null || exprExpr.Operator == SqlPredicate.Operator.NotEqual;

							var ret = new SqlCondition(false, orCondition);

							return ret;
						}
					}
				}
			}

			return null;
		}

		#endregion

		#region Predicate Converter

		ISqlPredicate ConvertPredicate(SelectQuery context, Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Equal              :
				case ExpressionType.NotEqual           :
				case ExpressionType.GreaterThan        :
				case ExpressionType.GreaterThanOrEqual :
				case ExpressionType.LessThan           :
				case ExpressionType.LessThanOrEqual    :
					{
						var e = (BinaryExpression)expression;
						return ConvertCompare(context, expression.NodeType, e.Left, e.Right);
					}

				case ExpressionType.Call               :
					{
						var e = (MethodCallExpression)expression;

						ISqlPredicate predicate = null;

						if (e.Method.Name == "Equals" && e.Object != null && e.Arguments.Count == 1)
							return ConvertCompare(context, ExpressionType.Equal, e.Object, e.Arguments[0]);

						if (e.Method.DeclaringType == typeof(string))
						{
							switch (e.Method.Name)
							{
								case "Contains"   : predicate = ConvertLikePredicate(context, e, "%", "%"); break;
								case "StartsWith" : predicate = ConvertLikePredicate(context, e, "",  "%"); break;
								case "EndsWith"   : predicate = ConvertLikePredicate(context, e, "%", "");  break;
							}
						}
						else if (e.Method.Name == "Contains")
						{
							if (e.Method.DeclaringType == typeof(Enumerable) ||
								typeof(IList).        IsSameOrParentOf(e.Method.DeclaringType) ||
								typeof(ICollection<>).IsSameOrParentOf(e.Method.DeclaringType))
							{
								predicate = ConvertInPredicate(context, e);
							}
						}
						else if (e.Method.Name == "ContainsValue" && typeof(Dictionary<,>).IsSameOrParentOf(e.Method.DeclaringType))
						{
							var args = e.Method.DeclaringType.GetGenericArguments(typeof(Dictionary<,>));
							var minf = EnumerableMethods
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[1]);

							var expr = Expression.Call(
								minf,
								Expression.PropertyOrField(e.Object, "Values"),
								e.Arguments[0]);

							predicate = ConvertInPredicate(context, expr);
						}
						else if (e.Method.Name == "ContainsKey" && typeof(IDictionary<,>).IsSameOrParentOf(e.Method.DeclaringType))
						{
							var args = e.Method.DeclaringType.GetGenericArguments(typeof(IDictionary<,>));
							var minf = EnumerableMethods
								.First(m => m.Name == "Contains" && m.GetParameters().Length == 2)
								.MakeGenericMethod(args[0]);

							var expr = Expression.Call(
								minf,
								Expression.PropertyOrField(e.Object, "Keys"),
								e.Arguments[0]);

							predicate = ConvertInPredicate(context, expr);
						}
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
						else if (e.Method == ReflectionHelper.Functions.String.Like11) predicate = ConvertLikePredicate(context, e);
						else if (e.Method == ReflectionHelper.Functions.String.Like12) predicate = ConvertLikePredicate(context, e);
#endif
						else if (e.Method == ReflectionHelper.Functions.String.Like21) predicate = ConvertLikePredicate(context, e);
						else if (e.Method == ReflectionHelper.Functions.String.Like22) predicate = ConvertLikePredicate(context, e);

						if (predicate != null)
							return Convert(context, predicate);

						var attr = GetExpressionAttribute(e.Method);

						if (attr != null && attr.IsPredicate)
							break;

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ExpressionType.Conditional  :
					return Convert(context,
						new SqlPredicate.ExprExpr(
							ConvertToSql(context, expression),
							SqlPredicate.Operator.Equal,
							new SqlValue(true)));

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expression;

						if (e.Member.Name == "HasValue" &&
							e.Member.DeclaringType.IsGenericTypeEx() &&
							e.Member.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>))
						{
							var expr = ConvertToSql(context, e.Expression);
							return Convert(context, new SqlPredicate.IsNull(expr, true));
						}

						var attr = GetExpressionAttribute(e.Member);

						if (attr != null && attr.IsPredicate)
							break;

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ExpressionType.TypeIs:
					{
//						var e   = (TypeBinaryExpression)expression;
//						var ctx = GetContext(context, e.Expression);
//
//						if (ctx != null && ctx.IsExpression(e.Expression, 0, RequestFor.Table).Result)
//							return MakeIsPredicate(ctx, e);

						throw new NotImplementedException();

						break;
					}

				case ExpressionType.Convert:
					{
						var e = (UnaryExpression)expression;

						if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
							return ConvertPredicate(context, e.Operand);

						return ConvertPredicate(context, AddEqualTrue(expression));
					}

				case ChangeTypeExpression.ChangeTypeType:
					return ConvertPredicate(context, AddEqualTrue(expression));
			}

			var ex = ConvertToSql(context, expression);

			if (SqlExpression.NeedsEqual(ex))
				return Convert(context, new SqlPredicate.ExprExpr(ex, SqlPredicate.Operator.Equal, new SqlValue(true)));

			return Convert(context, new SqlPredicate.Expr(ex));
		}

		Expression AddEqualTrue(Expression expr)
		{
			return Equal(MappingSchema, Expression.Constant(true), expr);
		}

		#region ConvertCompare

		ISqlPredicate ConvertCompare(SelectQuery query, ExpressionType nodeType, Expression left, Expression right)
		{
			if (left.NodeType == ExpressionType.Convert
				&& left.Type == typeof(int)
				&& (right.NodeType == ExpressionType.Constant || right.NodeType == ExpressionType.Convert))
			{
				var conv  = (UnaryExpression)left;

				if (conv.Operand.Type == typeof(char))
				{
					left  = conv.Operand;
					right = right.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression) right).Value))
						: ((UnaryExpression) right).Operand;
				}
			}

			if (left.NodeType == ExpressionType.Convert
				&& left.Type == typeof(int?)
				&& (right.NodeType == ExpressionType.Constant
					|| (right.NodeType == ExpressionType.Convert
						&& ((UnaryExpression)right).Operand.NodeType == ExpressionType.Convert)))
			{
				var conv = (UnaryExpression)left;

				if (conv.Operand.Type == typeof(char?))
				{
					left = conv.Operand;
					right = right.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)right).Value))
						: ((UnaryExpression)((UnaryExpression)right).Operand).Operand;
				}
			}

			if (right.NodeType == ExpressionType.Convert
				&& right.Type == typeof(int)
				&& (left.NodeType == ExpressionType.Constant || left.NodeType == ExpressionType.Convert))
			{
				var conv = (UnaryExpression)right;

				if (conv.Operand.Type == typeof(char))
				{
					right = conv.Operand;
					left  = left.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression) left).Value))
						: ((UnaryExpression) left).Operand;
				}
			}

			if (right.NodeType == ExpressionType.Convert
				&& right.Type == typeof(int?)
				&& (left.NodeType == ExpressionType.Constant
					|| (left.NodeType == ExpressionType.Convert
						&& ((UnaryExpression)left).Operand.NodeType == ExpressionType.Convert)))
			{
				var conv = (UnaryExpression)right;

				if (conv.Operand.Type == typeof(char?))
				{
					right = conv.Operand;
					left = left.NodeType == ExpressionType.Constant
						? Expression.Constant(ConvertTo<char>.From(((ConstantExpression)left).Value))
						: ((UnaryExpression)((UnaryExpression)left).Operand).Operand;
				}
			}

			bool IsNullValue(Expression exp)
			{
				return (exp.NodeType == ExpressionType.Constant && ((ConstantExpression)exp).Value == null) ||
				       (exp is DefaultValueExpression);
			}

			switch (nodeType)
			{
				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
					{
						if (IsNullValue(left))
						{
							var tmp = left;
							left = right;
							right = tmp;
						}

						SqlPredicate predicate;

						if (IsNullValue(left))
							predicate = new SqlPredicate.Expr(new SqlValue(nodeType == ExpressionType.Equal));
						else
						{
							if (IsNullValue(right))
								predicate = new SqlPredicate.IsNull(ConvertToSql(query, left),
									nodeType == ExpressionType.NotEqual);
							else
								predicate = new SqlPredicate.ExprExpr(
									ConvertToSql(query, left),
									nodeType == ExpressionType.Equal
										? SqlPredicate.Operator.Equal
										: SqlPredicate.Operator.NotEqual,
									ConvertToSql(query, right));

						}

						return predicate;
					}
			}

			SqlPredicate.Operator op;

			switch (nodeType)
			{
				case ExpressionType.Equal             : op = SqlPredicate.Operator.Equal;          break;
				case ExpressionType.NotEqual          : op = SqlPredicate.Operator.NotEqual;       break;
				case ExpressionType.GreaterThan       : op = SqlPredicate.Operator.Greater;        break;
				case ExpressionType.GreaterThanOrEqual: op = SqlPredicate.Operator.GreaterOrEqual; break;
				case ExpressionType.LessThan          : op = SqlPredicate.Operator.Less;           break;
				case ExpressionType.LessThanOrEqual   : op = SqlPredicate.Operator.LessOrEqual;    break;
				default: throw new InvalidOperationException();
			}

			if (left.NodeType == ExpressionType.Convert || right.NodeType == ExpressionType.Convert)
			{
				var p = ConvertEnumConversion(query, left, op, right);
				if (p != null)
					return p;
			}

			var l = ConvertToSql(query, left);
			var r = ConvertToSql(query, right, true);

			var lValue = l as SqlValue;
			var rValue = r as SqlValue;

			if (lValue != null)
				lValue.ValueType = GetDataType(r, lValue.ValueType);

			if (rValue != null)
				rValue.ValueType = GetDataType(l, rValue.ValueType);

			switch (nodeType)
			{
				case ExpressionType.Equal   :
				case ExpressionType.NotEqual:

					if (!query.IsParameterDependent &&
						(l is SqlParameter && l.CanBeNull || r is SqlParameter && r.CanBeNull))
						query.IsParameterDependent = true;

					// | (SqlQuery(Select([]) as q), SqlValue(null))
					// | (SqlValue(null), SqlQuery(Select([]) as q))  =>

					var q =
						l.ElementType == QueryElementType.SqlQuery &&
						r.ElementType == QueryElementType.SqlValue &&
						((SqlValue)r).Value == null &&
						((SelectQuery)l).Select.Columns.Count == 0 ?
							(SelectQuery)l :
						r.ElementType == QueryElementType.SqlQuery &&
						l.ElementType == QueryElementType.SqlValue &&
						((SqlValue)l).Value == null &&
						((SelectQuery)r).Select.Columns.Count == 0 ?
							(SelectQuery)r :
							null;

					q?.Select.Columns.Add(new SqlColumn(q, new SqlValue(1)));

					break;
			}

			if (l is SqlSearchCondition)
				l = Convert(query, new SqlFunction(typeof(bool), "CASE", l, new SqlValue(true), new SqlValue(false)));

			if (r is SqlSearchCondition)
				r = Convert(query, new SqlFunction(typeof(bool), "CASE", r, new SqlValue(true), new SqlValue(false)));

			return Convert(query, new SqlPredicate.ExprExpr(l, op, r));
		}

		#endregion

		#region ConvertEnumConversion

		ISqlPredicate ConvertEnumConversion(SelectQuery context, Expression left, SqlPredicate.Operator op, Expression right)
		{
			Expression value;
			Expression operand;

			if (left is MemberExpression)
			{
				operand = left;
				value   = right;
			}
			else if (left.NodeType == ExpressionType.Convert && ((UnaryExpression)left).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else if (right is MemberExpression)
			{
				operand = right;
				value   = left;
			}
			else if (right.NodeType == ExpressionType.Convert && ((UnaryExpression)right).Operand is MemberExpression)
			{
				operand = ((UnaryExpression)right).Operand;
				value   = left;
			}
			else if (left.NodeType == ExpressionType.Convert)
			{
				operand = ((UnaryExpression)left).Operand;
				value   = right;
			}
			else
			{
				operand = ((UnaryExpression)right).Operand;
				value = left;
			}

			var type = operand.Type;

			if (!type.ToNullableUnderlying().IsEnumEx())
				return null;

			var dic = new Dictionary<object, object>();

			var mapValues = MappingSchema.GetMapValues(type);

			if (mapValues != null)
				foreach (var mv in mapValues)
					if (!dic.ContainsKey(mv.OrigValue))
						dic.Add(mv.OrigValue, mv.MapValues[0].Value);

			switch (value.NodeType)
			{
				case ExpressionType.Constant:
					{
						var name = Enum.GetName(type, ((ConstantExpression)value).Value);

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (name == null)
							return null;
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						var origValue = Enum.Parse(type, name, false);

						if (!dic.TryGetValue(origValue, out var mapValue))
							mapValue = origValue;

						ISqlExpression l, r;

						SqlValue sqlvalue;
						var ce = MappingSchema.GetConverter(new DbDataType(type), new DbDataType(typeof(DataParameter)), false);

						if (ce != null)
						{
							sqlvalue = new SqlValue(ce.ConvertValueToParameter(origValue).Value);
						}
						else
						{
							sqlvalue = MappingSchema.GetSqlValue(type, mapValue);
						}

						if (left.NodeType == ExpressionType.Convert)
						{
							l = ConvertToSql(context, operand);
							r = sqlvalue;
						}
						else
						{
							r = ConvertToSql(context, operand);
							l = sqlvalue;
						}

						return Convert(context, new SqlPredicate.ExprExpr(l, op, r));
					}

				case ExpressionType.Convert:
					{
						value = ((UnaryExpression)value).Operand;

						var l = ConvertToSql(context, operand);
						var r = ConvertToSql(context, value);

						return Convert(context, new SqlPredicate.ExprExpr(l, op, r));
					}
			}

			return null;
		}

		#endregion

		#region ConvertObjectNullComparison

		ISqlPredicate ConvertObjectNullComparison(SelectQuery context, Expression left, Expression right, bool isEqual)
		{
			if (right.NodeType == ExpressionType.Constant && ((ConstantExpression)right).Value == null)
			{
				if (left.NodeType == ExpressionType.MemberAccess || left.NodeType == ExpressionType.Parameter)
				{

					if (left.Type.IsClassEx())
					{
						return new SqlPredicate.Expr(new SqlValue(!isEqual));
					}
				}
			}

			return null;

			throw new NotImplementedException();
		}

		#endregion

		#region ConvertObjectComparison

		bool IsNullConstant(Expression expr)
		{
			return expr.NodeType == ExpressionType.Constant && ((ConstantExpression)expr).Value == null;
		}

		Expression RemoveNullPropagation(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
					var conditional = (ConditionalExpression)expr;
					if (conditional.Test.NodeType == ExpressionType.NotEqual)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right))
						{
							if (IsNullConstant(conditional.IfFalse))
							{
								return conditional.IfTrue.Transform(e => RemoveNullPropagation(e));
							}
						}
					}
					else if (conditional.Test.NodeType == ExpressionType.Equal)
					{
						var binary = (BinaryExpression)conditional.Test;
						if (IsNullConstant(binary.Right))
						{
							if (IsNullConstant(conditional.IfTrue))
							{
								return conditional.IfFalse.Transform(e => RemoveNullPropagation(e));
							}
						}
					}
					break;
			}

			return expr;
		}

		public bool ProcessProjection(Dictionary<MemberInfo,Expression> members, Expression expression)
		{
			switch (expression.NodeType)
			{
				// new { ... }
				//
				case ExpressionType.New        :
					{
						var expr = (NewExpression)expression;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (expr.Members == null)
							return false;
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						for (var i = 0; i < expr.Members.Count; i++)
						{
							var member = expr.Members[i];

							var converted = expr.Arguments[i].Transform(e => RemoveNullPropagation(e));
							members.Add(member, converted);

							if (member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				// new MyObject { ... }
				//
				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo.Name, _ => _.i);

						foreach (var binding in expr.Bindings.Cast<MemberAssignment>().OrderBy(b => dic.ContainsKey(b.Member.Name) ? dic[b.Member.Name] : 1000000))
						{
							var converted = binding.Expression.Transform(e => RemoveNullPropagation(e));
							members.Add(binding.Member, converted);

							if (binding.Member is MethodInfo info)
								members.Add(info.GetPropertyInfo(), converted);
						}

						return true;
					}

				// .Select(p => everything else)
				//
				default                        :
					return false;
			}
		}


		public ISqlPredicate ConvertObjectComparison(
			ExpressionType nodeType,
			SelectQuery  leftContext,
			Expression     left,
			SelectQuery  rightContext,
			Expression     right)
		{


			//TODO
			var sl = left.Type.IsClassEx();
			var sr = left.Type.IsClassEx();

			bool      isNull;
			SqlInfo[] lcols;

			var rmembers = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

			if (sl == false && sr == false)
			{
				var lmembers = new Dictionary<MemberInfo,Expression>(new MemberInfoComparer());

				var isl = ProcessProjection(lmembers, left);
				var isr = ProcessProjection(rmembers, right);

				if (!isl && !isr)
					return null;

				if (lmembers.Count == 0)
				{
					var r = right;
					right = left;
					left  = r;

					var c = rightContext;
					rightContext = leftContext;
					leftContext  = c;

					sr = false;

					var lm = lmembers;
					lmembers = rmembers;
					rmembers = lm;
				}

				isNull = right is ConstantExpression expression && expression.Value == null;
				lcols  = lmembers.Select(m => new SqlInfo(m.Key) { Sql = ConvertToSql(leftContext, m.Value) }).ToArray();
			}
//			else
//			{
//				if (sl == false)
//				{
//					var r = right;
//					right = left;
//					left  = r;
//
//					var c = rightContext;
//					rightContext = leftContext;
//					leftContext  = c;
//
//					var q = qsr;
//					qsl = q;
//
//					sr = false;
//				}
//
//				isNull = right is ConstantExpression expression && expression.Value == null;
//				lcols  = qsl.ConvertToSql(left, 0, ConvertFlags.Key);
//
//				if (!sr)
//					ProcessProjection(rmembers, right);
//			}
//
//			if (lcols.Length == 0)
//				return null;
//
//			var condition = new SqlSearchCondition();
//
//			foreach (var lcol in lcols)
//			{
//				if (lcol.MemberChain.Count == 0)
//					throw new InvalidOperationException();
//
//				ISqlExpression rcol = null;
//
//				var lmember = lcol.MemberChain[lcol.MemberChain.Count - 1];
//
//				if (sr)
//					rcol = ConvertToSql(rightContext, Expression.MakeMemberAccess(right, lmember));
//				else if (rmembers.Count != 0)
//					rcol = ConvertToSql(rightContext, rmembers[lmember]);
//
//				var rex =
//					isNull ?
//						MappingSchema.GetSqlValue(right.Type, null) :
//						rcol ?? GetParameter(right, lmember);
//
//				var predicate = Convert(leftContext, new SqlPredicate.ExprExpr(
//					lcol.Sql,
//					nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
//					rex));
//
//				condition.Conditions.Add(new SqlCondition(false, predicate));
//			}
//
//			if (nodeType == ExpressionType.NotEqual)
//				foreach (var c in condition.Conditions)
//					c.IsOr = true;
//
//			return condition;

			throw new NotImplementedException();
		}

		internal ISqlPredicate ConvertNewObjectComparison(SelectQuery context, ExpressionType nodeType, Expression left, Expression right)
		{
//			left  = FindExpression(left);
//			right = FindExpression(right);
//
//			var condition = new SqlSearchCondition();
//
//			if (left.NodeType != ExpressionType.New)
//			{
//				var temp = left;
//				left  = right;
//				right = temp;
//			}
//
//			var newRight = right as NewExpression;
//			var newExpr  = (NewExpression)left;
//
//// ReSharper disable ConditionIsAlwaysTrueOrFalse
//// ReSharper disable HeuristicUnreachableCode
//			if (newExpr.Members == null)
//				return null;
//// ReSharper restore HeuristicUnreachableCode
//// ReSharper restore ConditionIsAlwaysTrueOrFalse
//
//			for (var i = 0; i < newExpr.Arguments.Count; i++)
//			{
//				var lex = ConvertToSql(context, newExpr.Arguments[i]);
//				var rex =
//					newRight != null ?
//						ConvertToSql(context, newRight.Arguments[i]) :
//						GetParameter(right, newExpr.Members[i]);
//
//				var predicate = Convert(context,
//					new SqlPredicate.ExprExpr(
//						lex,
//						nodeType == ExpressionType.Equal ? SqlPredicate.Operator.Equal : SqlPredicate.Operator.NotEqual,
//						rex));
//
//				condition.Conditions.Add(new SqlCondition(false, predicate));
//			}
//
//			if (nodeType == ExpressionType.NotEqual)
//				foreach (var c in condition.Conditions)
//					c.IsOr = true;
//
//			return condition;

			throw new NotImplementedException();
		}

		ISqlExpression GetParameter(Expression ex, MemberInfo member)
		{
//			if (member is MethodInfo)
//				member = ((MethodInfo)member).GetPropertyInfo();
//
//			var vte  = ReplaceParameter(_expressionAccessors, ex, _ => { });
//			var par  = vte.ValueExpression;
//			var expr = Expression.MakeMemberAccess(par.Type == typeof(object) ? Expression.Convert(par, member.DeclaringType) : par, member);
//			var p    = CreateParameterAccessor(DataContext, expr, vte.DataTypeExpression, vte.DbTypeExpression, expr, ExpressionParam, ParametersParam, member.Name);
//
//			_parameters.Add(expr, p);
//			CurrentSqlParameters.Add(p);
//
//			return p.SqlParameter;

			throw new NotImplementedException();
		}

		DbDataType GetMemberDataType(MemberInfo member)
		{
			var typeResult = new DbDataType(member.GetMemberType());

			var dta      = MappingSchema.GetAttribute<DataTypeAttribute>(member.ReflectedTypeEx(), member);
			var ca       = MappingSchema.GetAttribute<ColumnAttribute>  (member.ReflectedTypeEx(), member);

			var dataType = ca?.DataType ?? dta?.DataType;

			if (dataType != null)
				typeResult = typeResult.WithDataType(dataType.Value);

			var dbType = ca?.DbType ?? dta?.DbType;
			if (dbType != null)
				typeResult = typeResult.WithDbType(dbType);

			return typeResult;
		}

		static DbDataType GetDataType(ISqlExpression expr, DbDataType baseType)
		{
			var systemType = baseType.SystemType;
			var dataType   = baseType.DataType;
			string dbType  = baseType.DbType;

			QueryVisitor.Find(expr, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlField:
						dataType   = ((SqlField)e).DataType;
						dbType     = ((SqlField)e).DbType;
						//systemType = ((SqlField)e).SystemType;
						return true;
					case QueryElementType.SqlParameter:
						dataType   = ((SqlParameter)e).DataType;
						dbType     = ((SqlParameter)e).DbType;
						//systemType = ((SqlParameter)e).SystemType;
						return true;
					case QueryElementType.SqlDataType:
						dataType   = ((SqlDataType)e).DataType;
						dbType     = ((SqlDataType)e).DbType;
						//systemType = ((SqlDataType)e).SystemType;
						return true;
					case QueryElementType.SqlValue:
						dataType   = ((SqlValue)e).ValueType.DataType;
						dbType     = ((SqlValue)e).ValueType.DbType;
						//systemType = ((SqlValue)e).ValueType.SystemType;
						return true;
					default:
						return false;
				}
			});

			return new DbDataType(
				systemType ?? baseType.SystemType,
				dataType == DataType.Undefined ? baseType.DataType : dataType,
				string.IsNullOrEmpty(dbType) ? baseType.DbType : dbType
			);
		}

		internal static ParameterAccessor CreateParameterAccessor(
			IDataContext        dataContext,
			Expression          accessorExpression,
			Expression          dataTypeAccessorExpression,
			Expression          dbTypeAccessorExpression,
			Expression          expression,
			ParameterExpression expressionParam,
			ParameterExpression parametersParam,
			string              name,
			ExpressionBuilder.BuildParameterType  buildParameterType = ExpressionBuilder.BuildParameterType.Default,
			LambdaExpression    expr = null)
		{
			var type = accessorExpression.Type;

			if (buildParameterType != ExpressionBuilder.BuildParameterType.InPredicate)
				expr = expr ?? dataContext.MappingSchema.GetConvertExpression(type, typeof(DataParameter), createDefault: false);
			else
				expr = null;

			if (expr != null)
			{
				if (accessorExpression == null || dataTypeAccessorExpression == null || dbTypeAccessorExpression == null)
				{
					var body = expr.GetBody(accessorExpression);

					accessorExpression         = Expression.PropertyOrField(body, "Value");
					dataTypeAccessorExpression = Expression.PropertyOrField(body, "DataType");
					dbTypeAccessorExpression   = Expression.PropertyOrField(body, "DbType");
				}
			}
			else
			{
				if (type == typeof(DataParameter))
				{
					var dp = expression.EvaluateExpression() as DataParameter;
					if (dp?.Name?.IsNullOrEmpty() == false)
						name = dp.Name;

					dataTypeAccessorExpression = Expression.PropertyOrField(accessorExpression, "DataType");
					dbTypeAccessorExpression   = Expression.PropertyOrField(accessorExpression, "DbType");
					accessorExpression         = Expression.PropertyOrField(accessorExpression, "Value");
				}
				else
				{
					var defaultType = Converter.GetDefaultMappingFromEnumType(dataContext.MappingSchema, type);

					if (defaultType != null)
					{
						var enumMapExpr = dataContext.MappingSchema.GetConvertExpression(type, defaultType);
						accessorExpression = enumMapExpr.GetBody(accessorExpression);
					}
				}
			}

			// see #820
			accessorExpression = accessorExpression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression, Expression.Constant(null, ma.Expression.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					case ExpressionType.Convert:
						var ce = (UnaryExpression) e;
						if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
						{
							return Expression.Condition(
								Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
								Expression.Default(e.Type),
								e);
						}
						return e;
					default:
						return e;
				}
			});

			var mapper = Expression.Lambda<Func<Expression,object[],object>>(
				Expression.Convert(accessorExpression, typeof(object)),
				new [] { expressionParam, parametersParam });

			var dataTypeAccessor = Expression.Lambda<Func<Expression,object[],DataType>>(
				Expression.Convert(dataTypeAccessorExpression, typeof(DataType)),
				new [] { expressionParam, parametersParam });

			var dbTypeAccessor = Expression.Lambda<Func<Expression,object[],string>>(
				Expression.Convert(dbTypeAccessorExpression, typeof(string)),
				new [] { expressionParam, parametersParam });

			return new ParameterAccessor
			(
				expression,
				mapper.Compile(),
				dataTypeAccessor.Compile(),
				dbTypeAccessor.Compile(),
				new SqlParameter(accessorExpression.Type, name, null) { IsQueryParameter = !(dataContext.InlineParameters && accessorExpression.Type.IsScalar(false)) }
			);
		}

		static Expression FindExpression(Expression expr)
		{
			var ret = expr.Find(pi =>
			{
				switch (pi.NodeType)
				{
					case ExpressionType.Convert      :
						{
							var e = (UnaryExpression)expr;

							return
								e.Operand.NodeType == ExpressionType.ArrayIndex &&
								ReferenceEquals(((BinaryExpression)e.Operand).Left, ParametersParam);
						}

					case ExpressionType.MemberAccess :
					case ExpressionType.New          :
						return true;
				}

				return false;
			});

			if (ret == null)
				throw new NotImplementedException();

			return ret;
		}

		#endregion

		#region ConvertInPredicate

		private ISqlPredicate ConvertInPredicate(SelectQuery context, MethodCallExpression expression)
		{
//			var e        = expression;
//			var argIndex = e.Object != null ? 0 : 1;
//			var arr      = e.Object ?? e.Arguments[0];
//			var arg      = e.Arguments[argIndex];
//
//			ISqlExpression expr = null;
//
//			var ctx = GetContext(context, arg);
//
//			if (ctx is TableBuilder.TableContext &&
//				ctx.SelectQuery != context.SelectQuery &&
//				ctx.IsExpression(arg, 0, RequestFor.Object).Result)
//			{
//				expr = ctx.SelectQuery;
//			}
//
//			if (expr == null)
//			{
//				var sql = ConvertExpressions(context, arg, ConvertFlags.Key);
//
//				if (sql.Length == 1 && sql[0].MemberChain.Count == 0)
//					expr = sql[0].Sql;
//				else
//					expr = new ObjectSqlExpression(MappingSchema, sql);
//			}
//
//			switch (arr.NodeType)
//			{
//				case ExpressionType.NewArrayInit :
//					{
//						var newArr = (NewArrayExpression)arr;
//
//						if (newArr.Expressions.Count == 0)
//							return new SqlPredicate.Expr(new SqlValue(false));
//
//						var exprs  = new ISqlExpression[newArr.Expressions.Count];
//
//						for (var i = 0; i < newArr.Expressions.Count; i++)
//							exprs[i] = ConvertToSql(context, newArr.Expressions[i]);
//
//						return new SqlPredicate.InList(expr, false, exprs);
//					}
//
//				default :
//
//					if (CanBeCompiled(arr))
//					{
//						var p = BuildParameter(arr, ExpressionBuilder.BuildParameterType.InPredicate).SqlParameter;
//						p.IsQueryParameter = false;
//						return new SqlPredicate.InList(expr, false, p);
//					}
//
//					break;
//			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		#endregion

		#region LIKE predicate

		ISqlPredicate ConvertLikePredicate(SelectQuery context, MethodCallExpression expression, string start, string end)
		{
//			var e = expression;
//			var o = ConvertToSql(context, e.Object);
//			var a = ConvertToSql(context, e.Arguments[0]);
//
//			if (a is SqlValue sqlValue)
//			{
//				var value = sqlValue.Value;
//
//				if (value == null)
//					throw new LinqException("NULL cannot be used as a LIKE predicate parameter.");
//
//				return value.ToString().IndexOfAny(new[] { '%', '_' }) < 0?
//					new SqlPredicate.Like(o, false, new SqlValue(start + value + end), null):
//					new SqlPredicate.Like(o, false, new SqlValue(start + EscapeLikeText(value.ToString()) + end), new SqlValue('~'));
//			}
//
//			if (a is SqlParameter p)
//			{
//				var ep = (from pm in CurrentSqlParameters where pm.SqlParameter == p select pm).First();
//
//				ep = new ParameterAccessor
//				(
//					ep.Expression,
//					ep.Accessor,
//					ep.DataTypeAccessor,
//					ep.DbTypeAccessor,
//					new SqlParameter(ep.Expression.Type, p.Name, p.Value)
//					{
//						LikeStart        = start,
//						LikeEnd          = end,
//						ReplaceLike      = p.ReplaceLike,
//						IsQueryParameter = !(DataContext.InlineParameters && ep.Expression.Type.IsScalar(false)),
//						DbType           = p.DbType
//					}
//				);
//
//				CurrentSqlParameters.Add(ep);
//
//				return new SqlPredicate.Like(o, false, ep.SqlParameter, new SqlValue('~'));
//			}
//
//			var mi = MemberHelper.MethodOf(() => "".Replace("", ""));
//			var ex =
//				Expression.Call(
//				Expression.Call(
//				Expression.Call(
//					e.Arguments[0],
//						mi, Expression.Constant("~"), Expression.Constant("~~")),
//						mi, Expression.Constant("%"), Expression.Constant("~%")),
//						mi, Expression.Constant("_"), Expression.Constant("~_"));
//
//			var expr = ConvertToSql(context, ConvertExpression(ex));
//
//			if (!string.IsNullOrEmpty(start))
//				expr = new SqlBinaryExpression(typeof(string), new SqlValue("%"), "+", expr);
//
//			if (!string.IsNullOrEmpty(end))
//				expr = new SqlBinaryExpression(typeof(string), expr, "+", new SqlValue("%"));
//
//			return new SqlPredicate.Like(o, false, expr, new SqlValue('~'));

			throw new NotImplementedException();
		}

		ISqlPredicate ConvertLikePredicate(SelectQuery context, MethodCallExpression expression)
		{
//			var e  = expression;
//			var a1 = ConvertToSql(context, e.Arguments[0]);
//			var a2 = ConvertToSql(context, e.Arguments[1]);
//
//			ISqlExpression a3 = null;
//
//			if (e.Arguments.Count == 3)
//				a3 = ConvertToSql(context, e.Arguments[2]);
//
//			return new SqlPredicate.Like(a1, false, a2, a3);

			throw new NotImplementedException();
		}

		static string EscapeLikeText(string text)
		{
			if (text.IndexOfAny(new[] { '%', '_' }) < 0)
				return text;

			var builder = new StringBuilder(text.Length);

			foreach (var ch in text)
			{
				switch (ch)
				{
					case '%':
					case '_':
					case '~':
						builder.Append('~');
						break;
				}

				builder.Append(ch);
			}

			return builder.ToString();
		}

		#endregion

		#region MakeIsPredicate

		internal ISqlPredicate MakeIsPredicate(TableBuilder.TableContext table, Type typeOperand)
		{
//			if (typeOperand == table.ObjectType && table.InheritanceMapping.All(m => m.Type != typeOperand))
//				return Convert(table, new SqlPredicate.Expr(new SqlValue(true)));
//
//			return MakeIsPredicate(table, table.InheritanceMapping, typeOperand, name => table.SqlTable.Fields.Values.First(f => f.Name == name));
			throw new NotImplementedException();
		}

		internal ISqlPredicate MakeIsPredicate(
			SelectQuery                 context,
			List<InheritanceMapping>    inheritanceMapping,
			Type                        toType,
			Func<string,ISqlExpression> getSql)
		{
			var mapping = inheritanceMapping
				.Where (m => m.Type == toType && !m.IsDefault)
				.ToList();

			switch (mapping.Count)
			{
				case 0 :
					{
						var cond = new SqlSearchCondition();

						if (inheritanceMapping.Any(m => m.Type == toType))
						{
							foreach (var m in inheritanceMapping.Where(m => !m.IsDefault))
							{
								cond.Conditions.Add(
									new SqlCondition(
										false,
										Convert(context,
											new SqlPredicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SqlPredicate.Operator.NotEqual,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code)))));
							}
						}
						else
						{
							foreach (var m in inheritanceMapping.Where(m => toType.IsSameOrParentOf(m.Type)))
							{
								cond.Conditions.Add(
									new SqlCondition(
										false,
										Convert(context,
											new SqlPredicate.ExprExpr(
												getSql(m.DiscriminatorName),
												SqlPredicate.Operator.Equal,
												MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
										true));
							}
						}

						return cond;
					}

				case 1 :
					return Convert(context,
						new SqlPredicate.ExprExpr(
							getSql(mapping[0].DiscriminatorName),
							SqlPredicate.Operator.Equal,
							MappingSchema.GetSqlValue(mapping[0].Discriminator.MemberType, mapping[0].Code)));

				default:
					{
						var cond = new SqlSearchCondition();

						foreach (var m in mapping)
						{
							cond.Conditions.Add(
								new SqlCondition(
									false,
									Convert(context,
										new SqlPredicate.ExprExpr(
											getSql(m.DiscriminatorName),
											SqlPredicate.Operator.Equal,
											MappingSchema.GetSqlValue(m.Discriminator.MemberType, m.Code))),
									true));
						}

						return cond;
					}
			}
		}

//		ISqlPredicate MakeIsPredicate(IBuildContext context, TypeBinaryExpression expression)
//		{
//			var typeOperand = expression.TypeOperand;
//			var table       = new TableBuilder.TableContext(this, new BuildInfo((IBuildContext)null, Expression.Constant(null), new SelectQuery()), typeOperand);
//
//			if (typeOperand == table.ObjectType && table.InheritanceMapping.All(m => m.Type != typeOperand))
//				return Convert(table, new SqlPredicate.Expr(new SqlValue(true)));
//
//			var mapping = table.InheritanceMapping.Select((m, i) => new { m, i }).Where(m => typeOperand.IsAssignableFrom(m.m.Type) && !m.m.IsDefault).ToList();
//			var isEqual = true;
//
//			if (mapping.Count == 0)
//			{
//				mapping = table.InheritanceMapping.Select((m,i) => new { m, i }).Where(m => !m.m.IsDefault).ToList();
//				isEqual = false;
//			}
//
//			Expression expr = null;
//
//			foreach (var m in mapping)
//			{
//				var field = table.SqlTable.Fields[table.InheritanceMapping[m.i].DiscriminatorName];
//				var ttype = field.ColumnDescriptor.MemberAccessor.TypeAccessor.Type;
//				var obj   = expression.Expression;
//
//				if (obj.Type != ttype)
//					obj = Expression.Convert(expression.Expression, ttype);
//
//				var left = Expression.PropertyOrField(obj, field.Name);
//				var code = m.m.Code;
//
//				if (code == null)
//					code = left.Type.GetDefaultValue();
//				else if (left.Type != code.GetType())
//					code = Converter.ChangeType(code, left.Type, MappingSchema);
//
//				Expression right = Expression.Constant(code, left.Type);
//
//				var e = isEqual ? Expression.Equal(left, right) : Expression.NotEqual(left, right);
//
//				if (!isEqual)
//					expr = expr != null ? Expression.AndAlso(expr, e) : e;
//				else
//					expr = expr != null ? Expression.OrElse(expr, e) : e;
//			}
//
//			return ConvertPredicate(context, expr);
//		}

		#endregion

		#endregion

		#region OptimizeExpression

		private MethodInfo[] _enumerableMethods;
		public  MethodInfo[]  EnumerableMethods => _enumerableMethods ?? (_enumerableMethods = typeof(Enumerable).GetMethodsEx());

		private MethodInfo[] _queryableMethods;
		public  MethodInfo[]  QueryableMethods  => _queryableMethods  ?? (_queryableMethods  = typeof(Queryable). GetMethodsEx());

		readonly Dictionary<Expression, Expression> _optimizedExpressions = new Dictionary<Expression, Expression>();

		Expression OptimizeExpression(Expression expression)
		{
			if (_optimizedExpressions.TryGetValue(expression, out var expr))
				return expr;

			expr = ExposeExpression(expression);
			expr = expr.Transform((Func<Expression,TransformInfo>)OptimizeExpressionImpl);

			_optimizedExpressions[expression] = expr;

			return expr;
		}

		TransformInfo OptimizeExpressionImpl(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var me = (MemberExpression)expr;

						// Replace Count with Count()
						//
						if (me.Member.Name == "Count")
						{
							var isList = typeof(ICollection).IsAssignableFromEx(me.Member.DeclaringType);

							if (!isList)
								isList =
									me.Member.DeclaringType.IsGenericTypeEx() &&
									me.Member.DeclaringType.GetGenericTypeDefinition() == typeof(ICollection<>);

							if (!isList)
								isList = me.Member.DeclaringType.GetInterfacesEx()
									.Any(t => t.IsGenericTypeEx() && t.GetGenericTypeDefinition() == typeof(ICollection<>));

							if (isList)
							{
								var mi = EnumerableMethods
									.First(m => m.Name == "Count" && m.GetParameters().Length == 1)
									.MakeGenericMethod(me.Expression.Type.GetItemType());

								return new TransformInfo(Expression.Call(null, mi, me.Expression));
							}
						}

						if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
						{
							var ex = ConvertIQueryable(expr);

							if (!ReferenceEquals(ex, expr))
								return new TransformInfo(ConvertExpressionTree(ex));
						}

						return new TransformInfo(ConvertSubquery(expr));
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expr;

						if (call.IsQueryable() || call.IsAsyncExtension())
						{
							switch (call.Method.Name)
							{
								case "Where"                : return new TransformInfo(ConvertWhere         (call));
								case "GroupBy"              : return new TransformInfo(ConvertGroupBy       (call));
								case "SelectMany"           : return new TransformInfo(ConvertSelectMany    (call));
								case "Select"               : return new TransformInfo(ConvertSelect        (call));
								case "LongCount"            :
								case "Count"                :
								case "Single"               :
								case "SingleOrDefault"      :
								case "First"                :
								case "FirstOrDefault"       : return new TransformInfo(ConvertPredicate     (call));
								case "LongCountAsync"       :
								case "CountAsync"           :
								case "SingleAsync"          :
								case "SingleOrDefaultAsync" :
								case "FirstAsync"           :
								case "FirstOrDefaultAsync"  : return new TransformInfo(ConvertPredicateAsync(call));
								case "Min"                  :
								case "Max"                  : return new TransformInfo(ConvertSelector      (call, true));
								case "Sum"                  :
								case "Average"              : return new TransformInfo(ConvertSelector      (call, false));
								case "MinAsync"             :
								case "MaxAsync"             : return new TransformInfo(ConvertSelectorAsync (call, true));
								case "SumAsync"             :
								case "AverageAsync"         : return new TransformInfo(ConvertSelectorAsync (call, false));
								case "ElementAt"            :
								case "ElementAtOrDefault"   : return new TransformInfo(ConvertElementAt     (call));
								case "LoadWith"             : return new TransformInfo(expr, true);
							}
						}
						else
						{
							var l = ConvertMethodExpression(call.Object?.Type ?? call.Method.ReflectedTypeEx(), call.Method);

							if (l != null)
								return new TransformInfo(OptimizeExpression(ConvertMethod(call, l)));

							if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
							{
								var attr = GetTableFunctionAttribute(call.Method);

								if (attr == null)
								{
									var ex = ConvertIQueryable(expr);

									if (!ReferenceEquals(ex, expr))
										return new TransformInfo(ConvertExpressionTree(ex));
								}
							}
						}

						return new TransformInfo(ConvertSubquery(expr));
					}
			}

			return new TransformInfo(expr);
		}

		LambdaExpression ConvertMethodExpression(Type type, MemberInfo mi)
		{
			var attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, mi, a => a.Configuration);

			if (attr != null)
			{
				if (attr.Expression != null)
					return attr.Expression;

				if (!string.IsNullOrEmpty(attr.MethodName))
				{
					Expression expr;

					if (mi is MethodInfo method && method.IsGenericMethod)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = string.Format(attr.MethodName, names);

						expr = Expression.Call(
							mi.DeclaringType,
							name,
							name != attr.MethodName ? Array<Type>.Empty : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType, attr.MethodName, Array<Type>.Empty);
					}

					var call = Expression.Lambda<Func<LambdaExpression>>(Expression.Convert(expr,
						typeof(LambdaExpression)));

					return call.Compile()();
				}
			}

			return null;
		}

		Expression ConvertSubquery(Expression expr)
		{
			var ex = expr;

			while (ex != null)
			{
				switch (ex.NodeType)
				{
					case ExpressionType.MemberAccess : ex = ((MemberExpression)ex).Expression; break;
					case ExpressionType.Call         :
						{
							var call = (MethodCallExpression)ex;

							if (call.Object == null)
							{
								if (call.IsQueryable())
									switch (call.Method.Name)
									{
										case "Single"          :
										case "SingleOrDefault" :
										case "First"           :
										case "FirstOrDefault"  :
											return ConvertSingleOrFirst(expr, call);
									}

								return expr;
							}

							ex = call.Object;

							break;
						}
					default: return expr;
				}
			}

			return expr;
		}

		Expression ConvertSingleOrFirst(Expression expr, MethodCallExpression call)
		{
			var param    = Expression.Parameter(call.Type, "p");
			var selector = expr.Transform(e => e == call ? param : e);
			var method   = GetQueryableMethodInfo(call, (m, _) => m.Name == call.Method.Name && m.GetParameters().Length == 1);
			var select   = call.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArgumentsEx().Length == 2) :
				QueryableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArgumentsEx()[0].GetGenericArgumentsEx().Length == 2);

			call   = (MethodCallExpression)OptimizeExpression(call);
			select = select.MakeGenericMethod(call.Type, expr.Type);
			method = method.MakeGenericMethod(expr.Type);

			return Expression.Call(null, method,
				Expression.Call(null, select, call.Arguments[0], Expression.Lambda(selector, param)));
		}

		#endregion

		#region Helpers

		MethodInfo GetQueryableMethodInfo(MethodCallExpression method, [InstantHandle] Func<MethodInfo,bool,bool> predicate)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods.FirstOrDefault(m => predicate(m, false)) ?? EnumerableMethods.First(m => predicate(m, true)):
				QueryableMethods. FirstOrDefault(m => predicate(m, false)) ?? QueryableMethods. First(m => predicate(m, true));
		}

		MethodInfo GetMethodInfo(MethodCallExpression method, string name)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(m => m.Name == name && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArgumentsEx().Length == 2) :
				QueryableMethods
					.Where(m => m.Name == name && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArgumentsEx()[0].GetGenericArgumentsEx().Length == 2);
		}

		static Type[] GetMethodGenericTypes(MethodCallExpression method)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				method.Method.GetParameters()[1].ParameterType.GetGenericArgumentsEx() :
				method.Method.GetParameters()[1].ParameterType.GetGenericArgumentsEx()[0].GetGenericArgumentsEx();
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Gets Expression.Equal if <paramref name="left"/> and <paramref name="right"/> expression types are not same
		/// <paramref name="right"/> would be converted to <paramref name="left"/>
		/// </summary>
		/// <param name="mappringSchema"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		internal static BinaryExpression Equal(MappingSchema mappringSchema, Expression left, Expression right)
		{
			if (left.Type != right.Type)
			{
				if (right.Type.CanConvertTo(left.Type))
					right = Expression.Convert(right, left.Type);
				else if (left.Type.CanConvertTo(right.Type))
					left = Expression.Convert(left, right.Type);
				else
				{
					var rightConvert = ConvertBuilder.GetConverter(mappringSchema, right.Type, left. Type);
					var leftConvert  = ConvertBuilder.GetConverter(mappringSchema, left. Type, right.Type);

					var leftIsPrimitive  = left. Type.IsPrimitiveEx();
					var rightIsPrimitive = right.Type.IsPrimitiveEx();

					if (leftIsPrimitive == true && rightIsPrimitive == false && rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftIsPrimitive == false && rightIsPrimitive == true && leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
					else if (rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
				}
			}

			return Expression.Equal(left, right);
		}

		#endregion

		#region ConvertExpression

		public ParameterExpression SequenceParameter;

		public Expression ConvertExpressionTree(Expression expression)
		{
			throw new NotImplementedException();

//			var expr = expression;
//
//			expr = ConvertParameters (expr);
//			expr = OptimizeExpression(expr);
//
//			var paramType   = expr.Type;
//			var isQueryable = false;
//
//			if (expression.NodeType == ExpressionType.Call)
//			{
//				var call = (MethodCallExpression)expression;
//
//				if (call.IsQueryable() && call.Object == null && call.Arguments.Count > 0 && call.Type.IsGenericTypeEx())
//				{
//					var type = call.Type.GetGenericTypeDefinition();
//
//					if (type == typeof(IQueryable<>) || type == typeof(IEnumerable<>))
//					{
//						var arg = call.Type.GetGenericArgumentsEx();
//
//						if (arg.Length == 1)
//						{
//							paramType   = arg[0];
//							isQueryable = true;
//						}
//					}
//				}
//			}
//
//			SequenceParameter = Expression.Parameter(paramType, "cp");
//
//			var sequence = ConvertSequence(new BuildInfo((IBuildContext)null, expr, new SelectQuery()), SequenceParameter, false);
//
//			if (sequence != null)
//			{
//				if (sequence.Expression.Type != expr.Type)
//				{
//					if (isQueryable)
//					{
//						var p = sequence.ExpressionsToReplace.Single(s => s.Path.NodeType == ExpressionType.Parameter);
//
//						return Expression.Call(
//							((MethodCallExpression)expr).Method.DeclaringType,
//							"Select",
//							new[] { p.Path.Type, paramType },
//							sequence.Expression,
//							Expression.Lambda(p.Expr, (ParameterExpression)p.Path));
//					}
//
//					throw new InvalidOperationException();
//				}
//
//				return sequence.Expression;
//			}
//
//			return expr;
		}

		#region ConvertParameters

		internal static Expression AggregateExpression(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Or      :
					case ExpressionType.And     :
					case ExpressionType.OrElse  :
					case ExpressionType.AndAlso :
						{
							var stack  = new Stack<Expression>();
							var items  = new List<Expression>();
							var binary = (BinaryExpression) expr;

							stack.Push(binary.Right);
							stack.Push(binary.Left);
							while (stack.Count > 0)
							{
								var item = stack.Pop();
								if (item.NodeType == expr.NodeType)
								{
									binary  = (BinaryExpression) item;
									stack.Push(binary.Right);
									stack.Push(binary.Left);
								}
								else
									items.Add(item);
							}

							if (items.Count > 3)
							{
								// having N items will lead to NxM recursive calls in expression visitors and
								// will result in stack overflow on relatively small numbers (~1000 items).
								// To fix it we will rebalance condition tree here which will result in
								// LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
								//
								// E.g. we have condition A OR B OR C OR D OR E
								// as an expression tree it represented as tree with depth 5
								//   OR
								// A    OR
								//    B    OR
								//       C    OR
								//          D    E
								// for rebalanced tree it will have depth 4
								//                  OR
								//        OR
								//   OR        OR        OR
								// A    B    C    D    E    F
								// Not much on small numbers, but huge improvement on bigger numbers
								while (items.Count != 1)
								{
									items = CompactTree(items, expr.NodeType);
								}

								return items[0];
							}
							break;
						}
				}

				return expr;
			});
		}

		private static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
		{
			var result = new List<Expression>();

			// traverse list from left to right to preserve calculation order
			for (var i = 0; i < items.Count; i += 2)
			{
				if (i + 1 == items.Count)
				{
					// last non-paired item
					result.Add(items[i]);
				}
				else
				{
					result.Add(Expression.MakeBinary(nodeType, items[i], items[i + 1]));
				}
			}

			return result;
		}

		internal static Expression ExpandExpression(Expression expression)
		{
			if (Common.Configuration.Linq.UseBinaryAggregateExpression)
				expression = AggregateExpression(expression);

			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)expr;

							List<Expression> newArgs = null;
							for (var index = 0; index < mc.Arguments.Count; index++)
							{
								var arg = mc.Arguments[index];
								Expression newArg = null;
								if (typeof(LambdaExpression).IsSameOrParentOf(arg.Type))
								{
									var argUnwrapped = arg.Unwrap();
									if (argUnwrapped.NodeType == ExpressionType.MemberAccess ||
									    argUnwrapped.NodeType == ExpressionType.Call)
									{
										if (argUnwrapped.EvaluateExpression() is LambdaExpression lambda)
											newArg = ExpandExpression(lambda);
									}
								}

								if (newArg == null)
									newArgs?.Add(arg);
								else
								{
									if (newArgs == null)
										newArgs = new List<Expression>(mc.Arguments.Take(index));
									newArgs.Add(newArg);
								}
							}

							if (newArgs != null)
							{
								mc = mc.Update(mc.Object, newArgs);
							}


							if (mc.Method.Name == "Compile" && typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
							{
								if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
								{
									return ExpandExpression(lambda);
								}
							}

							return mc;
						}

					case ExpressionType.Invoke:
						{
							var invocation = (InvocationExpression)expr;
							if (invocation.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)invocation.Expression;
								if (mc.Method.Name == "Compile" &&
								    typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
								{
									if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
									{
										var map = new Dictionary<Expression, Expression>();
										for (int i = 0; i < invocation.Arguments.Count; i++)
										{
											map.Add(lambda.Parameters[i], invocation.Arguments[i]);
										}

										var newBody = lambda.Body.Transform(se =>
										{
											if (se.NodeType == ExpressionType.Parameter &&
											    map.TryGetValue(se, out var newExpr))
												return newExpr;
											return se;
										});

										return ExpandExpression(newBody);
									}
								}
							}
							break;
						}
				}

				return expr;
			});
		}

		Expression ConvertParameters(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Parameter:
						if (CompiledParameters != null)
						{
							var idx = Array.IndexOf(CompiledParameters, (ParameterExpression)expr);

							if (idx > 0)
								return
									Expression.Convert(
										Expression.ArrayIndex(
											ParametersParam,
											Expression.Constant(Array.IndexOf(CompiledParameters, (ParameterExpression)expr))),
										expr.Type);
						}

						break;
				}

				return expr;
			});
		}

		#endregion

		#region ExposeExpression

		Expression ExposeExpression(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)expr;
							var l  = ConvertMethodExpression(me.Expression?.Type ?? me.Member.ReflectedTypeEx(), me.Member);

							if (l != null)
							{
								var body  = l.Body.Unwrap();
								var parms = l.Parameters.ToDictionary(p => p);
								var ex    = body.Transform(wpi =>
								{
									if (wpi.NodeType == ExpressionType.Parameter && parms.ContainsKey((ParameterExpression)wpi))
									{
										if (wpi.Type.IsSameOrParentOf(me.Expression.Type))
										{
											return me.Expression;
										}

										if (DataContextParam.Type.IsSameOrParentOf(wpi.Type))
										{
											if (DataContextParam.Type != wpi.Type)
												return Expression.Convert(DataContextParam, wpi.Type);
											return DataContextParam;
										}

										throw new LinqToDBException($"Can't convert {wpi} to expression.");
									}

									return wpi;
								});

								if (ex.Type != expr.Type)
									ex = new ChangeTypeExpression(ex, expr.Type);

								return ExposeExpression(ex);
							}

							break;
						}

					case ExpressionType.Constant :
						{
							var c = (ConstantExpression)expr;

							// Fix Mono behaviour.
							//
							//if (c.Value is IExpressionQuery)
							//	return ((IQueryable)c.Value).Expression;

							if (c.Value is IQueryable queryable && !(queryable is ITable))
							{
								var e = queryable.Expression;

								if (!_visitedExpressions.Contains(e))
								{
									_visitedExpressions.Add(e);
									return ExposeExpression(e);
								}
							}

							break;
						}

					case ExpressionType.Invoke:
						{
							var invocation = (InvocationExpression)expr;
							if (invocation.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)invocation.Expression;
								if (mc.Method.Name == "Compile" &&
								    typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType))
								{
									if (mc.Object.EvaluateExpression() is LambdaExpression lambds)
									{
										var map = new Dictionary<Expression, Expression>();
										for (int i = 0; i < invocation.Arguments.Count; i++)
										{
											map.Add(lambds.Parameters[i], invocation.Arguments[i]);
										}

										var newBody = lambds.Body.Transform(se =>
										{
											if (se.NodeType == ExpressionType.Parameter &&
											    map.TryGetValue(se, out var newExpr))
												return newExpr;
											return se;
										});

										return ExposeExpression(newBody);
									}
								}
							}
							break;
						}

				}

				return expr;
			});
		}

		#endregion

		#region ConvertWhere

		Expression ConvertWhere(MethodCallExpression method)
		{
			var sequence  = OptimizeExpression(method.Arguments[0]);
			var predicate = OptimizeExpression(method.Arguments[1]);
			var lambda    = (LambdaExpression)predicate.Unwrap();
			var lparam    = lambda.Parameters[0];
			var lbody     = lambda.Body;

			if (lambda.Parameters.Count > 1)
				return method;

			var exprs     = new List<Expression>();

			lbody.Visit(ex =>
			{
				if (ex.NodeType == ExpressionType.Call)
				{
					var call = (MethodCallExpression)ex;

					if (call.Arguments.Count > 0)
					{
						var arg = call.Arguments[0];

						if (call.IsAggregate(MappingSchema))
						{
							while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression)arg).Method.Name == "Select")
								arg = ((MethodCallExpression)arg).Arguments[0];

							if (arg.NodeType == ExpressionType.Call)
								exprs.Add(ex);
						}
						else if (call.IsQueryable(CountBuilder.MethodNames))
						{
							//while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression) arg).Method.Name == "Select")
							//	arg = ((MethodCallExpression) arg).Arguments[0];

							if (arg.NodeType == ExpressionType.Call)
								exprs.Add(ex);
						}
					}
				}
			});

			Expression expr = null;

			if (exprs.Count > 0)
			{
				expr = lparam;

				foreach (var ex in exprs)
				{
					var type   = typeof(ExpressionHoder<,>).MakeGenericType(expr.Type, ex.Type);
					var fields = type.GetFieldsEx();

					expr = Expression.MemberInit(
						Expression.New(type),
						Expression.Bind(fields[0], expr),
						Expression.Bind(fields[1], ex));
				}

				var dic  = new Dictionary<Expression, Expression>();
				var parm = Expression.Parameter(expr.Type, lparam.Name);

				for (var i = 0; i < exprs.Count; i++)
				{
					Expression ex = parm;

					for (var j = i; j < exprs.Count - 1; j++)
						ex = Expression.PropertyOrField(ex, "p");

					ex = Expression.PropertyOrField(ex, "ex");

					dic.Add(exprs[i], ex);

					if (_subQueryExpressions == null)
						_subQueryExpressions = new HashSet<Expression>();
					_subQueryExpressions.Add(ex);
				}

				var newBody = lbody.Transform(ex =>
				{
					Expression e;
					return dic.TryGetValue(ex, out e) ? e : ex;
				});

				var nparm = exprs.Aggregate<Expression,Expression>(parm, (c,t) => Expression.PropertyOrField(c, "p"));

				newBody   = newBody.Transform(ex => ReferenceEquals(ex, lparam) ? nparm : ex);
				predicate = Expression.Lambda(newBody, parm);

				var methodInfo = GetMethodInfo(method, "Select");

				methodInfo = methodInfo.MakeGenericMethod(lparam.Type, expr.Type);
				sequence   = Expression.Call(methodInfo, sequence, Expression.Lambda(expr, lparam));
			}

			if (!ReferenceEquals(sequence, method.Arguments[0]) || !ReferenceEquals(predicate, method.Arguments[1]))
			{
				var methodInfo  = method.Method.GetGenericMethodDefinition();
				var genericType = sequence.Type.GetGenericArgumentsEx()[0];
				var newMethod   = methodInfo.MakeGenericMethod(genericType);

				method = Expression.Call(newMethod, sequence, predicate);

				if (exprs.Count > 0)
				{
					var parameter = Expression.Parameter(expr.Type, lparam.Name);

					methodInfo = GetMethodInfo(method, "Select");
					methodInfo = methodInfo.MakeGenericMethod(expr.Type, lparam.Type);
					method     = Expression.Call(methodInfo, method,
						Expression.Lambda(
							exprs.Aggregate((Expression)parameter, (current,_) => Expression.PropertyOrField(current, "p")),
							parameter));
				}
			}

			return method;
		}

		#endregion

		#region ConvertGroupBy

		public class GroupSubQuery<TKey,TElement>
		{
			public TKey     Key;
			public TElement Element;
		}

		interface IGroupByHelper
		{
			void Set(bool wrapInSubQuery, Expression sourceExpression, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector);

			Expression AddElementSelectorQ  ();
			Expression AddElementSelectorE  ();
			Expression AddResultQ           ();
			Expression AddResultE           ();
			Expression WrapInSubQueryQ      ();
			Expression WrapInSubQueryE      ();
			Expression WrapInSubQueryResultQ();
			Expression WrapInSubQueryResultE();
		}

		class GroupByHelper<TSource,TKey,TElement,TResult> : IGroupByHelper
		{
			bool             _wrapInSubQuery;
			Expression       _sourceExpression;
			LambdaExpression _keySelector;
			LambdaExpression _elementSelector;
			LambdaExpression _resultSelector;

			public void Set(
				bool             wrapInSubQuery,
				Expression       sourceExpression,
				LambdaExpression keySelector,
				LambdaExpression elementSelector,
				LambdaExpression resultSelector)
			{
				_wrapInSubQuery   = wrapInSubQuery;
				_sourceExpression = sourceExpression;
				_keySelector      = keySelector;
				_elementSelector  = elementSelector;
				_resultSelector   = resultSelector;
			}

			public Expression AddElementSelectorQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<IGrouping<TKey,TSource>>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, _ => _)
					;

				var body   = func.Body.Unwrap();
				var keyArg = GetLambda(body, 1).Parameters[0]; // .GroupBy(keyParam

				return Convert(func, keyArg, null, null);
			}

			public Expression AddElementSelectorE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<IGrouping<TKey,TSource>>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, _ => _)
					;

				var body   = func.Body.Unwrap();
				var keyArg = GetLambda(body, 1).Parameters[0]; // .GroupBy(keyParam

				return Convert(func, keyArg, null, null);
			}

			public Expression AddResultQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<TResult>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .GroupBy(keyParam
				var elemArg = GetLambda(body, 0, 2).Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).   Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression AddResultE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<TResult>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .GroupBy(keyParam
				var elemArg = GetLambda(body, 0, 2).Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).   Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression WrapInSubQueryQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<IGrouping<TKey,TElement>>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 2).   Parameters[0]; // .GroupBy(..., elemParam

				return Convert(func, keyArg, elemArg, null);
			}

			public Expression WrapInSubQueryE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<IGrouping<TKey,TElement>>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 2).   Parameters[0]; // .GroupBy(..., elemParam

				return Convert(func, keyArg, elemArg, null);
			}

			public Expression WrapInSubQueryResultQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<TResult>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 0, 2).   Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).      Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression WrapInSubQueryResultE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<TResult>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 0, 2).   Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).      Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			Expression Convert(
				LambdaExpression    func,
				ParameterExpression keyArg,
				ParameterExpression elemArg,
				ParameterExpression resArg)
			{
				var body = func.Body.Unwrap();
				var expr = body.Transform(ex =>
				{
					if (ReferenceEquals(ex, func.Parameters[0]))
						return _sourceExpression;

					if (ReferenceEquals(ex, func.Parameters[1]))
						return _keySelector.Body.Transform(e => ReferenceEquals(e, _keySelector.Parameters[0]) ? keyArg : e);

					if (ReferenceEquals(ex, func.Parameters[2]))
					{
						Expression obj = elemArg;

						if (_wrapInSubQuery)
							obj = Expression.PropertyOrField(elemArg, "Element");

						if (_elementSelector == null)
							return obj;

						return _elementSelector.Body.Transform(e => ReferenceEquals(e, _elementSelector.Parameters[0]) ? obj : e);
					}

					if (ReferenceEquals(ex, func.Parameters[3]))
						return _resultSelector.Body.Transform(e =>
						{
							if (ReferenceEquals(e, _resultSelector.Parameters[0]))
								return Expression.PropertyOrField(resArg, "Key");

							if (ReferenceEquals(e, _resultSelector.Parameters[1]))
								return resArg;

							return e;
						});

					return ex;
				});

				return expr;
			}
		}

		static LambdaExpression GetLambda(Expression expression, params int[] n)
		{
			foreach (var i in n)
				expression = ((MethodCallExpression)expression).Arguments[i].Unwrap();
			return (LambdaExpression)expression;
		}

		Expression ConvertGroupBy(MethodCallExpression method)
		{
			if (method.Arguments[method.Arguments.Count - 1].Unwrap().NodeType != ExpressionType.Lambda)
				return method;

			var types = method.Method.GetGenericMethodDefinition().GetGenericArguments()
				.Zip(method.Method.GetGenericArguments(), (n, t) => new { n = n.Name, t })
				.ToDictionary(_ => _.n, _ => _.t);

			var sourceExpression = OptimizeExpression(method.Arguments[0].Unwrap());
			var keySelector      = (LambdaExpression)OptimizeExpression(method.Arguments[1].Unwrap());
			var elementSelector  = types.ContainsKey("TElement") ? (LambdaExpression)OptimizeExpression(method.Arguments[2].Unwrap()) : null;
			var resultSelector   = types.ContainsKey("TResult")  ?
				(LambdaExpression)OptimizeExpression(method.Arguments[types.ContainsKey("TElement") ? 3 : 2].Unwrap()) : null;

			var needSubQuery = null != ConvertExpression(keySelector.Body.Unwrap()).Find(IsExpression);

			if (!needSubQuery && resultSelector == null && elementSelector != null)
				return method;

			var gtype  = typeof(GroupByHelper<,,,>).MakeGenericType(
				types["TSource"],
				types["TKey"],
				types.ContainsKey("TElement") ? types["TElement"] : types["TSource"],
				types.ContainsKey("TResult")  ? types["TResult"]  : types["TSource"]);

			var helper =
				//Expression.Lambda<Func<IGroupByHelper>>(
				//	Expression.Convert(Expression.New(gtype), typeof(IGroupByHelper)))
				//.Compile()();
				(IGroupByHelper)Activator.CreateInstance(gtype);

			helper.Set(needSubQuery, sourceExpression, keySelector, elementSelector, resultSelector);

			if (method.Method.DeclaringType == typeof(Queryable))
			{
				if (!needSubQuery)
					return resultSelector == null ? helper.AddElementSelectorQ() : helper.AddResultQ();

				return resultSelector == null ? helper.WrapInSubQueryQ() : helper.WrapInSubQueryResultQ();
			}
			else
			{
				if (!needSubQuery)
					return resultSelector == null ? helper.AddElementSelectorE() : helper.AddResultE();

				return resultSelector == null ? helper.WrapInSubQueryE() : helper.WrapInSubQueryResultE();
			}
		}

		bool IsExpression(Expression ex)
		{
			switch (ex.NodeType)
			{
				case ExpressionType.Convert        :
				case ExpressionType.ConvertChecked :
				case ExpressionType.MemberInit     :
				case ExpressionType.New            :
				case ExpressionType.NewArrayBounds :
				case ExpressionType.NewArrayInit   :
				case ExpressionType.Parameter      : return false;
				case ExpressionType.MemberAccess   :
					{
						var ma   = (MemberExpression)ex;
						var attr = GetExpressionAttribute(ma.Member);

						if (attr != null)
							return true;

						return false;
					}
			}

			return true;
		}

		#endregion

		#region ConvertSelectMany

		interface ISelectManyHelper
		{
			void Set(Expression sourceExpression, LambdaExpression colSelector);

			Expression AddElementSelectorQ();
			Expression AddElementSelectorE();
		}

		class SelectManyHelper<TSource,TCollection> : ISelectManyHelper
		{
			Expression       _sourceExpression;
			LambdaExpression _colSelector;

			public void Set(Expression sourceExpression, LambdaExpression colSelector)
			{
				_sourceExpression = sourceExpression;
				_colSelector      = colSelector;
			}

			public Expression AddElementSelectorQ()
			{
				Expression<Func<IQueryable<TSource>,IEnumerable<TCollection>,IQueryable<TCollection>>> func = (source,col) => source
					.SelectMany(cp => col, (s,c) => c)
					;

				var body   = func.Body.Unwrap();
				var colArg = GetLambda(body, 1).Parameters[0]; // .SelectMany(colParam

				return Convert(func, colArg);
			}

			public Expression AddElementSelectorE()
			{
				Expression<Func<IEnumerable<TSource>,IEnumerable<TCollection>,IEnumerable<TCollection>>> func = (source,col) => source
					.SelectMany(cp => col, (s,c) => c)
					;

				var body   = func.Body.Unwrap();
				var colArg = GetLambda(body, 1).Parameters[0]; // .SelectMany(colParam

				return Convert(func, colArg);
			}

			Expression Convert(LambdaExpression func, ParameterExpression colArg)
			{
				var body = func.Body.Unwrap();
				var expr = body.Transform(ex =>
				{
					if (ex == func.Parameters[0])
						return _sourceExpression;

					if (ex == func.Parameters[1])
						return _colSelector.Body.Transform(e => e == _colSelector.Parameters[0] ? colArg : e);

					return ex;
				});

				return expr;
			}
		}

		Expression ConvertSelectMany(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2 || ((LambdaExpression)method.Arguments[1].Unwrap()).Parameters.Count != 1)
				return method;

			var types = method.Method.GetGenericMethodDefinition().GetGenericArguments()
				.Zip(method.Method.GetGenericArguments(), (n, t) => new { n = n.Name, t })
				.ToDictionary(_ => _.n, _ => _.t);

			var sourceExpression = OptimizeExpression(method.Arguments[0].Unwrap());
			var colSelector      = (LambdaExpression)OptimizeExpression(method.Arguments[1].Unwrap());

			var gtype  = typeof(SelectManyHelper<,>).MakeGenericType(types["TSource"], types["TResult"]);
			var helper =
				//Expression.Lambda<Func<ISelectManyHelper>>(
				//	Expression.Convert(Expression.New(gtype), typeof(ISelectManyHelper)))
				//.Compile()();
				(ISelectManyHelper)Activator.CreateInstance(gtype);

			helper.Set(sourceExpression, colSelector);

			return method.Method.DeclaringType == typeof(Queryable) ?
				helper.AddElementSelectorQ() :
				helper.AddElementSelectorE();
		}

		#endregion

		#region ConvertPredicate

		Expression ConvertPredicate(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2)
				return method;

			var cm = GetQueryableMethodInfo(method, (m,_) => m.Name == method.Method.Name && m.GetParameters().Length == 1);
			var wm = GetMethodInfo(method, "Where");

			var argType = method.Method.GetGenericArguments()[0];

			wm = wm.MakeGenericMethod(argType);
			cm = cm.MakeGenericMethod(argType);

			return Expression.Call(null, cm,
				Expression.Call(null, wm,
					OptimizeExpression(method.Arguments[0]),
					OptimizeExpression(method.Arguments[1])));
		}

		Expression ConvertPredicateAsync(MethodCallExpression method)
		{
			if (method.Arguments.Count != 3)
				return method;

			var cm = typeof(AsyncExtensions).GetMethodsEx().First(m => m.Name == method.Method.Name && m.GetParameters().Length == 2);
			var wm = GetMethodInfo(method, "Where");

			var argType = method.Method.GetGenericArguments()[0];

			wm = wm.MakeGenericMethod(argType);
			cm = cm.MakeGenericMethod(argType);

			return Expression.Call(null, cm,
				Expression.Call(null, wm,
					OptimizeExpression(method.Arguments[0]),
					OptimizeExpression(method.Arguments[1])),
				OptimizeExpression(method.Arguments[2]));
		}

		#endregion

		#region ConvertSelector

		Expression ConvertSelector(MethodCallExpression method, bool isGeneric)
		{
			if (method.Arguments.Count != 2)
				return method;

			isGeneric = isGeneric && method.Method.DeclaringType == typeof(Queryable);

			var types = GetMethodGenericTypes(method);
			var sm    = GetMethodInfo(method, "Select");
			var cm    = GetQueryableMethodInfo(method, (m,isDefault) =>
			{
				if (m.Name == method.Method.Name)
				{
					var ps = m.GetParameters();

					if (ps.Length == 1)
					{
						if (isGeneric)
							return true;

						var ts = ps[0].ParameterType.GetGenericArgumentsEx();
						return ts[0] == types[1] || isDefault && ts[0].IsGenericParameter;
					}
				}

				return false;
			});

			var argType = types[0];

			sm = sm.MakeGenericMethod(argType, types[1]);

			if (cm.IsGenericMethodDefinition)
				cm = cm.MakeGenericMethod(types[1]);

			return Expression.Call(null, cm,
				OptimizeExpression(Expression.Call(null, sm,
					method.Arguments[0],
					method.Arguments[1])));
		}

		Expression ConvertSelectorAsync(MethodCallExpression method, bool isGeneric)
		{
			if (method.Arguments.Count != 3)
				return method;

			isGeneric = isGeneric && method.Method.DeclaringType == typeof(AsyncExtensions);

			var types = GetMethodGenericTypes(method);
			var sm    = GetMethodInfo(method, "Select");
			var cm    = typeof(AsyncExtensions).GetMethodsEx().First(m =>
			{
				if (m.Name == method.Method.Name)
				{
					var ps = m.GetParameters();

					if (ps.Length == 2)
					{
						if (isGeneric)
							return true;

						var ts = ps[0].ParameterType.GetGenericArgumentsEx();
						return ts[0] == types[1];// || isDefault && ts[0].IsGenericParameter;
					}
				}

				return false;
			});

			var argType = types[0];

			sm = sm.MakeGenericMethod(argType, types[1]);

			if (cm.IsGenericMethodDefinition)
				cm = cm.MakeGenericMethod(types[1]);

			return Expression.Call(null, cm,
				OptimizeExpression(Expression.Call(null, sm,
					method.Arguments[0],
					method.Arguments[1])),
				OptimizeExpression(method.Arguments[2]));
		}

		#endregion

		#region ConvertSelect

		Expression ConvertSelect(MethodCallExpression method)
		{
			var sequence = OptimizeExpression(method.Arguments[0]);
			var lambda   = (LambdaExpression)method.Arguments[1].Unwrap();

			if (lambda.Parameters.Count > 1 ||
				sequence.NodeType != ExpressionType.Call ||
				((MethodCallExpression)sequence).Method.Name != method.Method.Name)
			{
				return method;
			}

			var slambda = (LambdaExpression)((MethodCallExpression)sequence).Arguments[1].Unwrap();
			var sbody   = slambda.Body.Unwrap();

			if (slambda.Parameters.Count > 1 || sbody.NodeType != ExpressionType.MemberAccess)
				return method;

			lambda = (LambdaExpression)OptimizeExpression(lambda);

			var types1 = GetMethodGenericTypes((MethodCallExpression)sequence);
			var types2 = GetMethodGenericTypes(method);

			return Expression.Call(null,
				GetMethodInfo(method, "Select").MakeGenericMethod(types1[0], types2[1]),
				((MethodCallExpression)sequence).Arguments[0],
				Expression.Lambda(lambda.GetBody(sbody), slambda.Parameters[0]));
		}

		#endregion

		#region ConvertIQueryable

		Expression ConvertIQueryable(Expression expression)
		{
			if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
			{
				var p    = Expression.Parameter(typeof(Expression), "exp");
				var exas = expression.GetExpressionAccessors(p);
				var expr = ReplaceParameter(exas, expression, _ => {}).ValueExpression;

				if (expr.Find(e => e.NodeType == ExpressionType.Parameter && e != p) != null)
					return expression;

				var l    = Expression.Lambda<Func<Expression,IQueryable>>(Expression.Convert(expr, typeof(IQueryable)), new [] { p });
				var n    = _query.AddQueryableAccessors(expression, l);

				_expressionAccessors.TryGetValue(expression, out var accessor);

				var path =
					Expression.Call(
						Expression.Constant(_query),
						MemberHelper.MethodOf<Query>(a => a.GetIQueryable(0, null)),
						new[] { Expression.Constant(n), accessor ?? Expression.Constant(null, typeof(Expression)) });

				var qex = _query.GetIQueryable(n, expression);

				if (expression.NodeType == ExpressionType.Call && qex.NodeType == ExpressionType.Call)
				{
					var m1 = (MethodCallExpression)expression;
					var m2 = (MethodCallExpression)qex;

					if (m1.Method == m2.Method)
						return expression;
				}

				foreach (var a in qex.GetExpressionAccessors(path))
					if (!_expressionAccessors.ContainsKey(a.Key))
						_expressionAccessors.Add(a.Key, a.Value);

				return qex;
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region ConvertElementAt

		Expression ConvertElementAt(MethodCallExpression method)
		{
			var sequence   = OptimizeExpression(method.Arguments[0]);
			var index      = OptimizeExpression(method.Arguments[1]).Unwrap();
			var sourceType = method.Method.GetGenericArguments()[0];

			MethodInfo skipMethod;

			if (index.NodeType == ExpressionType.Lambda)
			{
				skipMethod = MemberHelper.MethodOf(() => LinqExtensions.Skip<object>(null, null));
				skipMethod = skipMethod.GetGenericMethodDefinition();
			}
			else
			{
				skipMethod = GetQueryableMethodInfo(method, (mi,_) => mi.Name == "Skip");
			}

			skipMethod = skipMethod.MakeGenericMethod(sourceType);

			var methodName  = method.Method.Name == "ElementAt" ? "First" : "FirstOrDefault";
			var firstMethod = GetQueryableMethodInfo(method, (mi,_) => mi.Name == methodName && mi.GetParameters().Length == 1);

			firstMethod = firstMethod.MakeGenericMethod(sourceType);

			return Expression.Call(null, firstMethod, Expression.Call(skipMethod, sequence, index));
		}

		#endregion

		#endregion

		#region ConvertExpression

		interface IConvertHelper
		{
			Expression ConvertNull(MemberExpression expression);
		}

		class ConvertHelper<T> : IConvertHelper
			where T : struct
		{
			public Expression ConvertNull(MemberExpression expression)
			{
				return Expression.Call(
					null,
					MemberHelper.MethodOf<T?>(p => Sql.ToNotNull(p)),
					expression.Expression);
			}
		}

		internal Expression ConvertExpression(Expression expression)
		{
			return expression.Transform(e =>
			{
				if (CanBeConstant(e) || CanBeCompiled(e))
				//if ((CanBeConstant(e) || CanBeCompiled(e)) && !PreferServerSide(e))
					return new TransformInfo(e, true);

				switch (e.NodeType)
				{
					//This is to handle VB's weird expression generation when dealing with nullable properties.
					case ExpressionType.Coalesce:
						{
							var b = (BinaryExpression)e;

							if (b.Left is BinaryExpression equalityLeft && b.Right is ConstantExpression constantRight)
								if (equalityLeft.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
									if (equalityLeft.NodeType == ExpressionType.Equal && equalityLeft.Left.Type == equalityLeft.Right.Type)
										if (constantRight.Value is bool val && val == false)
											return new TransformInfo(equalityLeft, false);

							break;
						}

					case ExpressionType.New:
						{
							var ex = ConvertNew((NewExpression)e);
							if (ex != null)
								return new TransformInfo(ConvertExpression(ex));
							break;
						}

					case ExpressionType.Call:
						{
							var expr = (MethodCallExpression)e;

							if (expr.Method.IsSqlPropertyMethodEx())
							{
								// transform Sql.Property into member access
								if (expr.Arguments[1].Type != typeof(string))
									throw new ArgumentException("Only strings are allowed for member name in Sql.Property expressions.");

								var entity           = ConvertExpression(expr.Arguments[0]);
								var memberName       = (string)expr.Arguments[1].EvaluateExpression();
								var entityDescriptor = MappingSchema.GetEntityDescriptor(entity.Type);

								var memberInfo = entityDescriptor[memberName]?.MemberInfo ?? entityDescriptor.Associations
									                 .SingleOrDefault(a => a.MemberInfo.Name == memberName)?.MemberInfo;
								if (memberInfo == null)
									memberInfo = MemberHelper.GetMemberInfo(expr);

								return new TransformInfo(ConvertExpression(Expression.MakeMemberAccess(entity, memberInfo)));
							}

							var cm = ConvertMethod(expr);
							if (cm != null)
								return new TransformInfo(ConvertExpression(cm));
							break;
						}

					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;
							var l  = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

							if (l != null)
							{
								var body = l.Body.Unwrap();
								var expr = body.Transform(wpi => wpi.NodeType == ExpressionType.Parameter ? ma.Expression : wpi);

								if (expr.Type != e.Type)
									expr = new ChangeTypeExpression(expr, e.Type);

								return new TransformInfo(ConvertExpression(expr));
							}

							if (ma.Member.IsNullableValueMember())
							{
								var ntype  = typeof(ConvertHelper<>).MakeGenericType(ma.Type);
								var helper = (IConvertHelper)Activator.CreateInstance(ntype);
								var expr   = helper.ConvertNull(ma);

								return new TransformInfo(ConvertExpression(expr));
							}

							if (ma.Member.DeclaringType == typeof(TimeSpan))
							{
								switch (ma.Expression.NodeType)
								{
									case ExpressionType.Subtract       :
									case ExpressionType.SubtractChecked:

										Sql.DateParts datePart;

										switch (ma.Member.Name)
										{
											case "TotalMilliseconds" : datePart = Sql.DateParts.Millisecond; break;
											case "TotalSeconds"      : datePart = Sql.DateParts.Second;      break;
											case "TotalMinutes"      : datePart = Sql.DateParts.Minute;      break;
											case "TotalHours"        : datePart = Sql.DateParts.Hour;        break;
											case "TotalDays"         : datePart = Sql.DateParts.Day;         break;
											default                  : return new TransformInfo(e);
										}

										var ex     = (BinaryExpression)ma.Expression;
										var method = MemberHelper.MethodOf(
											() => Sql.DateDiff(Sql.DateParts.Day, DateTime.MinValue, DateTime.MinValue));

										var call   =
											Expression.Convert(
												Expression.Call(
													null,
													method,
													Expression.Constant(datePart),
													Expression.Convert(ex.Right, typeof(DateTime?)),
													Expression.Convert(ex.Left,  typeof(DateTime?))),
												typeof(double));

										return new TransformInfo(ConvertExpression(call));
								}
							}

							break;
						}
				}

				return new TransformInfo(e);
			});
		}

		Expression ConvertMethod(MethodCallExpression pi)
		{
			var l = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);
			return l == null ? null : ConvertMethod(pi, l);
		}

		static Expression ConvertMethod(MethodCallExpression pi, LambdaExpression lambda)
		{
			var ef    = lambda.Body.Unwrap();
			var parms = new Dictionary<ParameterExpression,int>(lambda.Parameters.Count);
			var pn    = pi.Method.IsStatic ? 0 : -1;

			foreach (var p in lambda.Parameters)
				parms.Add(p, pn++);

			var pie = ef.Transform(wpi =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= pi.Arguments.Count)
						{
							if (DataContextParam.Type.IsSameOrParentOf(wpi.Type))
							{
								if (DataContextParam.Type != wpi.Type)
									return Expression.Convert(DataContextParam, wpi.Type);
								return DataContextParam;
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						return n < 0 ? pi.Object : pi.Arguments[n];
					}
				}

				return wpi;
			});

			if (pi.Method.ReturnType != pie.Type)
				pie = new ChangeTypeExpression(pie, pi.Method.ReturnType);

			return pie;
		}

		Expression ConvertNew(NewExpression pi)
		{
			var lambda = Expressions.ConvertMember(MappingSchema, pi.Type, pi.Constructor);

			if (lambda != null)
			{
				var ef    = lambda.Body.Unwrap();
				var parms = new Dictionary<string,int>(lambda.Parameters.Count);
				var pn    = 0;

				foreach (var p in lambda.Parameters)
					parms.Add(p.Name, pn++);

				return ef.Transform(wpi =>
				{
					if (wpi.NodeType == ExpressionType.Parameter)
					{
						var pe   = (ParameterExpression)wpi;
						var n    = parms[pe.Name];
						return pi.Arguments[n];
					}

					return wpi;
				});
			}

			return null;
		}

		#endregion

		#region CanBeCompiled

		Expression _lastExpr2;
		bool       _lastResult2;

		bool CanBeCompiled(Expression expr)
		{
			if (_lastExpr2 == expr)
				return _lastResult2;

			var allowedParams = new HashSet<Expression> { ParametersParam };

			var result = null == expr.Find(ex =>
			{
				if (IsServerSideOnly(ex))
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Parameter    :
						return !allowedParams.Contains(ex);

					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)ex;
							foreach (var arg in mc.Arguments)
							{
								if (arg.NodeType == ExpressionType.Lambda)
								{
									var lambda = (LambdaExpression)arg;
									foreach (var prm in lambda.Parameters)
										allowedParams.Add(prm);
								}
							}
							break;
						}
					case QuerySourceReferenceExpression.ExpressionType:
						{
							return true;
						}
				}

				return false;
			});

			_lastExpr2 = expr;
			return _lastResult2 = result;
		}

		#endregion


		#region CanBeConstant

		Expression _lastExpr1;
		bool       _lastResult1;

		bool CanBeConstant(Expression expr)
		{
			if (_lastExpr1 == expr)
				return _lastResult1;

			var result = null == expr.Find(ex =>
			{
				if (ex is BinaryExpression || ex is UnaryExpression /*|| ex.NodeType == ExpressionType.Convert*/)
					return false;

				if (MappingSchema.GetConvertExpression(ex.Type, typeof(DataParameter), false, false) != null)
					return true;

				switch (ex.NodeType)
				{
					case ExpressionType.Constant     :
						{
							var c = (ConstantExpression)ex;

							if (c.Value == null || ex.Type.IsConstantable())
								return false;

							break;
						}

					case ExpressionType.MemberAccess :
						{
							var ma = (MemberExpression)ex;

							var l = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

							if (l != null)
								return l.Body.Unwrap().Find(CanBeConstant) == null;

							if (ma.Member.DeclaringType.IsConstantable() || ma.Member.IsNullableValueMember())
								return false;

							break;
						}

					case ExpressionType.Call         :
						{
							var mc = (MethodCallExpression)ex;

							if (mc.Method.DeclaringType.IsConstantable() || mc.Method.DeclaringType == typeof(object))
								return false;

							var attr = GetExpressionAttribute(mc.Method);

							if (attr != null && !attr.ServerSideOnly)
								return false;

							break;
						}
				}

				return true;
			});


			_lastExpr1 = expr;
			return _lastResult1 = result;
		}

		#endregion

		#region IsServerSideOnly

		Expression _lastExpr3;
		bool       _lastResult3;

		bool IsServerSideOnly(Expression expr)
		{
			if (_lastExpr3 == expr)
				return _lastResult3;

			var result = false;

			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var ex = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, ex.Expression?.Type, ex.Member);

						if (l != null)
						{
							result = IsServerSideOnly(l.Body.Unwrap());
						}
						else
						{
							var attr = GetExpressionAttribute(ex.Member);
							result = attr != null && attr.ServerSideOnly;
						}

						break;
					}

				case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expr;

						if (e.Method.DeclaringType == typeof(Enumerable))
						{
							if (CountBuilder.MethodNames.Contains(e.Method.Name) || e.IsAggregate(MappingSchema))
								result = IsQueryMember(e.Arguments[0]);
						}
						else if (e.IsAggregate(MappingSchema) || e.IsAssociation(MappingSchema))
						{
							result = true;
						}
						else if (e.Method.DeclaringType == typeof(Queryable))
						{
							switch (e.Method.Name)
							{
								case "Any"      :
								case "All"      :
								case "Contains" : result = true; break;
							}
						}
						else
						{
							var l = Expressions.ConvertMember(MappingSchema, e.Object?.Type, e.Method);

							if (l != null)
							{
								result = l.Body.Unwrap().Find(IsServerSideOnly) != null;
							}
							else
							{
								var attr = GetExpressionAttribute(e.Method);
								result = attr != null && attr.ServerSideOnly;
							}
						}

						break;
					}
			}

			_lastExpr3 = expr;
			return _lastResult3 = result;
		}

		static bool IsQueryMember(Expression expr)
		{
			if (expr != null) switch (expr.NodeType)
			{
				case ExpressionType.Parameter    : return true;
				case ExpressionType.MemberAccess : return IsQueryMember(((MemberExpression)expr).Expression);
				case ExpressionType.Call         :
					{
						var call = (MethodCallExpression)expr;

						if (call.Method.DeclaringType == typeof(Queryable))
							return true;

						if (call.Method.DeclaringType == typeof(Enumerable) && call.Arguments.Count > 0)
							return IsQueryMember(call.Arguments[0]);

						return IsQueryMember(call.Object);
					}
			}

			return false;
		}

		#endregion

		#region BuildExpression

		SqlInfo[] ConvertExpressions(IBuildContext context, Expression expression, ConvertFlags queryConvertFlag)
		{
			expression = ConvertExpression(expression);

			switch (expression.NodeType)
			{
				case ExpressionType.New :
					{
						var expr = (NewExpression)expression;

// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable HeuristicUnreachableCode
						if (expr.Members == null)
							return Array<SqlInfo>.Empty;
// ReSharper restore HeuristicUnreachableCode
// ReSharper restore ConditionIsAlwaysTrueOrFalse

						return expr.Arguments
							.Select((arg,i) =>
							{
								var mi = expr.Members[i];
								if (mi is MethodInfo)
									mi = ((MethodInfo)mi).GetPropertyInfo();

								return ConvertExpressions(context, arg, queryConvertFlag).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}

				case ExpressionType.MemberInit :
					{
						var expr = (MemberInitExpression)expression;
						var dic  = TypeAccessor.GetAccessor(expr.Type).Members
							.Select((m,i) => new { m, i })
							.ToDictionary(_ => _.m.MemberInfo, _ => _.i);

						return expr.Bindings
							.Where  (b => b is MemberAssignment)
							.Cast<MemberAssignment>()
							.OrderBy(b => dic[b.Member])
							.Select (a =>
							{
								var mi = a.Member;
								if (mi is MethodInfo)
									mi = ((MethodInfo)mi).GetPropertyInfo();

								return ConvertExpressions(context, a.Expression, queryConvertFlag).Select(si => si.Clone(mi));
							})
							.SelectMany(si => si)
							.ToArray();
					}
			}

			throw new NotImplementedException();

//			var ctx = GetContext(context, expression);
//
//			if (ctx != null && ctx.IsExpression(expression, 0, RequestFor.Object).Result)
//				return ctx.ConvertToSql(expression, 0, queryConvertFlag);
//
//			return new[] { new SqlInfo { Sql = ConvertToSql(context, expression) } };
		}

		public ISqlExpression ConvertToSqlExpression(SelectQuery context, Expression expression)
		{
			var expr = ConvertExpression(expression);
			return ConvertToSql(context, expr);
		}

		private QuerySourceRegistry GetTableSourceRegistry(IQuerySource querySource)
		{
			if (!_registeredTableSources.TryGetValue(querySource, out var value))
			{
				throw new LinqToDBException("Query source not registered.");
			}

			return value;
		}

		public ISqlExpression ConvertToSql(SelectQuery context, Expression expression, bool unwrap = false)
		{
			if (typeof(IToSqlConverter).IsSameOrParentOf(expression.Type))
			{
				var sql = ConvertToSqlConvertible(context, expression);
				if (sql != null)
					return sql;
			}

			if (!PreferServerSide(expression, false))
			{
				if (CanBeConstant(expression))
					return BuildConstant(expression);

				if (CanBeCompiled(expression))
					return BuildParameter(expression).SqlParameter;
			}

			if (unwrap)
				expression = expression.Unwrap();

			switch (expression.NodeType)
			{
				case ExpressionType.AndAlso            :
				case ExpressionType.OrElse             :
				case ExpressionType.Not                :
				case ExpressionType.Equal              :
				case ExpressionType.NotEqual           :
				case ExpressionType.GreaterThan        :
				case ExpressionType.GreaterThanOrEqual :
				case ExpressionType.LessThan           :
				case ExpressionType.LessThanOrEqual    :
					{
						var condition = new SqlSearchCondition();
						BuildSearchCondition(context, expression, condition.Conditions);
						return condition;
					}

				case ExpressionType.And                :
				case ExpressionType.Or                 :
					{
						if (expression.Type == typeof(bool))
							goto case ExpressionType.AndAlso;
						goto case ExpressionType.Add;
					}

				case ExpressionType.Add                :
				case ExpressionType.AddChecked         :
				case ExpressionType.Divide             :
				case ExpressionType.ExclusiveOr        :
				case ExpressionType.Modulo             :
				case ExpressionType.Multiply           :
				case ExpressionType.MultiplyChecked    :
				case ExpressionType.Power              :
				case ExpressionType.Subtract           :
				case ExpressionType.SubtractChecked    :
				case ExpressionType.Coalesce           :
					{
						var e = (BinaryExpression)expression;
						var l = ConvertToSql(context, e.Left);
						var r = ConvertToSql(context, e.Right);
						var t = e.Type;

						switch (expression.NodeType)
						{
							case ExpressionType.Add             :
							case ExpressionType.AddChecked      : return Convert(context, new SqlBinaryExpression(t, l, "+", r, Precedence.Additive));
							case ExpressionType.And             : return Convert(context, new SqlBinaryExpression(t, l, "&", r, Precedence.Bitwise));
							case ExpressionType.Divide          : return Convert(context, new SqlBinaryExpression(t, l, "/", r, Precedence.Multiplicative));
							case ExpressionType.ExclusiveOr     : return Convert(context, new SqlBinaryExpression(t, l, "^", r, Precedence.Bitwise));
							case ExpressionType.Modulo          : return Convert(context, new SqlBinaryExpression(t, l, "%", r, Precedence.Multiplicative));
							case ExpressionType.Multiply:
							case ExpressionType.MultiplyChecked : return Convert(context, new SqlBinaryExpression(t, l, "*", r, Precedence.Multiplicative));
							case ExpressionType.Or              : return Convert(context, new SqlBinaryExpression(t, l, "|", r, Precedence.Bitwise));
							case ExpressionType.Power           : return Convert(context, new SqlFunction(t, "Power", l, r));
							case ExpressionType.Subtract        :
							case ExpressionType.SubtractChecked : return Convert(context, new SqlBinaryExpression(t, l, "-", r, Precedence.Subtraction));
							case ExpressionType.Coalesce        :
								{
									if (r is SqlFunction c)
									{
										if (c.Name == "Coalesce")
										{
											var parms = new ISqlExpression[c.Parameters.Length + 1];

											parms[0] = l;
											c.Parameters.CopyTo(parms, 1);

											return Convert(context, new SqlFunction(t, "Coalesce", parms));
										}
									}

									return Convert(context, new SqlFunction(t, "Coalesce", l, r));
								}
						}

						break;
					}

				case ExpressionType.UnaryPlus      :
				case ExpressionType.Negate         :
				case ExpressionType.NegateChecked  :
					{
						var e = (UnaryExpression)expression;
						var o = ConvertToSql(context, e.Operand);
						var t = e.Type;

						switch (expression.NodeType)
						{
							case ExpressionType.UnaryPlus     : return o;
							case ExpressionType.Negate        :
							case ExpressionType.NegateChecked :
								return Convert(context, new SqlBinaryExpression(t, new SqlValue(-1), "*", o, Precedence.Multiplicative));
						}

						break;
					}

				case ExpressionType.Convert        :
				case ExpressionType.ConvertChecked :
					{
						var e = (UnaryExpression)expression;

						var o = ConvertToSql(context, e.Operand);

						if (e.Method == null && e.IsLifted)
							return o;

						if (e.Type == typeof(bool) && e.Operand.Type == typeof(SqlBoolean))
							return o;

						var t = e.Operand.Type;
						var s = SqlDataType.GetDataType(t);

						if (o.SystemType != null && s.Type == typeof(object))
						{
							t = o.SystemType;
							s = SqlDataType.GetDataType(t);
						}

						if (e.Type == t ||
							t.IsEnumEx()      && Enum.GetUnderlyingType(t)      == e.Type ||
							e.Type.IsEnumEx() && Enum.GetUnderlyingType(e.Type) == t)
							return o;

						return Convert(
							context,
							new SqlFunction(e.Type, "$Convert$", SqlDataType.GetDataType(e.Type), s, o));
					}

				case ExpressionType.Conditional    :
					{
						var e = (ConditionalExpression)expression;
						var s = ConvertToSql(context, e.Test);
						var t = ConvertToSql(context, e.IfTrue);
						var f = ConvertToSql(context, e.IfFalse);

						if (f is SqlFunction c && c.Name == "CASE")
						{
							var parms = new ISqlExpression[c.Parameters.Length + 2];

							parms[0] = s;
							parms[1] = t;
							c.Parameters.CopyTo(parms, 2);

							return Convert(context, new SqlFunction(e.Type, "CASE", parms));
						}

						return Convert(context, new SqlFunction(e.Type, "CASE", s, t, f));
					}

				case ExpressionType.MemberAccess :
					{
						var ma   = (MemberExpression)expression;
						var attr = GetExpressionAttribute(ma.Member);

						if (attr != null)
						{
							var converted = attr.GetExpression(MappingSchema, context, ma,
								e => ConvertToSql(context, e));

							if (converted == null)
							{
								if (attr.ExpectExpression)
								{
									var exp = ConvertToSql(context, ma.Expression);
									converted = Convert(context, attr.GetExpression(ma.Member, exp));
								}
								else
								{
									converted = Convert(context, attr.GetExpression(ma.Member));
								}
							}

							return converted;
						}

						return ConvertMemberAccessToSql(context, ma, null);

						break;

//						var ctx = GetContext(context, expression);
//
//						if (ctx != null)
//						{
//							var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);
//
//							switch (sql.Length)
//							{
//								case 0  : break;
//								case 1  : return sql[0].Sql;
//								default : throw new InvalidOperationException();
//							}
//						}
//
//						break;
					}

				case ExpressionType.Parameter   :
					{
						throw new NotImplementedException();
//						var ctx = GetContext(context, expression);
//
//						if (ctx != null)
//						{
//							var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);
//
//							switch (sql.Length)
//							{
//								case 0  : break;
//								case 1  : return sql[0].Sql;
//								default : throw new InvalidOperationException();
//							}
//						}
//
//						break;
					}

				case ExpressionType.Call        :
					{
						var e = (MethodCallExpression)expression;
						var isAggregation = e.IsAggregate(MappingSchema);
						if ((isAggregation || e.IsQueryable()) && !ContainsBuilder.IsConstant(e))
						{
							if (IsSubQuery(context, e))
								return SubQueryToSql(context, e);

							if (isAggregation || CountBuilder.MethodNames.Contains(e.Method.Name))
							{
								throw new NotImplementedException();
//								var ctx = GetContext(context, expression);
//
//								if (ctx != null)
//								{
//									var sql = ctx.ConvertToSql(expression, 0, ConvertFlags.Field);
//
//									if (sql.Length != 1)
//										throw new InvalidOperationException();
//
//									return sql[0].Sql;
//								}
//
//								break;
							}

							return SubQueryToSql(context, e);
						}

						var expr = ConvertMethod(e);

						if (expr != null)
							return ConvertToSql(context, expr, unwrap);

						var attr = GetExpressionAttribute(e.Method);

						if (attr != null)
						{
							var inlineParameters = DataContext.InlineParameters;

							if (attr.InlineParameters)
								DataContext.InlineParameters = true;

							var sqlExpression = attr.GetExpression(MappingSchema, context, e, _ => ConvertToSql(context, _));
							if (sqlExpression != null)
								return Convert(context, sqlExpression);

							var parms = new List<ISqlExpression>();

							if (e.Object != null)
								parms.Add(ConvertToSql(context, e.Object));

							ParameterInfo[] pis = null;

							for (var i = 0; i < e.Arguments.Count; i++)
							{
								var arg = e.Arguments[i];

								if (arg is NewArrayExpression nae)
								{
									if (pis == null)
										pis = e.Method.GetParameters();

									var p = pis[i];

									if (p.GetCustomAttributesEx(true).OfType<ParamArrayAttribute>().Any())
									{
										parms.AddRange(nae.Expressions.Select(a => ConvertToSql(context, a)));
									}
									else
									{
										parms.Add(ConvertToSql(context, nae));
									}
								}
								else
								{
									parms.Add(ConvertToSql(context, arg));
								}
							}

							DataContext.InlineParameters = inlineParameters;

							return Convert(context, attr.GetExpression(e.Method, parms.ToArray()));
						}

						if (e.Method.IsSqlPropertyMethodEx())
							return ConvertToSql(context, ConvertExpression(expression), unwrap);

						break;
					}

				case ExpressionType.Invoke :
					{
						var pi = (InvocationExpression)expression;
						var ex = pi.Expression;

						if (ex.NodeType == ExpressionType.Quote)
							ex = ((UnaryExpression)ex).Operand;

						if (ex.NodeType == ExpressionType.Lambda)
						{
							var l   = (LambdaExpression)ex;
							var dic = new Dictionary<Expression,Expression>();

							for (var i = 0; i < l.Parameters.Count; i++)
								dic.Add(l.Parameters[i], pi.Arguments[i]);

							var pie = l.Body.Transform(wpi => dic.TryGetValue(wpi, out var ppi) ? ppi : wpi);

							return ConvertToSql(context, pie);
						}

						break;
					}

				case ExpressionType.TypeIs :
					{
						var condition = new SqlSearchCondition();
						BuildSearchCondition(context, expression, condition.Conditions);
						return condition;
					}

				case ChangeTypeExpression.ChangeTypeType :
					return ConvertToSql(context, ((ChangeTypeExpression)expression).Expression);

				case ExpressionType.Extension :
					{
						if (expression.CanReduce)
							return ConvertToSql(context, expression.Reduce());

						break;
					}
			}

			if (expression.Type == typeof(bool) && _convertedPredicates.Add(expression))
			{
				var predicate = ConvertPredicate(context, expression);
				_convertedPredicates.Remove(expression);
				if (predicate != null)
					return new SqlSearchCondition(new SqlCondition(false, predicate));
			}

			throw new LinqException("'{0}' cannot be converted to SQL.", expression);
		}

		private Dictionary<MemberExpression, ISqlExpression> _memberMappingHelper = new Dictionary<MemberExpression, ISqlExpression>(new ExpressionEqualityComparer());

		private ISqlExpression ConvertMemberAccessToSql(SelectQuery selectQuery, MemberExpression ma, MemberInfo mi)
		{
			if (_memberMappingHelper.TryGetValue(ma, out var result))
				return result;

			var transformed = GetMemberTransformation(ma);
			if (transformed != null)
			{
				result = ConvertToSql(transformed.SelectQuery, transformed.Transformation);

				if (!(result is SqlColumn column) || column.Parent != transformed.SelectQuery)
				{
					result = transformed.SelectQuery.Select.Columns[transformed.SelectQuery.Select.Add(result)];
				}
			}
			else
			{
				if (ma.Expression is QuerySourceReferenceExpression reference)
				{
					var querySourceRegistry = GetTableSourceRegistry(reference.QuerySource);
					result = ConvertQuerySourceMemberToSql(querySourceRegistry, ma);
//					result = querySourceRegistry.SelectQuery.Select.Columns[querySourceRegistry.SelectQuery.Select.Add(result)];
				}
				else if (ma.Expression is MemberExpression subMemberExpression)
				{
					result = ConvertMemberAccessToSql(selectQuery, subMemberExpression, mi ?? ma.Member);
				}
			}

			if (mi == null)
				_memberMappingHelper.Add(ma, result);

			return result;
		}
		
		//TODO: Not making it generalized to be ready for proper refactoring
		private ISqlExpression ConvertQuerySourceMemberToSql(QuerySourceRegistry querySourceRegistry, MemberExpression memberExpression)
		{
			ISqlExpression result;
			switch (querySourceRegistry.QuerySource)
			{
				case TableSource ts:
					{
						var sqlTable = (SqlTable)querySourceRegistry.TableSource.Source;
						SqlField field;
						if (!sqlTable.Fields.TryGetValue(memberExpression.Member.Name, out field))
							throw new LinqToDBException($"Can not find field for expression '{memberExpression}'");
						result = field;
						break;
					}
				case UnionClause uc:
					{
						throw new NotImplementedException();
						break;
					}
				case SetQuerySource setQuerySource:
					{
						if (!_setSelectors.TryGetValue(setQuerySource.QuerySource, out var setRegistration))
							throw new LinqToDBException("Set source is not registered");
						var setSource = GetTableSourceRegistry(setQuerySource.QuerySource);

						ISqlExpression seq1Sql;
						ISqlExpression seq2Sql;

						var seq1Ts = GetTableSourceRegistry(setQuerySource.QuerySource.Sequence1.GetQuerySource());
						var seq2Ts = GetTableSourceRegistry(setQuerySource.QuerySource.Sequence2.GetQuerySource());

						if (!setRegistration.Sequence1Transformations.TryGetValue(memberExpression,
							out var seq1Transformation))
						{
							seq1Sql = new SqlValue(memberExpression.Type, null);
						}
						else
						{
							seq1Sql = ConvertToSql(null, seq1Transformation.Transformation);
						}

						seq1Sql = seq1Ts.SelectQuery.Select.Columns[seq1Ts.SelectQuery.Select.AddNew(seq1Sql)];
						result = seq1Sql;

//						result = setSource.SelectQuery.Select.Columns[setSource.SelectQuery.Select.AddNew(seq1Sql)];

						if (!setRegistration.Sequence2Transformations.TryGetValue(memberExpression,
							out var seq2Transformation))
						{
							seq2Sql = new SqlValue(memberExpression.Type, null);
						}
						else
						{
							seq2Sql = ConvertToSql(null, seq2Transformation.Transformation);
						}

						seq2Sql = seq2Ts.SelectQuery.Select.Columns[seq2Ts.SelectQuery.Select.AddNew(seq2Sql)];
					
						break;
					}

				default:
					throw new NotImplementedException();
			}

			return result;
		}


/*
		private ISqlExpression ConvertMemberAccessToSql(SelectQuery context, MemberExpression ma, bool generateColumn)
		{
			if (_memberMappingHelper.TryGetValue(ma, out var result))
				return result;

			QuerySourceReferenceExpression realOwner = null;
			Expression current = ma.Expression;
			List<MemberTransformationInfo> sourcesStack = new List<MemberTransformationInfo>();

			while (current != null)
			{
				if (current is QuerySourceReferenceExpression reference)
				{
					realOwner = reference;
					break;
				}
				else if (current.NodeType == ExpressionType.MemberAccess)
				{
					var transformed = GetMemberTransformation(((MemberExpression)current).Member);
					if (transformed == null)
						break;
					sourcesStack.Add(transformed);
					current = transformed.Transformation;
				}
			}

			if (realOwner == null)
				throw new LinqToDBException($"Expression '{ma}' can not be converted to SQL");

			var querySourceRegistry = GetTableSourceRegistry(realOwner.QuerySource);

			result = realOwner.QuerySource.ConvertToSql(querySourceRegistry.TableSource.Source, ma);

//			for (int i = sourcesStack.Count - 1; i >= 0; i--)
//			{
//				if (querySourceRegistry.TableSource != sourcesStack[i].TableSource)
//				{
//					var q = sourcesStack[i].SelectQuery;
//					result = q.Select.Columns[q.Select.Add(result)];
//				}
//			}

			_memberMappingHelper.Add(ma, result);

			return result;
		}
*/

		ISqlExpression ConvertToSqlConvertible(SelectQuery context, Expression expression)
		{
			var l = Expression.Lambda<Func<IToSqlConverter>>(expression);
			var f = l.Compile();
			var c = f();

			return c.ToSql(expression);
		}

		readonly HashSet<Expression> _convertedPredicates = new HashSet<Expression>();

		#endregion

		#region PreferServerSide

		bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var pi = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Expression?.Type, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Transform(wpi => wpi == l.Parameters[0] ? pi.Expression : wpi);

							return info.Find(e => PreferServerSide(e, enforceServerSide)) != null;
						}

						var attr = GetExpressionAttribute(pi.Member);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return l.Body.Unwrap().Find(e => PreferServerSide(e, enforceServerSide)) != null;

						var attr = GetExpressionAttribute(pi.Method);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr);
					}
			}

			return false;
		}

		#endregion

		#region Build Parameter

		readonly Dictionary<Expression,ParameterAccessor> _parameters = new Dictionary<Expression,ParameterAccessor>();

		public readonly HashSet<Expression> AsParameters = new HashSet<Expression>();

		internal enum BuildParameterType
		{
			Default,
			InPredicate
		}

		ParameterAccessor BuildParameter(Expression expr, ExpressionBuilder.BuildParameterType buildParameterType = ExpressionBuilder.BuildParameterType.Default)
		{
			if (_parameters.TryGetValue(expr, out var p))
				return p;

			string name = null;

			var newExpr = ReplaceParameter(_expressionAccessors, expr, nm => name = nm);

			if (!DataContext.SqlProviderFlags.IsParameterOrderDependent)
				foreach (var accessor in _parameters)
					if (accessor.Key.EqualsTo(expr, new Dictionary<Expression, QueryableAccessor>(), compareConstantValues: true))
						p = accessor.Value;

			if (p == null)
			{
				LambdaExpression convertExpr = null;

				if (buildParameterType != ExpressionBuilder.BuildParameterType.InPredicate)
				{
					convertExpr = MappingSchema.GetConvertExpression(
						newExpr.DataType,
						newExpr.DataType.WithSystemType(typeof(DataParameter)), createDefault: false);

					if (convertExpr != null)
					{
						var body = convertExpr.GetBody(newExpr.ValueExpression);

						newExpr.ValueExpression    = Expression.PropertyOrField(body, "Value");
						newExpr.DataTypeExpression = Expression.PropertyOrField(body, "DataType");
						newExpr.DbTypeExpression   = Expression.PropertyOrField(body, "DbType");
					}
				}

				p = CreateParameterAccessor(
					DataContext, newExpr.ValueExpression, newExpr.DataTypeExpression, newExpr.DbTypeExpression, expr, ExpressionParam, ParametersParam, name, buildParameterType, expr: convertExpr);
				CurrentSqlParameters.Add(p);
			}

			_parameters.Add(expr, p);

			return p;
		}

		class ValueTypeExpression
		{
			public Expression ValueExpression;
			public Expression DataTypeExpression;
			public Expression DbTypeExpression;

			public DbDataType DataType;
		}

		ValueTypeExpression ReplaceParameter(IDictionary<Expression,Expression> expressionAccessors, Expression expression, Action<string> setName)
		{
			var result = new ValueTypeExpression
			{
				DataType           = new DbDataType(expression.Type),
				DataTypeExpression = Expression.Constant(DataType.Undefined),
				DbTypeExpression   = Expression.Constant(null, typeof(string))
			};

			var unwrapped = expression.Unwrap();
			if (unwrapped.NodeType == ExpressionType.MemberAccess)
			{
				var ma = (MemberExpression)unwrapped;
				setName(ma.Member.Name);
			}

			result.ValueExpression = expression.Transform(expr =>
			{
				if (expr.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)expr;

					if (!expr.Type.IsConstantable() || AsParameters.Contains(c))
					{
						if (expressionAccessors.TryGetValue(expr, out var val))
						{
							expr = Expression.Convert(val, expr.Type);

							if (expression.NodeType == ExpressionType.MemberAccess)
							{
								var ma = (MemberExpression)expression;

								var mt = GetMemberDataType(ma.Member);

								if (mt.DataType != DataType.Undefined)
								{
									result.DataType.WithDataType(mt.DataType);
									result.DataTypeExpression = Expression.Constant(mt.DataType);
								}

								if (mt.DbType != null)
								{
									result.DataType.WithDbType(mt.DbType);
									result.DbTypeExpression = Expression.Constant(mt.DbType);
								}

								setName(ma.Member.Name);
							}
						}
					}
				}

				return expr;
			});

			return result;
		}

		#endregion


		internal ISqlExpression SubQueryToSql(SelectQuery context, MethodCallExpression expression)
		{
			throw new NotImplementedException();
//
//			var sequence = GetSubQuery(context, expression);
//			var subSql   = sequence.GetSubQuery(context);
//
//			if (subSql == null)
//			{
//				var query    = context.SelectQuery;
//				var subQuery = sequence.SelectQuery;
//
//				// This code should be moved to context.
//				//
//				if (!query.GroupBy.IsEmpty && !subQuery.Where.IsEmpty)
//				{
//					var fromGroupBy = sequence.SelectQuery.Properties
//						.OfType<Tuple<string,SelectQuery>>()
//						.Any(p => p.Item1 == "from_group_by" && ReferenceEquals(p.Item2, context.SelectQuery));
//
//					if (fromGroupBy)
//					{
//						if (subQuery.Select.Columns.Count == 1 &&
//							subQuery.Select.Columns[0].Expression.ElementType == QueryElementType.SqlFunction &&
//							subQuery.GroupBy.IsEmpty && !subQuery.Select.HasModifier && !subQuery.HasUnion &&
//							subQuery.Where.SearchCondition.Conditions.Count == 1)
//						{
//							var cond = subQuery.Where.SearchCondition.Conditions[0];
//
//							if (cond.Predicate.ElementType == QueryElementType.ExprExprPredicate && query.GroupBy.Items.Count == 1 ||
//								cond.Predicate.ElementType == QueryElementType.SearchCondition &&
//								query.GroupBy.Items.Count == ((SqlSearchCondition)cond.Predicate).Conditions.Count)
//							{
//								var func = (SqlFunction)subQuery.Select.Columns[0].Expression;
//
//								if (CountBuilder.MethodNames.Contains(func.Name))
//									return SqlFunction.CreateCount(func.SystemType, query);
//							}
//						}
//					}
//				}
//
//				subSql = sequence.SelectQuery;
//			}
//
//			return subSql;
		}

		#region Build Constant

		readonly Dictionary<Expression,SqlValue> _constants = new Dictionary<Expression,SqlValue>();

		SqlValue BuildConstant(Expression expr)
		{
			if (_constants.TryGetValue(expr, out var value))
				return value;

			var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expr, typeof(object)));
			var v      = lambda.Compile()();

			if (v != null && v.GetType().IsEnumEx())
			{
				var attrs = v.GetType().GetCustomAttributesEx(typeof(Sql.EnumAttribute), true);

				if (attrs.Length == 0)
					v = MappingSchema.EnumToValue((Enum)v);
			}

			value = MappingSchema.GetSqlValue(expr.Type, v);

			_constants.Add(expr, value);

			return value;
		}

		#endregion

		#region IsSubQuery

		bool IsSubQuery(SelectQuery context, MethodCallExpression call)
		{
			var isAggregate = call.IsAggregate(MappingSchema);

			if (isAggregate || call.IsQueryable())
			{
				var info = new BuildInfo((IBuildContext)null, call, new SelectQuery { ParentSelect = context });

				if (!IsSequence(info))
					return false;

				var arg = call.Arguments[0];

				if (isAggregate)
					while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression)arg).Method.Name == "Select")
						arg = ((MethodCallExpression)arg).Arguments[0];

				arg = arg.SkipPathThrough();

				var mc = arg as MethodCallExpression;

				while (mc != null)
				{
					if (!mc.IsQueryable())
						return GetTableFunctionAttribute(mc.Method) != null;

					mc = mc.Arguments[0] as MethodCallExpression;
				}

				return arg.NodeType == ExpressionType.Call || IsSubQuerySource(context, arg);
			}

			return false;
		}

		bool IsSubQuerySource(SelectQuery context, Expression expr)
		{
			if (expr == null)
				return false;

			return false;
//			var ctx = GetContext(context, expr);
//
//			if (ctx != null && ctx.IsExpression(expr, 0, RequestFor.Object).Result)
//				return true;
//
//			while (expr != null)
//			{
//				switch (expr)
//				{
//					case MemberExpression me:
//						expr = me.Expression;
//						continue;
//					case MethodCallExpression mc when mc.IsQueryable("AsQueryable"):
//						expr = mc.Arguments[0];
//						continue;
//				}
//
//				break;
//			}
//
//			return expr != null && expr.NodeType == ExpressionType.Constant;
		}

		bool IsGroupJoinSource(IBuildContext context, MethodCallExpression call)
		{
			if (!call.IsQueryable() || CountBuilder.MethodNames.Contains(call.Method.Name))
				return false;

			Expression expr = call;

			while (expr.NodeType == ExpressionType.Call)
				expr = ((MethodCallExpression)expr).Arguments[0];

			throw new NotImplementedException();
//			var ctx = GetContext(context, expr);
//
//			return ctx != null && ctx.IsExpression(expr, 0, RequestFor.GroupJoin).Result;
		}

		#endregion

		bool IsSequence(BuildInfo buildInfo)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			return ModelTranslator.IsSequence(buildInfo.Expression);
		}

	}


}
