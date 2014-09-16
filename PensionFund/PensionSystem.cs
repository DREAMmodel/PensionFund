using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PensionFund
{
  enum PensionFunds { Livsrente, Rate };

  class PensionSystem
  {
    /// <summary>
    /// Estimeret rente, dvs. den rente pensionskasserne regner med
    /// </summary>
    private static double _rForecasted = Program.r;
    /// <summary>
    /// Faktisk rente i simuleringen
    /// </summary>
    private static double _r = Program.r;

    /// <summary>
    /// Estimeret rente, dvs. den rente pensionskasserne regner med, opdelt på m årlige terminer
    /// </summary>
    /// <param name="m">Antal årlige terminer</param>
    /// <returns></returns>
    public static double InterestRateForecasted(int m = 1)
    {
      return Math.Pow(1 + _rForecasted, 1 / Convert.ToDouble(m)) - 1; //rente ved m årlige terminer
    }

    /// <summary>
    /// Faktisk/observeret rente, dvs. den rente der viser sig i simuleringen, opdelt på m årlige terminer
    /// </summary>
    /// <param name="m">Antal årlige terminer</param>
    /// <returns></returns>
    public static double InterestRate(int m = 1)
    {
      return Math.Pow(1 + _r, 1 / m) - 1; //rente ved m årlige terminer
    }
  
  }
}
