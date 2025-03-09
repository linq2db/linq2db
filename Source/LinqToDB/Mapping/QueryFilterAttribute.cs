using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Contains reference to filter function defined by <see cref="EntityMappingBuilder{T}.HasQueryFilter(Expression{Func{T, IDataContext, bool}})"/>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
	public class QueryFilterAttribute : MappingAttribute
	{
		/// <summary>
		/// Filter LambdaExpression. <code>Expression&lt;Func&lt;TEntity, IDataContext, bool&gt;&gt;</code>
		/// <para>
		/// For example (e, db) => e.IsDeleted == false "/>
		/// </para>
		/// </summary>
		public LambdaExpression? FilterLambda { get; set; }

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
		public Delegate? FilterFunc { get; set; }

		public override string GetObjectID()
		{
			return FormattableString.Invariant($"{IdentifierBuilder.GetObjectID(FilterLambda)}{IdentifierBuilder.GetObjectID(FilterFunc?.Method)}");
		}
	}
}
