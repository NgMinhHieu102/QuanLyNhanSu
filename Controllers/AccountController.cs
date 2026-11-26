using System.Security.Claims;
using HumaHr.Models;
using HumaHr.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HumaHr.Controllers;

public class AccountController(AccountService accounts, HrRepository repository) : Controller
{
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        base.OnActionExecuting(context);
        var action = context.ActionDescriptor.RouteValues["action"];
        if (User.Identity?.IsAuthenticated == true && User.HasClaim("must_change_password", "true")
            && action != nameof(ChangePassword) && action != nameof(Logout))
            context.Result = RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string email)
    {
        if (!string.IsNullOrWhiteSpace(email)) repository.CreatePasswordResetRequest(email);
        TempData["Message"] = "Nếu email thuộc tài khoản nhân viên hợp lệ, yêu cầu sẽ được gửi cho quản trị viên xử lý.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = accounts.Find(model.Email);
        if (user is null || !user.IsEnabled || !accounts.Verify(user, model.Password))
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            return View(model);
        }
        if (user.Role == "Employee" && user.EmployeeId is int employeeId && !accounts.IsEmployeeActive(employeeId))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa. Vui lòng liên hệ phòng nhân sự.");
            return View(model);
        }
        accounts.UpgradePasswordHash(user, model.Password);
        await SignIn(user);
        if (user.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        return LocalRedirect(string.IsNullOrWhiteSpace(model.ReturnUrl) ? Url.Action("Index", "Home")! : model.ReturnUrl);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = accounts.Find(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user is null || !accounts.ChangePassword(user, model.CurrentPassword, model.NewPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View(model);
        }
        user = accounts.Find(user.Id)!;
        await SignIn(user);
        TempData["Message"] = "Đã đổi mật khẩu.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task SignIn(AppUser user)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("must_change_password", user.MustChangePassword ? "true" : "false")
        ], CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14) });
    }
}
