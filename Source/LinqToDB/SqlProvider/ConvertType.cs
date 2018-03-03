using System;

namespace LinqToDB.SqlProvider
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
		///            ^   and   ^
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
		/// Provided name should be converted to query database.
		/// For example:
		///     MyDatabase -> [MyDatabase]
		/// for the following query:
		///     SELECT * FROM [MyDatabase]..[Person]
		///                   ^ and      ^
		/// </summary>
		NameToDatabase,

		/// <summary>
		/// Provided name should be converted to query database.
		/// For example:
		///     dbo -> [dbo]
		/// for the following query:
		///     SELECT * FROM [ dbo ].[Person]
		///                   ^ and ^
		/// </summary>
		NameToSchema,

		[Obsolete("Use NameToSchema instead.")]
		NameToOwner = NameToSchema,

		/// <summary>
		/// Provided name should be converted to query table name.
		/// For example:
		///     Person -> [Person]
		/// for the following query:
		///     SELECT * FROM [Person]
		///                   ^ and  ^
		/// </summary>
		NameToQueryTable,

		/// <summary>
		/// Provided name should be converted to query table alias.
		/// For example:
		///     table1 -> [table1]
		/// for the following query:
		///     SELECT * FROM [Person] [table1]
		///                            ^ and  ^
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
	}
}
