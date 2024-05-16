using System;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public class SqlTypesTranslationDefault : IMemberTranslator
	{
		TranslationRegistration _registration = new TranslationRegistration();

		public SqlTypesTranslationDefault()
		{
			_registration.RegisterMember(() => Sql.Types.Bit,      ConvertBit);
			_registration.RegisterMember(() => Sql.Types.BigInt,   ConvertBigInt);
			_registration.RegisterMember(() => Sql.Types.Int,      ConvertInt);
			_registration.RegisterMember(() => Sql.Types.SmallInt, ConvertSmallInt);
			_registration.RegisterMember(() => Sql.Types.TinyInt,  ConvertTinyInt);
			_registration.RegisterMember(() => Sql.Types.DefaultDecimal, ConvertDefaultDecimal);

			_registration.RegisterMethod(() => Sql.Types.Decimal(0),  ConvertDecimalPrecision);
			_registration.RegisterMethod(() => Sql.Types.Decimal(0, 0), ConvertDecimalPrecisionScale);

			_registration.RegisterMember(() => Sql.Types.Money, ConvertMoney);
			_registration.RegisterMember(() => Sql.Types.SmallMoney, ConvertSmallMoney);
			
			//TODO: What is it?
			_registration.RegisterMember(() => Sql.Types.Float, ConvertFloat);

			_registration.RegisterMember(() => Sql.Types.Real, ConvertReal);
			_registration.RegisterMember(() => Sql.Types.DateTime, ConvertDateTime);
			_registration.RegisterMember(() => Sql.Types.DateTime2, ConvertDateTime2);
			_registration.RegisterMember(() => Sql.Types.SmallDateTime, ConvertSmallDateTime);
			_registration.RegisterMember(() => Sql.Types.Date, ConvertDate);

#if NET6_0_OR_GREATER
			_registration.RegisterMember(() => Sql.Types.DateOnly, ConvertDateOnly);
#endif
			_registration.RegisterMember(() => Sql.Types.Time, ConvertTime);
			_registration.RegisterMember(() => Sql.Types.DateTimeOffset, ConvertDateTimeOffset);
			_registration.RegisterMethod(() => Sql.Types.Char(0), ConvertCharLength);
			_registration.RegisterMember(() => Sql.Types.DefaultChar, ConvertDefaultChar);
			_registration.RegisterMethod(() => Sql.Types.VarChar(0), ConvertVarChar);

			_registration.RegisterMember(() => Sql.Types.DefaultVarChar, ConvertDefaultVarChar);
			_registration.RegisterMethod(() => Sql.Types.NChar(0), ConvertNChar);
			_registration.RegisterMember(() => Sql.Types.DefaultNChar, ConvertDefaultNChar);
			_registration.RegisterMethod(() => Sql.Types.NVarChar(0), ConvertNVarChar);
			_registration.RegisterMember(() => Sql.Types.DefaultNVarChar, ConvertDefaultNVarChar);
		}

		#region Convert functions

		protected virtual Expression? ConvertBit(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Boolean));

		protected virtual Expression? ConvertBigInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Int64));

		protected virtual Expression? ConvertInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertSmallInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertTinyInt(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDefaultDecimal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDecimalPrecision(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(4));
		}

		protected virtual Expression? ConvertDecimalPrecisionScale(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var precision))
				return null;

			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[1], out var scale))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, t => t.WithPrecision(precision).WithScale(scale));
		}

		protected virtual Expression? ConvertMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Money));

		protected virtual Expression? ConvertSmallMoney(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.SmallMoney));

		protected virtual Expression? ConvertFloat(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertReal(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime));

		protected virtual Expression? ConvertDateTime2(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTime2));

		protected virtual Expression? ConvertSmallDateTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);

		protected virtual Expression? ConvertDate(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Date));

		protected virtual Expression? ConvertCharLength(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithDataType(DataType.Char).WithSystemType(typeof(string)).WithLength(length));
		}

#if NET6_0_OR_GREATER
		protected virtual Expression? ConvertDateOnly(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression);
#endif

		protected virtual Expression? ConvertTime(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.Time));

		protected virtual Expression? ConvertDateTimeOffset(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
			=> MakeSqlTypeExpression(translationContext, memberExpression, t => t.WithDataType(DataType.DateTimeOffset));

		protected virtual Expression? ConvertDefaultChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));
			dbDataType = dbDataType.WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.VarChar));
		}

		protected virtual Expression? ConvertDefaultVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataType.VarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertNChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(char), t => t.WithSystemType(typeof(string)).WithLength(length).WithDataType(DataType.NChar));
		}

		protected virtual Expression? ConvertDefaultNChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(char));

			dbDataType = dbDataType.WithDataType(DataType.NChar).WithSystemType(typeof(string)).WithLength(null);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
		}

		protected virtual Expression? ConvertNVarChar(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.TryEvaluate<int>(methodCall.Arguments[0], out var length))
				return null;

			return MakeSqlTypeExpression(translationContext, methodCall, typeof(string), t => t.WithLength(length).WithDataType(DataType.NVarChar));
		}

		protected virtual Expression? ConvertDefaultNVarChar(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			var dbDataType = translationContext.MappingSchema.GetDbDataType(typeof(string));

			dbDataType = dbDataType.WithDataType(DataType.NVarChar);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlValue(dbDataType, ""), memberExpression);
		}

		#endregion

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Func<DbDataType, DbDataType>? correctFunc = null) 
			=> MakeSqlTypeExpression(translationContext, basedOn, basedOn.Type, correctFunc);

		protected Expression MakeSqlTypeExpression(ITranslationContext translationContext, Expression basedOn, Type type, Func<DbDataType, DbDataType>? correctFunc = null)
		{
			var dbDataType = translationContext.MappingSchema.GetDbDataType(type);

			if (correctFunc != null)
				dbDataType = correctFunc(dbDataType);

			var sqlDataType = new SqlDataType(dbDataType);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlDataType, basedOn);
		}

		public Expression? Translate(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			MemberInfo memberInfo;

			if (memberExpression is MethodCallExpression methodCall)
				memberInfo = methodCall.Method;
			else if (memberExpression is MemberExpression member)
				memberInfo = member.Member;
			else
				return null;

			var translationFunc = _registration.GetTranslation(memberInfo);
			if (translationFunc == null)
				return null;

			var result = translationFunc(translationContext, memberExpression, translationFlags);
			return result;
		}
	}
}
