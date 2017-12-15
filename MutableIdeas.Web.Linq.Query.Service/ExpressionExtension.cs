using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class ExpressionExtension
    {
		readonly static MethodInfo _stringContainsMethod = typeof(string).GetRuntimeMethod("Contains", new[] { typeof(string) });

		public static ConstantExpression GetArrayConstantValue<V>(string value)
		{
			Type valueType = typeof(V);
			V[] values = value.Replace("[", string.Empty)
				.Replace("]", string.Empty)
				.Split(',')
				.Select(p => (V)Convert.ChangeType(p.UnescapeUrlValue(), valueType))
				.ToArray();

			return Expression.Constant(values, typeof(V[]));
		}

		public static Expression ContainsExpression(Expression left, Expression right)
		{
			return Expression.Call(left, _stringContainsMethod, right);
		}

		public static Expression GetPropertyExpression(ParameterExpression parameterExpression, string propertyName)
		{
			string[] properties = propertyName.ToLower().Split('.');
			Expression leftExpression = parameterExpression;

			properties.Each(property =>
			{
				PropertyInfo propertyInfo = leftExpression.Type.GetPropertyInfo(property);
				if (propertyInfo.PropertyType.IsEnumerable())
					throw new InvalidOperationException("Cannot sort enumerable properties.");

				leftExpression = Expression.Property(leftExpression, propertyInfo);
			});

			return leftExpression;
		}
    }
}