using System;
using System.Linq;
using System.Reflection;

namespace VndbSharp.Extensions
{
	internal static class ReflectionExtensions
	{

		public static Boolean HasAttribute<T>(this FieldInfo prop)
			where T : Attribute
			=> prop.GetCustomAttribute<T>() != null;

		public static Boolean HasAttribute<T>(this MemberInfo prop)
			where T : Attribute
			=> prop.GetCustomAttribute<T>() != null;
	}
}
