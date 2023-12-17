namespace Example.WebApplication.Controllers;

using System.Diagnostics;

using Example.WebApplication.Accessor;
using Example.WebApplication.Models;

using Microsoft.AspNetCore.Mvc;

using Smart.Data.Accessor;

public sealed class HomeController : Controller
{
    private readonly ISampleAccessor sampleAccessor;

    public HomeController(IAccessorResolver<ISampleAccessor> sampleAccessor)
    {
        this.sampleAccessor = sampleAccessor.Accessor;
    }

    public async ValueTask<IActionResult> Index(DataListForm form)
    {
        ViewBag.Count = await sampleAccessor.CountDataAsync().ConfigureAwait(false);
        ViewBag.List = await sampleAccessor.QueryDataAsync(form.Type).ConfigureAwait(false);

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
