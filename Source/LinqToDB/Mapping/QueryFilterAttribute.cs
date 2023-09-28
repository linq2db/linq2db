using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	using Common.Internal;

	/// <summary>
	/// Contains reference to filter function defined by <see cref="EntityMappingBuilder{T}.HasQueryFilter(Func{IQueryable{T},IDataContext,IQueryable{T}})"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class QueryFilterAttribute : MappingAttribute
	{
		// we cannot use
		// <see cref="System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}}"/>
		// as it produce compiler/documentation errors due https://github.com/dotnet/csharplang/issues/401
		/// <summary>
		/// Filter function of type <see cref="Func{T1, T2, TResult}"/>, where
		/// <list type="bullet">
		/// <item>- T1 and TResult are <see cref="IQueryable{T}"/></item>
		/// <item>- T2 is <see cref="IDataContext"/></item>
		/// </list>
		/// </summary>
		public LambdaExpression? FilterFunc { get; set; }

		public override string GetObjectID()
		{
			return IdentifierBuilder.GetObjectID(FilterFunc).ToString();
		}
	}
}
