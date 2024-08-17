using System;
using System.Threading.Tasks;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public static class AAA
	{
		internal static ArrangeResult<T, Unit> Arrange<T>(this T @object, Action<T> action)
		{
			action(@object);
			return new ArrangeResult<T, Unit>(@object, default);
		}

		internal static ArrangeResult<T, Unit> Arrange<T>(T @object) 
			=> new(@object, default);

		internal static ArrangeResult<T, TMock> Arrange<T, TMock>(this TMock mock, Func<TMock, T> @object)
			where TMock: notnull
			=> new(@object(mock), mock);

		internal static ActResult<T, TMock> Act<T, TMock>(this ArrangeResult<T, TMock> arrange, Action<T> act)
			where T : notnull
			where TMock : notnull
		{
#pragma warning disable CA1031 // Do not catch general exception types
			try
			{
				act(arrange.Object);
				return new ActResult<T, TMock>(arrange.Object, arrange.Mock, default);
			}
			catch (Exception e)
			{
				return new ActResult<T, TMock>(arrange.Object, arrange.Mock, e);
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		internal static ActResult<TResult, TMock> Act<T, TMock, TResult>(this ArrangeResult<T, TMock> arrange, Func<T, TResult> act)
			where TResult : notnull
			where TMock : notnull
		{
#pragma warning disable CA1031 // Do not catch general exception types
			try
			{
				return new ActResult<TResult, TMock>(act(arrange.Object), arrange.Mock, default);
			}
			catch (Exception e)
			{
				return new ActResult<TResult, TMock>(default, arrange.Mock, e);
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		internal static void Assert<T, TMock>(this ActResult<T, TMock> act, Action<T?> assert)
			where T : notnull
			where TMock : notnull
		{
			act.Exception?.Throw();
			assert(act.Object);
		}

		internal static void Assert<T, TMock>(this ActResult<T, TMock> act, Action<T?, TMock?> assert)
			where T : notnull
			where TMock : notnull
		{
			act.Exception?.Throw();
			assert(act.Object, act.Mock);
		}

		internal static Task<ArrangeResult<T, Unit>> ArrangeAsync<T>(T @object)
			=> Task.FromResult(new ArrangeResult<T, Unit>(@object, default));

		internal static async Task<ActResult<TResult, TMock>> Act<T, TMock, TResult>(this Task<ArrangeResult<T, TMock>> arrange, Func<T, Task<TResult>> act)
			where TMock : notnull
			where TResult : notnull
		{
			var a = await arrange;
#pragma warning disable CA1031 // Do not catch general exception types
			try
			{
				return new ActResult<TResult, TMock>(await act(a.Object), a.Mock, default);
			}
			catch (Exception e)
			{
				return new ActResult<TResult, TMock>(default, a.Mock, e);
			}
#pragma warning restore CA1031 // Do not catch general exception types
		}

		internal static async Task Assert<T, TMock>(this Task<ActResult<T, TMock>> act, Func<T?, Task> assert)
			where T : notnull
			where TMock : notnull
		{
			var result = await act;
			await assert(result.Object);
		}

		internal readonly record struct ArrangeResult<T, TMock>(T Object, TMock? Mock)
			where TMock : notnull;

		internal readonly record struct ActResult<T, TMock>(T? Object, TMock? Mock, Exception? Exception)
			where T: notnull;
	}
}
