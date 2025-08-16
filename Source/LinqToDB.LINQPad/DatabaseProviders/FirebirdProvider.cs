using System;
using System.Collections.Generic;
using System.Data.Common;

using FirebirdSql.Data.FirebirdClient;

namespace LinqToDB.LINQPad;

internal sealed class FirebirdProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.Firebird  , "Detect Dialect Automatically", true),
		new (ProviderName.Firebird25, "Firebird 2.5"                      ),
		new (ProviderName.Firebird3 , "Firebird 3"                        ),
		new (ProviderName.Firebird4 , "Firebird 4"                        ),
		new (ProviderName.Firebird5 , "Firebird 5"                        ),
	];

	public FirebirdProvider()
		: base(ProviderName.Firebird, "Firebird", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		FbConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return FirebirdClientFactory.Instance;
	}
}
