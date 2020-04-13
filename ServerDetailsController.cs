using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HydraMVC.Controllers
{
    public class ServerDetailsController : Controller
    {
        //
        // GET: /ServerDetails/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ServerDetails()
        {
            ViewBag.Message = "Server Details";

            return View();
        }
	}
}