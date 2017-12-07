using MutableIdeas.Web.Linq.Query.Domain.Enums;
using MutableIdeas.Web.Linq.Query.Domain.Services;

namespace MutableIdeas.Web.Linq.Query.Service
{
    public static class IFilterServiceExtension
    {
		public static IFilterService<T> And<T>(this IFilterService<T> filterService, string property, string value, FilterType filterType)
			where T : class
		{
			filterService.And();
			filterService.By(property, value, filterType);

			return filterService;
		}

		public static IFilterService<T> Or<T>(this IFilterService<T> filterService, string property, string value, FilterType filterType)
			where T : class
		{
			filterService.Or();
			filterService.By(property, value, filterType);

			return filterService;
		}

		public static IFilterService<T> For<T>(this IFilterService<T> filterService, string property, string value, FilterType filterType)
			where T : class
		{
			filterService.By(property, value, filterType);
			return filterService;
		}
    }
}