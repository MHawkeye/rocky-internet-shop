using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rocky_DataAccess.Repository.IRepository;
using Rocky_Models;
using Rocky_Models.ViewModels;
using Rocky_Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Rocky.Controllers
{
    [Authorize(WC.AdminRole)]
    public class InquiryController : Controller
    {
        private readonly IInquiryHeaderRepository _inqHRepo;
        private readonly IInquiryDetailRepository _inqDRepo;
        public InquiryVM InquiryVM { get; set; }

        public InquiryController(IInquiryHeaderRepository inqHRepo, IInquiryDetailRepository inqDRepo)
        {
            _inqDRepo = inqDRepo;
            _inqHRepo = inqHRepo;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int id)
        {
            InquiryVM = new InquiryVM()
            {
                InquiryHeader = _inqHRepo.FirstOrDefault(u => u.Id == id),
                InquiryDetail = _inqDRepo.GetAll(u => u.InquiryHeaderId == id, includeProperties:"Product" )
            };

            return View(InquiryVM);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Details(InquiryVM inquiryVM)
        {
            List<ShoppingCart> shoppingCartList = new List<ShoppingCart>();
            var test = inquiryVM.InquiryHeader.Id;
            inquiryVM.InquiryDetail = _inqDRepo.GetAll(u => u.InquiryHeaderId == inquiryVM.InquiryHeader.Id);

            foreach(var detail in inquiryVM.InquiryDetail)
            {
                ShoppingCart shoppingCart = new ShoppingCart()
                {
                    ProductId = detail.ProductId
                };

                shoppingCartList.Add(shoppingCart);
            }

            HttpContext.Session.Clear();

            HttpContext.Session.Set(WC.SessionCart, shoppingCartList);
            HttpContext.Session.Set(WC.SessionInquiryId, inquiryVM.InquiryHeader.Id);

            return RedirectToAction("Index", "Cart");
        }

        [HttpPost]
        public IActionResult Delete(InquiryVM inquiryVM)
        {
            InquiryHeader inquiryHeader = _inqHRepo.FirstOrDefault(u => u.Id == inquiryVM.InquiryHeader.Id);
            IEnumerable<InquiryDetail> inquiryDetail = _inqDRepo.GetAll(u => u.InquiryHeaderId == inquiryVM.InquiryHeader.Id);

            _inqDRepo.RemoveRange(inquiryDetail);
            _inqHRepo.Remove(inquiryHeader);

            _inqHRepo.Save();

            return RedirectToAction(nameof(Index));
        }




        #region API CALLS
        [HttpGet]
        public IActionResult GetInquiryList()
        {
            return Json(new { data = _inqHRepo.GetAll() });
        }

        #endregion


    }
}
