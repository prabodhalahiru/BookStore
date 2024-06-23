using BookStore_Practice2Web.Data;
using BookStore_Practice2Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace BookStore_Practice2Web.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<BookCategory> objCategoryList = _db.BookCategories;
            return View(objCategoryList);
        }
        //GET
        public IActionResult Create()
        {
            return View();
        }

        //POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(BookCategory obj)
        {
            if (ModelState.IsValid)
            {
                _db.BookCategories.Add(obj);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(obj);

        }
    }
}
