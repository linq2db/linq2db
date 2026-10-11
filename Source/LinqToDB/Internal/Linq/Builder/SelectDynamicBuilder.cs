using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	[BuildsMethodCall(nameof(LinqExtensions.SelectDynamicCore))]
	sealed class SelectDynamicBuilder : MethodCallBuilder
	{
		public static bool CanBuildMethod(MethodCallExpression call) => true;

		protected override BuildSequenceResult BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var staticSelector = methodCall.Arguments[1].UnwrapLambda();
			var names          = methodCall.Arguments[2].EvaluateExpression<string[]>()!;
			var cellArray      = (NewArrayExpression)methodCall.Arguments[3];
			var resultType     = methodCall.Method.GetGenericArguments()[1];

			var buildResult = builder.TryBuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

			if (buildResult.BuildContext == null)
				return buildResult;

			var sequence = buildResult.BuildContext;

			// finalizing context
			_ = builder.BuildExtractExpression(sequence, new ContextRefExpression(sequence.ElementType, sequence));

			sequence.SetAlias(staticSelector.Parameters[0].Name);
			sequence = new SubQueryContext(sequence) { IsSelectWrapper = true };

			var staticBody = SequenceHelper.PrepareBody(staticSelector, sequence);

			// A pivot that nests its cells leaves a PivotCells<,> placeholder at the member they belong to, at any
			// depth; with no placeholder the generated columns go into the result type's own dynamic-columns store.
			var placeholder = staticBody.Find(IsCellsPlaceholder);
			var storeType   = placeholder?.Type ?? resultType;

			var entityDescriptor = sequence.MappingSchema.GetEntityDescriptor(storeType);

			if (entityDescriptor.DynamicColumnSetter == null)
				throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.Pivot.Error_NoDynamicColumnsStore, storeType.Name));

			var cells = new SqlGenericConstructorExpression.Assignment[names.Length];

			for (var i = 0; i < names.Length; i++)
			{
				var cellBody = SequenceHelper.PrepareBody(cellArray.Expressions[i].UnwrapLambda(), sequence);

				cells[i] = new SqlGenericConstructorExpression.Assignment(
					new DynamicColumnInfo(storeType, cellBody.Type, names[i]),
					cellBody, true, false);
			}

			if (placeholder != null)
			{
				var constructor = storeType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;
				var store       = new SqlGenericConstructorExpression(Expression.New(constructor)).WithMappingSchema(sequence.MappingSchema);

				foreach (var cell in cells)
					store = store.AppendAssignment(cell);

				staticBody = staticBody.Replace(placeholder, store);
			}

			var generic = staticBody switch
			{
				MemberInitExpression memberInit => new SqlGenericConstructorExpression(memberInit),
				NewExpression newExpression     => new SqlGenericConstructorExpression(newExpression),
				_ => throw new LinqToDBException(ErrorHelper.Pivot.Error_StaticProjectionNotAnObject),
			};

			generic = generic.WithMappingSchema(sequence.MappingSchema);

			if (placeholder == null)
			{
				foreach (var cell in cells)
					generic = generic.AppendAssignment(cell);
			}

			var context = new SelectContext(buildInfo.Parent, generic, sequence, buildInfo.IsSubQuery);

#if DEBUG
			context.Debug_MethodCall = methodCall;
#endif

			return BuildSequenceResult.FromContext(context);
		}

		static bool IsCellsPlaceholder(Expression expression)
			=> expression is ConstantExpression { Value: null }
				&& expression.Type.IsGenericType
				&& expression.Type.GetGenericTypeDefinition() == typeof(PivotCells<,>);
	}
}
