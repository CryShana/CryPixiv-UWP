using System.Collections.Generic;

namespace CryPixiv2.Classes
{
    public class SearchQuery
    {
        public string Query { get; set; }
        // can add more options
    }

    public class SearchSession
    {
        public SearchQuery Query { get; set; }
        public PixivObservableCollection Collection { get; set; }
        public SearchSession(SearchQuery query)
        {
            Query = query;
            Collection = new PixivObservableCollection(x => x.SearchPosts(query.Query));
        }
    }
}
