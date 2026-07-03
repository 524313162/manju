using Microsoft.AspNetCore.Mvc;

namespace ManjuCraft.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "首页";
        return View();
    }
}