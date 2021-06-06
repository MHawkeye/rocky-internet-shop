using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess.Data;
using Rocky_Models;
using Rocky_Models.ViewModels;
using Rocky_Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Rocky_DataAccess.Repository.IRepository;

namespace Rocky.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IProductRepository _prodRepo;
        private readonly IApplicationUserRepository _userRepo;
        private readonly IInquiryHeaderRepository _inqHRepo;
        private readonly IInquiryDetailRepository _inqDRepo;

        [BindProperty]
        public ProductUserVM ProductUserVM { get; set; }

        public CartController(IProductRepository prodRepo, IApplicationUserRepository userRepo,
                              IInquiryHeaderRepository inqHRepo, IInquiryDetailRepository inqDRepo)
        {
            _prodRepo = prodRepo;
            _userRepo = userRepo;
            _inqDRepo = inqDRepo;
            _inqHRepo = inqHRepo;
            
        }
        public IActionResult Index()
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();

            IEnumerable<Product> productList = _prodRepo.GetAll(p => prodInCart.Contains(p.Id));

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
                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            List<int> prodInCart = shoppingCartList.Select(i => i.ProductId).ToList();

            IEnumerable<Product> productList = _prodRepo.GetAll(p => prodInCart.Contains(p.Id));

            ProductUserVM = new ProductUserVM()
            {
                ApplicationUser = _userRepo.FirstOrDefault(u => u.Id == claim.Value),
                ProductList = productList.ToList()
            };


            return View(ProductUserVM);
        }

        [HttpPost]
        public async Task<IActionResult> Summary(ProductUserVM productUserVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            List<string> prod = new List<string>();

            foreach (var item in productUserVM.ProductList)
            {
                prod.Add("Order:" +
                    "ID:" + item.Id + " Name:" + item.Name + " Price:" + item.Price);
                ;
            }

            InquiryHeader inquiryHeader = new InquiryHeader()
            {
                ApplicationUserId = claim.Value,
                FullName = productUserVM.ApplicationUser.FullName,
                Email = productUserVM.ApplicationUser.Email,
                PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                InquiryDate = DateTime.Now
            };

            _inqHRepo.Add(inquiryHeader);
            _inqHRepo.Save();

            foreach(var pr in productUserVM.ProductList)
            {
                InquiryDetail inquiryDetail = new InquiryDetail()
                {
                    InquiryHeaderId = inquiryHeader.Id,
                    ProductId = pr.Id
                };

                _inqDRepo.Add(inquiryDetail);
            }
            _inqDRepo.Save();

            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int id)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            if (HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null)
                
//                Get<IEnumerable<ShoppingCart>>(WC.SessionCart) != null
//                && HttpContext.Session.Get<IEnumerable<ShoppingCart>>(WC.SessionCart).Any())
            {
                shoppingCartList = HttpContext.Session.Get<List<ShoppingCart>>(WC.SessionCart);
            }

            var removeToCartList = shoppingCartList.SingleOrDefault(i => i.ProductId == id);

            if (removeToCartList != null)
            {
                shoppingCartList.Remove(removeToCartList);
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);

            if (shoppingCartList.Count > 0)
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