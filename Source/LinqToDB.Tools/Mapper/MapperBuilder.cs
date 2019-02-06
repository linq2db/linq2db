using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

using JANotNull = JetBrains.Annotations.NotNullAttribute;

namespace LinqToDB.Tools.Mapper
{
	using Extensions;
	using Mapping;
	using Reflection;

	/// <summary>
	/// Builds a mapper that maps an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
	/// </summary>
	/// <typeparam name="TFrom">Type to map from.</typeparam>
	/// <typeparam name="TTo">Type to map to.</typeparam>
	[PublicAPI]
	public class MapperBuilder<TFrom,TTo> : IMapperBuilder
	{
		[JANotNull]
		private MappingSchema _mappingSchema = MappingSchema.Default;

		/// <summary>
		/// Mapping schema.
		/// </summary>
		public MappingSchema MappingSchema
		{
			get => _mappingSchema;
			set => _mappingSchema = value ?? throw new ArgumentNullException(nameof(value), "MappingSchema cannot be null.");
		}

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// Returned expression is compatible to IQueryable.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure]
		public Expression<Func<TFrom,TTo>> GetMapperExpression()
			=> (Expression<Func<TFrom,TTo>>)GetExpressionMapper().GetExpression();

		LambdaExpression IMapperBuilder.GetMapperLambdaExpression()
			=> GetExpressionMapper().GetExpression();

		/// <summary>
		/// Returns a mapper expression to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, JANotNull]
		public Expression<Func<TFrom,TTo,IDictionary<object,object>,TTo>> GetMapperExpressionEx()
			=> (Expression<Func<TFrom,TTo,IDictionary<object,object>,TTo>>)GetExpressionMapper().GetExpressionEx();

		[JANotNull]
		LambdaExpression IMapperBuilder.GetMapperLambdaExpressionEx()
			=> GetExpressionMapper().GetExpressionEx();

		/// <summary>
		/// Returns a mapper to map an object of <i>TFrom</i> type to an object of <i>TTo</i> type.
		/// </summary>
		/// <returns>Mapping expression.</returns>
		[Pure, JANotNull]
		public Mapper<TFrom,TTo> GetMapper()
			=> new Mapper<TFrom,TTo>(this);

		/// <summary>
		/// Sets mapping schema.
		/// </summary>
		/// <param name="schema">Mapping schema to set.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> SetMappingSchema([JANotNull] MappingSchema schema)
		{
			_mappingSchema = schema ?? throw new ArgumentNullException(nameof(schema));
			return this;
		}

		/// <summary>
		/// Filters target members to map.
		/// </summary>
		public Func<MemberAccessor,bool> MemberFilter { get; set; } = _ => true;

		/// <summary>
		/// Adds a predicate to filter target members to map.
		/// </summary>
		/// <param name="predicate">Predicate to filter members to map.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> SetMemberFilter([JANotNull] Func<MemberAccessor,bool> predicate)
		{
			MemberFilter = predicate ?? throw new ArgumentNullException(nameof(predicate));
			return this;
		}

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		public Dictionary<Type,Dictionary<string,string>> FromMappingDictionary { get; set; }

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> FromMapping([JANotNull] Type type, [JANotNull] string memberName, [JANotNull] string mapName)
		{
			if (type       == null) throw new ArgumentNullException(nameof(type));
			if (memberName == null) throw new ArgumentNullException(nameof(memberName));
			if (mapName    == null) throw new ArgumentNullException(nameof(mapName));

			if (FromMappingDictionary == null)
				FromMappingDictionary = new Dictionary<Type,Dictionary<string,string>>();

			if (!FromMappingDictionary.TryGetValue(type, out var dic))
				FromMappingDictionary[type] = dic = new Dictionary<string,string>();

			dic[memberName] = mapName;

			return this;
		}

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> FromMapping<T>(
			[JANotNull] string memberName,
			[JANotNull] string mapName)
			=> FromMapping(typeof(T), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> FromMapping([JANotNull] string memberName, [JANotNull] string mapName)
			=> FromMapping(typeof(TFrom), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> FromMapping([JANotNull] Type type, [JANotNull] IReadOnlyDictionary<string,string> mapping)
		{
			if (type    == null) throw new ArgumentNullException(nameof(type));
			if (mapping == null) throw new ArgumentNullException(nameof(mapping));

			foreach (var item in mapping)
				FromMapping(type, item.Key, item.Value);

			return this;
		}

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> FromMapping<T>([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> FromMapping(typeof(T), mapping);

		/// <summary>
		/// Defines member name mapping for source types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> FromMapping([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> FromMapping(typeof(TFrom), mapping);

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		public Dictionary<Type,Dictionary<string,string>> ToMappingDictionary { get; set; }

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> ToMapping([JANotNull] Type type, [JANotNull] string memberName, string mapName)
		{
			if (ToMappingDictionary == null)
				ToMappingDictionary = new Dictionary<Type,Dictionary<string,string>>();

			if (!ToMappingDictionary.TryGetValue(type, out var dic))
				ToMappingDictionary[type] = dic = new Dictionary<string,string>();

			dic[memberName] = mapName;

			return this;
		}

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> ToMapping<T>(string memberName, string mapName)
			=> ToMapping(typeof(T), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> ToMapping(string memberName, string mapName)
			=> ToMapping(typeof(TTo), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> ToMapping([JANotNull] Type type, [JANotNull] IReadOnlyDictionary<string,string> mapping)
		{
			if (type    == null) throw new ArgumentNullException(nameof(type));
			if (mapping == null) throw new ArgumentNullException(nameof(mapping));

			foreach (var item in mapping)
				ToMapping(type, item.Key, item.Value);

			return this;
		}

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> ToMapping<T>([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> ToMapping(typeof(T), mapping);

		/// <summary>
		/// Defines member name mapping for destination types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> ToMapping([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> ToMapping(typeof(TTo), mapping);

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> Mapping([JANotNull] Type type, [JANotNull] string memberName, [JANotNull] string mapName)
			=> FromMapping(type, memberName, mapName).ToMapping(type, memberName, mapName);

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> Mapping<T>([JANotNull] string memberName, [JANotNull] string mapName)
			=> Mapping(typeof(T), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <param name="memberName">Type member name.</param>
		/// <param name="mapName">Mapping name.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> Mapping(string memberName, string mapName)
			=> Mapping(typeof(TFrom), memberName, mapName).Mapping(typeof(TTo), memberName, mapName);

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <param name="type">Type to map.</param>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> Mapping([JANotNull] Type type, [JANotNull] IReadOnlyDictionary<string,string> mapping)
		{
			if (type    == null) throw new ArgumentNullException(nameof(type));
			if (mapping == null) throw new ArgumentNullException(nameof(mapping));

			foreach (var item in mapping)
				Mapping(type, item.Key, item.Value);

			return this;
		}

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <typeparam name="T">Type to map.</typeparam>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> Mapping<T>([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> Mapping(typeof(T), mapping);

		/// <summary>
		/// Defines member name mapping for source and destination types.
		/// </summary>
		/// <param name="mapping">Mapping parameters.</param>
		/// <returns>Returns this mapper.</returns>
		public MapperBuilder<TFrom,TTo> Mapping([JANotNull] IReadOnlyDictionary<string,string> mapping)
			=> Mapping(typeof(TFrom), mapping).Mapping(typeof(TFrom), mapping);

		/// <summary>
		/// Member mappers.
		/// </summary>
		public List<MemberMapperInfo> MemberMappers { get; set; }

		/// <summary>
		/// Adds member mapper.
		/// </summary>
		/// <typeparam name="T">Type of the member to map.</typeparam>
		/// <param name="toMember">Expression that returns a member to map.</param>
		/// <param name="setter">Expression to set the member.</param>
		/// <returns>Returns this mapper.</returns>
		/// <example>
		/// This example shows how to explicitly convert one value to another.
		/// </example>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> MapMember<T>([JANotNull] Expression<Func<TTo,T>> toMember,
			[JANotNull] Expression<Func<TFrom,T>> setter)
		{
			if (toMember == null) throw new ArgumentNullException(nameof(toMember));
			if (setter   == null) throw new ArgumentNullException(nameof(setter));

			if (MemberMappers == null)
				MemberMappers = new List<MemberMapperInfo>();

			MemberMappers.Add(new MemberMapperInfo { ToMember = toMember, Setter = setter });

			return this;
		}

		/// <summary>
		/// If true, processes object cross references.
		/// if default (null), the <see cref="GetMapperExpression"/> method does not process cross references,
		/// however the <see cref="GetMapperExpressionEx"/> method does.
		/// </summary>
		public bool? ProcessCrossReferences { get; set; }

		/// <summary>
		/// If true, processes object cross references.
		/// </summary>
		/// <param name="doProcess">If true, processes object cross references.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom, TTo> SetProcessCrossReferences(bool? doProcess)
		{
			ProcessCrossReferences = doProcess;
			return this;
		}

		/// <summary>
		/// If true, performs deep copy.
		/// if default (null), the <see cref="GetMapperExpression"/> method does not do deep copy,
		/// however the <see cref="GetMapperExpressionEx"/> method does.
		/// </summary>
		public bool? DeepCopy { get; set; }

		/// <summary>
		/// Type to map from.
		/// </summary>
		public Type FromType => typeof(TFrom);

		/// <summary>
		/// Type to map to.
		/// </summary>
		public Type ToType => typeof(TTo);

		/// <summary>
		/// If true, performs deep copy.
		/// </summary>
		/// <param name="deepCopy">If true, performs deep copy.</param>
		/// <returns>Returns this mapper.</returns>
		[JANotNull]
		public MapperBuilder<TFrom,TTo> SetDeepCopy(bool? deepCopy)
		{
			DeepCopy = deepCopy;
			return this;
		}

		/// <summary>
		/// Gets an instance of <see cref="ExpressionBuilder"/> class.
		/// </summary>
		/// <returns><see cref="ExpressionBuilder"/>.</returns>
		[JANotNull]
		internal ExpressionBuilder GetExpressionMapper()
			=> new ExpressionBuilder(this, MemberMappers?.Select(mm => Tuple.Create(GetMembersInfo(mm.ToMember), mm.Setter)).ToArray());

		/// <summary>
		/// Gets the <see cref="MemberInfo"/>.
		/// </summary>
		/// <param name="expression">The expression to analyze.</param>
		/// <returns>
		/// The <see cref="MemberInfo"/> instance.
		/// </returns>
		[JANotNull, Pure]
		internal static MemberInfo[] GetMembersInfo([JANotNull] LambdaExpression expression)
		{
			if (expression == null) throw new ArgumentNullException(nameof(expression));

			var body = expression.Body;
			if (body is UnaryExpression unary)
				body = unary.Operand;

			return GetMembers(body).Reverse().ToArray();
		}

		[JANotNull, ItemNotNull]
		static IEnumerable<MemberInfo> GetMembers([JANotNull] Expression expression, bool passIndexer = true)
		{
			MemberInfo lastMember = null;

			for (;;)
			{
				switch (expression.NodeType)
				{
					case ExpressionType.Parameter:
						if (lastMember == null)
							goto default;
						yield break;

					case ExpressionType.Call:
						{
							if (lastMember == null)
								goto default;

							var cExpr = (MethodCallExpression)expression;
							var expr = cExpr.Object;

							if (expr == null)
							{
								if (cExpr.Arguments.Count == 0)
									goto default;

								expr = cExpr.Arguments[0];
							}

							if (expr.NodeType != ExpressionType.MemberAccess)
								goto default;

							var member = ((MemberExpression)expr).Member;
							var mType = member.GetMemberType();

							if (lastMember.ReflectedTypeEx() != mType.GetItemType())
								goto default;

							expression = expr;

							break;
						}

					case ExpressionType.MemberAccess:
						{
							var mExpr = (MemberExpression)expression;
							var member = lastMember = mExpr.Member;

							yield return member;

							expression = mExpr.Expression;

							break;
						}

					case ExpressionType.ArrayIndex:
						{
							if (passIndexer)
							{
								expression = ((BinaryExpression)expression).Left;
								break;
							}

							goto default;
						}

					default:
						throw new InvalidOperationException($"Expression '{expression}' is not an association.");
				}
			}
		}
	}
}
