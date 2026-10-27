using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using MyChat.BLL.Interfaces;
using MyChat.Web.Models;
using MyChat.Web.ViewModels;

namespace MyChat.Web.Controllers;
//[Authorize]
public class UserController: Controller
{
    private readonly IUserService _userService;
    private readonly IMessageService _service;
    private readonly UserManager<User> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IEmailService _emailService;

    public UserController(IUserService userService, IMessageService service,
        UserManager<User> userManager, IWebHostEnvironment webHostEnvironment, IEmailService emailService)
    {
        _userService= userService;
        _service = service;
        _userManager=userManager;
        _webHostEnvironment=webHostEnvironment;
        _emailService = emailService;
    }
    
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
       
        return View(currentUser);
    }
    
    
    //[Authorize(Roles="admin, user")]
    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        var model = new EditProfileViewModel
        {
            UserName = currentUser.UserName,
            Email = currentUser.Email,
            BirthOfDate = currentUser.BirthOfDate
        };
        return View(model);
    }
    
    
   // [Authorize(Roles=" user")]
    [HttpPost]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return NotFound();

        if (model.Avatar != null)
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
            currentUser.Avatar = $"/images/avatars/{newFileName}";
            var fileWithPath=Path.Combine(path, newFileName);
            using var fileStream=new FileStream(fileWithPath, FileMode.Create);
            await model.Avatar.CopyToAsync(fileStream);
        }

        currentUser.UserName = model.UserName;
        currentUser.Email = model.Email;
        currentUser.BirthOfDate= model.BirthOfDate;
        
        await _userService.UpdateAsync(currentUser);
        await _emailService.SendEmailAsync(currentUser.Email, "Профиль изменен",
            "Данные Вашего профиля в MyChat были изменены");
        
        return RedirectToAction("Index");
    }
    
    
    [Authorize(Roles="admin")]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        return View(users);
    }
    
    [Authorize(Roles="admin")]
    [HttpGet]
    public async Task<ActionResult> BlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        var result = await _userService.BlockUserAsync(user);
      
        if (result)
        {
            return RedirectToAction("Users");
        }
        ModelState.AddModelError("", "Ошибка при блокировке пользователя.");
        return View("Users");
    }
    
    //[Authorize(Roles="admin")]
    [HttpGet]
    public async Task<IActionResult> EditProfile(int id)
    {
        var currentUser = await _userManager.FindByIdAsync(id.ToString());
        if (_userManager == null)
        {
            return NotFound();
        }

        var model = new EditProfileViewModel
        {
            UserName = currentUser.UserName,
            Email = currentUser.Email,
            BirthOfDate = currentUser.BirthOfDate
        };
        return View(model);
    }
    
    //[Authorize(Roles="admin")]
    [HttpPost]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model, int id )
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        user.UserName=model.UserName;
        user.Email=model.Email;
        user.BirthOfDate=model.BirthOfDate;
        
        if (model.Avatar != null)
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
            user.Avatar = $"/images/avatars/{newFileName}";
            var fileWithPath=Path.Combine(path, newFileName);
            using var fileStream=new FileStream(fileWithPath, FileMode.Create);
            await model.Avatar.CopyToAsync(fileStream);
        }
        await _userService.UpdateAsync(user);
        return RedirectToAction("Users", "User", new{id=user.Id});
    }
    
    [Authorize(Roles="admin")]
    [HttpGet]
    public IActionResult CreateProfile()
    {
        return View();
    }
    
    
    [Authorize(Roles="admin")]
    [HttpPost]
    public async Task<IActionResult> Create(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            User user = new User()
            {
                UserName = model.UserName,
                Email = model.Email,
                BirthOfDate = model.BirthOfDate,
                Avatar = "/images/avatars/default.png"
            };
            var result = await _userService.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("Index");
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> Profile(int id)
    { 
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        var messageCount = await _service.GetMessageCountByUserIdAsync(user.Id);
        ViewBag.MessageCount = messageCount;
        return View(user);
    }

    //[Authorize(Roles="admin, user")]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    //[Authorize(Roles = "admin, user")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return NotFound();
        
        var result = await _userManager.ChangePasswordAsync(
            user, 
            model.CurrentPassword,
            model.NewPassword
            );

        if (result.Succeeded)
        {
            await _emailService.SendEmailAsync(
                user.Email, "Пароль изменен",
                "Пароль Вашей учетнойзаписи MyChat был зменен");
        }

        return RedirectToAction("Index");

       
    }
    
}