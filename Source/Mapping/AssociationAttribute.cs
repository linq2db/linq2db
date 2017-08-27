using System;

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
	/// records. To load data into association, you should explicitly specify it in your query using <see cref="LinqExtensions.LoadWith{T}(ITable{T}, System.Linq.Expressions.Expression{Func{T, object}})"/> method.
	/// </summary>
	[PublicAPI]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false)]
	public class AssociationAttribute : Attribute
	{
		/// <summary>
		/// Creates attribute instance.
		/// </summary>
		public AssociationAttribute()
		{
			CanBeNull = true;
		}

		/// <summary>
		/// Gets or sets mapping schema configuration name, for which this attribute should be taken into account.
		/// <see cref="ProviderName"/> for standard names.
		/// Attributes with <c>null</c> or empty string <see cref="Configuration"/> value applied to all configurations (if no attribute found for current configuration).
		/// </summary>
		public string       Configuration       { get; set; }

		/// <summary>
		/// Gets or sets comma-separated list of association key members on this side of association.
		/// Those keys will be used for join predicate generation and must be compatible with <see cref="OtherKey"/> keys.
		/// You must specify keys it you do not use custom predicate (see <see cref="ExpressionPredicate"/>).
		/// </summary>
		public string       ThisKey             { get; set; }

		/// <summary>
		/// Gets or sets comma-separated list of association key members on another side of association.
		/// Those keys will be used for join predicate generation and must be compatible with <see cref="ThisKey"/> keys.
		/// You must specify keys it you do not use custom predicate (see <see cref="ExpressionPredicate"/>).
		/// </summary>
		public string       OtherKey            { get; set; }

		/// <summary>
		/// Specifies static property or method without parameters, that returns join predicate expression. This predicate will be used together with
		/// <see cref="ThisKey"/>/<see cref="OtherKey"/> join keys, if they are specified.
		/// Predicate expression lambda function takes two parameters: this record and other record and returns boolean result.
		/// </summary>
		public string       ExpressionPredicate { get; set; }

		/// <summary>
		/// Specify name of property or field to store association value, loaded using <see cref="LinqExtensions.LoadWith{T}(ITable{T}, System.Linq.Expressions.Expression{Func{T, object}})"/> method.
		/// When not specified, current association memeber will be used.
		/// </summary>
		public string       Storage             { get; set; }

		/// <summary>
		/// Defines type of join:
		/// - inner join for <c>CanBeNull = false</c>;
		/// - left join for <c>CanBeNull = true</c>.
		/// Default value: <c>true</c>.
		/// </summary>
		public bool         CanBeNull           { get; set; }

		// TODO: V2 - remove?
		/// <summary>
		/// This property is not used by linq2db.
		/// </summary>
		public string       KeyName             { get; set; }

		// TODO: V2 - remove?
		/// <summary>
		/// This property is not used by linq2db.
		/// </summary>
		public string       BackReferenceName   { get; set; }

		// TODO: V2 - remove?
		/// <summary>
		/// This property is not used by linq2db.
		/// </summary>
		public bool         IsBackReference     { get; set; }

		// TODO: V2 - remove?
		/// <summary>
		/// This property is not used by linq2db.
		/// </summary>
		public Relationship Relationship        { get; set; }

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
