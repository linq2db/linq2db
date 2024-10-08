using System;
using System.Collections.Generic;

namespace LinqToDB.EntityFrameworkCore.Tests.Models.Shared
{
	public static class Id
	{
		public static Id<T, long> AsId<T>(this long id) where T : IHasId<T, long> => id.AsId<T, long>();
		
		public static Id<T, TId> AsId<T, TId>(this TId id)
			where T : IHasId<T, TId>
			where TId : notnull
			=> new(id);
	}   
	
	public readonly struct Id<T, TId> : IEquatable<Id<T, TId>>
		where T : IHasId<T, TId>
		where TId : notnull
	{
		internal Id(TId value) => Value = value;
		TId Value { get; }

		public static implicit operator TId (in Id<T, TId> id) => id.Value;
		public static bool operator == (Id<T, TId> left, Id<T, TId> right) 
			=> EqualityComparer<TId>.Default.Equals(left.Value, right.Value);
		public static bool operator != (Id<T, TId> left, Id<T, TId> right) => !(left == right);

		public override string ToString() => $"{typeof(T).Name}({Value})";
		public bool Equals(Id<T, TId> other) => EqualityComparer<TId>.Default.Equals(Value, other.Value);
		public override bool Equals(object? obj) => obj is Id<T, TId> other && Equals(other);
		public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Value);
	}
}
