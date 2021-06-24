using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rocky_DataAccess.Data;
using Rocky_Models;
using Rocky_Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Rocky_Utility;
using Rocky_DataAccess.Repository.IRepository;

namespace Rocky.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _prodRepo;
        private readonly ICategoryRepository _catRepo;

        public HomeController(ILogger<HomeController> logger, IProductRepository prodRepo, ICategoryRepository catRepo)
        {
            _logger = logger;
            _prodRepo = prodRepo;
            _catRepo = catRepo;
        }

        public IActionResult Index()
        {
            HomeVM homeVM = new HomeVM()
            {
                Products = _prodRepo.GetAll(includeProperties:"Category"),
                //_db.Product.Include(u => u.Category),
                Categories = _catRepo.GetAll() //_db.Category

            };
            return View(homeVM);
        }


        public IActionResult Details(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>("ShoppingCartSession") != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>("ShoppingCartSession").Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>("ShoppingCartSession");
            }

            DetailsVM detailsVM = new DetailsVM()
            {
                Product = _prodRepo.FirstOrDefault(u=>u.Id == id, includeProperties: "Category"),
//                _db.Product.Include(u => u.Category).Where(u=>u.Id==id).FirstOrDefault(),
                ExistsInCart = false
            };

            foreach(var item in shoppingCartList)
            {
                if(item.ProductId == id)
                {
                    detailsVM.ExistsInCart = true;
                }
            }
            return View(detailsVM);
        }

        [HttpPost, ActionName("Details")]
        public IActionResult DetailsPost(int id, DetailsVM detailsVM)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>("ShoppingCartSession") != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>("ShoppingCartSession").Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>("ShoppingCartSession");
            }

            shoppingCartList.Add(new ShoppingCart { ProductId = id,  SqFt= detailsVM.Product.TempSqFt});
            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveFromCart(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }
            var itemToRemove = shoppingCartList.SingleOrDefault(r => r.ProductId == id);
            if (itemToRemove != null)
            {
                shoppingCartList.Remove(itemToRemove);
            }
            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            return RedirectToAction(nameof(Index));
        }

    }
}
