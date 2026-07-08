using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartInternshipPortal.Models;
using System.Threading.Tasks;
namespace SmartInternshipPortal.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    Role = model.Role
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Assign role
                    await _userManager.AddToRoleAsync(user, model.Role);
                    // Sign user in
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToDashboard();
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard();
            }
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return RedirectToDashboard();
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            }
            return View(model);
             }
        [HttpGet]
public IActionResult ForgotPassword()
{
    return View();
}

[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
{
    if (!ModelState.IsValid)
        return View(model);

    var user = await _userManager.FindByEmailAsync(model.Email);

    if (user == null)
    {
        ModelState.AddModelError("", "Email not found.");
        return View(model);
    }

    var token = await _userManager.GeneratePasswordResetTokenAsync(user);

    var result = await _userManager.ResetPasswordAsync(
        user,
        token,
        model.NewPassword);

    if (result.Succeeded)
    {
        TempData["SuccessMessage"] =
            "Password reset successfully. Please login.";

        return RedirectToAction(nameof(Login));
    }

    foreach (var error in result.Errors)
    {
        ModelState.AddModelError("", error.Description);
    }

    return View(model);
}     
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpGet]
        public async Task<IActionResult> GetLogout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        [HttpPost]
public IActionResult GoogleLogin()
{
    var redirectUrl = Url.Action(nameof(GoogleResponse), "Account");
    var properties = _signInManager.ConfigureExternalAuthenticationProperties(
        "Google",
        redirectUrl);

    return Challenge(properties, "Google");
}

public async Task<IActionResult> GoogleResponse()
{
    var info = await _signInManager.GetExternalLoginInfoAsync();

    if (info == null)
        return RedirectToAction(nameof(Login));

    var result = await _signInManager.ExternalLoginSignInAsync(
        info.LoginProvider,
        info.ProviderKey,
        false);

    if (result.Succeeded)
        return RedirectToDashboard();

    var email = info.Principal.FindFirst(
        System.Security.Claims.ClaimTypes.Email)?.Value;

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        FullName = "Google User",
        Role = "Student",
        EmailConfirmed = true
    };

    await _userManager.CreateAsync(user);
    await _userManager.AddLoginAsync(user, info);
    await _userManager.AddToRoleAsync(user, "Student");

    await _signInManager.SignInAsync(user, false);

    return RedirectToDashboard();
}
        [HttpGet]
        public async Task<IActionResult> SimulateLogin(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email is required.");
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Sign out the current user
            await _signInManager.SignOutAsync();

            // Sign in the new user
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["SuccessMessage"] = $"Impersonated {user.FullName} ({user.Role}) successfully!";

            if (user.Role == "Company")
            {
                return RedirectToAction("Index", "Company");
            }
            else
            {
                return RedirectToAction("Index", "Student");
            }
        }

        private IActionResult RedirectToDashboard()
        {
            if (User.IsInRole("Company"))
            {
                return RedirectToAction("Index", "Company");
            }
            else if (User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Student");
            }
            else if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Student"); // Or Admin dashboard
            }
            
            // If authentication succeeded but roles are still propagating in current request
            // We can fetch the user details to redirect accurately
            var username = User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                var userTask = _userManager.FindByNameAsync(username);
                userTask.Wait();
                var user = userTask.Result;
                if (user != null)
                {
                    if (user.Role == "Company")
                        return RedirectToAction("Index", "Company");
                    else
                        return RedirectToAction("Index", "Student");
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
