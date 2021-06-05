using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess.Data;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    [Authorize(Roles = WC.AdminRole)]
    public class CategoryController : Controller
    {
        private readonly ICategoryRepository _catRepo;

        public CategoryController(ICategoryRepository catRepo)
        {
            _catRepo = catRepo;
        }
        public IActionResult Index()
        {
            IEnumerable<Category> objList = _catRepo.GetAll();
            return View(objList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _catRepo.Add(category);
                _catRepo.Save();

                return RedirectToAction("Index");
            }

            return View(category);
        }


        [HttpGet]
        public IActionResult Edit(int? id)
        {
            if(id == 0)
            {
                return NotFound();
            }
            var obj = _catRepo.Find(id.GetValueOrDefault());
            //_db.Category.Find(id);
            if(obj == null)
            {
                return NotFound();
            }
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _catRepo.Update(category);
                _catRepo.Save();

                return RedirectToAction("Index");
            }

            return View(category);
        }


        [HttpGet]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obj = _catRepo.Find(id.GetValueOrDefault());
            //_db.Category.Find(id);

            return View(obj);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var obj = _catRepo.Find(id.GetValueOrDefault());
            //_db.Category.Find(id);

            _catRepo.Remove(obj);
            _catRepo.Save();
//            _db.Category.Remove(obj);
//            _db.SaveChanges();
            return RedirectToAction("Index");


        }
    }
}
