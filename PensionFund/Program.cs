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
      PensionSystem pensionSystem = new PensionSystem(); //opret pensionssystem
      Auditor _revisor = new Auditor(); //Start revisor

      Console.WriteLine("Indbetal 1800 om året til livrentepension i 30 fulde år");
      Console.WriteLine("Indbetal 1200 til ratepension i 30 fulde år");
      int[] månedligIndbetalingLr = new int[12];
      int[] månedligIndbetalingR = new int[12];
      for (int m = 0; m < 12; m++)
      {
        månedligIndbetalingLr[m] = 0;// 1800 / 12;
        månedligIndbetalingR[m] = 1200 / 12;
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
        Console.WriteLine("Udbetaling som " + age + "-årig (" + (i+2) + ". år): " + udbetaling + " Kr.");
        PensionSystem.PensionfundLivrente.YearEnd();
        year++;
      }
      
      Console.ReadKey();
    }

    private static void YearStart(Auditor _revisor)
    {
      _revisor.YearStart(); //Opjuster enkelt personers pensions beholdning
      PensionSystem.PensionfundLivrente.YearStart();
      PensionSystem.PensionfundRate.YearStart();
    }

    private static void YearEnd()
    {
      PensionSystem.PensionfundLivrente.YearEnd(); //afslut år, opgør samlet pensionsformue
      PensionSystem.PensionfundRate.YearEnd(); //afslut år, opgør samlet pensionsformue
    }

  }
}