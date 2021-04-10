using System.Linq;
using System.Reflection;

namespace LinqToDB.Common.Internal
{
	public static class MethodInfoExtensions
	{
		public static string ShortDisplayName(this MethodInfo methodInfo)
			=> methodInfo.DisplayName(fullName: false, multiline: false);

		public static string DisplayName(this MethodInfo methodInfo, bool fullName = true, bool multiline = true)
		{
			if (methodInfo == null)
				return "null";

			var declaringType = methodInfo.DeclaringType?.DisplayName(fullName);

			var returnType = methodInfo.ReturnType.DisplayName(fullName);
			var pms = string.Join(multiline ? ",\n\t" : ", ",
				methodInfo.GetParameters().Select(p => $"{p.ParameterType.DisplayName(fullName)} {p.Name}"));

			if (!string.IsNullOrEmpty(pms) && multiline)
				pms += "\n\t";

			var genericArgs = string.Empty;
			if (methodInfo.IsGenericMethod)
			{
				genericArgs = string.Join(", ", methodInfo.GetGenericArguments().Select(t => t.DisplayName(fullName)));
				genericArgs = $"<{genericArgs}>";
			}

			return $"{returnType} {methodInfo.Name}{genericArgs}({pms})";
		}		
	}
}
