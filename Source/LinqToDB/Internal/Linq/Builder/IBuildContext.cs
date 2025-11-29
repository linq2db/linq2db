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
		IBuildContext? GetContext(Expression   expression, BuildInfo  buildInfo);
		void           SetAlias(string?        alias);
		SqlStatement   GetResultStatement();
		void           Detach();
		bool           IsSingleElement       { get; }
		bool           AutomaticAssociations { get; }
	}
}
