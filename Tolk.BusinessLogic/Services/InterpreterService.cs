using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tolk.BusinessLogic.Data;
using Tolk.BusinessLogic.Entities;
using Tolk.BusinessLogic.Helpers;

namespace Tolk.BusinessLogic.Services
{
    public class InterpreterService
    {
        private readonly TolkDbContext _dbContext;
        private readonly UserManager<AspNetUser> _userManager;
        private readonly ILogger<InterpreterService> _logger;
        private readonly TolkOptions _options;
        private readonly ISwedishClock _clock;
        private readonly UserService _userService;

        public InterpreterService(
            TolkDbContext dbContext,
            UserManager<AspNetUser> userManager,
            ILogger<InterpreterService> logger,
            IOptions<TolkOptions> options,
            ISwedishClock clock,
            UserService userService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _logger = logger;
            _options = options.Value;
            _clock = clock;
            _userService = userService;
        }

        // GetInterpreterId might be a bit missleading - this method kicks of the entire
        // interpreter registration if needed.
        public async Task<int> GetInterpreterId(int brokerId, string newInterpreterEmail)
        {
            var user = await GetOrCreateUser(newInterpreterEmail);
            await LoadOrCreateInterpreter(user);

            ConnectInterpreterToBroker(user.Interpreter, brokerId);

            // Save changes to get an id generated.
            _dbContext.SaveChanges();

            return user.InterpreterId.Value;
        }

        private void ConnectInterpreterToBroker(Interpreter interpreter, int brokerId)
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
                _logger.LogInformation("Creating new interpreter user for {email}", newInterpreterEmail);

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

                await _userService.SendInviteToInterpreter(user);
            }

            return user;
        }

    }
}
