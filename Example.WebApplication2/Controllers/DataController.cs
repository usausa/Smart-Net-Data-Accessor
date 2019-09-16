namespace Example.WebApplication2.Controllers
{
    using System.Threading.Tasks;

    using Example.WebApplication2.Accessor;
    using Example.WebApplication2.Models;

    using Microsoft.AspNetCore.Mvc;

    public class DataController : Controller
    {
        private readonly IPrimaryAccessor primaryAccessor;

        private readonly ISecondaryAccessor secondaryAccessor;

        public DataController(IPrimaryAccessor primaryAccessor, ISecondaryAccessor secondaryAccessor)
        {
            this.primaryAccessor = primaryAccessor;
            this.secondaryAccessor = secondaryAccessor;
        }

        public async Task<IActionResult> Primary(DataListForm form)
        {
            ViewBag.Count = await primaryAccessor.CountDataAsync().ConfigureAwait(false);
            ViewBag.List = await primaryAccessor.QueryDataAsync(form.Type).ConfigureAwait(false);
            return View();
        }

        public async Task<IActionResult> Secondary(DataListForm form)
        {
            ViewBag.Count = await secondaryAccessor.CountDataAsync().ConfigureAwait(false);
            ViewBag.List = await secondaryAccessor.QueryDataAsync(form.Type).ConfigureAwait(false);
            return View();
        }
    }
}
