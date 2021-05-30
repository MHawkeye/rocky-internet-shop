using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rocky.Data;
using Rocky.Models;
using Rocky.Models.ViewModels;
using Rocky.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Rocky.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        [BindProperty]
        public ProductUserVM ProductUserVM { get; set; }

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count()>0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();

            IEnumerable<Product> productList = _db.Product.Where(p => prodInCart.Contains(p.Id));

            return View(productList);
        }

        [HttpPost, ActionName("Index")]
        [ValidateAntiForgeryToken]
        public IActionResult IndexPost()
        {
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count() > 0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();

            IEnumerable<Product> productList = _db.Product.Where(p => prodInCart.Contains(p.Id));

            ProductUserVM = new ProductUserVM()
            {
                ApplicationUser = _db.ApplicationUser.FirstOrDefault(u => u.Id == claim.Value),
                ProductList = productList.ToList()
            };


            return View(ProductUserVM);
        }

        [HttpPost]
        public async Task<IActionResult> Summary(ProductUserVM productUserVM)
        {
            EmailService emailService = new EmailService();

            List<string> prod = new List<string>();

            foreach(var item in productUserVM.ProductList)
            {
                prod.Add("Order:" +
                    "ID:" + item.Id + " Name:" + item.Name + " Price:" + item.Price);
                    ;
            }

            switch (prod.Count)
            {
                case 1:
                    await emailService.SendEmailAsync("kzylbayev.mukhammed@gmail.com", "Order", ProductUserVM.ApplicationUser.Email+"/n"+ prod[0]+"<br/>");
                    break;

                case 2:
                    await emailService.SendEmailAsync("kzylbayev.mukhammed@gmail.com", "Order", ProductUserVM.ApplicationUser.Email + prod[0]+" " +prod[1] + "<br/>");
                    break;
                case 3:
                    await emailService.SendEmailAsync("kzylbayev.mukhammed@gmail.com", "Order", ProductUserVM.ApplicationUser.Email + prod[0] + " " + prod[1] + " " + prod[2] + "<br/>");
                    break;
                case 4:
                    await emailService.SendEmailAsync("kzylbayev.mukhammed@gmail.com", "Order", ProductUserVM.ApplicationUser.Email + prod[0] + " " + prod[1] + " " + prod[2] + " " + prod[3] + "<br/>");
                    break;
            }

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Count() > 0)
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            var removeToCartList = shoppingCartList.SingleOrDefault(i=>i.ProductId == id);

            if (removeToCartList != null)
            {
                shoppingCartList.Remove(removeToCartList);
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);

            if (shoppingCartList.Count() > 0)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }


        }
    }
}
