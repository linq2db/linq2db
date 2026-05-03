using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public class ConvertMemberTranslatorDefault : MemberTranslatorBase
	{
		public ConvertMemberTranslatorDefault()
		{
			RegisterBoolean();
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

			HandleGuid();
		}

#pragma warning disable RS0030, CA1305, MA0011 // Do not used banned APIs

		void RegisterBoolean()
		{
			Registration.RegisterMethod((bool     obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((byte     obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((char     obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((DateTime obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((decimal  obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((double   obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((short    obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((int      obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((long     obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((object   obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((sbyte    obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((float    obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((string   obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((ushort   obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((uint     obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
			Registration.RegisterMethod((ulong    obj) => Convert.ToBoolean(obj), TranslateConvertToBoolean);
		}

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

		protected void HandleGuid()
		{
			Registration.RegisterReplacement(obj => Sql.Convert<string, Guid>(obj), (Guid obj) => obj.ToString());
		}

		protected virtual Expression? ConvertToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationFlags.HasFlag(TranslationFlags.Expression))
				return null;

			var obj = methodCall.Object!;

			var objPlaceholder = translationContext.TranslateNoRequiredObjectExpression(obj);

			if (objPlaceholder == null)
				return null;

			var fromType = translationContext.ExpressionFactory.GetDbDataType(objPlaceholder.Sql);
			DbDataType toType;

			if (translationContext.CurrentColumnDescriptor != null)
				toType = translationContext.CurrentColumnDescriptor.GetDbDataType(true);
			else
				toType = translationContext.MappingSchema.GetDbDataType(typeof(string));

			// ToString called on custom type already mapped to text-based db type or string
			if (fromType.IsTextType())
			{
				if (fromType.SystemType.IsEnum)
				{
					var enumValues = translationContext.MappingSchema.GetMapValues(fromType.SystemType)!;

					List<SqlCaseExpression.CaseItem>? cases = null;

					foreach (var field in enumValues)
					{
						if (field.MapValues.Length > 0)
						{
							var cond = field.MapValues.Length == 1
								? translationContext.ExpressionFactory.Equal(
									objPlaceholder.Sql,
									translationContext.ExpressionFactory.Value(fromType, field.MapValues[0].Value))
								: translationContext.ExpressionFactory
									.SearchCondition(isOr: true)
									.AddRange(
									field.MapValues.Select(
										v => translationContext.ExpressionFactory.Equal(
											objPlaceholder.Sql,
											translationContext.ExpressionFactory.Value(fromType, v.Value))));

							(cases ??= []).Add(
								new SqlCaseExpression.CaseItem(
									cond,
									translationContext.ExpressionFactory.Value(toType, string.Create(CultureInfo.InvariantCulture, $"{field.OrigValue}"))
								)
							);
						}
					}

					var defaultSql = objPlaceholder.Sql;

					var expr = cases == null ? defaultSql : new SqlCaseExpression(toType, cases, defaultSql);

					return translationContext.CreatePlaceholder(
						translationContext.CurrentSelectQuery,
						expr,
						methodCall);
				}

				return objPlaceholder.WithType(typeof(string));
			}

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				translationContext.ExpressionFactory.Cast(objPlaceholder.Sql, toType),
				methodCall);
		}

		protected bool ProcessToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall is not { Object: not null, Method.Name: nameof(ToString) })
			{
				return false;
			}

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

			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall.Object))
				return true;

			translated = ConvertToString(translationContext, methodCall, translationFlags);

			if (translated == null)
				return false;

			return true;
		}

		protected bool ProcessSqlConvert(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Sql))
				return false;

			if (methodCall.Method.Name is not nameof(Sql.Convert))
				return false;

			if (methodCall.Arguments.Count == 1)
				//TODO: Implement conversion
				return true;

			if (methodCall.Arguments.Count == 2)
			{
				if (!translationContext.TranslateExpression(methodCall.Arguments[1], out var argument, out _))
				{
					return false;
				}

				if (!translationContext.TranslateExpression(methodCall.Arguments[0], out var typeExpression, out _))
				{
					return false;
				}

				var translatedSqlExpression = TranslateConvert(translationContext, typeExpression, argument, translationFlags);

				if (translatedSqlExpression == null)
					return false;

				translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedSqlExpression, methodCall);
				return true;
			}

			return false;
		}

		protected bool ProcessSqlConvertTo(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType == null || !typeof(Sql.ConvertTo<>).IsSameOrParentOf(methodCall.Method.DeclaringType))
				return false;

			if (methodCall.Method.Name is not nameof(Sql.ConvertTo<>.From))
				return false;

			translated = TranslateSqlConvertTo(translationContext, methodCall, translationFlags);

			return true;
		}

		protected virtual ISqlExpression? TranslateConvert(ITranslationContext translationContext, ISqlExpression typeExpression, ISqlExpression sqlExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;

			if (typeExpression.SystemType == typeof(bool))
			{
				return TranslateConvertToBooleanSql(translationContext, sqlExpression, translationFlags);
			}

			var toDataType = QueryHelper.GetDbDataType(typeExpression, translationContext.MappingSchema);
			return factory.Cast(sqlExpression, toDataType);
		}

		protected virtual ISqlExpression? TranslateConvertToBooleanSql(ITranslationContext translationContext, ISqlExpression sqlExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;

			var sc = factory.SearchCondition();
			var predicate = factory.Equal(
					sqlExpression,
					factory.Value(0),
					translationContext.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null)
				.MakeNot();

			sc.Add(predicate);

			return sc;
		}

		protected virtual Expression? TranslateConvertToBoolean(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCallExpression.Arguments[0]))
				return null;

			var value = methodCallExpression.Arguments[0].UnwrapConvert();

			if (!translationContext.TranslateToSqlExpression(value, out var sqlExpression, out var error))
				return error;

			var translatedSql = TranslateConvertToBooleanSql(translationContext, sqlExpression, translationFlags);

			if (translatedSql == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedSql, methodCallExpression);
		}

		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is MethodCallExpression methodCallExpression)
			{
				Expression? translated;

				if (ProcessToString(translationContext, methodCallExpression, translationFlags, out translated))
					return translated;

				if (ProcessSqlConvert(translationContext, methodCallExpression, translationFlags, out translated))
					return translated;

				if (ProcessSqlConvertTo(translationContext, methodCallExpression, translationFlags, out translated))
					return translated;
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		protected virtual Expression? TranslateSqlConvertTo(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			// For Sql.ConvertTo<T>.From - no rounding, direct cast
			var targetType = methodCallExpression.Method.ReturnType;
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], targetType, translationFlags, true);
		}

		protected virtual Expression? TranslateConvertDefault(ITranslationContext translationContext, MethodCallExpression methodCallExpression, Expression value, Type targetType, TranslationFlags translationFlags, bool serverSideOnly)
		{
			if (!serverSideOnly && translationContext.CanBeEvaluatedOnClient(value))
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

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(byte), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToChar(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(char), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToDateTime(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(DateTime), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToDecimal(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(decimal), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToDouble(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(double), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToInt16(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(short), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToInt32(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(int), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToInt64(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(long), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToSByte(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(sbyte), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToSingle(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(float), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToString(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			return TranslateConvertDefault(translationContext, methodCallExpression, methodCallExpression.Arguments[0], typeof(string), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToUInt16(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(ushort), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToUInt32(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(uint), translationFlags, false);
		}

		protected virtual Expression? TranslateConvertToUInt64(ITranslationContext translationContext, MethodCallExpression methodCallExpression, TranslationFlags translationFlags)
		{
			var value = methodCallExpression.Arguments[0];
			var valueType = value.Type;
			
			if (valueType == typeof(decimal) || valueType == typeof(double) || valueType == typeof(float))
			{
				value = Expression.Call(typeof(Math), nameof(Math.Round), [], value);
			}

			return TranslateConvertDefault(translationContext, methodCallExpression, value, typeof(ulong), translationFlags, false);
		}
	}
}
