// Authors Jared Nay and Andrew Fryzel
// CS 3500 University of Utah
// April 15, 2019

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BoggleService.Controllers
{
    // Home page when you first start up. Leave alone
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
    }
}
