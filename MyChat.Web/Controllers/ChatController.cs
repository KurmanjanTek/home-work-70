using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MyChat.Web.Models;

namespace MyChat.Web.Controllers;

public class ChatController:Controller
{
    private readonly UserManager<User> _userManager;

    public ChatController( UserManager<User> userManager)
    {
        _userManager = userManager;
    }
    
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

}