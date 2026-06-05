using System;

using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	public class YdbSqlExpressionConvertVisitor : SqlExpressionConvertVisitor
	{
		public YdbSqlExpressionConvertVisitor(bool allowModify) : base(allowModify) { }

		/// <inheritdoc/>
		protected override bool SupportsNullInColumn => false;

		// YQL has no NULLIF builtin. Keep the CASE WHEN a = b THEN NULL ELSE a END form (which YDB
		// supports) instead of folding it to NULLIF.
		protected override bool SupportsNullIf => false;

		// YQL's `||` auto-coerces non-string operands to text, so explicit CAST(x AS String) on
		// concat operands is redundant. Omit it for cleaner SQL (like the other `||` providers).
		protected override bool ConcatRequiresExplicitStringCast => false;

		#region (I)LIKE https://ydb.tech/docs/en/yql/reference/syntax/expressions#check-match

		protected static string[] YdbLikeCharactersToEscape = {"%", "_"};

		public override string[] LikeCharactersToEscape => YdbLikeCharactersToEscape;

		// escape value literal should have String type
		public override ISqlExpression CreateLikeEscapeCharacter() => new SqlValue(new DbDataType(typeof(string), DataType.VarBinary), LikeEscapeCharacter);

		public override ISqlPredicate ConvertSearchStringPredicate(SqlPredicate.SearchString predicate)
		{
			var searchPredicate = ConvertSearchStringPredicateViaLike(predicate);

			// use ILIKE for case-insensitive search
			if (false == predicate.CaseSensitive.EvaluateBoolExpression(EvaluationContext) && searchPredicate is SqlPredicate.Like likePredicate)
			{
				searchPredicate = new SqlPredicate.Like(likePredicate.Expr1, likePredicate.IsNot, likePredicate.Expr2, likePredicate.Escape, "ILIKE");
			}

			return searchPredicate;
		}

		#endregion

		// YQL's IN requires a single-column source. A row-valued IN-subquery projects N columns
		// ("(a, b) IN (SELECT c1, c2 ...)"), which YDB rejects ("expecting single column table source").
		// Fold the projection into one tuple column ("(a, b) IN (SELECT (c1, c2) ...)") - the form YQL
		// accepts, since the left side is already a row. The correlated-EXISTS rewrite the base would use
		// is unavailable here (YDB doesn't support correlated subqueries).
		protected internal override IQueryElement VisitInSubQueryPredicate(SqlPredicate.InSubQuery predicate)
		{
			if (!predicate.DoNotConvert
				&& QueryHelper.UnwrapNullablity(predicate.Expr1) is SqlRowExpression
				&& predicate.SubQuery.Select.Columns.Count > 1)
			{
				var subQuery = predicate.SubQuery.CloneQuery();

				var values = new ISqlExpression[subQuery.Select.Columns.Count];
				for (var i = 0; i < values.Length; i++)
					values[i] = subQuery.Select.Columns[i].Expression;

				subQuery.Select.Columns.Clear();
				subQuery.Select.Columns.Add(new SqlColumn(subQuery, new SqlRowExpression(values)));

				return Visit(new SqlPredicate.InSubQuery(predicate.Expr1, predicate.IsNot, subQuery, predicate.DoNotConvert));
			}

			return base.VisitInSubQueryPredicate(predicate);
		}

		public override ISqlExpression ConvertSqlUnaryExpression(SqlUnaryExpression element)
		{
			if (element.Operation is SqlUnaryOperation.BitwiseNegation)
			{
				var argType = QueryHelper.GetDbDataType(element.Expr, MappingSchema);
				if (!argType.IsUnsignedType())
				{
					var expr = new SqlCastExpression(element.Expr, argType.ToUnsigned(), null, false);
					return new SqlUnaryExpression(element.Type, expr, element.Operation, element.Precedence);
				}
			}

			return base.ConvertSqlUnaryExpression(element);
		}

		public override IQueryElement ConvertSqlBinaryExpression(SqlBinaryExpression element)
		{
			// YQL arithmetic is strict about numeric types: mixing a floating-point operand with a
			// Decimal operand (e.g. CAST(x AS Double) / Decimal('15')) is rejected. Align by casting
			// the Decimal side to Double.
			if (element.Operation is "+" or "-" or "*" or "/" or "%")
			{
				var aligned = AlignFloatingDecimal(element);
				if (!ReferenceEquals(aligned, element))
					return Visit(Optimize(aligned));
			}

			switch (element.Operation)
			{
				case "+" when element.Type.DataType
					is DataType.Binary
					or DataType.VarBinary
					or DataType.Blob:
				{
					return new SqlBinaryExpression(element.SystemType, element.Expr1, "||", element.Expr2, element.Precedence);
				}

				case "&" or "|" or "^":
				{
					var expr1Type = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
					var expr2Type = QueryHelper.GetDbDataType(element.Expr2, MappingSchema);
					var expr1     = expr1Type.IsUnsignedType() ? element.Expr1 : new SqlCastExpression(element.Expr1, expr1Type.ToUnsigned(), null, false);
					var expr2     = expr2Type.IsUnsignedType() ? element.Expr2 : new SqlCastExpression(element.Expr2, expr2Type.ToUnsigned(), null, false);

					if (expr1 != element.Expr1 || expr2 != element.Expr2)
					{
						ISqlExpression expr = new SqlBinaryExpression(element.SystemType, expr1, element.Operation, expr2, element.Precedence);
						var oldType         = QueryHelper.GetDbDataType(element, MappingSchema);
						var newType         = QueryHelper.GetDbDataType(element, MappingSchema);

						if (!oldType.EqualsDbOnly(newType))
						{
							expr = new SqlCastExpression(expr, oldType, null, false);
						}

						return expr;
					}

					break;
				}

				//case "%":
				//{
				//	var dbType = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);

				//	if (dbType.SystemType.ToNullableUnderlying() != typeof(decimal))
				//	{
				//		var toType   = MappingSchema.GetDbDataType(typeof(decimal));
				//		var newLeft  = PseudoFunctions.MakeCast(element.Expr1, toType);

				//		var sysType  = dbType.SystemType?.IsNullable() == true
				//			? typeof(decimal?)
				//			: typeof(decimal);

				//		var newExpr  = PseudoFunctions.MakeMandatoryCast(
				//			new SqlBinaryExpression(sysType, newLeft, "%", element.Expr2),
				//			toType);

				//		return Visit(Optimize(newExpr));
				//	}

				//	break;
				//}
			}

			return base.ConvertSqlBinaryExpression(element);
		}

		// Cast a Decimal operand to Double when the other operand is floating-point, so YQL accepts the
		// mixed-type arithmetic. (Decimal/Decimal and floating/floating pairs are left untouched.)
		SqlBinaryExpression AlignFloatingDecimal(SqlBinaryExpression element)
		{
			var t1 = QueryHelper.GetDbDataType(element.Expr1, MappingSchema);
			var t2 = QueryHelper.GetDbDataType(element.Expr2, MappingSchema);

			static bool IsFloating(DbDataType t) => t.DataType is DataType.Double or DataType.Single
				|| t.SystemType.UnwrappedNullableType == typeof(double) || t.SystemType.UnwrappedNullableType == typeof(float);
			static bool IsDecimal(DbDataType t) => t.DataType == DataType.Decimal
				|| t.SystemType.UnwrappedNullableType == typeof(decimal);

			var doubleType = MappingSchema.GetDbDataType(typeof(double));

			if (IsFloating(t1) && IsDecimal(t2))
				return new SqlBinaryExpression(element.SystemType, element.Expr1, element.Operation, new SqlCastExpression(element.Expr2, doubleType, null), element.Precedence);

			if (IsDecimal(t1) && IsFloating(t2))
				return new SqlBinaryExpression(element.SystemType, new SqlCastExpression(element.Expr1, doubleType, null), element.Operation, element.Expr2, element.Precedence);

			return element;
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			// Math::Floor/Ceil/Trunc are Double-only. For a Decimal argument, route through Double and
			// cast the result back to the argument's Decimal type (ConvertConversion turns the
			// Double->Decimal cast into the CAST(CAST(x AS Text) AS Decimal(p,s)) round-trip).
			if (func.Name is "Math::Floor" or "Math::Ceil" or "Math::Trunc" && func.Parameters.Length == 1)
			{
				var argType = QueryHelper.GetDbDataType(func.Parameters[0], MappingSchema);
				if (argType.DataType == DataType.Decimal || argType.SystemType.UnwrappedNullableType == typeof(decimal))
				{
					var doubleType = MappingSchema.GetDbDataType(typeof(double));
					var applied    = new SqlFunction(doubleType, func.Name, func.CanBeNull, new SqlCastExpression(func.Parameters[0], doubleType, null));

					return (ISqlExpression)Visit(Optimize(new SqlCastExpression(applied, argType, null)));
				}
			}

			return func.Name switch
			{
				PseudoFunctions.TO_LOWER => func.WithName("Unicode::ToLower"),
				PseudoFunctions.TO_UPPER => func.WithName("Unicode::ToUpper"),
				PseudoFunctions.REPLACE  => func.WithName("Unicode::ReplaceAll"),

				_                        => base.ConvertSqlFunction(func),
			};

			////----------------------------------------------------------------
			//// save cast
			//case PseudoFunctions.TRY_CONVERT:
			//	// CAST(x AS <type>?) → null
			//	return new SqlExpression(
			//		func.Type,
			//		"CAST({0} AS {1}?)",
			//		Precedence.Primary,
			//		func.Parameters[2],      // value
			//		func.Parameters[0]);     // type

			//case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
			//	// COALESCE(CAST(x AS <type>?), default)
			//	return new SqlExpression(
			//			func.Type,
			//			"COALESCE(CAST({0} AS {1}?), {2})",
			//			Precedence.Primary,
			//			func.Parameters[2],    // value
			//			func.Parameters[0],    // type
			//			func.Parameters[3])    // default
			//	{
			//		CanBeNull =
			//				func.Parameters[2].CanBeNullable(NullabilityContext) ||
			//				func.Parameters[3].CanBeNullable(NullabilityContext)
			//	};

			////----------------------------------------------------------------
			//// CharIndex (there is no POSITION analog in YDB; using FIND)
			//case "CharIndex":
			//	switch (func.Parameters.Length)
			//	{
			//		// CharIndex(substr, str)
			//		case 2:
			//			return new SqlExpression(
			//				func.Type,
			//				"COALESCE(FIND({1}, {0}) + 1, 0)",
			//				Precedence.Primary,
			//				func.Parameters[0],    // substring
			//				func.Parameters[1]);   // source

			//		// CharIndex(substr, str, start)
			//		case 3:
			//			return new SqlExpression(
			//				func.Type,
			//				"COALESCE(FIND(SUBSTRING({1}, {2} - 1), {0}) + {2}, 0)",
			//				Precedence.Primary,
			//				func.Parameters[0],    // substring
			//				func.Parameters[1],    // source
			//				func.Parameters[2]);   // start
			//	}

			//	break;
		}

		protected override ISqlExpression ConvertConversion(SqlCastExpression cast)
		{
			// string -> Datetime/Timestamp: YQL CAST accepts only ISO-8601 with the 'T'/'Z' markers; the
			// conventional 'yyyy-MM-dd HH:mm:ss' linq2db emits casts to an empty Optional that Unwrap then
			// fails on ("Failed to unwrap empty optional"). Rewrite to ISO (space -> 'T', append 'Z') and parse
			// with DateTime::ParseIso8601 + Make*. We avoid CAST (its string-source would re-enter
			// ConvertConversion) and the curried DateTime::Parse(fmt)(value) (evaluated as a callable, it breaks
			// with "unresolved argument" when reused in a nested / Coalesce context such as a subquery filter).
			// Date is left to the base CAST (it accepts a bare 'yyyy-MM-dd'); only date+time has the space.
			var makeFunction = cast.ToType.DataType switch
			{
				DataType.DateTime                             => "DateTime::MakeDatetime",
				DataType.DateTime2 or DataType.DateTimeOffset => "DateTime::MakeTimestamp",
				_                                             => null,
			};

			if (makeFunction != null
				&& QueryHelper.GetDbDataType(cast.Expression, MappingSchema).SystemType.UnwrappedNullableType == typeof(string))
			{
				var stringType = MappingSchema.GetDbDataType(typeof(string));
				var canBeNull  = cast.Expression.CanBeNullable(NullabilityContext);

				var iso = new SqlConcatExpression(
					true,
					new SqlFunction(stringType, "Unicode::ReplaceAll", canBeNull, cast.Expression, new SqlValue(stringType, " "), new SqlValue(stringType, "T")),
					new SqlValue(stringType, "Z"));

				var parsed = new SqlFunction(cast.ToType, "DateTime::ParseIso8601", canBeNull, iso);

				return new SqlFunction(cast.ToType, makeFunction, canBeNull, parsed);
			}

			// string -> Interval (Time): YQL CAST(string AS Interval) parses only ISO-8601 durations ('PT..');
			// the conventional 'H:MM:SS' silently casts to a zero Interval. Split on ':' and sum the parts as
			// seconds. SplitToList/ListSkip/ListHead avoid raw list indexing; a strptime parse is unusable here
			// because the hour is unpadded (DateTimeValue.Hour) and %H rejects a single digit.
			if (cast.ToType.DataType is DataType.Time or DataType.Interval
				&& QueryHelper.GetDbDataType(cast.Expression, MappingSchema).SystemType.UnwrappedNullableType == typeof(string))
			{
				var stringType = MappingSchema.GetDbDataType(typeof(string));
				var intType    = MappingSchema.GetDbDataType(typeof(int));
				var canBeNull  = cast.Expression.CanBeNullable(NullabilityContext);

				SqlFunction Split()        => new SqlFunction(stringType, "Unicode::SplitToList", canBeNull, cast.Expression, new SqlValue(stringType, ":"));
				ISqlExpression Component(int index)
				{
					var element = index == 0
						? new SqlFunction(stringType, "ListHead", canBeNull, Split())
						: new SqlFunction(stringType, "ListHead", canBeNull, new SqlFunction(stringType, "ListSkip", canBeNull, Split(), new SqlValue(intType, index)));

					return new SqlCastExpression(element, intType, null);
				}

				var seconds = new SqlBinaryExpression(intType,
					new SqlBinaryExpression(intType,
						new SqlBinaryExpression(intType, Component(0), "*", new SqlValue(intType, 3600), Precedence.Multiplicative),
						"+",
						new SqlBinaryExpression(intType, Component(1), "*", new SqlValue(intType, 60), Precedence.Multiplicative),
						Precedence.Additive),
					"+",
					Component(2),
					Precedence.Additive);

				return new SqlFunction(cast.ToType, "DateTime::IntervalFromSeconds", canBeNull, seconds);
			}

			// YQL has no direct floating-point -> Decimal cast: CAST(<Double/Float> AS Decimal(p,s))
			// fails with a type-annotation error. Route it through a string, which YDB accepts:
			// CAST(CAST(x AS Text) AS Decimal(p,s)).
			if (cast.ToType.DataType == DataType.Decimal
				|| cast.ToType.SystemType.UnwrappedNullableType == typeof(decimal))
			{
				var fromType = QueryHelper.GetDbDataType(cast.Expression, MappingSchema);
				var fromSys  = fromType.SystemType.UnwrappedNullableType;

				if (fromType.DataType is DataType.Double or DataType.Single
					|| fromSys == typeof(double) || fromSys == typeof(float))
				{
					var stringType = MappingSchema.GetDbDataType(typeof(string)).WithDataType(DataType.NVarChar);
					var viaString  = new SqlCastExpression(cast.Expression, stringType, null);

					return new SqlCastExpression(viaString, cast.ToType, cast.FromType, cast.IsMandatory);
				}
			}

			// YQL CAST(<Decimal> AS <integer>) rounds, but C# (int)decimal truncates toward zero.
			// CAST(<Double> AS <integer>) truncates, so route Decimal->integer casts through Double.
			if (cast.ToType.SystemType.UnwrappedNullableType.IsIntegerType)
			{
				var fromType = QueryHelper.GetDbDataType(cast.Expression, MappingSchema);

				if (fromType.DataType == DataType.Decimal || fromType.SystemType.UnwrappedNullableType == typeof(decimal))
				{
					var doubleType = MappingSchema.GetDbDataType(typeof(double));
					var viaDouble  = new SqlCastExpression(cast.Expression, doubleType, null);

					return base.ConvertConversion(new SqlCastExpression(viaDouble, cast.ToType, cast.FromType, cast.IsMandatory));
				}
			}

			return base.ConvertConversion(cast);
		}

		protected override ISqlExpression ConvertSqlCaseExpression(SqlCaseExpression element)
		{
			// ELSE required
			if (element.ElseExpression == null)
				return new SqlCaseExpression(element.Type, element.Cases, new SqlValue(element.Type, null));

			return base.ConvertSqlCaseExpression(element);
		}
	}
}
