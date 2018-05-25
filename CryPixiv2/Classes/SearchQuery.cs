using System.Collections.Generic;

namespace CryPixiv2.Classes
{
    public class SearchQuery
    {
        public string Query { get; set; }
        // can add more options

        public override bool Equals(object obj)
        {
            var query = obj as SearchQuery;
            return query != null &&
                   Query == query.Query;
        }
        public override int GetHashCode()
        {
            return 2071171835 + EqualityComparer<string>.Default.GetHashCode(Query);
        }
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
