using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class RequestGroupUpdateLatestAnswerTime : UpdateBase
    {
        public int RequestGroupId { get; set; }

        [ForeignKey(nameof(RequestGroupId))]
        public RequestGroup RequestGroup { get; set; }
    }
}
