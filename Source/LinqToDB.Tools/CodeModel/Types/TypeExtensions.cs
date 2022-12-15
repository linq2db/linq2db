using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinqToDB.CodeModel;

namespace LinqToDB.CodeModel
{
	internal static class TypeExtensions
	{
		public static void SetNameChangeHandler(this IType type, Action<string> handler)
		{
			// Set used to handle circular type references
			SetNameChangeHandlerInternal(new HashSet<IType>(), type, handler);
		}

		private static void SetNameChangeHandlerInternal(ISet<IType> visited, IType type, Action<string> handler)
		{
			if (!visited.Add(type))
				return;

			if (type.Namespace != null)
				foreach (var part in type.Namespace)
					part.OnChange += handler;

			if (type.Name != null)
				type.Name.OnChange += handler;

			if (type.Parent != null)
				SetNameChangeHandlerInternal(visited, type.Parent, handler);

			if (type.ArrayElementType != null)
				SetNameChangeHandlerInternal(visited, type.ArrayElementType, handler);

			if (type.TypeArguments != null)
				foreach (var arg in type.TypeArguments)
					SetNameChangeHandlerInternal(visited, arg, handler);
		}
	}
}
