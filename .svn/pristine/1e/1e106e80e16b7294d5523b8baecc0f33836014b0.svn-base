using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Contracts;
using ReliefApi.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using JsonWebToken = ReliefApi.Models.JsonWebToken;
using System.Reflection;

namespace ReliefApi.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors()]
    public class HomeController:ControllerBase
    {
        private readonly IBranches _Branches;
       

        public HomeController(IBranches branches)
        {
            _Branches = branches;
        }

        [AllowAnonymous]
        [HttpPost,Route("login")]
        public async Task<IActionResult> Login(UserLogin User)
        {
            if (!ModelState.IsValid) return BadRequest("Token failed to generate");

            Employee EmployeeUserObj = await _Branches.GetByUserName(User.UserName);
            var username = User.UserName.ToLower();
            if (EmployeeUserObj != null)
            {
                if(EmployeeUserObj.EMPBLOCKED==0)
                {
                    var decryptedPassword = /*EqualLiveApp.Services.Helpers.*/EncryptDecrypt.Decrypt(EmployeeUserObj.EMPUSERPASSWORD, "25hui88fg");
                    var model = (decryptedPassword == User.Password);
                    //var model = (user.UserName == ClientObj.UserName);
                    if (!model) return Unauthorized();

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, EmployeeUserObj.EMPUSRNAME),
                        new Claim(ClaimTypes.Role, "Administrator"),
                        new Claim("UserId", EmployeeUserObj.EMPID.ToString())
                    };


                    var strAcctkn = GenerateAccessToken(claims);
                    var strRefTkn = GenerateRefreshToken();
                    DateTime dtRefTknExp = DateTime.Now.AddHours(6);

                    await _Branches.AddRefreshToken(new ConsoleRefreshTokenModel { Id = 0, UserId = EmployeeUserObj.EMPID, UserType = "A", Token = strRefTkn, Created = DateTime.Now, Expires = dtRefTknExp, Revoked = false });

                    return Ok(new JsonWebToken()
                    {
                        access_token = strAcctkn,
                        expires_in = 6000,
                        token_type = "bearer",
                        user_id = EmployeeUserObj.EMPID,
                        user_name = EmployeeUserObj.EMPNAME,
                        refresh_token = strRefTkn,
                        refresh_expires_in = dtRefTknExp
                    });

                   

                }
                else
                {
                    return BadRequest("This Employee Is Blocked");
                }
              
                






            }
            else
            {
                return Unauthorized();
            }



        }


        [HttpPost, Route("RefreshToken")]
        public async Task<IActionResult> RefreshToken(RefreshTokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.token) || string.IsNullOrWhiteSpace(request.reftoken))
            {
                return BadRequest("Invalid client request.");
            }

            // Decode JWT to extract UserId
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(request.token))
            {
                return Unauthorized("Invalid token format.");
            }

            var jwtToken = handler.ReadJwtToken(request.token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("UserId not found in token.");
            }

            // Validate refresh token
            var refreshToken = await _Branches.GetRefreshTokenByUserId(userId);

            if (refreshToken == null)
            {
                return Unauthorized("Refresh token not found.");
            }
            else if (refreshToken.Token != request.reftoken)
            {
                return Unauthorized("Invalid refresh token.");
            }
            else if (refreshToken.Expires < DateTime.Now)
            {
                return Unauthorized("Refresh token expired.");
            }

            // Fetch user details from the database
            var employee = await _Branches.GetByUserId(userId);  // assuming this method exists
            if (employee == null)
            {
                return Unauthorized("User not found.");
            }


            // Generate new access token and refresh token
            var claims = new[]
            {
        new Claim(ClaimTypes.Name, employee.EMPNAME),  // User name
        new Claim(ClaimTypes.Role, "Administrator"),    // User role (or modify accordingly)
        new Claim("UserId", userId.ToString())         // User ID
    };

            var newAccessToken = GenerateAccessToken(claims);
            var newRefreshToken = GenerateRefreshToken();
            DateTime newRefreshTokenExpiration = DateTime.Now.AddHours(6);

            // Store the new refresh token in the database
            await _Branches.AddRefreshToken(new ConsoleRefreshTokenModel
            {
                UserId = userId,
                Token = newRefreshToken,
                Expires = newRefreshTokenExpiration,
                Created = DateTime.Now,
                Revoked = false
            });

            // Return the new tokens with user information
            return Ok(new JsonWebToken
            {
                access_token = newAccessToken,
                expires_in = 6000,
                token_type = "bearer",
                refresh_token = newRefreshToken,
                refresh_expires_in = newRefreshTokenExpiration,
                user_id = userId,
                user_name = employee.EMPNAME,
            });
        }



        //#region token generation and validation
        private string GenerateAccessToken(IEnumerable<Claim> claims)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SupersecretKey@9846760609"));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            var tokeOptions = new JwtSecurityToken(
                issuer: "me",
                audience: "you",
                claims: claims,
                expires: DateTime.Now.AddMinutes(60),
                signingCredentials: signinCredentials
            );
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);
            return tokenString;
        }


        private string GenerateRefreshToken()
        {


            return Guid.NewGuid().ToString();
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            try
            {
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false, //you might want to validate the audience and issuer depending on your use case
                    ValidateIssuer = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SupersecretKey@9846760609")),
                    ValidateLifetime = false //here we are saying that we don't care about the token's expiration date
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken securityToken;
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out securityToken);
                var jwtSecurityToken = securityToken as JwtSecurityToken;

                if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    //throw new SecurityTokenException("Invalid token");
                    return null;
                }

                return principal;

            }
            catch (Exception)
            {

                return null;
            }
        }

        public class RefreshTokenRequest
        {
            public string token { get; set; }
            public string reftoken { get; set; }

        }


    

            [HttpPost("SendWhatsappMessage")]
            public async Task<IActionResult> SendWhatsAppMessage(string mobileNo, string message)
            {
                if (string.IsNullOrEmpty(mobileNo) || string.IsNullOrEmpty(message))
                {
                    return BadRequest("Mobile number and message cannot be null or empty.");
                }

                // Call the service method, passing mobile number and message
                var smsStatus = await _Branches.SendNewWahtsAppMsg(mobileNo, message);

                if (smsStatus.Status)
                {
                    return Ok(new { Message = "WhatsApp message sent successfully.", Status = "1" });
                }
                else
                {
                    return Ok(new { Message = "Failed to send WhatsApp message.", Status = "2" });
                }
            }
        }

    }

