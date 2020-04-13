using HydraMVC.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HydraMVC
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            MorpheusController m = new MorpheusController();

            m.PostAsyncAll(AutomationThreads.SERVER_STATUS);//Launch a background thread
            m.PostAsyncAll(AutomationThreads.ACTIVE_USERS);
            m.PostAsyncAll(AutomationThreads.SERVER_NAME);
            m.PostAsyncAll(AutomationThreads.REPORT_SERVER);
            m.PostAsyncAll(AutomationThreads.BOI_VERSION);
            m.PostAsyncAll(AutomationThreads.HOTFIX);
            m.PostAsyncAll(AutomationThreads.PLAYER_VERSION);
            m.PostAsyncAll(AutomationThreads.GAMESERVER);

            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Server.MapPath("~/Web.config")));
        }
    }
}
