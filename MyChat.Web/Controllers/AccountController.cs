using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyChat.BLL.Interfaces;
using MyChat.Web.Models;
using MyChat.Web.ViewModels;

namespace MyChat.Web.Controllers;

public class AccountController: Controller
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager; 
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IAccountService _accountService;
    private readonly IEmailService _emailService;

    public AccountController(UserManager<User> userManager, SignInManager<User> signInManager,
        IWebHostEnvironment webHostEnvironment, IAccountService accountService, IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
        _accountService = accountService;
        _emailService = emailService;
    }
    
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if(ModelState.IsValid)
        {
            var wwwPath=_webHostEnvironment.WebRootPath;
              var path=Path.Combine(wwwPath, "images", "avatars");
              if(!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
               var extension=Path.GetExtension(model.Avatar.FileName);
              var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".jfif" };
              
               if (!allowedExtensions.Contains(extension))
               {
                   throw new InvalidOperationException($"Only {string.Join(",", allowedExtensions)} are allowed");
               }
               var newFileName = $"{Guid.NewGuid()}{extension}";
               var fileWithPath=Path.Combine(path, newFileName);
               using var fileStream=new FileStream(fileWithPath, FileMode.Create);
               await model.Avatar.CopyToAsync(fileStream);
               User user = new User {
                   UserName = model.UserName,
                   Email = model.Email,
                   BirthOfDate = model.BirthOfDate,
                   Avatar = $"/images/avatars/{newFileName}"
               };   
           
            var result = await _accountService.RegisterAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _emailService.SendEmailAsync(user.Email, "Добро пожаловать!",
                    "Вы успешно зарегистрировались в MyChat");
               await _signInManager.SignInAsync(user, false);
                return RedirectToAction("Index", "Home");
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }
    
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (ModelState.IsValid)
        {
            User? user = await _accountService.FindUserAsync(model.LoginOrEmail);
            if (user == null)
            {
                user = await _accountService.FindUserAsync(model.LoginOrEmail);
            }
            if (user != null)
            {
               Microsoft.AspNetCore.Identity.SignInResult result = await _signInManager.PasswordSignInAsync(
                    user,
                    model.Password,
                    model.RememberMe,
                    false
                );

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                    {
                        return Redirect(model.ReturnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }
            ModelState.AddModelError("", "Неправильный логин и (или) пароль");
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Login");
    }
}