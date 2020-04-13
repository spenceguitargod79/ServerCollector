using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HydraMVC.Controllers
{
    public class BingoCardController : Controller
    {
        public static List<string> numbersList = new List<string>();
        public static string cardFailed = "NULL";
        public static List<string> numbersListCopy = new List<string>();//Used to save data that will repopulate text fields on page refresh

        public ActionResult Index()
        {
            return View();
        }

        //Send resulting bingo card numbers here when user clicks 'Save Card' button
        //Split into an array, seprated by a delimiter, then convert to a list and remove last element.
        [HttpPost]
        public ActionResult SetCard(string numbers)
        {
            numbersList = numbers.Split('|').ToList();
            numbersList.RemoveAt(numbersList.Count - 1);

            numbersListCopy = numbers.Split('|').ToList();
            numbersListCopy.RemoveAt(numbersList.Count - 1);

            foreach (string s in numbersList)
            {
                System.Console.WriteLine(numbersList);
            }
            //If the card failed validation
            if (BingoCardValidator(numbersList) == false)
            {
                //pop up an err alert to user
                Response.Write("ERROR: one or more values in your bingo card are invalid. " + 
                    "Criteria: No duplicates, no characters, only numbers from 1 to 75 are accepted, and no blanks.");
                cardFailed = "FAILED";
            }
            else
            {
                //Bingo card passed checks
                Response.Write("Bingo Card passed validation");
                cardFailed = "PASSED";
            }

            return null;
        }

        public List<string> GetCard()
        {
            return numbersList;
        }

        public static bool? BingoCardValidator(List<string> card)
        {
            bool? result = null;//use a nullable bool so the foreach loop doesn't jump to passed when it shouldn't
            var hashset = new HashSet<string>();

            try
            {
                foreach (string s in numbersList)
                {
                    if (s.Equals(""))
                    {//check if user left the textbox empty
                        result = false;
                        break;
                    }
                    else if (!hashset.Add(s))
                    {//check for duplicate values
                        result = false;
                        break;
                    }
                    else if (Int32.Parse(s) < 1 || Int32.Parse(s) > 75)//check the possible number boundries
                    {
                        result = false;
                        break;
                    }
                    else//all checks passed
                    {
                        result = true;
                        break;
                    }
                }
            }
            catch
            {
                result = false;//a character existed somewhere in the card, or some other invalid value that wasnt handled above.
            }

            return result;
        }

        //Fill the card with 25 empty values
        public static void InitializeCard()
        {
            
            if(numbersList.Count < 1)//This should only initialize it once
            {
                for(int i = 0; i < 25; i++)
                {
                    numbersList.Add("");
                }
            }
            else
            {
                Console.WriteLine("numberlist is already initialized");
            }
        }

        //Replace all card values with empty strings (call this in force/index via ajax)
        [HttpPost]
        public ActionResult ClearCard()
        {
            numbersList.RemoveRange(0, numbersList.Count);
             
            return null;
        }

        //Return the requested index string value
        public static string GetCardIndexValue(int index){
            if (numbersList != null)
            {
                return numbersList[index];
            }
            else
            {
                return "";
            }
            
        }
	}
}