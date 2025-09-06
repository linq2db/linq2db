namespace LinqToDB.Internal.SqlProvider
{
	public enum ConvertType
	{
		/// <summary>
		/// Provided name should be converted to query parameter name.
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     SELECT * FROM Person WHERE FirstName = @firstName
		///                                            ^ here
		/// </summary>
		NameToQueryParameter,

		/// <summary>
		/// Provided name should be converted to command parameter name.
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ here
		/// </summary>
		NameToCommandParameter,

		/// <summary>
		/// Provided name should be converted to stored procedure parameter name.
		/// For example:
		///     firstName -> @firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ here
		/// </summary>
		NameToSprocParameter,

		/// <summary>
		/// Provided name should be converted to query field name.
		/// For example:
		///     FirstName -> [FirstName]
		/// for the following query:
		///     SELECT [FirstName] FROM Person WHERE ID = 1
		///            ^   add   ^
		/// </summary>
		NameToQueryField,

		/// <summary>
		/// Provided name should be converted to query field alias.
		/// For example:
		///     ID -> "ID"
		/// for the following query:
		///     SELECT "ID" as "ID" FROM Person WHERE "ID" = 1
		///                    ^  ^ here
		/// </summary>
		NameToQueryFieldAlias,

		/// <summary>
		/// Provided name should be converted to linked server name.
		/// For example:
		///     host name\named instance -> [host name\named instance]
		/// for the following query:
		///     SELECT * FROM [host name\named instance]..[Person]
		///                   ^ add      ^
		/// </summary>
		NameToServer,

		/// <summary>
		/// Provided name should be converted to query database.
		/// For example:
		///     MyDatabase -> [MyDatabase]
		/// for the following query:
		///     SELECT * FROM [MyDatabase]..[Person]
		///                   ^ add      ^
		/// </summary>
		NameToDatabase,

		/// <summary>
		/// Provided name should be converted to query schema/owner.
		/// For example:
		///     dbo -> [dbo]
		/// for the following query:
		///     SELECT * FROM [ dbo ].[Person]
		///                   ^ add ^
		/// </summary>
		NameToSchema,

		/// <summary>
		/// Provided name should be converted to package/module/library name.
		/// </summary>
		NameToPackage,

		/// <summary>
		/// Provided name should be converted to function/procedure name.
		/// </summary>
		NameToProcedure,

		/// <summary>
		/// Provided name should be converted to query table name.
		/// For example:
		///     Person -> [Person]
		/// for the following query:
		///     SELECT * FROM [Person]
		///                   ^ add  ^
		/// </summary>
		NameToQueryTable,

		/// <summary>
		/// Provided name should be converted to CTE name.
		/// For example:
		///     Cte -> [Cte]
		/// for the following query:
		///     WITH [Cte] AS (....)
		///     SELECT * FROM [Cte]
		///                   ^ add  ^
		/// </summary>
		NameToCteName,

		/// <summary>
		/// Provided name should be converted to query table alias.
		/// For example:
		///     table1 -> [table1]
		/// for the following query:
		///     SELECT * FROM [Person] [table1]
		///                            ^ add  ^
		/// </summary>
		NameToQueryTableAlias,

		/// <summary>
		/// Provided stored procedure parameter name should be converted to name.
		/// For example:
		///     @firstName -> firstName
		/// for the following query:
		///     db.Parameter("@firstName") = "John";
		///                   ^ '@' has to be removed
		/// </summary>
		SprocParameterToName,

		/// <summary>
		/// Gets error number from a native exception.
		/// For example:
		///     SqlException -> SqlException.Number,
		///   OleDbException -> OleDbException.Errors[0].NativeError
		/// </summary>
		ExceptionToErrorNumber,

		/// <summary>
		/// Gets error message from a native exception.
		/// For example:
		///     SqlException -> SqlException.Message,
		///   OleDbException -> OleDbException.Errors[0].Message
		/// </summary>
		ExceptionToErrorMessage,

		/// <summary>
		/// Provided name should be converted to sequence name.
		/// </summary>
		SequenceName,

		/// <summary>
		/// Provided name should be converted to trigger name.
		/// </summary>
		TriggerName,
	}
}
