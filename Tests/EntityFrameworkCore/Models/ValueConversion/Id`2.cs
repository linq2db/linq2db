using System;
using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.ValueConversion
{
	public static class Id
	{
		public static Id<T, long> AsId<T>(this long id) where T : IEntity<long> => id.AsId<T, long>();

		public static Id<T, Guid> AsId<T>(this Guid guid) where T : IEntity<Guid> => guid.AsId<T, Guid>();
		
		public static Id<TEntity, TKey> AsId<TEntity, TKey>(this TKey @object)
			where TEntity : IEntity<TKey>
			where TKey : notnull
			=> new(@object);
	}
	
	public readonly struct Id<TEntity, TKey> : IEquatable<Id<TEntity, TKey>> where TEntity : IEntity<TKey>
		where TKey: notnull
	{
		internal Id(TKey id) => Value = id;
		internal TKey Value { get; }
		public static implicit operator TKey(Id<TEntity, TKey> id) => id.Value;
		public override string ToString() => $"{typeof(TEntity).Name}({Value})";
		public bool Equals(Id<TEntity, TKey> other) => EqualityComparer<TKey>.Default.Equals(Value, other.Value);
		public override bool Equals(object? obj) => obj is Id<TEntity, TKey> other && Equals(other);
		public override int GetHashCode() => EqualityComparer<TKey>.Default.GetHashCode(Value);
		public static bool operator == (Id<TEntity, TKey> left, Id<TEntity, TKey> right) 
			=> EqualityComparer<TKey>.Default.Equals(left.Value, right.Value);
		public static bool operator != (Id<TEntity, TKey> left, Id<TEntity, TKey> right) => !(left == right);
	}
}
