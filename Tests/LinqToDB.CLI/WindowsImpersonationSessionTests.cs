using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine.Commands.QueryExecution;

using Microsoft.Win32.SafeHandles;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Checks the real Windows session with duplicates of the current user's token: no password needed, and each
	/// duplicate has its own token id, so the test can tell which token a thread is impersonating.
	/// </summary>
	[TestFixture]
	[Platform(Include = "Win")]
	[SupportedOSPlatform("windows")]
	public sealed class WindowsImpersonationSessionTests
	{
		const int SecurityImpersonation = 2;
		const int TokenStatistics       = 10;

		[Test]
		public async Task IdentityFlowsAcrossAwaitsAndDisposal()
		{
			using var session = CreateSession(out var tokenId);

			var seen   = new List<long?>();
			var result = await session.RunAsync(async () =>
			{
				seen.Add(GetThreadTokenId());

				await Task.Delay(20);
				seen.Add(GetThreadTokenId());

				await using (new DisposalProbe(seen))
					await Task.Yield();

				return 42;
			});

			using (Assert.EnterMultipleScope())
			{
				result.ShouldBe(42);
				seen.  Count.ShouldBe(3);
				seen.  ShouldAllBe(id => id == tokenId);
				GetThreadTokenId().ShouldBeNull();
			}
		}

		[Test]
		public async Task IdentityIsRestoredAfterException()
		{
			using var session = CreateSession(out var tokenId);

			long? inside = null;

			await Should.ThrowAsync<InvalidOperationException>(() => session.RunAsync<int>(async () =>
			{
				await Task.Delay(20);
				inside = GetThreadTokenId();
				throw new InvalidOperationException("Expected.");
			}));

			using (Assert.EnterMultipleScope())
			{
				inside.            ShouldBe(tokenId);
				GetThreadTokenId().ShouldBeNull();
			}
		}

		[Test]
		public async Task IdentityIsRestoredAfterCancellation()
		{
			using var session = CreateSession(out _);
			using var cts     = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

			await Should.ThrowAsync<TaskCanceledException>(() => session.RunAsync(async () =>
			{
				await Task.Delay(Timeout.Infinite, cts.Token);
				return 0;
			}));

			GetThreadTokenId().ShouldBeNull();
		}

		[Test]
		public async Task OverlappingSessionsKeepTheirOwnIdentity()
		{
			using var first  = CreateSession(out var firstTokenId);
			using var second = CreateSession(out var secondTokenId);

			var firstSeen   = new List<long?>();
			var secondSeen  = new List<long?>();
			var firstGate   = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var secondGate  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

			// The two runs alternate: each resumes on a pool thread the other one may just have left.
			//
			var firstRun = first.RunAsync(async () =>
			{
				firstSeen.Add(GetThreadTokenId());
				await firstGate.Task;
				firstSeen.Add(GetThreadTokenId());
				secondGate.SetResult();
				await Task.Delay(20);
				firstSeen.Add(GetThreadTokenId());
				return 1;
			});

			var secondRun = second.RunAsync(async () =>
			{
				secondSeen.Add(GetThreadTokenId());
				firstGate.SetResult();
				await secondGate.Task;
				secondSeen.Add(GetThreadTokenId());
				await Task.Delay(20);
				secondSeen.Add(GetThreadTokenId());
				return 2;
			});

			await Task.WhenAll(firstRun, secondRun);

			using (Assert.EnterMultipleScope())
			{
				firstTokenId.ShouldNotBe(secondTokenId);
				firstSeen.   Count.ShouldBe(3);
				secondSeen.  Count.ShouldBe(3);
				firstSeen.   ShouldAllBe(id => id == firstTokenId);
				secondSeen.  ShouldAllBe(id => id == secondTokenId);
				GetThreadTokenId().ShouldBeNull();
			}
		}

		sealed class DisposalProbe(List<long?> seen) : IAsyncDisposable
		{
			public async ValueTask DisposeAsync()
			{
				await Task.Yield();
				seen.Add(GetThreadTokenId());
			}
		}

		static WindowsImpersonationSession CreateSession(out long tokenId)
		{
			using var current = WindowsIdentity.GetCurrent(TokenAccessLevels.Duplicate | TokenAccessLevels.Query);

			if (!DuplicateToken(current.AccessToken, SecurityImpersonation, out var duplicate))
				throw new Win32Exception(Marshal.GetLastWin32Error());

			tokenId = GetTokenId(duplicate);

			return new WindowsImpersonationSession(duplicate);
		}

		static long? GetThreadTokenId()
		{
			using var identity = WindowsIdentity.GetCurrent(ifImpersonating: true);

			return identity == null ? null : GetTokenId(identity.AccessToken);
		}

		static long GetTokenId(SafeAccessTokenHandle token)
		{
			// TOKEN_STATISTICS starts with the token id (a LUID).
			//
			var buffer = Marshal.AllocHGlobal(128);

			try
			{
				if (!GetTokenInformation(token, TokenStatistics, buffer, 128, out _))
					throw new Win32Exception(Marshal.GetLastWin32Error());

				return Marshal.ReadInt64(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(buffer);
			}
		}

		[DllImport("advapi32.dll", SetLastError = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool DuplicateToken(SafeAccessTokenHandle existingToken, int impersonationLevel, out SafeAccessTokenHandle duplicateToken);

		[DllImport("advapi32.dll", SetLastError = true)]
		[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, IntPtr information, int informationLength, out int returnLength);
	}
}
