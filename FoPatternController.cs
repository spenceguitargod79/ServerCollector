using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HydraMVC.Controllers
{
    public class FoPatternController : Controller
    {
        // GET: /Pattern/
        public ActionResult Index()
        {
            return View();
        }

        public static string validationResultText = "";
        public static List<string> patternListCopy = new List<string>();//Used to save data that will repopulate text field on page refresh

        //Send resulting pattern here when user clicks 'set pattern' button
        //Split into an array, seprated by a delimiter
        [HttpPost]
        public ActionResult SetPattern(string pattern, string ipAddress)
        {
            List<string> patternList = pattern.Split(' ').ToList();
            patternListCopy = pattern.Split('|').ToList();

            //patternList.RemoveAt(patternList.Count - 1);
            foreach (string s in patternList)
            {
                System.Console.WriteLine(patternList);
            }

            //Runs the user's numbers through validation and also returns a text result that we can print to the user.
            validationResultText = ValidateNumbers(patternList, ipAddress);

            return null;
        }

        public string ValidateNumbers(List<string> pattern, string ip)
        {
            string errDuplicate = "A duplicate number exists in your pattern. Try again please.";
            string errOutOfBounds = "A number in your pattern is outside of the acceptable boundries (must be >= 0 and <= 75). Try again please.";
            string errNotANumber = "Only numbers are accepted and 1 or more values. Try again please.";
            string success = "Your numbers have been successfully saved! May the force be with your EPS!";
            string finalResult = "";
            var hashset = new HashSet<string>();//for checking dups
            bool passesAllChecks = true;

            //Extract distinct values from the pattern list and save to a new list (safeguard against user putting in dups).
            List<string> distinctPattern = pattern.Distinct().ToList();

            try
            {
                //Validation
                foreach (string s in distinctPattern)
                {
                    //check number boundries
                    if (Int32.Parse(s) >= 0 && Int32.Parse(s) <= 75)
                    {
                        //check for duplicates
                        if (hashset.Add(s))
                        {
                            //passed duplicate check and bounds check
                            passesAllChecks = true;
                        }
                        else
                        {
                            //This shouldnt trigger because duplicates are removed before the loop. But its here as a back up check.
                            Trace.WriteLine(errDuplicate);
                            passesAllChecks = false;
                            finalResult = errDuplicate;
                        }
                    }
                    else
                    {
                        Trace.WriteLine(errOutOfBounds);
                        passesAllChecks = false;
                        finalResult = errOutOfBounds;
                        break;// do not continue the foreach
                    }
                }
            }
            catch
            {
                Trace.WriteLine(errNotANumber);
                passesAllChecks = false;
                finalResult = errNotANumber;
            }

            //Update Fo.txt on the server, if the list passed all validation
            if (passesAllChecks == true)
            {
                ForceController.EditFOtxtOnHydra(distinctPattern, ip);
                ForceController.EchoIntoFO(distinctPattern, ip);
                finalResult = success;
            }

            return finalResult;
        }

        public static string GetValidationTextResult()
        {
            return validationResultText;
        }

        public static string ResetValidationTextResult()
        {
            validationResultText = "";

            return null;
        }
    }
}