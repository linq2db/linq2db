using System;

namespace LinqToDB.Expressions
{
	using Mapping;

	/// <summary>
	/// Generic conversions provider.
	/// Implementation class must be generic, as type parameters will be used for conversion initialization in
	/// <see cref="SetInfo(MappingSchema)"/> method.
	/// <example>
	/// // this conversion provider adds conversion from IEnumerable`T to ImmutableList`T for specific T type parameter
	/// class EnumerableToImmutableListConvertProvider&lt;T&gt; : IGenericInfoProvider
	/// {
	///     public void SetInfo(MappingSchema mappingSchema)
	///     {
	///         mappingSchema.SetConvertExpression&lt;IEnumerable&lt;T&gt;,ImmutableList&lt;T&gt;&gt;(
	///             t =&gt; ImmutableList.Create(t.ToArray()));
	///     }
	/// }
	/// </example>
	/// <see cref="MappingSchema.SetGenericConvertProvider(Type)"/> for more details.
	/// </summary>
	public interface IGenericInfoProvider
	{
		/// <summary>
		/// IMplementation should use this method to provide conversions for generic types with type parameters, used
		/// to instantiate instance of current class.
		/// </summary>
		/// <param name="mappingSchema">Mapping schema, to which conversions should be added.</param>
		void SetInfo(MappingSchema mappingSchema);
	}
}
