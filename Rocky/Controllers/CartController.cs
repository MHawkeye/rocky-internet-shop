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
using Rocky_Utility.BrainTree;
using Braintree;

namespace Rocky.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly IProductRepository _prodRepo;
        private readonly IApplicationUserRepository _userRepo;
        private readonly IInquiryHeaderRepository _inqHRepo;
        private readonly IInquiryDetailRepository _inqDRepo;

        private readonly IOrderHeaderRepository _orderHRepo;
        private readonly IOrderDetailRepository _orderDRepo;

        private readonly IBrainTreeGate _brain;

        [BindProperty]
        public ProductUserVM ProductUserVM { get; set; }

        public CartController(IProductRepository prodRepo, IApplicationUserRepository userRepo,
                              IInquiryHeaderRepository inqHRepo, IInquiryDetailRepository inqDRepo,
                              IOrderHeaderRepository orderHRepo, IOrderDetailRepository orderDRepo,
                              IBrainTreeGate brain
                              )
        {
            _prodRepo = prodRepo;
            _userRepo = userRepo;
            _inqDRepo = inqDRepo;
            _inqHRepo = inqHRepo;
            _orderDRepo = orderDRepo;
            _orderHRepo = orderHRepo;
            _brain = brain;             
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


            IEnumerable<Product> productListTemp = _prodRepo.GetAll(p => prodInCart.Contains(p.Id));
            IList<Product> prodList = new List<Product>();

            foreach(var pr in shoppingCartList)
            {
                Product prodTemp = productListTemp.FirstOrDefault(u => u.Id == pr.ProductId);
                prodTemp.TempSqFt = pr.SqFt;
                prodList.Add(prodTemp);
            }
            return View(prodList);
        }

        [HttpPost, ActionName("Index")]
        [ValidateAntiForgeryToken]
        public IActionResult IndexPost(IEnumerable<Product> products)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            foreach (Product prod in products)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = prod.Id, SqFt = prod.TempSqFt });
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);

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


            IEnumerable<Product> productListTemp = _prodRepo.GetAll(p => prodInCart.Contains(p.Id));
            IList<Product> prodList = new List<Product>();

            foreach (var pr in shoppingCartList)
            {
                Product prodTemp = productListTemp.FirstOrDefault(u => u.Id == pr.ProductId);
                prodTemp.TempSqFt = pr.SqFt;
                prodList.Add(prodTemp);
            }

            ProductUserVM = new ProductUserVM()
            {
                ApplicationUser = _userRepo.FirstOrDefault(u => u.Id == claim.Value),
                ProductList = prodList.ToList()
            };


            var gateway = _brain.GetGateWay();
            var client = gateway.ClientToken.Generate();

            ViewBag.ClientToken = client;
            return View(ProductUserVM);
        }

        [HttpPost]
        public IActionResult Summary(IFormCollection collection, ProductUserVM productUserVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (User.IsInRole(WC.AdminRole))
            {
//                var orderTotal = 0.0;

/*                foreach(Product p in productUserVM.ProductList)
                {
                    orderTotal += p.Price * p.TempSqFt;
                }
*/
                OrderHeader orderHeader = new OrderHeader()
                {
                    CreateByUserId = claim.Value,
                    FinalOrderTotal = productUserVM.ProductList.Sum(x=>x.TempSqFt*x.Price),
                    City = productUserVM.ApplicationUser.City,
                    StreetAddress = productUserVM.ApplicationUser.StreetAddress,
                    State = productUserVM.ApplicationUser.State,
                    PostalCode = productUserVM.ApplicationUser.PostalCode,
                    FullName = productUserVM.ApplicationUser.FullName,
                    Email = productUserVM.ApplicationUser.Email,
                    PhoneNumber = productUserVM.ApplicationUser.PhoneNumber,
                    OrderDate = DateTime.Now,
                    OrderStatus = WC.StatusPending
                };

                _orderHRepo.Add(orderHeader);

                _orderHRepo.Save();

                foreach(var pr in ProductUserVM.ProductList)
                {
                    OrderDetail orderDetail = new OrderDetail()
                    {
                        OrderHeaderId = orderHeader.Id,
                        PricePerSqFt = pr.Price,
                        Sqft = pr.TempSqFt,
                        ProductId = pr.Id
                    };

                    _orderDRepo.Add(orderDetail);
                }

                _orderDRepo.Save();

                string nonceFromTheClient = collection["payment_method_nonce"];

                var request = new TransactionRequest
                {
                    Amount = Convert.ToDecimal(orderHeader.FinalOrderTotal),
                    PaymentMethodNonce = nonceFromTheClient,
                    OrderId = orderHeader.Id.ToString(),
                    Options = new TransactionOptionsRequest
                    {
                        SubmitForSettlement = true
                    }

                };

                var gateway = _brain.GetGateWay();
                Result<Transaction> result = gateway.Transaction.Sale(request);

                if (result.Target.ProcessorResponseText == "Approved")
                {
                    orderHeader.TransactionId = result.Target.Id;
                    orderHeader.OrderStatus = WC.StatusApproved;
                }
                else
                {
                    orderHeader.OrderStatus = WC.StatusCancelled;
                }
                _orderHRepo.Save();
                return RedirectToAction(nameof(InquiryConfirmation),new { id =orderHeader.Id});
            }

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

            foreach (var pr in productUserVM.ProductList)
            {
                InquiryDetail inquiryDetail = new InquiryDetail()
                {
                    InquiryHeaderId = inquiryHeader.Id,
                    ProductId = pr.Id
                };

                _inqDRepo.Add(inquiryDetail);
            }
            _inqDRepo.Save();

            return RedirectToAction(nameof(InquiryConfirmation));
        }

        public IActionResult Updated(IEnumerable<Product> products)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();

            foreach (Product prod in products)
            {
                shoppingCartList.Add(new ShoppingCart { ProductId = prod.Id, SqFt = prod.TempSqFt });
            }

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);

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


        public IActionResult InquiryConfirmation(int id=0)
        {
            OrderHeader orderHeader = _orderHRepo.FirstOrDefault(i => i.Id == id);
            HttpContext.Session.Clear();
            return View(orderHeader);
        }

    }
}