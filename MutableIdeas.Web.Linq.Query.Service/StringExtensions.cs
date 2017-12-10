using System;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class StringExtensions
    {
        public static string UnescapeUrlValue(this string value)
        {
			if (value.StartsWith("'"))
			{
				value = Uri.UnescapeDataString(value.Replace("'", string.Empty));
				return value;
			}

			return value;
        }
    }
}