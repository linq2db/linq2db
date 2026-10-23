using System.Globalization;
using System.Linq.Expressions;

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

			var entityDescriptor = sequence.MappingSchema.GetEntityDescriptor(resultType);

			if (entityDescriptor.DynamicColumnSetter == null)
				throw new LinqToDBException(string.Format(CultureInfo.InvariantCulture, ErrorHelper.SelectDynamic.Error_NoDynamicColumnsStore, resultType.Name));

			var generic = staticBody switch
			{
				MemberInitExpression memberInit => new SqlGenericConstructorExpression(memberInit),
				NewExpression newExpression     => new SqlGenericConstructorExpression(newExpression),
				_ => throw new LinqToDBException(ErrorHelper.SelectDynamic.Error_StaticProjectionNotAnObject),
			};

			generic = generic.WithMappingSchema(sequence.MappingSchema);

			for (var i = 0; i < names.Length; i++)
			{
				var cellBody = SequenceHelper.PrepareBody(cellArray.Expressions[i].UnwrapLambda(), sequence);

				generic = generic.AppendAssignment(new SqlGenericConstructorExpression.Assignment(
					new DynamicColumnInfo(resultType, cellBody.Type, names[i]),
					cellBody, true, false));
			}

			var context = new SelectContext(buildInfo.Parent, generic, sequence, buildInfo.IsSubQuery);

#if DEBUG
			context.Debug_MethodCall = methodCall;
#endif

			return BuildSequenceResult.FromContext(context);
		}
	}
}
