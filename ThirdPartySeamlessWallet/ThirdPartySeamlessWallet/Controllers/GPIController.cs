using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ThirdPartySeamlessWallet.Controllers
{
    public class GPIController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}