using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class ConvertMemberTranslatorDefault : MemberTranslatorBase
	{
		public ConvertMemberTranslatorDefault()
		{
			RegisterByte();
			RegisterChar();
			RegisterDateTime();
			RegisterDecimal();
			RegisterDouble();
			RegisterInt16();
			RegisterInt32();
			RegisterInt64();
			RegisterSByte();
			RegisterSingle();
			RegisterString();
			RegisterUInt16();
			RegisterUInt32();
			RegisterUInt64();
		}

#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs

		void RegisterByte()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((byte     obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((char     obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((DateTime obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((decimal  obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((double   obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((short    obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((int      obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((long     obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((object   obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((float    obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((string   obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((ushort   obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((uint     obj) => Convert.ToByte(obj), TranslateConvertToByte);
			Registration.RegisterMethod((ulong    obj) => Convert.ToByte(obj), TranslateConvertToByte);
		}

		void RegisterChar()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((byte     obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((char     obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((DateTime obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((decimal  obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((double   obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((short    obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((int      obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((long     obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((object   obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((float    obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((string   obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((ushort   obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((uint     obj) => Convert.ToChar(obj), TranslateConvertToChar);
			Registration.RegisterMethod((ulong    obj) => Convert.ToChar(obj), TranslateConvertToChar);
		}

		void RegisterDateTime()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((byte     obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((char     obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((DateTime obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((decimal  obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((double   obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((short    obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((int      obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((long     obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((object   obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((float    obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((string   obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((ushort   obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((uint     obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
			Registration.RegisterMethod((ulong    obj) => Convert.ToDateTime(obj), TranslateConvertToDateTime);
		}

		void RegisterDecimal()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((byte     obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((char     obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((DateTime obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((decimal  obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((double   obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((short    obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((int      obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((long     obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((object   obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((float    obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((string   obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((ushort   obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((uint     obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
			Registration.RegisterMethod((ulong    obj) => Convert.ToDecimal(obj), TranslateConvertToDecimal);
		}

		void RegisterDouble()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((byte     obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((char     obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((DateTime obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((decimal  obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((double   obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((short    obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((int      obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((long     obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((object   obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((float    obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((string   obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((ushort   obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((uint     obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
			Registration.RegisterMethod((ulong    obj) => Convert.ToDouble(obj), TranslateConvertToDouble);
		}

		void RegisterInt16()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((byte     obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((char     obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((DateTime obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((decimal  obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((double   obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((short    obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((int      obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((long     obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((object   obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((float    obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((string   obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((ushort   obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((uint     obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
			Registration.RegisterMethod((ulong    obj) => Convert.ToInt16(obj), TranslateConvertToInt16);
		}

		void RegisterInt32()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((byte     obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((char     obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((DateTime obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((decimal  obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((double   obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((short    obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((int      obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((long     obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((object   obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((float    obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((string   obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((ushort   obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((uint     obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
			Registration.RegisterMethod((ulong    obj) => Convert.ToInt32(obj), TranslateConvertToInt32);
		}

		void RegisterInt64()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((byte     obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((char     obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((DateTime obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((decimal  obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((double   obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((short    obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((int      obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((long     obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((object   obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((float    obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((string   obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((ushort   obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((uint     obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
			Registration.RegisterMethod((ulong    obj) => Convert.ToInt64(obj), TranslateConvertToInt64);
		}

		void RegisterSByte()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((byte     obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((char     obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((DateTime obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((decimal  obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((double   obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((short    obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((int      obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((long     obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((object   obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((float    obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((string   obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((ushort   obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((uint     obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
			Registration.RegisterMethod((ulong    obj) => Convert.ToSByte(obj), TranslateConvertToSByte);
		}

		void RegisterSingle()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((byte     obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((char     obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((DateTime obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((decimal  obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((double   obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((short    obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((int      obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((long     obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((object   obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((float    obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((string   obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((ushort   obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((uint     obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
			Registration.RegisterMethod((ulong    obj) => Convert.ToSingle(obj), TranslateConvertToSingle);
		}

		void RegisterString()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((byte     obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((char     obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((DateTime obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((decimal  obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((double   obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((short    obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((int      obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((long     obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((object   obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((float    obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((string   obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((ushort   obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((uint     obj) => Convert.ToString(obj), TranslateConvertToString);
			Registration.RegisterMethod((ulong    obj) => Convert.ToString(obj), TranslateConvertToString);
		}

		void RegisterUInt16()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((byte     obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((char     obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((DateTime obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((decimal  obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((double   obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((short    obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((int      obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((long     obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((object   obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((float    obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((string   obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((ushort   obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((uint     obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
			Registration.RegisterMethod((ulong    obj) => Convert.ToUInt16(obj), TranslateConvertToUInt16);
		}

		void RegisterUInt32()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((byte     obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((char     obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((DateTime obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((decimal  obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((double   obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((short    obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((int      obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((long     obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((object   obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((float    obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((string   obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((ushort   obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((uint     obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
			Registration.RegisterMethod((ulong    obj) => Convert.ToUInt32(obj), TranslateConvertToUInt32);
		}

		void RegisterUInt64()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((byte     obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((char     obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((DateTime obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((decimal  obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((double   obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((short    obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((int      obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((long     obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((object   obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((float    obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((string   obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((ushort   obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((uint     obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
			Registration.RegisterMethod((ulong    obj) => Convert.ToUInt64(obj), TranslateConvertToUInt64);
		}

#pragma warning restore RS0030, CA1305, MA0011 // Do not used banned APIs

		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is MethodCallExpression methodCallExpression)
			{
				if (methodCallExpression.Method.DeclaringType != null && typeof(Sql.ConvertTo<>).IsSameOrParentOf(methodCallExpression.Method.DeclaringType) && methodCallExpression.Method.Name.Equals(nameof(Sql.ConvertTo<>.From)))
				{
					var result = TranslateSqlConvertTo(translationContext, methodCallExpression, translationFlags);
					if (result != null)
						return result;
				}
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		protected virtual Expression? TranslateSqlConvertTo(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			// For Sql.ConvertTo<T>.From - no rounding, direct cast
			var targetType = methodCallExpression.Method.ReturnType;
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], targetType, translationFlags);
		}

		protected virtual Expression? TranslateConvertDefault(ITranslationContext translationContext, MethodCallExpression methodCallExpression, Expression value, Type targetType, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(value))
				return null;

			value = value.UnwrapConvert();

			if (!translationContext.TranslateToSqlExpression(value, out var sqlExpression, out var error))
				return error;

			var castExpression = translationContext.ExpressionFactory.Cast(sqlExpression, translationContext.MappingSchema.GetDbDataType(targetType));

			return translationContext.CreatePlaceholder(castExpression, methodCallExpression);
		}

		protected virtual Expression? TranslateConvertToByte(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(byte), translationFlags);
		}

		protected virtual Expression? TranslateConvertToChar(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(char), translationFlags);
		}

		protected virtual Expression? TranslateConvertToDateTime(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(DateTime), translationFlags);
		}

		protected virtual Expression? TranslateConvertToDecimal(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(decimal), translationFlags);
		}

		protected virtual Expression? TranslateConvertToDouble(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(double), translationFlags);
		}

		protected virtual Expression? TranslateConvertToInt16(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(short), translationFlags);
		}

		protected virtual Expression? TranslateConvertToInt32(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(int), translationFlags);
		}

		protected virtual Expression? TranslateConvertToInt64(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(long), translationFlags);
		}

		protected virtual Expression? TranslateConvertToSByte(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(sbyte), translationFlags);
		}

		protected virtual Expression? TranslateConvertToSingle(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(float), translationFlags);
		}

		protected virtual Expression? TranslateConvertToString(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(string), translationFlags);
		}

		protected virtual Expression? TranslateConvertToUInt16(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(ushort), translationFlags);
		}

		protected virtual Expression? TranslateConvertToUInt32(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(uint), translationFlags);
		}

		protected virtual Expression? TranslateConvertToUInt64(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(ulong), translationFlags);
		}
	}
}
