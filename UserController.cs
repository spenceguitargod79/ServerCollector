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
using System.DirectoryServices;

namespace HydraMVC.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private ServerContext db = new ServerContext();

        // GET: /User/
        public ActionResult Index()
        {
            return View(db.Users.ToList());
        }

        // GET: /User/Details/5
        public ActionResult Details(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // GET: /User/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /User/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserId,UserName,Permission")] User user)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    db.Users.Add(user);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            catch (DataException)
            {
                 ModelState.AddModelError("", "Unable to add User.");
    

            }

            return View(user);
        }

        // GET: /User/Edit/5
        public ActionResult Edit(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: /User/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include="UserId,UserName,Permission")] User user)
        {
            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(user);
        }

        // GET: /User/Delete/5
        public ActionResult Delete(int id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            User user = db.Users.Find(id);
            db.Users.Remove(user);
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
        
        //Get the current logged in user's permission level - SH
        public static String GetUserPermission(String currentUser)
        {
            using (var context = new ServerContext())
            {
                //TODO:Only run code if the user exists in User table
                //Get a count to determine if that UserName exists in the User table
                
                try
                {
                    var query = from p in context.Users
                                where p.UserName == currentUser
                                select p;

                    // This will raise an exception if entity not found
                    // Use SingleOrDefault instead
                    var users = query.SingleOrDefault();

                    return users.Permission;
                }
                catch (Exception e)
                {
                    Console.WriteLine("An error occurred: '{0}'", e);
                    return "ACCESS-DENIED";
                };
            }
        }

        //Get current user's email from AD - SIH
        public static String getUserEmail(string userName)
        {
            // get a DirectorySearcher object
            DirectorySearcher search = new DirectorySearcher();

            // specify the search filter
            search.Filter = "(&(objectClass=user)(anr=" + userName + "))";

            // specify which property(s) value to return in the search
            //search.PropertiesToLoad.Add("sn");          // last name
            search.PropertiesToLoad.Add("mail");        // smtp mail address
            // perform the search
            SearchResult result = search.FindOne();

            //System.Diagnostics.Debug.WriteLine(result.Properties["mail"][0].ToString());
            return result.Properties["mail"][0].ToString();
        }

        //Get current user's first name from AD - SIH
        public static String getUserFirstName(string userName)
        {
            // get a DirectorySearcher object
            DirectorySearcher search = new DirectorySearcher();

            // specify the search filter
            search.Filter = "(&(objectClass=user)(anr=" + userName + "))";

            // specify which property values to return in the search
            search.PropertiesToLoad.Add("givenName");// first name

            // perform the search
            SearchResult result = search.FindOne();

            //System.Diagnostics.Debug.WriteLine(result.Properties["givenName"][0].ToString());
            return result.Properties["givenName"][0].ToString();
        }
    }
}
