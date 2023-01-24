using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace LinqToDB.Common
{
	using Data;
	using Internal;
	using Mapping;

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

			private Func<object?, DataParameter>? _convertValueToParameter;
			public  Func<object?, DataParameter>   ConvertValueToParameter
			{
				get
				{
					if (_convertValueToParameter == null)
					{
						var type = Lambda.Parameters[0].Type;
						var parameterExpression = Expression.Parameter(typeof(object));
						var lambdaExpression = Expression.Lambda<Func<object?, DataParameter>>(
							Expression.Invoke(Lambda, Expression.Convert(parameterExpression, type)), parameterExpression);
						var convertFunc = lambdaExpression.CompileExpression();
						_convertValueToParameter = convertFunc;
					}

					return _convertValueToParameter;
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
				if (_hashCode != null)
					return _hashCode.Value;

				unchecked
				{
					var hashCode = IsSchemaSpecific ? 397 : 0;
					//if (Delegate != null)
					//	hashCode ^= Delegate.Method.GetHashCode();
					hashCode  = (hashCode * 397) ^ CheckNullLambdaID;
					hashCode  = (hashCode * 397) ^ LambdaID;
					_hashCode = hashCode;
				}

				return _hashCode.Value;
			}
		}

		readonly ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>> _expressions = new ();

		public void Set(Type from, Type to, LambdaInfo expr)
		{
			Set(_expressions, new DbDataType(from), new DbDataType(to), expr);
		}

		public void Set(DbDataType from, DbDataType to, LambdaInfo expr)
		{
			Set(_expressions, from, to, expr);
		}

		static void Set(ConcurrentDictionary<DbDataType,ConcurrentDictionary<DbDataType,LambdaInfo>> expressions, DbDataType from, DbDataType to, LambdaInfo expr)
		{
			if (!expressions.TryGetValue(from, out var dic))
				expressions[from] = dic = new ();

			dic[to] = expr;
		}

		public LambdaInfo? Get(DbDataType from, DbDataType to)
		{
			return _expressions.TryGetValue(from, out var dic) && dic.TryGetValue(to, out var li) ? li : null;
		}

		public LambdaInfo? Get(Type from, Type to)
		{
			return _expressions.TryGetValue(new DbDataType(from), out var dic) && dic.TryGetValue(new DbDataType(to), out var li) ? li : null;
		}

		public LambdaInfo Create(MappingSchema? mappingSchema, Type from, Type to)
		{
			return Create(mappingSchema, new DbDataType(from), new DbDataType(to));
		}

		public LambdaInfo Create(MappingSchema? mappingSchema, DbDataType from, DbDataType to)
		{
			var ex  = ConvertBuilder.GetConverter(mappingSchema, from.SystemType, to.SystemType);
			var lm  = ex.Item1.CompileExpression();
			var ret = new LambdaInfo(ex.Item1, ex.Item2, lm, ex.Item3);

			Set(_expressions, from, to , ret);

			return ret;
		}

		public int GetConfigurationID()
		{
			if (_expressions.IsEmpty)
				return 0;

			using var idBuilder = new IdentifierBuilder(_expressions.Count);

			foreach (var (id, types) in _expressions
				.Select (static e => (id : IdentifierBuilder.GetObjectID(e.Key), types : e.Value))
				.OrderBy(static t => t.id))
			{
				idBuilder.Add(id).Add(types.Count);

				foreach (var (id2, value) in types
					.Select (static e => (id2 : IdentifierBuilder.GetObjectID(e.Key), value : e.Value))
					.OrderBy(static t => t.id2))
				{
					idBuilder.Add(id2).Add(IdentifierBuilder.GetObjectID(value));
				}
			}

			return idBuilder.CreateID();
		}
	}
}
