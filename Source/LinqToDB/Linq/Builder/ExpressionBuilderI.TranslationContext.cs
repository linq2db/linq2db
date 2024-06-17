using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Linq.Translation;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;

	internal partial class ExpressionBuilder
	{
		class TranslationContext : ITranslationContext
		{
			class SqlExpressionFactory : ISqlExpressionFactory
			{
				readonly ITranslationContext _translationContext;

				public SqlExpressionFactory(ITranslationContext translationContext)
				{
					_translationContext = translationContext;
				}

				public DataOptions DataOptions                              => _translationContext.DataOptions;
				public DbDataType  GetDbDataType(ISqlExpression expression) => _translationContext.GetDbDataType(expression);
				public DbDataType  GetDbDataType(Type           type)       => _translationContext.MappingSchema.GetDbDataType(type);
			}

			public void Init(ExpressionBuilder builder, IBuildContext? currentContext, ColumnDescriptor? currentColumnDescriptor, string? currentAlias)
			{
				Builder                 = builder;
				CurrentContext          = currentContext;
				CurrentColumnDescriptor = currentColumnDescriptor;
				CurrentAlias            = currentAlias;
			}

			public void Cleanup()
			{
				Builder                 = default!;
				CurrentContext          = default!;
				CurrentColumnDescriptor = default!;
				CurrentAlias            = default!;
			}

			public TranslationContext()
			{
				ExpressionFactory = new SqlExpressionFactory(this);
			}

			public ISqlExpressionFactory ExpressionFactory { get; }

			public ExpressionBuilder Builder                 { get; private set; } = default!;
			public IBuildContext?    CurrentContext          { get; private set; }
			public ColumnDescriptor? CurrentColumnDescriptor { get; private set; }
			public string?           CurrentAlias            { get; private set; }

			static ProjectFlags GetProjectFlags(TranslationFlags translationFlags)
			{
				var flags = ProjectFlags.None;
				if (translationFlags.HasFlag(TranslationFlags.Sql))
				{
					flags = flags.SqlFlag();
				}

				if (translationFlags.HasFlag(TranslationFlags.Expression))
				{
					flags = flags.ExpressionFlag();
				}

				if (flags == ProjectFlags.None)
				{
					flags.SqlFlag();
				}

				return flags;
			}

			public Expression Translate(Expression expression, TranslationFlags translationFlags)
			{
				var flags = GetProjectFlags(translationFlags);
				return Builder.ConvertToSqlExpr(CurrentContext, expression, flags, alias : CurrentAlias);
			}

			public MappingSchema MappingSchema => CurrentContext?.MappingSchema ?? throw new InvalidOperationException();
			public DataOptions   DataOptions   => Builder.DataOptions;
			
			public SelectQuery CurrentSelectQuery => CurrentContext?.SelectQuery ?? throw new InvalidOperationException();

			public SqlPlaceholderExpression CreatePlaceholder(SelectQuery selectQuery, ISqlExpression sqlExpression, Expression basedOn)
			{
				return new SqlPlaceholderExpression(selectQuery, sqlExpression, basedOn);
			}

			public SqlErrorExpression CreateErrorExpression(Expression basedOn, string message)
			{
				return new SqlErrorExpression(basedOn, message, basedOn.Type);
			}

			public bool CanBeCompiled(Expression expression, TranslationFlags translationFlags)
			{
				return Builder.CanBeCompiled(expression, translationFlags.HasFlag(TranslationFlags.Expression));
			}

			public bool IsServerSideOnly(Expression expression, TranslationFlags translationFlags)
			{
				return Builder.IsServerSideOnly(expression, translationFlags.HasFlag(TranslationFlags.Expression));
			}

			public bool IsPreferServerSide(Expression expression, TranslationFlags translationFlags)
			{
				return Builder.PreferServerSide(expression, translationFlags.HasFlag(TranslationFlags.Expression));
			}

			public bool CanBeEvaluated(Expression expression)
			{
				return Builder.CanBeEvaluated(expression);
			}

			public object? Evaluate(Expression expression)
			{
				return Builder.Evaluate(expression);
			}

			public bool TryEvaluate(ISqlExpression expression, out object? result)
			{
				var context = new EvaluationContext();
				return expression.TryEvaluateExpression(context, out result);
			}
		}
	}
}
