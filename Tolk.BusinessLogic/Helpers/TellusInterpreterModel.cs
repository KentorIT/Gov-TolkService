using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public  class TellusInterpreterModel
    {
        public int interpreterId { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string cellphone { get; set; }
        public List<TellusInterpreterCompetenceModel> competences { get; set; }
    }
}
