using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyBetHistory.Service.Model
{
    public class SyncParams
    {
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public long key { get; set; }
        public int Offsets { get; set; }
        public int totalPage { get; set; }
        public int page { get; set; }
        public int rowPerPage { get; set; }
        public int range { get; set; }
        public int timeOfFrequency { get; set; }
        public DateTime DoneTime { get; set; }
    }
}
