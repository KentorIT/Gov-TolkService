using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
            UserManager<AspNetUser> userManager,
            ILogger<InterpreterService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        // GetInterpreterId might be a bit missleading - this method kicks of the entire
        // interpreter registration if needed.
        public async Task<int> GetInterpreterId(int brokerId, string newInterpreterEmail)
        {
            var user = await GetOrCreateUser(newInterpreterEmail);
            await LoadOrCreateInterpreter(user);

            ConnectInterpereterToBroker(user.Interpreter, brokerId);

            return user.InterpreterId.Value;
        }

        private void ConnectInterpereterToBroker(Interpreter interpreter, int brokerId)
        {
            if(!interpreter.Brokers.Any(ib => ib.BrokerId == brokerId))
            {
                interpreter.Brokers.Add(new InterpreterBroker
                {
                    BrokerId = brokerId
                });
            }
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
