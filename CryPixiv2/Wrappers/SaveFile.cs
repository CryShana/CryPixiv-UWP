using System;
using System.Collections.Generic;

namespace CryPixiv2.Wrappers
{
    [Serializable]
    public class SaveFile
    {
        public List<KeyValuePair<string, string>> TranslatedWords { get; set; }
        public List<string> SearchHistory { get; set; }
        public HashSet<int> BlockedIllustrations { get; set; }
    }
}
