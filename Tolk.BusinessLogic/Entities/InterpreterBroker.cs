using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tolk.BusinessLogic.Entities
{
    public class InterpreterBroker
    {
        public InterpreterBroker(string firstName, string lastName, int brokerId, string email, string phoneNumber, string officialInterpreterId)
        {
            FirstName = firstName;
            LastName = lastName;
            BrokerId = brokerId;
            Email = email;
            PhoneNumber = phoneNumber;
            OfficialInterpreterId = officialInterpreterId;
        }

        public int InterpreterBrokerId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(255)]
        public string LastName { get; set; }

        [MaxLength(255)]
        public string Email { get; set; }

        [Required]
        [MaxLength(255)]
        public string PhoneNumber { get; set; }

        public string FullName { get => $"{FirstName} {LastName}"; }

        public string CompleteContactInformation { get => $"{FullName}\nE-post: {Email ?? "-"}\nTel: {PhoneNumber ?? "-"}"; }

        public string OfficialInterpreterId { get; set; }

        public int BrokerId { get; set; }

        [ForeignKey(nameof(BrokerId))]
        public Broker Broker { get; set; }

        public int? InterpreterId { get; set; }

        [ForeignKey(nameof(InterpreterId))]
        public Interpreter Interpreter { get; set; }
    }
}
