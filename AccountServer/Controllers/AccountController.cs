using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountServer.DB;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccountServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;
        
        public AccountController(AppDbContext context)
        {
            _context = context;
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
                
                // TODO 서버 목록
                res.ServerList = new List<ServerInfo>
                {
                    new ServerInfo { Name = "Test Server", IP = "127.0.0.1", CrowdedLevel = 0 },
                    new ServerInfo { Name = "Test Server 2", IP = "127.0.0.1", CrowdedLevel = 3 },
                };
            }
            
            return res;
        }
    }
}
