using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PensionFund
{
  class Program
  {
    public static int year = 2012;
    /// <summary>
    /// Eksogent givet rente
    /// </summary>
    public static double r = 0.01; //hvordan skal denne sættes?

    static void Main(string[] args)
    {
      Console.WriteLine("Opretter pensionskasse");
      PensionFund pensionskasse = new PensionFund();

      Console.WriteLine("Ansætter revisor");
      Auditor revisor = new Auditor(pensionskasse); //Start revisor

      Console.WriteLine("Indbetal 1000 om året i 30 år");
      for (int i = 0; i < 30; i++)
      {
        year++; //nyt år
        revisor.YearStart(); //Opjuster enkelt personers pensions beholdning
        pensionskasse.YearStart();

        revisor.Contribution(1000); //indbetal 1000 om året
        pensionskasse.YearEnd(); //afslut år, opgør samlet pensionsformue
      }

      int udbetaling;

      for (int i = 0; i < 45; i++)
      {
        revisor.YearStart(); //Opjuster enkelt personers pensions beholdning
        pensionskasse.YearStart();
        int age = 65 + i;
        udbetaling = revisor.Installment(age);
        Console.WriteLine("Udbetaling som "+age+"-årig (" + i + ". år): " + udbetaling + " Kr.");
        pensionskasse.YearEnd();
        year++;
      }
      
      Console.ReadKey();
    }

  }
}