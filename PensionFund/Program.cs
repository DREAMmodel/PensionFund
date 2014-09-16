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

    private static PensionFundLivsrente _pensionskasseLr = new PensionFundLivsrente();
    private static PensionFundRate _pensionskasseR = new PensionFundRate();
    
    static void Main(string[] args)
    {
      Auditor _revisor = new Auditor(_pensionskasseLr, _pensionskasseR); //Start revisor

      Console.WriteLine("Indbetal 1800 om året til livrente og 1200 til ratepension i 30 fulde år");
      int[] månedligIndbetalingLr = new int[12];
      int[] månedligIndbetalingR = new int[12];
      for (int m=0; m < 12; m++)
      {
        månedligIndbetalingLr[m] = 1800 / 12;
        månedligIndbetalingR[m] = 0;// 1200 / 12;
      }

      for (int y = 0; y < 30; y++)
      {
        year++; //nyt år
        YearStart(_revisor);
        _revisor.Update(65-30+y, månedligIndbetalingR, månedligIndbetalingLr); //indbetaling  
        YearEnd(); //afslut år
      }

      //ingen indbetalinger
      for (int m=0; m < 12; m++)
      {
        månedligIndbetalingLr[m] = 0; 
        månedligIndbetalingR[m] = 0;
      }
      
      int udbetaling = _revisor.Update(65, månedligIndbetalingLr, månedligIndbetalingR, 0, 0); //start udbetaling af begge pensioner i første måned (=0)
      YearEnd(); //afslut år
      Console.WriteLine("Første årlige udbetaling: " + udbetaling + " Kr.");

      for (int i = 0; i < 45; i++)
      {
        year++; //nyt år
        YearStart(_revisor); 

        int age = 65 + i + 1;
        udbetaling = _revisor.Update(age, månedligIndbetalingLr, månedligIndbetalingR); //udbetaling
        Console.WriteLine("Udbetaling som " + age + "-årig (" + i + ". år): " + udbetaling + " Kr.");
        _pensionskasseLr.YearEnd();
        year++;
      }
      
      Console.ReadKey();
    }

    private static void YearStart(Auditor _revisor)
    {
      _revisor.YearStart(); //Opjuster enkelt personers pensions beholdning
      _pensionskasseLr.YearStart();
      _pensionskasseR.YearStart();
    }

    private static void YearEnd()
    {
    _pensionskasseLr.YearEnd(); //afslut år, opgør samlet pensionsformue
        _pensionskasseR.YearEnd(); //afslut år, opgør samlet pensionsformue
    }

  }
}