using System.Collections.Generic;
using System.Linq;
	
namespace gui_diff
{
	public static class MyExtensions
	{
		public static IEnumerable<IEnumerable<T>> Batch<T> (this IEnumerable<T> items, int maxCount)
		{
			return items.Select ((item, index) => new { item, index })
						.GroupBy (x => x.index / maxCount)
						.Select (g => g.Select (x => x.item));
		}
	}
}
