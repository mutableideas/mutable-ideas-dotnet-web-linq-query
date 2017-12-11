using System;
using System.Linq;
using System.Linq.Expressions;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class ExpressionExtension
    {
		public static ConstantExpression GetArrayConstantValue<V>(string value)
		{
			Type valueType = typeof(V);
			var values = value.Replace("[", string.Empty)
				.Replace("]", string.Empty)
				.Split(',')
				.Select(p => (V)Convert.ChangeType(p.UnescapeUrlValue(), valueType))
				.ToList();

			return Expression.Constant(values.ToArray(), typeof(V[]));
		}
    }
}
