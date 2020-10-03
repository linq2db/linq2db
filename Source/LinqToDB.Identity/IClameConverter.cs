using System.Security.Claims;

namespace LinqToDB.Identity
{
	/// <summary>
	///     Provides methods to convert from\to <see cref="Claim" />
	/// </summary>
	public interface IClameConverter
	{
		/// <summary>
		///     Constructs a new claim with the type and value.
		/// </summary>
		/// <returns></returns>
		Claim ToClaim();

		/// <summary>
		///     Initializes by copying ClaimType and ClaimValue from the other claim.
		/// </summary>
		/// <param name="other">The claim to initialize from.</param>
		void InitializeFromClaim(Claim other);
	}
}