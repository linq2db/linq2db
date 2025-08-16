﻿using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using LINQPad;

namespace LinqToDB.LINQPad
{
	internal static partial class PasswordManager
	{
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
		private static readonly Regex _tokenReplacer = new(@"\{pm:([^\}]+)\}", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

		[return: NotNullIfNotNull(nameof(value))]
		public static string? ResolvePasswordManagerFields(string? value)
		{
			if (value == null)
				return null;

			return _tokenReplacer.Replace(value, m => Util.GetPassword(m.Groups[1].Value));
		}
	}
}
