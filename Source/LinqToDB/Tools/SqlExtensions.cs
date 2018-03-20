using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Tools
{
	using Data;

	public static class SqlExtensions
	{
		#region In/NotIn

		[ExpressionMethod(nameof(InImpl1))]
		public static bool In<T>(this T value, IEnumerable<T> sequence)
		{
			return sequence.Contains(value);
		}

		static Expression<Func<T,IEnumerable<T>,bool>> InImpl1<T>()
		{
			return (value,sequence) => sequence.Contains(value);
		}

		[ExpressionMethod(nameof(InImpl2))]
		public static bool In<T>(this T value, IQueryable<T> sequence)
		{
			return sequence.Contains(value);
		}

		static Expression<Func<T,IQueryable<T>,bool>> InImpl2<T>()
		{
			return (value,sequence) => sequence.Contains(value);
		}

		[ExpressionMethod(nameof(InImpl3))]
		public static bool In<T>(this T value, params T[] sequence)
		{
			return sequence.Contains(value);
		}

		static Expression<Func<T,T[],bool>> InImpl3<T>()
		{
			return (value,sequence) => sequence.Contains(value);
		}

		[ExpressionMethod(nameof(InImpl4))]
		public static bool In<T>(this T value, T cmp1, T cmp2)
		{
			return object.Equals(value, cmp1) || object.Equals(value, cmp2);
		}

		static Expression<Func<T,T,T,bool>> InImpl4<T>()
		{
			return (value,cmp1,cmp2) => value.In(new[] { cmp1, cmp2 });
		}

		[ExpressionMethod(nameof(InImpl5))]
		public static bool In<T>(this T value, T cmp1, T cmp2, T cmp3)
		{
			return object.Equals(value, cmp1) || object.Equals(value, cmp2) || object.Equals(value, cmp3);
		}

		static Expression<Func<T,T,T,T,bool>> InImpl5<T>()
		{
			return (value,cmp1,cmp2,cmp3) => value.In(new[] { cmp1, cmp2, cmp3 });
		}

		[ExpressionMethod(nameof(NotInImpl1))]
		public static bool NotIn<T>(this T value, IEnumerable<T> sequence)
		{
			return !sequence.Contains(value);
		}

		static Expression<Func<T,IEnumerable<T>,bool>> NotInImpl1<T>()
		{
			return (value,sequence) => !sequence.Contains(value);
		}

		[ExpressionMethod(nameof(NotInImpl2))]
		public static bool NotIn<T>(this T value, IQueryable<T> sequence)
		{
			return !sequence.Contains(value);
		}

		static Expression<Func<T,IQueryable<T>,bool>> NotInImpl2<T>()
		{
			return (value,sequence) => !sequence.Contains(value);
		}

		[ExpressionMethod(nameof(NotInImpl3))]
		public static bool NotIn<T>(this T value, params T[] sequence)
		{
			return !sequence.Contains(value);
		}

		static Expression<Func<T,T[],bool>> NotInImpl3<T>()
		{
			return (value,sequence) => !sequence.Contains(value);
		}

		[ExpressionMethod(nameof(NotInImpl4))]
		public static bool NotIn<T>(this T value, T cmp1, T cmp2)
		{
			return !object.Equals(value, cmp1) && !object.Equals(value, cmp2);
		}

		static Expression<Func<T,T,T,bool>> NotInImpl4<T>()
		{
			return (value,cmp1,cmp2) => value.NotIn(new[] { cmp1, cmp2 });
		}

		[ExpressionMethod(nameof(NotInImpl5))]
		public static bool NotIn<T>(this T value, T cmp1, T cmp2, T cmp3)
		{
			return !object.Equals(value, cmp1) && !object.Equals(value, cmp2) && !object.Equals(value, cmp3);
		}

		static Expression<Func<T,T,T,T,bool>> NotInImpl5<T>()
		{
			return (value,cmp1,cmp2,cmp3) => value.NotIn(new[] { cmp1, cmp2, cmp3 });
		}

		#endregion

//		#region Truncate
//
//		public static int Truncate<T>(this ITable<T> table)
//		{
//			var tableName = table.GetTableName();
//
//			switch (table.DataContext)
//			{
//				case DataConnection dc : return dc.                    Execute<int>($"TRUNCATE TABLE {tableName}");
//				case DataContext    dx : return dx.GetDataConnection().Execute<int>($"TRUNCATE TABLE {tableName}");
//				default                : throw new NotImplementedException();
//			}
//		}
//
//		#endregion
	}
}
