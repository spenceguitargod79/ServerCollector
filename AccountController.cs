using System.Web.Mvc;
using System.Web.Security;

using HydraMVC.Models;

public class AccountController : Controller
{
    public ActionResult Login()
    {
        return this.View();
    }

    [HttpPost]
    public ActionResult Login(LoginViewModel model, string returnUrl)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        if (Membership.ValidateUser(model.UserName, model.Password))
        {
            FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
            if (this.Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
            {
                return this.Redirect(returnUrl);
            }

            return this.RedirectToAction("Index", "Server");
        }

        this.ModelState.AddModelError(string.Empty, "The user name or password provided is incorrect.");

        return this.View(model);
    }

    public ActionResult LogOff()
    {
        FormsAuthentication.SignOut();

        return this.RedirectToAction("Login", "Account");
    }
}