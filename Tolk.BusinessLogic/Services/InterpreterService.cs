using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;

namespace Tolk.BusinessLogic.Services
{
    public class InterpreterService
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;

        public InterpreterService(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GetInterpreterId might be a bit missleading - this method kicks of the entire
        // interpreter registration if needed.
        public async Task<int> GetInterpreterId(int brokerId, int regionId, string newInterpreterEmail)
        {
            var user = await GetOrCreateUser(newInterpreterEmail);
            await LoadOrCreateInterpreter(user);

            // Guard for if user assigned as new interpreter, despite it being an existing.

            // TODO: Ensur interpreter is connected to broker.

            return user.InterpreterId.Value;
        }

        private async Task LoadOrCreateInterpreter(AspNetUser user)
        {
            if (!user.InterpreterId.HasValue)
            {
                user.Interpreter = new Interpreter
                {
                    User = user,
                };
            }
            else
            {
                await _dbContext.Interpreters
                    .Include(i => i.Brokers)
                    .SingleOrDefaultAsync(i => i.InterpreterId == user.InterpreterId);
            }
        }

        private async Task<AspNetUser> GetOrCreateUser(string newInterpreterEmail)
        {
            // Use usermanager to run e-mail through normalization for matching.
            var user = await _userManager.FindByEmailAsync(newInterpreterEmail);

            if (user == null)
            {
                user = new AspNetUser
                {
                    UserName = newInterpreterEmail,
                    Email = newInterpreterEmail
                };

                var result = await _userManager.CreateAsync(user);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"User creation for {newInterpreterEmail} failed.");
                }
            }

            return user;
        }
    }
}
