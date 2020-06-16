using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Contains reference to filter function defined by <see cref="EntityMappingBuilder{T}.HasQueryFilter(System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}})"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class QueryFilterAttribute : Attribute
	{
#pragma warning disable CS1658, CS1584 // https://github.com/dotnet/csharplang/issues/401
		/// <summary>
		/// Filter function of type <see cref="System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}}"/>.
		/// </summary>
#pragma warning restore CS1658, CS1584
		public Delegate? FilterFunc { get; set; }
	}
}
