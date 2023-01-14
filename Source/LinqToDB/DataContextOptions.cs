﻿using System;
using System.Collections.Generic;

namespace LinqToDB
{
	using Common;
	using Common.Internal;
	using Data;
	using Interceptors;

	/// <param name="CommandTimeout">
	/// The command timeout, or <c>null</c> if none has been set.
	/// </param>
	/// <param name="Interceptors">
	/// Gets Interceptors to use with <see cref="DataConnection"/> instance.
	/// </param>
	public sealed record DataContextOptions
	(
		int?                         CommandTimeout = default,
		IReadOnlyList<IInterceptor>? Interceptors   = default
	)
		: IOptionSet, IApplicable<DataConnection>, IApplicable<DataContext>
	{
		public DataContextOptions() : this((int?)default)
		{
		}

		DataContextOptions(DataContextOptions original)
		{
			CommandTimeout = original.CommandTimeout;
			Interceptors   = original.Interceptors;
		}

		int? _configurationID;
		int IConfigurationID.ConfigurationID => _configurationID ??= new IdentifierBuilder()
			.Add(CommandTimeout)
			.AddTypes(Interceptors)
			.CreateID();

		public static readonly DataContextOptions Empty = new();

		void IApplicable<DataConnection>.Apply(DataConnection obj)
		{
			DataConnection.ConfigurationApplier.Apply(obj, this);
		}

		void IApplicable<DataContext>.Apply(DataContext obj)
		{
			DataContext.ConfigurationApplier.Apply(obj, this);
		}

		#region IEquatable implementation

		public bool Equals(DataContextOptions? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;

			return ((IOptionSet)this).ConfigurationID == ((IOptionSet)other).ConfigurationID;
		}

		public override int GetHashCode()
		{
			return ((IOptionSet)this).ConfigurationID;
		}

		#endregion
	}
}
