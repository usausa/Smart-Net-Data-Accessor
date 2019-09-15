namespace Example.WebApplication2.Controllers
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Example.WebApplication2.Accessor;
    using Example.WebApplication2.Models;

    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        private readonly ISampleAccessor sampleAccessor;

        public HomeController(ISampleAccessor sampleAccessor)
        {
            this.sampleAccessor = sampleAccessor;
        }

        public async Task<IActionResult> Index(DataListForm form)
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
}
