using System;
using System.Linq.Expressions;
using JetBrains.Annotations;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines relation between tables or views.
	/// Could be applied to:
	/// - instance properties and fields;
	/// - instance and static methods.
	/// 
	/// For associations, defined using static methods, <c>this</c> mapping side defined by type of first parameter.
	/// Also, optionally, you can pass data context object as extra method parameter.
	/// 
	/// Based on association type - to one or to multiple records - result type should be target record's mapping type or
	/// <see cref="IEquatable{T}"/> collection.
	/// 
	/// By default associations are used only for joins generation in LINQ queries and will have <c>null</c> value for loaded
	/// records. To load data into association, you should explicitly specify it in your query using <see cref="LinqExtensions.LoadWith{TEntity,TProperty}(System.Linq.IQueryable{TEntity},System.Linq.Expressions.Expression{System.Func{TEntity,TProperty}})"/> method.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
	public class AssociationAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string?      Configuration       { get; set; }

		/// <summary>
		/// Gets or sets comma-separated list of association key members on this side of association.
		/// Those keys will be used for join predicate generation and must be compatible with <see cref="OtherKey"/> keys.
		/// You must specify keys it you do not use custom predicate (see <see cref="ExpressionPredicate"/>).
		/// </summary>
		public string?      ThisKey             { get; set; }

		/// <summary>
		/// Gets or sets comma-separated list of association key members on another side of association.
		/// Those keys will be used for join predicate generation and must be compatible with <see cref="ThisKey"/> keys.
		/// You must specify keys it you do not use custom predicate (see <see cref="ExpressionPredicate"/>).
		/// </summary>
		public string?      OtherKey            { get; set; }

		/// <summary>
		/// Specifies static property or method without parameters, that returns join predicate expression. This predicate will be used together with
		/// <see cref="ThisKey"/>/<see cref="OtherKey"/> join keys, if they are specified.
		/// Predicate expression lambda function takes two parameters: this record and other record and returns boolean result.
		/// </summary>
		public string?      ExpressionPredicate { get; set; }

		/// <summary>
		/// Specifies predicate expression. This predicate will be used together with
		/// <see cref="ThisKey"/>/<see cref="OtherKey"/> join keys, if they are specified.
		/// Predicate expression lambda function takes two parameters: this record and other record and returns boolean result.
		/// </summary>
		public Expression?  Predicate           { get; set; }


		/// <summary>
		/// Specifies static property or method without parameters, that returns IQueryable expression. If is set, other association keys are ignored.
		/// Result of query method should be lambda which takes two parameters: this record, IDataContext and returns IQueryable result.
		/// <para>
		/// <example>
		/// <code>
		/// public class SomeEntity
		/// {
		///     [Association(ExpressionQueryMethod = nameof(OtherImpl), CanBeNull = true)]
		///     public SomeOtherEntity Other { get; set; }
		/// 
		///     public static Expression&lt;Func&lt;SomeEntity, IDataContext, IQueryable&lt;SomeOtherEntity&gt;&gt;&gt; OtherImpl()
		///     {
		///         return (e, db) =&gt; db.GetTable&lt;SomeOtherEntity&gt;().Where(se =&gt; se.Id == e.Id);
		///     }
		/// }
		/// </code>
		/// </example>
		/// </para>
		/// </summary>
		public string?      QueryExpressionMethod { get; set; }

		/// <summary>
		/// Specifies query expression. If is set, other association keys are ignored.
		/// Lambda function takes two parameters: this record, IDataContext and returns IQueryable result.
		/// <para>
		/// <example>
		/// <code>
		/// var Expression&lt;Func&lt;SomeEntity, IDataContext, IQueryable&lt;SomeOtherEntity&gt;&gt;&gt; associationQuery;
		/// <para />
		/// associationQuery = (e, db) =&gt; db.GetTable&lt;SomeOtherEntity&gt;().Where(se =&gt; se.Id == e.Id);
		/// </code>
		/// </example>
		/// </para>
		/// </summary>
		public Expression?  QueryExpression       { get; set; }

		/// <summary>
		/// Specify name of property or field to store association value, loaded using <see cref="LinqExtensions.LoadWith{TEntity,TProperty}(System.Linq.IQueryable{TEntity},System.Linq.Expressions.Expression{System.Func{TEntity,TProperty}})"/> method.
		/// When not specified, current association member will be used.
		/// </summary>
		public string?      Storage             { get; set; }


		private bool? _canBeNull;

		/// <summary>
		/// Defines type of join:
		/// - inner join for <c>CanBeNull = false</c>;
		/// - left join for <c>CanBeNull = true</c>.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool CanBeNull
		{
			get => _canBeNull ?? true;
			set => _canBeNull = value;
		}

		/// <summary>
		/// Defines type of join:
		/// - inner join for <c>CanBeNull = false</c>;
		/// - left join for <c>CanBeNull = true</c>;
		/// - auto detect jon if value is <c>null</c>.
		/// </summary>
		public bool? CanBeNullNullable { get => _canBeNull; set => _canBeNull = value; }

		/// <summary>
		/// This property is not used by linq2db and could be used for informational purposes.
		/// </summary>
		public string?      KeyName             { get; set; }

		/// <summary>
		/// This property is not used by linq2db and could be used for informational purposes.
		/// </summary>
		public string?      BackReferenceName   { get; set; }

		/// <summary>
		/// This property is not used by linq2db and could be used for informational purposes.
		/// </summary>
		public bool         IsBackReference     { get; set; }

		/// <summary>
		/// This property is not used by linq2db and could be used for informational purposes.
		/// </summary>
		public Relationship Relationship        { get; set; }

		/// <summary>
		/// Gets or sets alias for association. Used in SQL generation process.
		/// </summary>
		public string?      AliasName           { get; set; }

		/// <summary>
		/// Returns <see cref="ThisKey"/> value as a list of key member names.
		/// </summary>
		/// <returns>List of key members.</returns>
		public string[] GetThisKeys () { return AssociationDescriptor.ParseKeys(ThisKey);  }

		/// <summary>
		/// Returns <see cref="OtherKey"/> value as a list of key member names.
		/// </summary>
		/// <returns>List of key members.</returns>
		public string[] GetOtherKeys() { return AssociationDescriptor.ParseKeys(OtherKey); }
	}
}
