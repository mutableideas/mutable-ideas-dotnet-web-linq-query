using System;
using System.Linq;
using System.Linq.Expressions;

namespace MutableIdeas.Web.Linq.Query.Domain.Services
{
    public interface IQueryStringExpressionService<T>
		where T : class
    {
		Expression<Func<T, bool>> GetExpression(string filter);
		IQueryable<T> GetSortExpression(string sort, IQueryable<T> queryable);
	}
}
