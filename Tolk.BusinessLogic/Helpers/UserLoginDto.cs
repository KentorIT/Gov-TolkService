using System;
namespace Tolk.BusinessLogic.Helpers
{
    public class UserLoginDto
    {
        public int UserId { get; set; }
        public DateTimeOffset LoggedAt { get; set; }
    }

}