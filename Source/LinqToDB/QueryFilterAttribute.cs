using System;
using LinqToDB.Mapping;

namespace LinqToDB
{
	/// <summary>
	/// Contains reference to filter function defined by <see cref="EntityMappingBuilder{T}.HasQueryFilter(System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}})"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class QueryFilterAttribute : Attribute
	{
		/// <summary>
		/// Filter function of type <see cref="System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}}"/>.
		/// </summary>
		public Delegate? FilterFunc { get; set; }
	}
}
