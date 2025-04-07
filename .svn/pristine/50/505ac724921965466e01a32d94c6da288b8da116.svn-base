using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReliefApi.Models
{
    public class UserLogin
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    public class ConsoleRefreshTokenModel 
    {
        public long Id {  get; set; }
        public long UserId { get; set; }
        public string UserType { get; set; }  // A - ADMIN, M - MEMBER
        public string Token { get; set; }
        public DateTimeOffset Expires { get; set; }
        public DateTimeOffset Created { get; set; }
        public bool Revoked { get; set; }
    }

    public class JsonWebToken
    {
        public string access_token { get; set; }

        public string token_type { get; set; } = "bearer";

        public int expires_in { get; set; }
        public DateTimeOffset refresh_expires_in { get; set; }
        public string user_name { get; set; }
        public string refresh_token { get; set; }

        public string UserType { get; set; }

        public long user_id { get; set; }

        public string clientid { get; set; }
        public long currencyId { get; set; }
    }
}
