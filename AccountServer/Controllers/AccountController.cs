using AccountServer.DB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedDB;

namespace AccountServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserAccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly SharedDbContext _shared;
        
        public UserAccountController(AppDbContext context, SharedDbContext shared)
        {
            _context = context;
            _shared = shared;
        }
        
        [HttpPost]
        [Route("create")]
        public CreateUserAccountPacketResponse CreateAccount([FromBody] CreateUserAccountPacketRequired required)
        {
            CreateUserAccountPacketResponse res = new();
            var account = _context.User
                .AsNoTracking()
                .FirstOrDefault(account => account.UserName == required.UserName);

            if (account == null)
            {
                var newUser = new User
                {
                    UserName = required.UserName,
                    Password = required.Password,
                    Role = UserRole.User,
                    State = UserState.Activate,
                    CreatedAt = DateTime.Now,
                    RankPoint = 0,
                    Gold = 500,
                    Gem = 50
                };
                
                _context.User.Add(newUser);
                bool success = _context.SaveChangesExtended();

                CreateFirstDecks(newUser.UserId);
                
                res.CreateOK = success;
            }
            else
            {
                res.CreateOK = false;
            }
            
            return res;
        }

        private void CreateFirstDecks(int userId)
        {
            int[] sheepUnitIds = { 103, 106, 109, 112, 115, 124 };
            int[] wolfUnitIds = { 503, 506, 509, 512, 515, 521 };

            foreach (var unitId in sheepUnitIds)
            {
                _context.UserUnit.Add(new UserUnit { UserId = userId, UnitId = unitId });
            }

            foreach (var unitId in wolfUnitIds)
            {
                _context.UserUnit.Add(new UserUnit { UserId = userId, UnitId = unitId });
            }
            
            _context.SaveChangesExtended();
            
            var deck = new Deck { UserId = userId, Camp = Camp.Sheep };
            _context.Deck.Add(deck);
            _context.SaveChangesExtended();
            
            
        }

        [HttpPost]
        [Route("login")]
        public LoginUserAccountPacketResponse LoginAccount([FromBody] LoginUserAccountPacketRequired required)
        {
            LoginUserAccountPacketResponse res = new();
            var account = _context.User
                .AsNoTracking()
                .FirstOrDefault(account => account.UserName == required.UserName && account.Password == required.Password);

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
                
                TokenDb? tokenDb = _shared.Tokens.FirstOrDefault(token => token != null && token.AccountDbId == account.UserId);
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
                        AccountDbId = account.UserId,
                        Token = new Random().Next(int.MinValue, int.MaxValue),
                        Expired = expired,
                    };
                    _shared.Add(tokenDb);
                    _shared.SaveChangesExtended();
                }

                res.UserId = account.UserId;
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
