using System;
using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	internal interface IBuildContext
	{
#if DEBUG
		string? SqlQueryText  { get; }
		string  Path          { get; }
		int     ContextId     { get; }
#endif

		ExpressionBuilder   Builder             { get; }
		MappingSchema       MappingSchema       { get; }
		Expression?         Expression          { get; }
		SelectQuery         SelectQuery         { get; }
		IBuildContext?      Parent              { get; set; } // TODO: probably not needed
		TranslationModifier TranslationModifier { get; }

		SelectQuery GetResultQuery();

		Type ElementType { get; }

		Expression    MakeExpression(Expression path, ProjectFlags flags);
		/// <summary>
		/// Optional cardinality for associations
		/// </summary>
		bool          IsOptional { get; }

		IBuildContext Clone(CloningContext context);

		void           SetRunQuery<T>(Query<T> query,      Expression expr);

		/// <summary>
		/// Configures element-selection delegates (<c>Query&lt;T&gt;.GetElement</c> and
		/// <c>Query&lt;T&gt;.GetElementAsync</c>) on the query so that scalar operators
		/// (First/FirstOrDefault/Single/SingleOrDefault, sync and async) get the correct
		/// cardinality semantics on top of <c>query.GetResultEnumerable</c>. Default
		/// implementation is a no-op — only contexts that produce scalar results override it.
		/// </summary>
		/// <remarks>
		/// Called by code paths that install their own <c>GetResultEnumerable</c>
		/// (the eager-loading buffer / CteUnion strategies) so the cardinality rules stay
		/// shared between normal and eager-loading execution.
		/// </remarks>
		void           SetElementSelection<T>(Query<T> query);

		IBuildContext? GetContext(Expression   expression, BuildInfo  buildInfo);
		void           SetAlias(string?        alias);
		SqlStatement   GetResultStatement();
		void           Detach();
		bool           IsSingleElement       { get; }
		bool           AutomaticAssociations { get; }
	}
}
