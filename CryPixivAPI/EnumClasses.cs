using System;
using System.Collections.Generic;
using System.Text;

namespace CryPixivAPI
{
    public static class PixivParameters
    {
        public static class SearchSortMode
        {
            public const string Ascending = "date_asc";
            public const string Descending = "date_desc";
            public const string Popular = "popular_desc";
        }

        public static class SearchDuration
        {
            public const string None = null;
            public const string WithinLastDay = "within_last_day";
            public const string WithinLastWeek = "within_last_week";
            public const string WithinLastMonth = "within_last_month";
        }

        public static class SearchTarget
        {
            // use these for novels
            public const string Text = "text";
            public const string Keyword = "keyword";

            // use these for illustrations
            public const string TitleAndCaption = "title_and_caption";
            public const string ExactMatchForTags = "exact_match_for_tags";
            public const string PartialMatchForTags = "partial_match_for_tags";
        }
    }
}
