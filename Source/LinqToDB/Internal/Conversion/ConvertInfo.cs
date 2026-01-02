using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Conversion
{
	sealed class ConvertInfo
	{
		public static ConvertInfo Default = new ();

		public sealed class LambdaInfo : IEquatable<LambdaInfo>
		{
			public LambdaInfo(
				LambdaExpression  checkNullLambda,
				LambdaExpression? lambda,
				Delegate?         @delegate,
				bool              isSchemaSpecific)
			{
				CheckNullLambda  = checkNullLambda;
				Lambda           = lambda ?? checkNullLambda;
				Delegate         = @delegate;
				IsSchemaSpecific = isSchemaSpecific;
			}

			public readonly LambdaExpression Lambda;
			public readonly LambdaExpression CheckNullLambda;
			public readonly Delegate?        Delegate;
			public readonly bool             IsSchemaSpecific;

			public Func<object?, DataParameter> ConvertValueToParameter
			{
				get
				{
					return field ??= BuildField();

					Func<object?, DataParameter> BuildField()
					{
						var type = Lambda.Parameters[0].Type;
						var parameterExpression = Expression.Parameter(typeof(object));
						var lambdaExpression = Expression.Lambda<Func<object?, DataParameter>>(
							Expression.Invoke(Lambda, Expression.Convert(parameterExpression, type)), parameterExpression);
						var convertFunc = lambdaExpression.CompileExpression();
						return convertFunc;
					}
				}
			}

			public bool Equals(LambdaInfo? other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;

				return
					IsSchemaSpecific  == other.IsSchemaSpecific  &&
					CheckNullLambdaID == other.CheckNullLambdaID &&
					LambdaID          == other.LambdaID;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != GetType()) return false;

				return Equals((LambdaInfo)obj);
			}

			int? _hashCode;
			int? _checkNullLambdaID;
			int? _lambdaID;

			int CheckNullLambdaID => _checkNullLambdaID ??= IdentifierBuilder.GetObjectID(CheckNullLambda);
			int LambdaID          => _lambdaID          ??= IdentifierBuilder.GetObjectID(Lambda);

			// ReSharper disable NonReadonlyMemberInGetHashCode
			public override int GetHashCode()
			{
				return _hashCode ??= HashCode.Combine(
					IsSchemaSpecific,
					CheckNullLambdaID,
					LambdaID
				);
			}
		}

		readonly ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>>  _expressions = new ();
		         ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>>? _toDatabaseExpressions;
		         ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>>? _fromDatabaseExpressions;

		readonly Lock _sync = new();

		ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>> GetForSetExpressions(ConversionType conversionType)
		{
			switch (conversionType)
			{
				case ConversionType.Common:
					return _expressions;
				case ConversionType.FromDatabase:
					lock (_sync)
						return _fromDatabaseExpressions ??= new();
				case ConversionType.ToDatabase:
					lock (_sync)
						return _toDatabaseExpressions ??= new();
				default:
					throw new ArgumentOutOfRangeException(nameof(conversionType), conversionType, null);
			}
		}

		public void Set(Type from, Type to, ConversionType conversionType, LambdaInfo expr)
		{
			Set(GetForSetExpressions(conversionType), new DbDataType(from), new DbDataType(to), expr);
		}

		public void Set(DbDataType from, DbDataType to, ConversionType conversionType, LambdaInfo expr)
		{
			Set(GetForSetExpressions(conversionType), from, to, expr);
		}

		static void Set(ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>> expressions, DbDataType from, DbDataType to, LambdaInfo expr)
		{
			(expressions.GetOrAdd(from, _ => new()))[to] = expr;
		}

		public LambdaInfo? Get(DbDataType from, DbDataType to, ConversionType conversionType)
		{
			switch (conversionType)
			{
				case ConversionType.FromDatabase:
				{
					if (_fromDatabaseExpressions != null &&
						_fromDatabaseExpressions.TryGetValue(from, out var dic) && dic.TryGetValue(to, out var li))
						return li;
					break;
				}
				case ConversionType.ToDatabase:
				{
					if (_toDatabaseExpressions != null &&
						_toDatabaseExpressions.TryGetValue(from, out var dic) && dic.TryGetValue(to, out var li))
						return li;
					break;
				}
			}

			{
				return _expressions.TryGetValue(from, out var dic) && dic.TryGetValue(to, out var li) ? li : null;
			}
		}

		public LambdaInfo? Get(Type from, Type to, ConversionType conversionType)
		{
			return Get(new DbDataType(from), new DbDataType(to), conversionType);
		}

		public LambdaInfo Create(MappingSchema? mappingSchema, Type from, Type to, ConversionType conversionType)
		{
			return Create(mappingSchema, new DbDataType(from), new DbDataType(to), conversionType);
		}

		public LambdaInfo Create(MappingSchema? mappingSchema, DbDataType from, DbDataType to, ConversionType conversionType)
		{
			var ex  = ConvertBuilder.GetConverter(mappingSchema, from.SystemType, to.SystemType);
			var lm  = ex.CheckNullLambda.CompileExpression();
			var ret = new LambdaInfo(ex.CheckNullLambda, ex.Lambda, lm, ex.IsSchemaSpecific);

			Set(GetForSetExpressions(conversionType), from, to , ret);

			return ret;
		}

		public int GetConfigurationID()
		{
			if (_expressions.IsEmpty && _fromDatabaseExpressions == null && _toDatabaseExpressions == null)
				return 0;

			using var idBuilder = new IdentifierBuilder(_expressions.Count + (_fromDatabaseExpressions?.Count ?? 0) + (_toDatabaseExpressions?.Count ?? 0));

			IdentifyExpressions(idBuilder, _expressions);

			if (_fromDatabaseExpressions != null) IdentifyExpressions(idBuilder, _fromDatabaseExpressions);
			if (_toDatabaseExpressions   != null) IdentifyExpressions(idBuilder, _toDatabaseExpressions);

			return idBuilder.CreateID();

			static void IdentifyExpressions(IdentifierBuilder identifierBuilder, ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>> expressions)
			{
				foreach (var (id, types) in expressions
					.Select (static e => (id : IdentifierBuilder.GetObjectID(e.Key), types : e.Value))
					.OrderBy(static t => t.id))
				{
					identifierBuilder.Add(id).Add(types.Count);

					foreach (var (id2, value) in types
						.Select (static e => (id2 : IdentifierBuilder.GetObjectID(e.Key), value : e.Value))
						.OrderBy(static t => t.id2))
					{
						identifierBuilder.Add(id2).Add(IdentifierBuilder.GetObjectID(value));
					}
				}
			}
		}
	}
}
