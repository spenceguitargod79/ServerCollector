using HydraMVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HydraMVC.Controllers
{
    public class LinksController : Controller
    {
        //
        // GET: /Links/
        public ActionResult Index()
        {
            

            //Create a list
            List<LinksModel> newList = new List<LinksModel>();

            //Create first item
            LinksModel newLinksModel = new LinksModel
            {
                ID = 1,
                Description = "Sup Yo?",
                Comments = "Chillin over here"
            };

            //Create first item
            LinksModel newLinksModel2 = new LinksModel
            {
                ID = 2,
                Description = "Sup Homie?",
                Comments = "Chillin over there"
            };

            //Add the items to our list
            newList.Add(newLinksModel);
            newList.Add(newLinksModel2);

            //return View();
            return View(newList);

        }
	}
}