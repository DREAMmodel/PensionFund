using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PensionFund
{
  class PensionFundRate
  {
    /// <summary>
    /// Pensionskassens samlede beholdning under optælling
    /// </summary>
    private int _holdings;
    private double[] _installmentFactor = new double[12 * 10]; //max restløbetid er sat til 10 år
   
    public PensionFundRate(int initialHoldings = 0)
    {
      _holdings = initialHoldings;
    }

    public void InitialAccount(int holdings)
    {
      _holdings += holdings;
    }

    public int Installment(int ratesLeft, int personalHoldings)
    {
      int installment;

      if (ratesLeft <= 0)
        installment = personalHoldings; //udbetal resterende ratepensions-beholdning hvis sidste udbetaling
      else
        installment = Convert.ToInt32(personalHoldings / _installmentFactor[ratesLeft]); //bestem udbetalingens størrelse

      _holdings -= installment; //pengene tages ud af pensionskassens samlede beholdning
      return installment;
    }

    public void Contribution(int contribution)
    {
      _holdings += contribution; //opdater samlet pensionsbeholdning
    }

    public void YearStart()
    {
      InstallmentFactor(); //skal køres hver gang renten ændres, hvis renten er konstant i hele simuleringen kan vi nøjes med at køre denne en gang (i constructoren)
    }

    public void YearEnd()
    {
//      Console.WriteLine("Beholdning ratepension: " + _holdings + " Kr.");
    }

    public void PersonExit(int holdings, int m)
    {
      _holdings -= holdings;
//      _nonPersonalHoldings += holdings; //en person dør og pensionsdepotet overgår til pensionskassen
      //Implementer: Overførsel af opsparing til evt. ægtefælle
    }


    public void InstallmentFactor()
    {
      for (int r = 0; r < _installmentFactor.Length; r++)
      {
        if (PensionSystem.InterestRateForecasted(12) == 0)
          _installmentFactor[r] = r; //enhedsannuitet for given restløbetid. Bemærk r+1 fordi 0 betyder at der er en rate tilbage (den der er ved at blive udbetalt)
        else
          _installmentFactor[r] = (1 - Math.Pow(1 + PensionSystem.InterestRateForecasted(12), -(r + 1))) / PensionSystem.InterestRateForecasted(12); //enhedsannuitet for given restløbetid. Bemærk r+1 fordi 0 betyder at der er en rate tilbage (den der er ved at blive udbetalt)
      }
    }

  }
}