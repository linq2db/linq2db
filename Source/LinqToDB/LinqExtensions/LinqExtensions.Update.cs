using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Async;
using LinqToDB.Internal.Async;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB
{

	public static partial class LinqExtensions
	{
		#region Update against ITable<T> target

		/// <summary>
		/// Builds an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<UpdateOutput<TTarget>> UpdateWithOutput<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
			where TTarget: class
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr);
		}

		/// <summary>
		/// Builds an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>An async sequence that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : class
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{TSource,TTarget}(IQueryable{TSource},ITable{TTarget},Expression{Func{TSource,TTarget}})"/>
		/// into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<UpdateOutput<TTarget>[]> UpdateWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
			where TTarget : class
		{
			return source.UpdateWithOutputAsync(target, setter).ToArrayAsync(token);
		}

		/// <summary>
		/// Builds an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TTarget : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr);
		}

		/// <summary>
		/// Builds an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>Async sequence of projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
			where TTarget : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{TSource,TTarget,TOutput}(IQueryable{TSource},ITable{TTarget},Expression{Func{TSource,TTarget}},Expression{Func{TSource,TTarget,TTarget,TOutput}})"/>
		/// into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<TOutput[]> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression,
							CancellationToken token)
			where TTarget : class
		{
			return source.UpdateWithOutputAsync(target, setter, outputExpression).ToArrayAsync(token);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                ITable<TTarget>                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
			                ITable<TTarget>                   outputTable,
			                CancellationToken                 token = default)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/> and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>The number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                ITable<TOutput>                                   outputTable,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TTarget : class
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/> and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                ITable<TTarget>                                   target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
			                ITable<TOutput>                                   outputTable,
			                Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression,
			                CancellationToken                                 token = default)
			where TTarget : class
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TTarget>)target).Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update against Expression target

		/// <summary>
		/// Builds an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<UpdateOutput<TTarget>> UpdateWithOutput<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>An async sequence that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<TTarget>> UpdateWithOutputAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<TTarget>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{TSource,TTarget}(IQueryable{TSource},Expression{Func{TSource,TTarget}},Expression{Func{TSource,TTarget}})"/>
		/// into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<UpdateOutput<TTarget>[]> UpdateWithOutputAsync<TSource, TTarget>(
							IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync(target, setter).ToArrayAsync(token);
		}

		/// <summary>
		/// Builds an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>Async sequence of projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							this IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (target           == null) throw new ArgumentNullException(nameof(target));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, target, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{TSource,TTarget,TOutput}(IQueryable{TSource},Expression{Func{TSource,TTarget}},Expression{Func{TSource,TTarget}},Expression{Func{TSource,TTarget,TTarget,TOutput}})"/>
		/// into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<TOutput[]> UpdateWithOutputAsync<TSource, TTarget, TOutput>(
							IQueryable<TSource> source,
							Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
							Expression<Func<TSource, TTarget, TTarget, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync(target, setter, outputExpression).ToArrayAsync(token);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
							ITable<TTarget>					  outputTable)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget>(
			                this IQueryable<TSource>          source,
			                Expression<Func<TSource,TTarget>> target,
			[InstantHandle] Expression<Func<TSource,TTarget>> setter,
							ITable<TTarget>					  outputTable,
							CancellationToken                 token = default)
			where TTarget : class
		{
			if (source ==      null) throw new ArgumentNullException(nameof(source));
			if (target ==      null) throw new ArgumentNullException(nameof(target));
			if (setter ==      null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TTarget>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/> and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>The number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							ITable<TOutput>					                  outputTable,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement that targets the row selected by <paramref name="target"/> and uses <paramref name="source"/> as the driving query.
		/// Projects provider output into <typeparamref name="TOutput"/> and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">
		/// Target selection expression.
		/// Provider translates it to a target table reference (or a table expression) for the UPDATE statement.
		/// </param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is a <typeparamref name="TSource"/> record.
		/// The expression must be a <typeparamref name="TTarget"/> record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="TSource"/> source, <typeparamref name="TTarget"/> deleted, <typeparamref name="TTarget"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected target records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<TSource,TTarget,TOutput>(
			                this IQueryable<TSource>                          source,
			                Expression<Func<TSource,TTarget>>                 target,
			[InstantHandle] Expression<Func<TSource,TTarget>>                 setter,
							ITable<TOutput>					                  outputTable,
							Expression<Func<TSource,TTarget,TTarget,TOutput>> outputExpression,
							CancellationToken                                 token = default)
			where TOutput : class
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (target ==           null) throw new ArgumentNullException(nameof(target));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputTable ==      null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, target, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(target),
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update from source

		/// <summary>
		/// Builds an UPDATE statement for records produced by <paramref name="source"/>.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<UpdateOutput<T>> UpdateWithOutput<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement for records produced by <paramref name="source"/>.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>An async sequence that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter),
				currentSource.Expression,
				Expression.Quote(setter));

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{T}(IQueryable{T},Expression{Func{T,T}})"/> into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<UpdateOutput<T>[]> UpdateWithOutputAsync<T>(
					        IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync(setter).ToArrayAsync(token);
		}

		/// <summary>
		/// Builds an UPDATE statement for records produced by <paramref name="source"/>.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                Expression<Func<T,T,TOutput>> outputExpression)
		{
			if (source ==           null) throw new ArgumentNullException(nameof(source));
			if (setter ==           null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement for records produced by <paramref name="source"/>.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>Async sequence of projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							Expression<Func<T, T, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, setter, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{T,TOutput}(IQueryable{T},Expression{Func{T,T}},Expression{Func{T,T,TOutput}})"/> into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<TOutput[]> UpdateWithOutputAsync<T, TOutput>(
					        IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
							Expression<Func<T, T, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync(setter, outputExpression).ToArrayAsync(token);
		}

		/// <summary>
		/// Executes an UPDATE statement for records produced by <paramref name="source"/> and writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter,
			                ITable<T>             outputTable)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<T>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement for records produced by <paramref name="source"/> and writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T>(
			           this IQueryable<T>         source,
			[InstantHandle] Expression<Func<T,T>> setter,
			                ITable<T>             outputTable,
			                CancellationToken     token = default)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (setter      == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<T>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes an UPDATE statement for records produced by <paramref name="source"/>,
		/// projects provider output into <typeparamref name="TOutput"/>, and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement for records produced by <paramref name="source"/>,
		/// projects provider output into <typeparamref name="TOutput"/>, and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">A query that identifies records to update.</param>
		/// <param name="setter">
		/// Update setter expression.
		/// The parameter is the updated record.
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T,TOutput>(
			           this IQueryable<T>                 source,
			[InstantHandle] Expression<Func<T,T>>         setter,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression,
			                CancellationToken             token = default)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (setter           == null) throw new ArgumentNullException(nameof(setter));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, setter, outputTable, outputExpression),
				currentSource.Expression,
				Expression.Quote(setter),
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region IUpdatable

		/// <summary>
		/// Builds an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <returns>A query that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel, Pure]
		public static IEnumerable<UpdateOutput<T>> UpdateWithOutput<T>(this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query.
		/// Returns per-row output with old/new images when supported by the provider.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <returns>An async sequence that yields <see cref="UpdateOutput{T}"/> rows for affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<UpdateOutput<T>> UpdateWithOutputAsync<T>(
					   this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source),
				currentSource.Expression);

			return currentSource.CreateQuery<UpdateOutput<T>>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{T}(IUpdatable{T})"/> into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<UpdateOutput<T>[]> UpdateWithOutputAsync<T>(
					        IUpdatable<T> source,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync().ToArrayAsync(token);
		}

		/// <summary>
		/// Builds an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>A query that yields projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IEnumerable<TOutput> UpdateWithOutput<T,TOutput>(
			this IUpdatable<T>            source,
			Expression<Func<T,T,TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query         = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsEnumerable();
		}

		/// <summary>
		/// Builds an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query.
		/// Projects provider output into <typeparamref name="TOutput"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <returns>An async sequence that yields projected output rows.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// <item>Firebird 2.5+ (prior to version 5 returns only one record; database limitation)</item>
		/// <item>PostgreSQL (v18+ required to access data from <c>deleted</c> table)</item>
		/// <item>SQLite 3.35+  (doesn't support old data; database limitation)</item>
		/// </list>
		/// Execution is deferred until enumeration and the method is terminal.
		/// Output availability and exact semantics are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static IAsyncEnumerable<TOutput> UpdateWithOutputAsync<T, TOutput>(
					   this IUpdatable<T> source,
							Expression<Func<T, T, TOutput>> outputExpression)
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutput, source, outputExpression),
				currentSource.Expression,
				Expression.Quote(outputExpression));

			return currentSource.CreateQuery<TOutput>(expr).AsAsyncEnumerable();
		}

		/// <summary>
		/// Obsolete: materializes <see cref="UpdateWithOutputAsync{T,TOutput}(IUpdatable{T},Expression{Func{T,T,TOutput}})"/> into an array.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with IAsyncEnumerable return type. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static ValueTask<TOutput[]> UpdateWithOutputAsync<T, TOutput>(
					        IUpdatable<T> source,
							Expression<Func<T, T, TOutput>> outputExpression,
							CancellationToken token)
		{
			return source.UpdateWithOutputAsync(outputExpression).ToArrayAsync(token);
		}

		/// <summary>
		/// Executes an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query and writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<T>(
			           this IUpdatable<T>         source,
			                ITable<T>             outputTable)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<T>)outputTable).Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query and writes output rows into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T>(
			           this IUpdatable<T>         source,
			                ITable<T>             outputTable,
			                CancellationToken     token = default)
			where T : class
		{
			if (source      == null) throw new ArgumentNullException(nameof(source));
			if (outputTable == null) throw new ArgumentNullException(nameof(outputTable));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable),
				currentSource.Expression,
				((IQueryable<T>)outputTable).Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query,
		/// projects provider output into <typeparamref name="TOutput"/>, and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <returns>The number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int UpdateWithOutputInto<T,TOutput>(
			           this IUpdatable<T>                 source,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an UPDATE statement for an already configured <see cref="IUpdatable{T}"/> query,
		/// projects provider output into <typeparamref name="TOutput"/>, and writes it into <paramref name="outputTable"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TOutput">Output table record type.</typeparam>
		/// <param name="source">A configured updatable query.</param>
		/// <param name="outputExpression">
		/// Output projection expression.
		/// Parameters: (<typeparamref name="T"/> deleted, <typeparamref name="T"/> inserted).
		/// The expression must be a record constructor (or object initializer) with member initializers.
		/// </param>
		/// <param name="outputTable">Table that receives output rows.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>A task that completes with the number of affected records.</returns>
		/// <remarks>
		/// Database support:
		/// <list type="bullet">
		/// <item>SQL Server 2005+</item>
		/// </list>
		/// Execution is immediate and the method is terminal.
		/// Output availability and exact behavior are provider-defined.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static Task<int> UpdateWithOutputIntoAsync<T,TOutput>(
			           this IUpdatable<T>                 source,
			                ITable<TOutput>               outputTable,
			                Expression<Func<T,T,TOutput>> outputExpression,
			                CancellationToken             token = default)
			where TOutput : class
		{
			if (source           == null) throw new ArgumentNullException(nameof(source));
			if (outputTable      == null) throw new ArgumentNullException(nameof(outputTable));
			if (outputExpression == null) throw new ArgumentNullException(nameof(outputExpression));

			var query = ((Updatable<T>)source).Query;
			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(UpdateWithOutputInto, source, outputTable, outputExpression),
				currentSource.Expression,
				((IQueryable<TOutput>)outputTable).Expression,
				Expression.Quote(outputExpression));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		#endregion

		#region Update

		/// <summary>
		/// Obsolete: executes an update-from-source statement that targets <paramref name="target"/>.
		/// Use the overload that takes a target selection lambda.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with lambda argument for target parameter. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static int Update<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTarget.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
	}

		/// <summary>
		/// Obsolete: executes an update-from-source statement that targets <paramref name="target"/>.
		/// Use the overload that takes a target selection lambda.
		/// </summary>
		/// <remarks>
		/// This overload will be removed in version 7.
		/// </remarks>
		// TODO: Remove in v7
		[Obsolete("Use overload with lambda argument for target parameter. API will be removed in version 7"), EditorBrowsable(EditorBrowsableState.Never)]
		public static Task<int> UpdateAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
							ITable<TTarget> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
			where TTarget : notnull
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTarget.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, ((IQueryable<TTarget>)target).Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
}

		/// <summary>
		/// Executes update operation using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		/// <remarks>
		/// Execution is immediate and the method is terminal.
		/// SQL semantics are represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=DML; Execution=Immediate; Composability=Terminal; Affects=DmlStatement; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		public static int Update<T>(this IQueryable<T> source, [InstantHandle] Expression<Func<T, T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, T>> setter,
			CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(
							this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, bool>> predicate,
			[InstantHandle] Expression<Func<T, T>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdatePredicateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously using source query as record filter with additional filter expression.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="predicate">Filter expression, to specify what records from source query should be updated.</param>
		/// <param name="setter">Update expression. Uses updated record as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(
					   this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, bool>> predicate,
			[InstantHandle] Expression<Func<T, T>> setter,
			CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (predicate == null) throw new ArgumentNullException(nameof(predicate));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdatePredicateSetter.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(predicate), Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes update operation for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<T>(this IUpdatable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var query = ((Updatable<T>)source).Query;

			var currentSource = query.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateUpdatable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes update operation asynchronously for already configured update query.
		/// </summary>
		/// <typeparam name="T">Updated table record type.</typeparam>
		/// <param name="source">Update query.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<T>(this IUpdatable<T> source, CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var q = ((Updatable<T>)source).Query;

			var currentSource = q.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateUpdatable.MakeGenericMethod(typeof(T)),
				currentSource.Expression);

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>
		/// Executes an update-from-source statement using <paramref name="source"/> as the driving query,
		/// <paramref name="target"/> to select the target row, and <paramref name="setter"/> to produce new values.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <returns>Number of updated records.</returns>
		public static int Update<TSource, TTarget>(
							this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTargetFuncSetter.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, Expression.Quote(target), Expression.Quote(setter));

			return currentSource.Execute<int>(expr);
		}

		/// <summary>
		/// Executes an update-from-source statement asynchronously using <paramref name="source"/> as the driving query,
		/// <paramref name="target"/> to select the target row, and <paramref name="setter"/> to produce new values.
		/// </summary>
		/// <typeparam name="TSource">Source query record type.</typeparam>
		/// <typeparam name="TTarget">Target table mapping class.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table selection expression.</param>
		/// <param name="setter">Update expression. Uses record from source query as parameter. Expression supports only target table record new expression with field initializers.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Number of updated records.</returns>
		public static Task<int> UpdateAsync<TSource, TTarget>(
							this IQueryable<TSource> source,
			[InstantHandle] Expression<Func<TSource, TTarget>> target,
			[InstantHandle] Expression<Func<TSource, TTarget>> setter,
			CancellationToken token = default)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var currentSource = source.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.UpdateTargetFuncSetter.MakeGenericMethod(typeof(TSource), typeof(TTarget)),
				currentSource.Expression, Expression.Quote(target), Expression.Quote(setter));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		internal sealed class Updatable<T> : IUpdatable<T>
		{
			public Updatable(IQueryable<T> query)
			{
				Query = query;
			}

			public IQueryable<T> Query;
		}

		/// <summary>
		/// Converts an <see cref="IQueryable{T}"/> into an <see cref="IUpdatable{T}"/> pipeline that can be configured with <see cref="Set{T,TV}(IQueryable{T},Expression{Func{T,TV}},Expression{Func{T,TV}})"/>
		/// and executed by <see cref="Update{T}(IUpdatable{T})"/>.
		/// </summary>
		/// <typeparam name="T">Query record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <returns>An updatable query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> AsUpdatable<T>(this IQueryable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));

			var currentSource = source.ProcessIQueryable();

			var query = currentSource.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.AsUpdatable.MakeGenericMethod(typeof(T)),
					currentSource.Expression));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="update">Value expression that produces the new column value. The parameter is the updated record.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[InstantHandle] Expression<Func<T, TV>> update)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update == null) throw new ArgumentNullException(nameof(update));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryablePrev.MakeGenericMethod(typeof(T), typeof(TV)),
				currentSource.Expression, Expression.Quote(extract), Expression.Quote(update));

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="update">Value expression that produces the new column value. The parameter is the updated record.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							this IUpdatable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[InstantHandle] Expression<Func<T, TV>> update)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatablePrev.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="update">Value expression that produces the new column value without referencing the updated record.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[InstantHandle] Expression<Func<TV>> update)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update == null) throw new ArgumentNullException(nameof(update));

			var query = source.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetQueryableExpression.MakeGenericMethod(typeof(T), typeof(TV)),
					source.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="update">Value expression that produces the new column value without referencing the updated record.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							this IUpdatable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[InstantHandle] Expression<Func<TV>> update)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));
			if (update == null) throw new ArgumentNullException(nameof(update));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatableExpression.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Quote(update)));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query, assigning a constant <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="value">Value assigned to the selected column.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							 this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[SkipIfConstant] TV value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryableValue.MakeGenericMethod(typeof(T), typeof(TV)),
				currentSource.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV)));

			var query = currentSource.Provider.CreateQuery<T>(expr);

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a column assignment to an updatable query, assigning a constant <paramref name="value"/>.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <typeparam name="TV">Column type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="extract">Column selector.</param>
		/// <param name="value">Value assigned to the selected column.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The update definition is represented in the SQL AST and emitted into SQL text according to provider rules.
		/// <para>
		/// AI-Tags: Group=Update; Execution=Deferred; Composability=Composable; Affects=SqlSemantics; Pipeline=ExpressionTree,SqlAST,SqlText; Provider=ProviderDefined;
		/// </para>
		/// </remarks>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T, TV>(
							 this IUpdatable<T> source,
			[InstantHandle] Expression<Func<T, TV>> extract,
			[SkipIfConstant] TV value)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (extract == null) throw new ArgumentNullException(nameof(extract));

			var query = ((Updatable<T>)source).Query;

			query = query.Provider.CreateQuery<T>(
				Expression.Call(
					null,
					Methods.LinqToDB.Update.SetUpdatableValue.MakeGenericMethod(typeof(T), typeof(TV)),
					query.Expression, Expression.Quote(extract), Expression.Constant(value, typeof(TV))));

			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a provider-translated custom SET expression (string interpolation) to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="setExpression">Custom SET expression.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The supported interpolation patterns are provider-defined.
		/// </remarks>
		/// <example>
		/// The following example is illustrative.
		/// <code>
		///		db.Users.Where(u => u.UserId == id)
		///			.Set(u => $"{u.Name}" += {str}")
		///			.Update();
		/// </code>
		/// </example>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T>(
							this IQueryable<T> source,
			[InstantHandle] Expression<Func<T, string>> setExpression)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setExpression == null) throw new ArgumentNullException(nameof(setExpression));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetQueryableSetCustom.MakeGenericMethod(typeof(T)),
				currentSource.Expression, Expression.Quote(setExpression));

			var query = currentSource.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		/// <summary>
		/// Adds a provider-translated custom SET expression (string interpolation) to an updatable query.
		/// </summary>
		/// <typeparam name="T">Updated record type.</typeparam>
		/// <param name="source">Source query.</param>
		/// <param name="setExpression">Custom SET expression.</param>
		/// <returns>An <see cref="IUpdatable{T}"/> query.</returns>
		/// <remarks>
		/// Execution is deferred and the method is composable.
		/// The supported interpolation patterns are provider-defined.
		/// </remarks>
		/// <example>
		/// The following example is illustrative.
		/// <code>
		///		db.Users.Where(u => u.UserId == id)
		///			.AsUpdatable()
		///			.Set(u => $"{u.Name}" += {str}")
		///			.Update();
		/// </code>
		/// </example>
		[LinqTunnel]
		[Pure]
		public static IUpdatable<T> Set<T>(
							this IUpdatable<T> source,
			[InstantHandle] Expression<Func<T, string>> setExpression)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (setExpression == null) throw new ArgumentNullException(nameof(setExpression));

			var query = ((Updatable<T>)source).Query;

			var expr = Expression.Call(
				null,
				Methods.LinqToDB.Update.SetUpdatableSetCustom.MakeGenericMethod(typeof(T)),
				query.Expression, Expression.Quote(setExpression));

			query = query.Provider.CreateQuery<T>(expr);
			return new Updatable<T>(query);
		}

		#endregion
	}
}
