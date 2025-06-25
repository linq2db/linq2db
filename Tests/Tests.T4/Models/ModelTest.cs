using System.ComponentModel.DataAnnotations;

namespace Tests.T4.Models
{
	/// <summary>
	///
	/// </summary>
	partial class TestClass1
	{
		/// <summary>
		///
		/// </summary>
		partial class CustomValidator
		{
			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static ValidationResult? ValidateEditableLong1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}

			/// <summary>
			///
			/// </summary>
			/// <param name="obj"></param>
			/// <returns></returns>
			public static ValidationResult? ValidateEditableInt1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}
		}

		private void AddError(string name, string? errorMessage)
		{
		}

		private void RemoveError(string nameOfEditableInt1)
		{
		}
	}
}
