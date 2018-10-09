using System;
using System.Collections.Generic;
using static CryPixiv2.ViewModels.MainViewModel;

namespace CryPixiv2.Wrappers
{
    [Serializable]
    public class SaveFile
    {
        public List<KeyValuePair<string, string>> TranslatedWords { get; set; }
        public List<string> SearchHistory { get; set; }
        public HashSet<int> BlockedIllustrations { get; set; }
        public List<string> BlacklistedTags { get; set; }
        public PageAction PageAction_DetailsImageDoubleClick { get; set; }
    }
}
