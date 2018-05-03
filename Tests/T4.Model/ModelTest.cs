using System;
using System.ComponentModel.DataAnnotations;

namespace Tests.T4.Model
{
	partial class TestClass1
	{
		partial class CustomValidator
		{
			public static ValidationResult ValidateEditableLong1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}

			public static ValidationResult ValidateEditableInt1(TestClass1 obj)
			{
				return ValidationResult.Success;
			}
		}


		private void AddError(string name, string errorMessage)
		{
		}

		private void RemoveError(string nameOfEditableInt1)
		{
		}
	}
}
