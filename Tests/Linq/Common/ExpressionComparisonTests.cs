using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Internal.Expressions;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Common
{
	[TestFixture]
	public class ExpressionComparisonTests
	{
		// Keep the cache comparer internal; bind once so reflection is outside each comparison.
		static readonly Func<Expression?, Expression?, bool> CacheEquals =
			(Func<Expression?, Expression?, bool>)Delegate.CreateDelegate(
				typeof(Func<Expression?, Expression?, bool>),
				typeof(ExpressionEqualityComparer).Assembly
					.GetType("LinqToDB.Internal.Expressions.ExpressionVisitors.EqualsToVisitor", throwOnError: true)!
					.GetMethod("EqualsTo", BindingFlags.Static | BindingFlags.NonPublic, null, [typeof(Expression), typeof(Expression)], null)!);

		static void CompareBoth(Expression first, Expression second, bool expected)
		{
			CacheEquals(first, second)
				.ShouldBe(expected);
			CacheEquals(second, first)
				.ShouldBe(expected);
		}

		[Test]
		public void LambdaDeclarationsUsePosition()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "q");
			var x = Expression.Parameter(typeof(int), "p");
			var y = Expression.Parameter(typeof(int), "q");

			var first = Expression.Lambda(Expression.Subtract(a, b), a, b);
			CompareBoth(first, Expression.Lambda(Expression.Subtract(x, y), x, y), true);
			CompareBoth(first, Expression.Lambda(Expression.Subtract(y, x), x, y), false);
			CompareBoth(first, Expression.Lambda(Expression.Subtract(a, a), a, b), false);
		}

		[Test]
		public void LambdaDeclarationNamesArePartOfTheKey()
		{
			// The name of a declared parameter becomes a table alias, so it must match as well as the position.
			var a = Expression.Parameter(typeof(int), "p");
			var x = Expression.Parameter(typeof(int), "x");

			CompareBoth(Expression.Lambda(a, a), Expression.Lambda(x, x), false);
		}

		[Test]
		public void NestedLambdaBindings([Values] bool correlated)
		{
			Expression Build(bool useOuter)
			{
				var a = Expression.Parameter(typeof(int), "p");
				var b = Expression.Parameter(typeof(int), "p");
				return Expression.Lambda(Expression.Lambda(Expression.Subtract(b, useOuter ? a : b), b), a);
			}

			CompareBoth(Build(correlated), Build(correlated), true);
			CompareBoth(Build(correlated), Build(!correlated), false);
		}

		[Test]
		public void FreeParametersKeepNameAndTypeComparison()
		{
			var p = Expression.Parameter(typeof(int), "p");
			CompareBoth(p, Expression.Parameter(typeof(int), "p"), true);
			CompareBoth(p, Expression.Parameter(typeof(int), "q"), false);
			CompareBoth(p, Expression.Parameter(typeof(long), "p"), false);

			var bound = Expression.Parameter(typeof(int), "p");
			CompareBoth(Expression.Lambda(p, bound), Expression.Lambda(bound, bound), false);
		}

		[Test]
		public void SharedSubtreesRespectBindings([Values] bool shareLambda)
		{
			var a    = Expression.Parameter(typeof(int), "p");
			var b    = Expression.Parameter(typeof(int), "p");
			var body = Expression.Add(a, Expression.Constant(1));
			Expression shared = shareLambda ? Expression.Lambda(body) : body;

			CompareBoth(Expression.Lambda(shared, a), Expression.Lambda(shared, b), false);
		}

		[Test]
		public void SharedUnsupportedNodesRespectIdentityBindings()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			// .NET Framework requires at least one case.
			var shared = Expression.Switch(Expression.Constant(0), a, Expression.SwitchCase(a, Expression.Constant(1)));
			CompareBoth(Expression.Lambda(shared, a), Expression.Lambda(shared, a), true);
			CompareBoth(Expression.Lambda(shared, a), Expression.Lambda(shared, b), false);

			var unbound = Expression.Switch(Expression.Constant(0), Expression.Constant(1), Expression.SwitchCase(Expression.Constant(2), Expression.Constant(1)));
			CompareBoth(Expression.Lambda(unbound, a), Expression.Lambda(unbound, b), true);
		}

		sealed class SharedExtension(Expression? child, bool traversable = true) : Expression
		{
			public override ExpressionType NodeType  => ExpressionType.Extension;
			public override Type           Type      => typeof(int);
			public override bool           CanReduce => Child != null;

			public override Expression Reduce()
			{
				return Child ?? this;
			}

			protected override Expression VisitChildren(ExpressionVisitor visitor)
			{
				if (traversable)
					visitor.Visit(Child);

				return this;
			}

			public Expression? Child { get; } = child;
		}

		[Test]
		public void SharedExtensionNodesRespectIdentityBindings()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			var shared = new SharedExtension(a);
			CompareBoth(Expression.Lambda(shared, a), Expression.Lambda(shared, a), true);
			CompareBoth(Expression.Lambda(shared, a), Expression.Lambda(shared, b), false);

			// An unknown extension could hide a parameter, so it is rejected even when it references none.
			var leaf = new SharedExtension(null);
			CompareBoth(Expression.Lambda(leaf, a), Expression.Lambda(leaf, b), false);

			// Extensions known to hold no expression, such as a captured table's root, still match.
			Expression[] leaves =
			[
				new SqlQueryRootExpression(MappingSchema.Default, typeof(object)),
				new ConstantPlaceholderExpression(typeof(int)),
				new DefaultValueExpression(MappingSchema.Default, typeof(int)),
			];

			foreach (var known in leaves)
				CompareBoth(Expression.Lambda(known, a), Expression.Lambda(known, b), true);
		}

		[Test]
		public void SharedOpaqueExtensionNodesAreRejected()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");

			// Both nodes hide their child from visitors: the left one reads the inner parameter, the right one the outer.
			var opaque = new SharedExtension(a, traversable: false);
			CompareBoth(Expression.Lambda(Expression.Lambda(opaque, a), a), Expression.Lambda(Expression.Lambda(opaque, a), a), true);
			CompareBoth(Expression.Lambda(Expression.Lambda(opaque, a), a), Expression.Lambda(Expression.Lambda(opaque, b), a), false);

			var constructor = typeof(Holder).GetConstructor([typeof(int)])!;
			var access      = new SqlGenericParamAccessExpression(Expression.New(constructor, a), constructor.GetParameters()[0]);
			CompareBoth(Expression.Lambda(Expression.Lambda(access, a), a), Expression.Lambda(Expression.Lambda(access, b), a), false);
		}

		sealed class CustomKindNode(Expression child, ExpressionType nodeType) : Expression
		{
			public override ExpressionType NodeType  => nodeType;
			public override Type           Type      => child.Type;
			public override bool           CanReduce => true;

			public override Expression Reduce()
			{
				return child;
			}

			protected override Expression VisitChildren(ExpressionVisitor visitor)
			{
				return this;
			}
		}

		[Test]
		public void SharedNodesOfUnknownKindAreRejected([Values(-1, (int)ChangeTypeExpression.ChangeTypeType, 1001, (int)ExpressionType.Add)] int nodeType)
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");

			var opaque = new CustomKindNode(a, (ExpressionType)nodeType);
			CompareBoth(Expression.Lambda(Expression.Lambda(opaque, a), a), Expression.Lambda(Expression.Lambda(opaque, b), a), false);
		}

		[Test]
		public void SharedSubtreeDeclarationsAreLocal()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");

			// The shared subtree declares the parameter itself, so the outer bindings cannot reach it.
			var lambda = Expression.Lambda(a, a);
			CompareBoth(Expression.Lambda(lambda, a), Expression.Lambda(lambda, b), true);

			var block = Expression.Block([a], Expression.Assign(a, Expression.Constant(1)), a);
			CompareBoth(Expression.Lambda(block, a), Expression.Lambda(block, b), true);

			var e1 = Expression.Parameter(typeof(Exception), "e");
			var e2 = Expression.Parameter(typeof(Exception), "e");
			var handler = Expression.TryCatch(
				Expression.Constant(0),
				Expression.Catch(e1, Expression.Condition(Expression.Equal(e1, Expression.Constant(null)), Expression.Constant(1), Expression.Constant(2))));
			CompareBoth(Expression.Lambda(handler, e1), Expression.Lambda(handler, e2), true);

			// A parameter that is free in the shared subtree still has to resolve to the same declaration.
			var free = Expression.Lambda(Expression.Add(a, Expression.Constant(1)), Expression.Parameter(typeof(int), "q"));
			CompareBoth(Expression.Lambda(free, a), Expression.Lambda(free, b), false);

			// A declaration ends with its scope: a later reference in the same shared subtree resolves outside it.
			var afterLambda = Expression.Block(lambda, a);
			CompareBoth(Expression.Lambda(afterLambda, a), Expression.Lambda(afterLambda, b), false);

			var afterBlock = Expression.Block(block, a);
			CompareBoth(Expression.Lambda(afterBlock, a), Expression.Lambda(afterBlock, b), false);

			var afterCatch = Expression.Block(handler, e1);
			CompareBoth(Expression.Lambda(afterCatch, e1), Expression.Lambda(afterCatch, e2), false);
		}

		public sealed class Holder
		{
			public Holder()
			{
			}

			public Holder(int value)
			{
				Value = value;
			}

			public int Value { get; set; }
		}

		[Test]
		public void SharedMemberBindingsRespectBindings()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			var binding = Expression.Bind(typeof(Holder).GetProperty(nameof(Holder.Value))!, a);
			var first   = Expression.MemberInit(Expression.New(typeof(Holder)), binding);
			var second  = Expression.MemberInit(Expression.New(typeof(Holder)), binding);

			CompareBoth(Expression.Lambda(first, a), Expression.Lambda(second, b), false);
		}

		[Test]
		public void NestedDeclarationsRestoreOuterScope()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var x = Expression.Parameter(typeof(int), "p");
			var y = Expression.Parameter(typeof(int), "p");
			var first = Expression.Lambda(Expression.Block(Expression.Lambda(a, a), a), a);
			var second = Expression.Lambda(Expression.Block(Expression.Lambda(y, y), x), x);

			CompareBoth(first, second, true);
			CompareBoth(first, Expression.Lambda(Expression.Block(Expression.Lambda(y, y), y), x), false);

			// A block's variables go out of scope with the block as well.
			CompareBoth(
				Expression.Lambda(Expression.Block(Expression.Block([a], a), a), a),
				Expression.Lambda(Expression.Block(Expression.Block([y], y), x), x),
				true);
		}

		[Test]
		public void ReusedDeclarationInstancesUseInnermostScope()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			var x = Expression.Parameter(typeof(int), "p");
			var first  = Expression.Lambda(Expression.Lambda(a, b), a);
			var second = Expression.Lambda(Expression.Lambda(x, x), x);

			CompareBoth(first, second, false);
		}

		[Test]
		public void BlockDeclarationsUsePosition()
		{
			var a = Expression.Variable(typeof(int), "p");
			var b = Expression.Variable(typeof(int), "p");
			var x = Expression.Variable(typeof(int), "p");
			var y = Expression.Variable(typeof(int), "p");
			var first = Expression.Block([a, b], Expression.Subtract(a, b));

			CompareBoth(first, Expression.Block([x, y], Expression.Subtract(x, y)), true);
			CompareBoth(first, Expression.Block([a, b], Expression.Subtract(b, a)), false);
			CompareBoth(first, Expression.Block([a], Expression.Subtract(a, b)), false);
			CompareBoth(first, Expression.Block([a, b], Expression.Empty(), Expression.Subtract(a, b)), false);
		}

		[Test]
		public void CatchDeclarationsScopeFilterAndBody([Values] bool filter, [Values] bool useOuter)
		{
			Expression Build(bool outerReference, string name)
			{
				var outer = Expression.Parameter(typeof(Exception), name);
				var error = Expression.Parameter(typeof(Exception), name);
				var reference = outerReference ? outer : error;
				var handler = Expression.MakeCatchBlock(typeof(Exception), error,
					filter ? error : reference,
					Expression.NotEqual(filter ? reference : error, Expression.Constant(null, typeof(Exception))));
				return Expression.Lambda(Expression.TryCatch(Expression.Throw(outer, typeof(Exception)), handler), outer);
			}

			CompareBoth(Build(useOuter, "p"), Build(useOuter, "p"), true);
			CompareBoth(Build(useOuter, "p"), Build(!useOuter, "p"), false);
		}

		[Test]
		public void CatchScopeDoesNotEscapeHandler()
		{
			var a = Expression.Parameter(typeof(Exception), "p");
			var x = Expression.Parameter(typeof(Exception), "p");
			var y = Expression.Parameter(typeof(Exception), "p");
			var first = Expression.Lambda(Expression.Block(
				Expression.TryCatch(Expression.Default(typeof(Exception)), Expression.Catch(a, a)), a), a);
			var second = Expression.Lambda(Expression.Block(
				Expression.TryCatch(Expression.Default(typeof(Exception)), Expression.Catch(y, y)), x), x);

			CompareBoth(first, second, true);
		}

		[Test]
		public void FinallyAndFaultUseOuterBindings([Values] bool fault)
		{
			Expression Build(bool useOuter, string name)
			{
				var outer = Expression.Parameter(typeof(int), name);
				var free  = Expression.Parameter(typeof(int), name);
				var tail  = Expression.Block(useOuter ? outer : free, Expression.Empty());
				return Expression.Lambda(Expression.MakeTry(typeof(void), Expression.Empty(),
					fault ? null : tail, fault ? tail : null, null), outer);
			}

			CompareBoth(Build(true, "p"), Build(true, "p"), true);
			CompareBoth(Build(true, "p"), Build(false, "p"), false);
		}

		[Test]
		public void DeepBindingsGrowAndRestoreStack()
		{
			Expression Build(bool useOuter)
			{
				var parameters = new ParameterExpression[16];
				for (var i = 0; i < parameters.Length; i++)
					parameters[i] = Expression.Parameter(typeof(int), "p");

				Expression body = useOuter ? parameters[0] : parameters[parameters.Length - 1];
				for (var i = parameters.Length - 1; i >= 0; i--)
					body = Expression.Lambda(body, parameters[i]);

				return body;
			}

			CompareBoth(Build(true), Build(true), true);
			CompareBoth(Build(true), Build(false), false);
			CompareBoth(Expression.Parameter(typeof(int), "p"), Expression.Parameter(typeof(int), "p"), true);
		}

		sealed class CompareExpressionAttribute : SqlQueryDependentAttribute
		{
			public override bool ExpressionsEqual<TContext>(TContext context, Expression expr1, Expression expr2,
				Func<TContext, Expression, Expression, bool> comparer)
			{
				return comparer(context, expr1, expr2);
			}
		}

		static int QueryDependent([CompareExpression] int value) => value;

		[Test]
		public void QueryDependentCallbackKeepsBindings()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			var method = typeof(ExpressionComparisonTests).GetMethod(nameof(QueryDependent), BindingFlags.Static | BindingFlags.NonPublic)!;
			CompareBoth(Expression.Lambda(Expression.Call(method, a), a), Expression.Lambda(Expression.Call(method, b), b), true);
			CompareBoth(Expression.Lambda(Expression.Call(method, a), a), Expression.Lambda(Expression.Call(method, a), b), false);
		}

		[Test]
		public void FailedComparisonDoesNotRetainBindings()
		{
			var a = Expression.Parameter(typeof(int), "p");
			var b = Expression.Parameter(typeof(int), "p");
			CompareBoth(Expression.Lambda(a, a), Expression.Lambda(a, b), false);
			// A pair left over from the comparison above would bind a on one side only.
			CompareBoth(a, Expression.Parameter(typeof(int), "p"), true);

			var unsupported = Expression.Loop(Expression.Empty());
			Action act = () => CacheEquals(
				Expression.Lambda(unsupported, a), Expression.Lambda(Expression.Loop(Expression.Empty()), b));
			act.ShouldThrow<InvalidOperationException>();
			CompareBoth(a, Expression.Parameter(typeof(int), "p"), true);
		}
	}
}
