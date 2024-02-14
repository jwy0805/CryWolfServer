using AccountServer.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedDB;

namespace AccountServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly SharedDbContext _shared;
        
        public AccountController(AppDbContext context, SharedDbContext shared)
        {
            _context = context;
            _shared = shared;
        }
        
        [HttpPost]
        [Route("create")]
        public CreateAccountPacketResponse CreateAccount([FromBody] CreateAccountPacketRequired required)
        {
            CreateAccountPacketResponse res = new();
            var account = _context.Accounts
                .AsNoTracking()
                .FirstOrDefault(account => account.AccountName == required.AccountName);

            if (account == null)
            {
                _context.Accounts.Add(new AccountDb
                {
                    AccountName = required.AccountName,
                    Password = required.Password
                });

                bool success = _context.SaveChangesExtended();
                res.CreateOK = success;
            }
            else
            {
                res.CreateOK = false;
            }
            
            return res;
        }

        [HttpPost]
        [Route("login")]
        public LoginAccountPacketResponse LoginAccount([FromBody] LoginAccountPacketRequired required)
        {
            LoginAccountPacketResponse res = new();
            var account = _context.Accounts
                .AsNoTracking()
                .FirstOrDefault(account => account.AccountName == required.AccountName && account.Password == required.Password);

            if (account == null)
            {
                res.LoginOK = false;
            }
            else
            {
                res.LoginOK = true;
                
                // TODO 토큰 생성
                var expired = DateTime.UtcNow;
                var addSeconds = expired.AddSeconds(600);
                
                TokenDb? tokenDb = _shared.Tokens.FirstOrDefault(token => token != null && token.AccountDbId == account.AccountDbId);
                if (tokenDb != null)
                {
                    tokenDb.Token = new Random().Next(int.MinValue, int.MaxValue);
                    tokenDb.Expired = addSeconds;
                    _shared.SaveChangesExtended();
                }
                else
                {
                    tokenDb = new TokenDb
                    {
                        AccountDbId = account.AccountDbId,
                        Token = new Random().Next(int.MinValue, int.MaxValue),
                        Expired = expired,
                    };
                    _shared.Add(tokenDb);
                    _shared.SaveChangesExtended();
                }

                res.AccountId = account.AccountDbId;
                res.Token = tokenDb.Token;
                res.ServerList = new List<ServerInfo>();

                foreach (var serverDb in _shared.Servers)
                {
                    res.ServerList.Add(new ServerInfo
                    {
                        Name = serverDb.Name,
                        IP = serverDb.IpAddress,
                        Port = serverDb.Port,
                        BusyScore = serverDb.BusyScore
                    });
                }
            }
            
            return res;
        }
    }
}
