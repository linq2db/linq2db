- [Dataprovider Changes](#Dataprovider-changes)
- [IDynamicProviderAdapter Changes](#IDynamicProviderAdapter-changes)

# Dataprovider-changes

## Oracle

replace

    OracleTools.DefaultBulkCopyType = BulkCopyType.ProviderSpecific;
    OracleTools.DontEscapeLowercaseIdentifiers = true;

with

    OracleOptions.Default = new OracleOptions() { DontEscapeLowercaseIdentifiers = true, BulkCopyType = BulkCopyType.ProviderSpecific };

## SQLite

if you had code like this:

     SQLiteTools.GetDataProvider(ProviderName.SQLiteClassic)

switch to

     SQLiteTools.GetDataProvider(SQLiteProvider.System);

# IDynamicProviderAdapter-changes

the interface now also needs to return a db connection.

for example a implementation can look like:

        public DbConnection CreateConnection(string connectionString)
        {
            return new IngresConnection(connectionString);
        }