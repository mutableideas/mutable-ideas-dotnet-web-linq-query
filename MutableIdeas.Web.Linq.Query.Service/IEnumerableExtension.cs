using System;
using System.Collections.Generic;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class IEnumerableExtension
    {
		public static void Each<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			foreach (T item in enumerable)
				action(item);
		}
    }
}