using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using HydraMVC.Models;
using HydraMVC.DAL;
using HydraMVC.Controllers;
using System.Threading;
using System.Threading.Tasks;

namespace HydraMVC.Controllers
{
    public class ServerController : Controller
    {
        
        private ServerContext db = new ServerContext();//instantiates a database context object
        
        // GET: /Server/
        //The Index action method gets a list of servers from the Servers entity set by 
        //reading the Servers property of the database context instance:
        [Authorize]
        public ActionResult Index()
        {
            return View(db.Servers.ToList());
            
        }

        // GET: /Server/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Server server = db.Servers.Find(id);
            if (server == null)
            {
                return HttpNotFound();
            }
            return View(server);
        }

        // GET: /Server/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Server/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,IpAddress,ServerName,BOI,GameServer,HotFixes,PlayerVersions,ReportServer,Notes,Status,ActiveUsers,Rack")] Server server)
        {
            if (ModelState.IsValid)
            {
                db.Servers.Add(server);
              
                db.SaveChanges();
                HydraMVC.Controllers.MorpheusController.UpdateAllFieldsOnCreate(server.IpAddress);
                return RedirectToAction("Index");
            }
            return View(server);
        }

        // GET: /Server/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Server server = db.Servers.Find(id);
            if (server == null)
            {
                return HttpNotFound();
            }
            return View(server);
        }

        // POST: /Server/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        //The Bind attribute is another important security mechanism that keeps hackers from over-posting data to your model
        //You should only include properties in the bind attribute that you want to change.
        [HttpPost]//This attribute specifies that that overload of the Edit method can be invoked only for POST requests.
        [ValidateAntiForgeryToken]//helps prevent against cross-site request forgery
        public ActionResult Edit([Bind(Include = "Id,ServerName,IpAddress,BOI,GameServer,HotFixes,PlayerVersions,ReportServer,Notes,Status,ActiveUsers,Rack")] Server server)
        {
            if (ModelState.IsValid)
            {
                db.Entry(server).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(server);
        }

        // GET: /Server/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Server server = db.Servers.Find(id);
            if (server == null)
            {
                return HttpNotFound();
            }
            return View(server);
        }

        // POST: /Server/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Server server = db.Servers.Find(id);
            db.Servers.Remove(server);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //Generate and return a random sqa quote -SH
        //Static will allow to easily access this method in another file
        public static String SQAQuote()
        {
            //Declare/Initialize array
            string[] quotes = { 
                "“Quality is free, but only to those who are willing to pay heavily for it.”",
                "\"Quality means doing it right even when no one is looking.\"",
                "\"Testers don’t like to break things, they like to dispel the illusion that things work.\"",
                "\"The challenge of a tester is to test as little as possible. Test less, but test smarter.\"",
                "\"Testing is a skill. While this may come as a surprise to some people it is a simple fact.\"",
                "\"Discovering the unexpected is more important than confirming the known.\"",
                "\"Testing is an infinite process of comparing the invisible to the ambiguous in order to avoid the unthinkable happening to the anonymous.\"",
                "\"Just because you’ve counted all the trees doesn’t mean you’ve seen the forest.\"",
                "\"Right or wrong, it’s very pleasant to break something from time to time.\"",
                "\"Q: How many testers does it take to change a lightbulb? A: None, they just tell you that the room is dark.\"",
                "\"All code is guilty until proven innocent.\"",
                "\"Software testers succeed where others fail.\""
            };
            //Generate a random number within the range of our array
            Random random = new Random();
            int randomQuoteIndex = random.Next(0,quotes.Length);
            String result = quotes[randomQuoteIndex];

            //return the randomly selected quote
            return result;
        }
    }
}
