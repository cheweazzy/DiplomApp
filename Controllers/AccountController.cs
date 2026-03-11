using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DiplomApp.Models;

namespace DiplomApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<User> _signInManager;

        public AccountController(SignInManager<User> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost]
        [Route("/account/login")]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string? returnUrl = null)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                rememberMe,
                lockoutOnFailure: false
            );

            if (result.Succeeded)
            {
                return Redirect(returnUrl ?? "/");
            }

            // Redirect back to login page with error
            return Redirect($"/login?error=Invalid login attempt");
        }

        [HttpPost]
        [Route("/account/logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/");
        }
    }
}

