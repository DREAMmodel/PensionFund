using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PensionFund
{
  class PensionFundLivsrente
  {
    const int MAXAGE = 118;
    private string _mortalityRatesFile = @"F:\Demographics.2014_BASERUN.R1.Mortality.csv";

    /// <summary>
    /// Pensionskassens samlede beholdning under optælling
    /// </summary>
    private ulong _holdingsW;
    private static int _minPensionAge = 65;
    /// <summary>
    /// Teknisk pensionsalder....
    /// </summary>
    private static int _pensionAge = 65;
    private double[] _gammaB = new double[(MAXAGE - _minPensionAge) * 12];
    private double[] _alphaB = new double[_pensionAge * 12]; //defineret for alle mulige aldre frem til pension
    private double[] _alphaI = new double[_pensionAge * 12]; //defineret for alle mulige aldre frem til pension
    public double[] _p = new double[MAXAGE * 12]; //TODO: Skal være private!
    private double _bonus = 0;
    private double sumDx = 0;

    public PensionFundLivsrente(ulong initialHoldings = 0)
    {
      _holdingsW = initialHoldings;
    }

    public int CalculateInstallment(int age, int m, int personalHoldings)
    {
//      int installment = Convert.ToInt32(personalHoldings / _lifeSpan[age - _minPensionAge] / 12d); //bestem udbetalingens størrelse
      int installment = Convert.ToInt32(_gammaB[(age - _minPensionAge) * 12 + m] * personalHoldings); //bestem udbetalingens størrelse
//      _holdingsW -= installment; //pengene tages us af pensionskassensbeholdning
      return installment;
    }

    public void Installment(int installment)
    {
      _holdingsW -= Convert.ToUInt32(installment); //pengene tages us af pensionskassensbeholdning
    }

    public int InstallmentExpected(int age, int m, int personalHoldings, int contribution)
    {
      int installment = Convert.ToInt32(_alphaB[age * 12 + m] * personalHoldings + _alphaI[age * 12 + m] * contribution);
      return installment;
    }

    public void Contribution(int contribution)
    {
      _holdingsW += Convert.ToUInt32(contribution); //opdater samlet pensionsbeholdning
    }

    public void Growth(int bx)
    {
      _holdingsW += Convert.ToUInt32(bx * PensionSystem.InterestRate(12)); //forøg pensionsdebot med rente for persons personlige beholdning
    }

    public int CalculateDx(int age, int m, int ax)
    {
      int dx = Convert.ToInt32(1 / _p[age * 12 + m] * ax);
      sumDx += dx;
      return dx;
    }

    public void YearStart()
    {
      ReadMortalityRates();
      CalculateBonus();
      sumDx = 0;
    }

    public void YearEnd()
    {
      Console.WriteLine("Beholdning livrentepension: " + _holdingsW + " Kr.");
    }

    public void PersonExit(int holdings, int m)
    {
      if (m < 11) //ingen yderligere rente, hvis vi allerede er i december
        _holdingsW += Convert.ToUInt32(holdings * Math.Pow((1 + PensionSystem.InterestRate(12)), 11 - m) - holdings); //en person dør og pensionsdepotet overgår til pensionskassen (med renter for resten af året)

      //double tmp = holdings * Math.Pow(1 + PensionSystem.InterestRate(12), 11 - m);
//      Auditor.sumW += Convert.ToInt32(tmp);
      //Implementer: Overførsel af opsparing til evt. ægtefælle
    }

    private void ReadMortalityRates()
    {
      double[] mortalityrates = new double[MAXAGE];

      try
      {
        using (StreamReader sr = new StreamReader(_mortalityRatesFile))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            string[] cols = line.Split('\t');
            int age = Convert.ToInt32(cols[1]);

            if (Convert.ToInt32(cols[2]) == Program.year && age < MAXAGE) //hent kun dødsrater i det givne år for personer over 60
              mortalityrates[age] += Convert.ToDouble(cols[3]) / 2; //tag simpelt gennemsnit af raten for mænd og kvinder
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("The file could not be read:");
        Console.WriteLine(e.Message);
        throw new Exception();
      }

      decimal[] l = new decimal[MAXAGE * 12];
      l[0] = 1;
      for (int a = 1; a < MAXAGE * 12; a++)
      {
        int y = a / 12;
        int m = a % 12;
        if (m == 0) //januar måned
          l[a] = Convert.ToDecimal((1 - mortalityrates[y - 1])) * l[a-12];
        else
          l[a] = Convert.ToDecimal((1 - m / 12d * mortalityrates[y])) * l[y*12];
      }

      for (int a = 0; a < MAXAGE * 12 - 1; a++)
        _p[a] = 0.99;// Convert.ToDouble(l[a + 1] / l[a]);

      double v = 1 / (1 + PensionSystem.InterestRateForecasted(12));

      double[] L = new double[MAXAGE * 12];
      for (int a = 0; a < MAXAGE * 12; a++)
        L[a] = Convert.ToDouble(l[a]) * Math.Pow(v, a);


      for (int x = 0; x < _pensionAge * 12; x++)
      {
        double sum = 0;
        for (int y = _pensionAge * 12; y < MAXAGE * 12; y++)
          sum += L[y] / L[x];

        _alphaB[x] = Math.Pow(v * sum, -1);
      }

      for (int x = 0; x < _pensionAge * 12; x++)
      {
        double sum = 0;
        for (int y = x; y < _pensionAge * 12 - 1; y++)
          sum += L[y] / L[x];

        _alphaI[x] = v * sum * _alphaB[x];
      }
      
      for (int x = _minPensionAge * 12; x < MAXAGE * 12; x++)
      {
        double sum = 0;
        for (int y = x; y < MAXAGE * 12 - 1; y++)
          sum += L[y] / L[x];

        _gammaB[x - _minPensionAge * 12] = Math.Pow(v * sum, -1);
      }
    }

    double max = 0;
    double min = 0;
    private void CalculateBonus()
    {
      _bonus = sumDx == 0 ? 0 : _holdingsW / sumDx - 1;
      if (_bonus > max)
        max = _bonus;
      if (_bonus < min)
        min = _bonus;
      Console.WriteLine(_bonus);

    }

    public int UpdateHoldings(int age, int ax, int dx = 0, int m = 0)
    {
      if (m == 0)
        return dx == 0 ? ax : Convert.ToInt32((1 + _bonus) * dx);
      else
        return Convert.ToInt32(1 / _p[age * 12 + m - 1] * ax);
    }

  }
}