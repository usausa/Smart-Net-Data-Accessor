namespace Example.WebApplication.Controllers
{
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Example.WebApplication.Dao;
    using Example.WebApplication.Models;

    using Microsoft.AspNetCore.Mvc;

    public class HomeController : Controller
    {
        private readonly ISampleDao sampleDao;

        public HomeController(ISampleDao sampleDao)
        {
            this.sampleDao = sampleDao;
        }

        public async Task<IActionResult> Index(DataListForm form)
        {
            ViewBag.Count = await sampleDao.CountDataAsync().ConfigureAwait(false);
            ViewBag.List = await sampleDao.QueryDataAsync(form.Type).ConfigureAwait(false);

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
