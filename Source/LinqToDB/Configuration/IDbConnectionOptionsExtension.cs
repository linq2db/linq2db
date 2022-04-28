namespace LinqToDB.Configuration
{
	public interface IDbConnectionOptionsExtension
	{
		IDbConnectionOptions ApplyExtension(IDbConnectionOptions options);

		/// <summary>
		///     Gives the extension a chance to validate that all options in the extension are valid.
		///     Most extensions do not have invalid combinations and so this will be a no-op.
		///     If options are invalid, then an exception should be thrown.
		/// </summary>
		/// <param name="options"> The options being validated. </param>
		void Validate(IDbConnectionOptions options);
	}
}
