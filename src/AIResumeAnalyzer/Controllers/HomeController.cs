using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AIResumeAnalyzer.Models;

namespace AIResumeAnalyzer.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Route("Home/StatusCode")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HandleStatusCode(int code)
    {
        if (code == 404)
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        Response.StatusCode = code;
        return View("Error", new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorTitle = $"Request Could Not Be Completed (HTTP {code})",
            ErrorMessage = "Something went wrong while handling your request. Please try again or return to your dashboard."
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var model = new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ErrorTitle = "Something Went Wrong",
            ErrorMessage = "We ran into an unexpected hiccup while processing your request. Your data is safe — please try again or return to your dashboard.",
            FailedPath = HttpContext.Items["ErrorPath"] as string,
            DeveloperDetail = HttpContext.Items["ErrorDetail"] as string
        };

        return View(model);
    }
}
