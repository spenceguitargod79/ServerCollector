using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HydraMVC.Controllers
{
    public class PatternController : Controller
    {
        //
        // GET: /Pattern/
        public ActionResult Index()
        {
            return View();
        }

        public static string validationResultText = "";

        //Send resulting pattern here when user clicks 'set pattern' button
        //Split into an array, seprated by a delimiter
        [HttpPost]
        public ActionResult SetPattern(string pattern, string ipAddress)
        {
            List<string> patternList = pattern.Split('|').ToList();
            patternList.RemoveAt(patternList.Count - 1);

            foreach (string s in patternList)
            {
                System.Console.WriteLine(patternList);
            }

            if(BingoCardController.cardFailed.Equals("FAILED"))
            {
                Trace.WriteLine("Card Failed");
                validationResultText = "ERROR: Your changes were not saved. Invalid data was entered. Try again :^)";
                BingoCardController.cardFailed = "NULL";//reset value
            }
            else if (BingoCardController.cardFailed.Equals("PASSED"))
            {
                BuildFinalPattern(patternList, BingoCardController.numbersList, ipAddress);
                validationResultText = "Congrats, your pattern was saved!";
                BingoCardController.cardFailed = "NULL";//reset value
            }
            else
            {
                Trace.WriteLine("Card neither failed nor passed: " + BingoCardController.cardFailed);
                validationResultText = "ERROR: Your changes were not saved. Something very wrong happened. Try again please :^)";
            }
            
            return null;
        }

        public void BuildFinalPattern(List<string> pattern, List<string> bingoCard, string ip)
        {
            List<string> finalNumbersList = new List<string>();//this will end up being the values to put into Fo.txt on the desired server
            List<int> patternIndexes = new List<int>();//location in patternList of the elements the user checked
            int patternIndexesCount = 0;
            int remainingIndexesNeeded = 0;
            int totalNumsOnCard = 30;
            Random r = new Random();

            //Only grab the idexes that are not empty
            foreach (string s in pattern)
            {
                if (s.Equals(""))
                {
                    continue;
                }
                else
                {
                    patternIndexes.Add(Int32.Parse(s) - 1);//convert strings to ints, subtract 1 because checkbox values are not zero based
                }
            }

            patternIndexesCount = patternIndexes.Count;//how many numbers are in the users pattern
            remainingIndexesNeeded = totalNumsOnCard - patternIndexesCount;//how many more numbers we need to fill the bingo card to a total of 30 numbers
            
            //grab the elements from bingcard that reside in indexes based off of patternIndexes results
            foreach (int s in patternIndexes)
            {
                finalNumbersList.Add(bingoCard[s]);//Add users pattern to this list
            }

            //fill remaining elements with random numbers between 1 and 75
            //cannot be duplicate numbers
            for (int i = 0; i < remainingIndexesNeeded; i++)
            {
                int n = r.Next(1, 75);
                //check if the number generated is already a number in bingo card OR a number in final numbers list
                if(bingoCard.Contains(n.ToString()) || finalNumbersList.Contains(n.ToString())){
                    i--;//So this check never happened according to the iteration
                    continue;
                }
                else
                {
                    finalNumbersList.Add(n.ToString());
                }
            }

            //TODO:Update Fo.txt on the server
            ForceController.EditFOtxtOnHydra(finalNumbersList,ip);
            //ForceController.EchoIntoFO(finalNumbersList, ip);
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