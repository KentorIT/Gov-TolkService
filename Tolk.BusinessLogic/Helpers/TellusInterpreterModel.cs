using System;
using System.Collections.Generic;
using System.Text;

namespace Tolk.BusinessLogic.Helpers
{
    public  class TellusInterpreterModel
    {
        public int InterpreterId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Cellphone { get; set; }
        public List<TellusInterpreterCompetenceModel> Competences { get; set; }
    }
}
