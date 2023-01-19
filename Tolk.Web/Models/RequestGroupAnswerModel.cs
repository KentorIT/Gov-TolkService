using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Tolk.BusinessLogic.Enums;

namespace Tolk.Web.Models
{
    public class RequestGroupAnswerModel : IModel
    {
        public int RequestGroupId { get; set; }

        [Required]
        public InterpreterLocation InterpreterLocation { get; set; }

        public RadioButtonGroup SetLatestAnswerTimeForCustomer { get; set; }

        public DateTimeOffset? LatestAnswerTimeForCustomer { get; set; }

        public InterpreterAnswerModel InterpreterAnswerModel { get; set; }

        public InterpreterAnswerModel ExtraInterpreterAnswerModel { get; set; }

        public string BrokerReferenceNumber { get; set; }

        public List<FileModel> Files { get; set; }
    }
}
