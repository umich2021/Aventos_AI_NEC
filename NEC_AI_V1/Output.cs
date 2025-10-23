using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NEC_AI_V1
{
    public class ApiResponse
    {
        public string claude_response { get; set; }
        public string status { get; set; }
        public string stored_raw_text { get; set; }
    }

    public class OutletData
    {
        public string room_name { get; set; }
        public string room_type { get; set; }
        public List<Outlet> outlets { get; set; }
        public int outlet_count { get; set; }
        public string outlet_type_rules { get; set; }
        public string reasoning { get; set; }
        public string code_compliance { get; set; }
        public string room_boundaries { get; set; }
        public string warnings { get; set; }
    }

    public class Outlet
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Name { get; set; }
        public string FamilyName { get; set; }
    }
}