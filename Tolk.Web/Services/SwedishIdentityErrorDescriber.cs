using Microsoft.AspNetCore.Identity;

namespace Tolk.Web.Services
{
    public class SwedishIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = nameof(DefaultError),
                //Description = $"An unknown failure has occurred."
                Description = $"Ett okänt fel har inträffat."
            };
        }
        public override IdentityError ConcurrencyFailure()
        {
            return new IdentityError
            {
                Code = nameof(ConcurrencyFailure),
                //Description = "Optimistic concurrency failure, object has been modified."
                Description = "En annan ändring har gjorts samtidigt (Optimistic concurrency failure)."
            };
        }
        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                //Description = "Incorrect password."
                Description = "Felaktigt lösenord."
            };
        }

        public override IdentityError InvalidToken()
        {
            return new IdentityError
            {
                Code = nameof(InvalidToken),
                //Description = "Invalid token."
                Description = "Felaktig länk."
            };
        }
        public override IdentityError LoginAlreadyAssociated()
        {
            return new IdentityError
            {
                Code = nameof(LoginAlreadyAssociated),
                //Description = "A user with this login already exists."
                Description = "Användarnamnet är upptaget."
            };
        }
        public override IdentityError InvalidUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(InvalidUserName),
                //Description = $"User name '{userName}' is invalid, can only contain letters or digits."
                Description = $"Användarnamnet {userName} får bara innehålla bokstäver och siffror."
            };
        }
        public override IdentityError InvalidEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(InvalidEmail),
                //Description = $"Email '{email}' is invalid."
                Description = $"Felaktig e-postaddress '{email}'"
            };
        }
        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                //Description = $"User Name '{userName}' is already taken."
                Description = $"Användarnamnet '{userName}' är upptaget."
            };
        }
        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                //Description = $"Email '{email}' is already taken."
                Description = $"E-postadressen '{email}' är redan registrerad i systemet."
            };
        }
        public override IdentityError InvalidRoleName(string role)
        {
            return new IdentityError
            {
                Code = nameof(InvalidRoleName),
                //Description = $"Role name '{role}' is invalid."
                Description = $"Felaktigt rollnamn '{role}'."
            };
        }
        public override IdentityError DuplicateRoleName(string role)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateRoleName),
                //Description = $"Role name '{role}' is already taken."
                Description = $"Det finns redan en roll '{role}'."
            };
        }
        public override IdentityError UserAlreadyHasPassword()
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyHasPassword),
                //Description = "User already has a password set."
                Description = "Användaren har redan ett lösenord."
            };
        }
        public override IdentityError UserLockoutNotEnabled()
        {
            return new IdentityError
            {
                Code = nameof(UserLockoutNotEnabled),
                //Description = "Lockout is not enabled for this user."
                Description = "Utelåsning är inte aktiverat för den här användaren."
            };
        }
        public override IdentityError UserAlreadyInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserAlreadyInRole),
                //Description = $"User already in role '{role}'."
                Description = $"Användaren tillhör redan rollen '{role}'."
            };
        }
        public override IdentityError UserNotInRole(string role)
        {
            return new IdentityError
            {
                Code = nameof(UserNotInRole),
                //Description = $"User is not in role '{role}'."
                Description = $"Användaren är inte i rollen '{role}'."
            };
        }
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                //Description = $"Passwords must be at least {length} characters."
                Description = $"Lösenordet måste vara minst {length} tecken."
            };
        }
        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                //Description = "Passwords must have at least one non alphanumeric character."
                Description = "Lösenord måste innehålla minst ett tecken som inte är alfanumeriskt (bokstäver eller siffror)."
            };
        }
        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                //Description = "Passwords must have at least one digit ('0'-'9')."
                Description = "Lösenord måste innehålla minst en siffra ('0'-'9')."
            };
        }
        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                //Description = "Passwords must have at least one lowercase ('a'-'z')."
                Description = "Lösenord måste innehålla minst en liten bokstav ('a'-'z')."
            };
        }
        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                //Description = "Passwords must have at least one uppercase ('A'-'Z')."
                Description = "Lösenord måste innehålla minst en stor bokstav ('A'-'Z')."
            };
        }
    }
}
