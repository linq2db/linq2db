using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.InteropServices;

using FluentNHibernate.Mapping;

using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace LinqToDB.NHibernate.Tests.Models.UserTypes
{
	// Single-column IUserType conversions: a decimal-backed value struct and an enum stored as a string.
	// Both are the shape the bridge supports — one column, no session use.

	public readonly struct Money : IEquatable<Money>
	{
		public Money(decimal amount) => Amount = amount;

		public decimal Amount { get; }

		public bool Equals(Money other)     => Amount == other.Amount;
		public override bool Equals(object? obj) => obj is Money other && Equals(other);
		public override int GetHashCode()   => Amount.GetHashCode();
		public override string ToString()   => Amount.ToString(CultureInfo.InvariantCulture);

		public static bool operator ==(Money left, Money right) => left.Equals(right);
		public static bool operator !=(Money left, Money right) => !left.Equals(right);
	}

	public enum Priority
	{
		Low,
		High,
	}

	public class MoneyUserType : IUserType
	{
		public SqlType[] SqlTypes   => new[] { NHibernateUtil.Decimal.SqlType };
		public Type      ReturnedType => typeof(Money);
		public bool      IsMutable    => false;

		public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			var value = NHibernateUtil.Decimal.NullSafeGet(rs, names[0], session);
			return value == null ? null : new Money((decimal)value);
		}

		public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
		{
			NHibernateUtil.Decimal.NullSafeSet(cmd, value == null ? null : ((Money)value).Amount, index, session);
		}

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y) || (x != null && x.Equals(y));
		public int GetHashCode(object x)             => x.GetHashCode();
		public object? DeepCopy(object? value)       => value;
		public object? Replace(object? original, object? target, object? owner) => original;
		public object? Assemble(object? cached, object? owner)                  => cached;
		public object? Disassemble(object? value)                               => value;
	}

	public class PriorityUserType : IUserType
	{
		public SqlType[] SqlTypes     => new[] { NHibernateUtil.String.SqlType };
		public Type      ReturnedType => typeof(Priority);
		public bool      IsMutable    => false;

		public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			var value = (string?)NHibernateUtil.String.NullSafeGet(rs, names[0], session);
			return value == null ? null : (object)(value == "H" ? Priority.High : Priority.Low);
		}

		public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
		{
			NHibernateUtil.String.NullSafeSet(cmd, value == null ? null : ((Priority)value == Priority.High ? "H" : "L"), index, session);
		}

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y) || (x != null && x.Equals(y));
		public int GetHashCode(object x)             => x.GetHashCode();
		public object? DeepCopy(object? value)       => value;
		public object? Replace(object? original, object? target, object? owner) => original;
		public object? Assemble(object? cached, object? owner)                  => cached;
		public object? Disassemble(object? value)                               => value;
	}

	// A user type spanning TWO columns — the shape the bridge cannot express as a single value conversion.
	[StructLayout(LayoutKind.Auto)]
	public readonly struct DayRange
	{
		public DayRange(int from, int to)
		{
			From = from;
			To   = to;
		}

		public int From { get; }
		public int To   { get; }
	}

	public class DayRangeUserType : IUserType
	{
		public SqlType[] SqlTypes     => new[] { NHibernateUtil.Int32.SqlType, NHibernateUtil.Int32.SqlType };
		public Type      ReturnedType => typeof(DayRange);
		public bool      IsMutable    => false;

		public object? NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
		{
			var from = NHibernateUtil.Int32.NullSafeGet(rs, names[0], session);
			var to   = NHibernateUtil.Int32.NullSafeGet(rs, names[1], session);

			return from == null || to == null ? null : new DayRange((int)from, (int)to);
		}

		public void NullSafeSet(DbCommand cmd, object? value, int index, ISessionImplementor session)
		{
			var range = value == null ? default : (DayRange)value;

			NHibernateUtil.Int32.NullSafeSet(cmd, value == null ? null : range.From, index,     session);
			NHibernateUtil.Int32.NullSafeSet(cmd, value == null ? null : range.To,   index + 1, session);
		}

		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y) || (x != null && x.Equals(y));
		public int GetHashCode(object x)             => x.GetHashCode();
		public object? DeepCopy(object? value)       => value;
		public object? Replace(object? original, object? target, object? owner) => original;
		public object? Assemble(object? cached, object? owner)                  => cached;
		public object? Disassemble(object? value)                               => value;
	}

	public class Reservation
	{
		public virtual int      Id     { get; set; }
		public virtual DayRange Period { get; set; }
	}

	public class ReservationMap : ClassMap<Reservation>
	{
		public ReservationMap()
		{
			Table("Reservation");
			Id(x => x.Id).GeneratedBy.Assigned().Column("ReservationId");

			var period = Map(x => x.Period).CustomType<DayRangeUserType>();
			period.Columns.Clear();
			period.Columns.Add("FromDay", "ToDay");
		}
	}

	public class Payment
	{
		public virtual int      Id       { get; set; }
		public virtual Money    Amount   { get; set; }
		public virtual Priority Priority { get; set; }
	}

	public class PaymentMap : ClassMap<Payment>
	{
		public PaymentMap()
		{
			Table("Payment");
			Id(x => x.Id).GeneratedBy.Assigned().Column("PaymentId");
			Map(x => x.Amount).Column("Amount").CustomType<MoneyUserType>().Not.Nullable();
			Map(x => x.Priority).Column("Priority").CustomType<PriorityUserType>().Length(1).Not.Nullable();
		}
	}
}
