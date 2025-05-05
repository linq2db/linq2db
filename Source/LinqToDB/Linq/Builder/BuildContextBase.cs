﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;

using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Builder
{
	[DebuggerDisplay("{BuildContextDebuggingHelper.GetContextInfo(this)}")]
	abstract class BuildContextBase : IBuildContext
	{

#if DEBUG
		public string SqlQueryText => SelectQuery?.SqlText ?? "";
		public string Path         => this.GetPath();
		public int    ContextId    { get; }
#endif

		protected BuildContextBase(TranslationModifier translationModifier, ExpressionBuilder builder, Type elementType, SelectQuery selectQuery)
		{
			TranslationModifier = translationModifier;
			Builder             = builder;
			ElementType         = elementType;
			SelectQuery         = selectQuery;
#if DEBUG
			ContextId = builder.GenerateContextId();
#endif
		}

		public          ExpressionBuilder Builder       { get; }
		public abstract MappingSchema     MappingSchema { get; }
		public virtual  Expression?       Expression    => null;
		public          SelectQuery       SelectQuery   { get; protected set; }
		public          IBuildContext?    Parent        { get; set; }

		public          TranslationModifier TranslationModifier { get; }
		public virtual  Type                ElementType         { get; }
		public abstract Expression          MakeExpression(Expression path, ProjectFlags flags);

		public abstract void SetRunQuery<T>(Query<T> query, Expression expr);

		public abstract IBuildContext Clone(CloningContext context);

		public abstract SqlStatement GetResultStatement();

		public virtual void SetAlias(string? alias)
		{
		}

		public virtual IBuildContext? GetContext(Expression expression, BuildInfo buildInfo)
		{
			return this;
		}

		public virtual bool IsOptional            => false;
		public virtual bool IsSingleElement       => false;
		public virtual bool AutomaticAssociations => true;

		public virtual void Detach()
		{
		}
	}
}
