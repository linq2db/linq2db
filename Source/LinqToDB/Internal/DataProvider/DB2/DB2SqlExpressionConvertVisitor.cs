using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.DataProvider.DB2
{
	public class DB2SqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public DB2SqlExpressionConvertVisitor(bool allowModify) : base(allowModify)
		{
		}

		protected override bool SupportsNullInColumn => false;

		static readonly string[] DB2LikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => DB2LikeCharactersToEscape;

		protected override bool SupportsNullIf => false;

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			return element.Operation switch
			{
				"%" => 
					new SqlFunction(
						element.Type,
						"Mod",
						!element.Expr1.SystemType!.IsIntegerType ? new SqlFunction(MappingSchema.GetDbDataType(typeof(int)), "Int", element.Expr1) : element.Expr1,
						element.Expr2
					),

				"&" => new SqlFunction(element.Type, "BitAnd", element.Expr1, element.Expr2),
				"|" => new SqlFunction(element.Type, "BitOr", element.Expr1, element.Expr2),
				"^" => new SqlFunction(element.Type, "BitXor", element.Expr1, element.Expr2),
				"+" => element.SystemType.IsStringType ? new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence) : element,
				_   => base.ConvertSqlBinaryExpression(element),
			};
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			return func.Name switch
			{
				PseudoFunctions.LENGTH       => func.WithName("CHAR_LENGTH"),
				"Millisecond"                => Div(new SqlFunction(func.Type, "Microsecond", func.Parameters), 1000),
				"SmallDateTime"              or
				"DateTime"                   or
				"DateTime2"                  => new SqlFunction(func.Type, "TimeStamp", func.Parameters),
				"UInt16"                     => new SqlFunction(func.Type, "Int", func.Parameters),
				"UInt32"                     => new SqlFunction(func.Type, "BigInt", func.Parameters),
				"UInt64"                     => new SqlFunction(func.Type, "Decimal", func.Parameters),
				"Byte" or "SByte" or "Int16" => new SqlFunction(func.Type, "SmallInt", func.Parameters),
				"Int32"                      => new SqlFunction(func.Type, "Int", func.Parameters),
				"Int64"                      => new SqlFunction(func.Type, "BigInt", func.Parameters),
				"Double"                     => new SqlFunction(func.Type, "Float", func.Parameters),
				"Single"                     => new SqlFunction(func.Type, "Real", func.Parameters),
				"Money"                      => new SqlFunction(func.Type, "Decimal", func.Parameters[0], new SqlValue(19), new SqlValue(4)),
				"SmallMoney"                 => new SqlFunction(func.Type, "Decimal", func.Parameters[0], new SqlValue(10), new SqlValue(4)),
				"VarChar" when func.Parameters[0].SystemType!.ToUnderlying() == typeof(decimal) => new SqlFunction(func.Type, "Char", func.Parameters[0]),
				"NChar" or "NVarChar"        => new SqlFunction(func.Type, "Char", func.Parameters),
				_                            => base.ConvertSqlFunction(func),
			};
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			cast = FloorBeforeConvert(cast);

			var argument = cast.Expression;

			var isNull = argument is SqlValue sqlValue && sqlValue.Value == null;

			if (isNull)
				return cast.MakeMandatory();

			var toType       = cast.ToType;
			var argumentType = QueryHelper.GetDbDataType(cast.Expression, MappingSchema);

			// type_func(null) is not allowed
			if (argument is not SqlParameter p || !NullabilityContext.CanBeNull(p))
			{
				if (toType.SystemType == typeof(string) && argumentType.SystemType != typeof(string))
					return new SqlFunction(cast.Type, "RTrim", new SqlFunction(MappingSchema.GetDbDataType(typeof(string)), "Char", argument));

				if (toType.Length > 0)
					return new SqlFunction(cast.Type, toType.DataType.ToString(), argument, new SqlValue(toType.Length));

				if (toType.Precision > 0)
					return new SqlFunction(cast.Type, toType.DataType.ToString(), argument, new SqlValue(toType.Precision), new SqlValue(toType.Scale ?? 0));
			}

			if (!cast.IsMandatory && QueryHelper.UnwrapNullablity(argument) is SqlParameter param)
			{
				if (toType.Equals(param.Type))
					return param;

				var paramSystemType = param.Type.SystemType.UnwrapNullableType();

				switch (toType.DataType)
				{
					case DataType.Int32:
						if (paramSystemType == typeof(short))
							return param;
						break;
					case DataType.Int64:
						if (paramSystemType == typeof(short))
							return param;
						if (paramSystemType == typeof(int))
							return param;
						break;

					//TODO: probably others
				}
			}

			return base.ConvertConversion(cast);
		}

		protected override ISqlExpression WrapColumnExpression(ISqlExpression expr)
		{
			var columnExpression = base.WrapColumnExpression(expr);

			if (columnExpression.SystemType == typeof(bool)
				&& QueryHelper.IsBoolean(columnExpression))
			{
				columnExpression = new SqlCastExpression(columnExpression, new DbDataType(columnExpression.SystemType!, DataType.Boolean), null, isMandatory: true);
	}

			return columnExpression;
}
	}
}
