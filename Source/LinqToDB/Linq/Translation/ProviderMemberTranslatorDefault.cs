using System;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Translation
{
	using System.Globalization;

	using LinqToDB.Expressions;
	using Common;
	using SqlQuery;

	public abstract class ProviderMemberTranslatorDefault : MemberTranslatorBase
	{
		protected virtual IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslationDefault();
		}

		protected abstract IMemberTranslator CreateDateFunctionsTranslator();

		protected ProviderMemberTranslatorDefault()
		{
			InitDefaultTranslators();
		}

	void InitDefaultTranslators()
		{
			CombinedMemberTranslator.Add(CreateSqlTypesTranslator());
			CombinedMemberTranslator.Add(CreateDateFunctionsTranslator());
		}

		protected SqlPlaceholderExpression? TranslateNoRequiredObjectExpression(ITranslationContext translationContext, Expression? objExpression, TranslationFlags translationFlags)
		{
			if (objExpression == null)
				return null;

			if (translationContext.CanBeCompiled(objExpression, translationFlags))
				return null;

			var obj = translationContext.Translate(objExpression);

			if (obj is not SqlPlaceholderExpression objPlaceholder)
				return null;

			return objPlaceholder;
		}

		protected virtual Expression ConvertToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var obj = methodCall.Object!;

			var translated = translationContext.Translate(obj, translationFlags);

			if (translated is not SqlPlaceholderExpression objPlaceholder)
				return translated;

			DbDataType toType;

			if (translationContext.CurrentColumnDescriptor != null)
			{
				toType = translationContext.CurrentColumnDescriptor.GetDbDataType(true);
			}
			else
			{
				toType = translationContext.MappingSchema.GetDbDataType(typeof(string));
			}

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlCastExpression(objPlaceholder.Sql, toType, null), methodCall);
		}

		protected bool ProcessToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Object != null && methodCall.Method.Name == nameof(ToString))
			{
				var parameters = methodCall.Method.GetParameters();
				if (parameters.Length > 1)
					return true;

				if (parameters.Length == 1)
				{
					if (parameters[0].ParameterType != typeof(IFormatProvider))
						return true;

					var cultureExpression = methodCall.Arguments[0];

					if (!translationContext.CanBeEvaluated(cultureExpression))
						return true;

					var culture = translationContext.Evaluate(cultureExpression);
					if (culture is not IFormatProvider formatProvider)
						return true;

					if (formatProvider != CultureInfo.InvariantCulture)
						return true;
				}

				if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeCompiled(methodCall.Object, translationFlags))
					return true;

				translated = ConvertToString(translationContext, methodCall, translationFlags);
				return true;
			}

			return false;
		}

		protected bool ProcessSqlConvert(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Sql))
				return false;

			if (methodCall.Method.Name != nameof(Sql.Convert))
				return false;

			if (methodCall.Arguments.Count != 1)
				return false;

			//TODO: Implement conversion
			return true;
		}

		protected bool ProcessConvertToBoolean(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Convert))
				return false;

			if (methodCall.Method.Name != nameof(Convert.ToBoolean))
				return false;

			if (methodCall.Arguments.Count != 1)
				return false;

			var argumentPlaceholder = TranslateNoRequiredObjectExpression(translationContext, methodCall.Arguments[0], translationFlags);

			if (argumentPlaceholder == null)
				return true;

			var translatedSqlExpression = TranslateConvertToBoolean(translationContext, argumentPlaceholder.Sql, translationFlags);

			if (translatedSqlExpression == null)
				return true;

			translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedSqlExpression, methodCall);

			return true;
		}

		protected virtual ISqlExpression? TranslateConvertToBoolean(ITranslationContext translationContext, ISqlExpression sqlExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		public virtual Expression? TranslateMethodCall(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			Expression? translated;

			if (ProcessToString(translationContext, methodCall, translationFlags, out translated))
				return translated;

			if (ProcessSqlConvert(translationContext, methodCall, translationFlags, out translated))
				return translated;

			if (ProcessConvertToBoolean(translationContext, methodCall, translationFlags, out translated))
				return translated;

			return null;
		}

		protected virtual Expression? TranslateMemberExpression(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is MethodCallExpression methodCallExpression)
			{
				var translated = TranslateMethodCall(translationContext, methodCallExpression, translationFlags);
				if (translated != null)
					return translated;

			}
			else if (memberExpression is MemberExpression member)
			{
				var translated = TranslateMemberExpression(translationContext, member, translationFlags);
				if (translated != null)
					return translated;
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}
	}
}
