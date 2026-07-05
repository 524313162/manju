using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ManjuCraft.Web.Controllers;

public class SettingsController : Controller
{
    private readonly IWebHostEnvironment _env;

    public SettingsController(IWebHostEnvironment env)
    {
        _env = env;
    }

    public IActionResult Index()
    {
        ViewData["Title"] = "设置";
        ViewBag.HideFooter = true;
        return View();
    }

    [HttpGet]
    public IActionResult DownloadDb()
    {
        var dbPath = Path.Combine(_env.ContentRootPath, "manju.db");
        if (!System.IO.File.Exists(dbPath))
        {
            return Content("数据库文件不存在", "text/plain");
        }
        var fileName = "manju.db";
        return File(System.IO.File.ReadAllBytes(dbPath), "application/octet-stream", fileName);
    }

    [HttpPost("DeleteDb")]
    public IActionResult DeleteDb()
    {
        var dbPath = Path.Combine(_env.ContentRootPath, "manju.db");
        if (System.IO.File.Exists(dbPath))
        {
            System.IO.File.Delete(dbPath);
        }
        return Json(new { success = true });
    }

    [HttpPost("DeleteDbBackup")]
    public IActionResult DeleteDbBackup()
    {
        var dbPath = Path.Combine(_env.ContentRootPath, "manju.db");
        var bakPath = Path.Combine(_env.ContentRootPath, "manju.db.bak");
        if (System.IO.File.Exists(bakPath))
        {
            System.IO.File.Delete(bakPath);
        }
        return Json(new { success = true });
    }
}
