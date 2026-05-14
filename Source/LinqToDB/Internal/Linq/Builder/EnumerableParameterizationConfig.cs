using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;

namespace LinqToDB.Internal.Linq.Builder
{
	/// <summary>
	/// Per-column parameter/inline rendering selector for <see cref="EnumerableContext"/>.
	/// </summary>
	sealed class EnumerableParameterizationConfig
	{
		public EnumerableParameterizationConfig(bool defaultForceParameter, ParameterExpression? parameter, IReadOnlyList<MemberExpression>? excepted)
		{
			DefaultForceParameter = defaultForceParameter;
			Parameter             = parameter;
			Excepted              = excepted;
		}

		/// <summary>
		/// Default rendering for every cell: <see langword="true"/> = SqlParameter, <see langword="false"/> = SqlValue.
		/// </summary>
		public bool DefaultForceParameter { get; }

		/// <summary>
		/// Lambda parameter used as the root for every <see cref="Excepted"/> expression. Null when no overrides.
		/// </summary>
		public ParameterExpression? Parameter { get; }

		/// <summary>
		/// Member access expressions (rooted at <see cref="Parameter"/>) whose mode flips relative to the default.
		/// </summary>
		public IReadOnlyList<MemberExpression>? Excepted { get; }

		public bool ShouldForceParameter(Expression accessExpression)
		{
			var force = DefaultForceParameter;

			if (Excepted == null || Parameter == null || Excepted.Count == 0)
				return force;

			var rerooted = ReRootToParameter(accessExpression, Parameter);
			if (rerooted == null)
				return force;

			foreach (var excepted in Excepted)
			{
				if (ExpressionEqualityComparer.Instance.Equals(excepted, rerooted))
					return !force;
			}

			return force;
		}

		static Expression? ReRootToParameter(Expression access, ParameterExpression parameter)
		{
			if (access is MemberExpression me)
			{
				var inner = me.Expression == null ? null : ReRootToParameter(me.Expression, parameter);
				if (inner == null)
					return null;

				return Expression.MakeMemberAccess(inner, me.Member);
			}

			// Replace the chain root with the stored parameter only when the type matches.
			return access.Type == parameter.Type ? parameter : null;
		}
	}
}
