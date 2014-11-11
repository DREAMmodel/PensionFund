using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PensionFund
{
  class PensionFundInvalide
  {
    const int MAXAGE = 118;
    private string _mortalityRatesFile = @"F:\Demographics.2014_BASERUN.R1.Mortality.csv";

    /// <summary>
    /// Pensionskassens samlede beholdning under optælling
    /// </summary>
    private ulong _holdingsW;
    private double[] _gammaB = new double[(MAXAGE - PensionSystem._minPensionAge) * 12];
    private double[] _alphaB = new double[MAXAGE * 12]; //defineret for alle mulige aldre frem til pension
    private double[] _alphaI = new double[MAXAGE * 12]; //defineret for alle mulige aldre frem til pension
    /// <summary>
    /// Sandsynlighed for at en person med given alder (målt i måneder) dør inden næste alder
    /// </summary>
    public double[] _p = new double[MAXAGE * 12]; //TODO: Skal være private!
    /// <summary>
    /// Sandsynlighed for at en person med given alder (målt i måneder) bliver invalid inden næste alder
    /// </summary>
    public double[] _qd = new double[MAXAGE * 12]; //TODO: Skal være private!
    double[] _beta = new double[MAXAGE * 12];
    double[] _theta = new double[PensionSystem._minPensionAge * 12];
    double[] _eta = new double[PensionSystem._minPensionAge * 12];
    private double[] _gamma = new double[(MAXAGE - PensionSystem._minPensionAge) * 12];

    private double _zeta = 00;

    private double _bonus = 1;
    private double _sumDx = 0;

    public PensionFundInvalide(ulong initialHoldings = 0)
    {
      _holdingsW = initialHoldings;

      //initialiser invalide sandsynligheder
      for (int i = 0; i < _qd.Length; i++)
        _qd[i] = 0.001; //ssh for invaliditet
    }

    public void InitialAccount(int holdings)
    {
      _holdingsW += Convert.ToUInt32(holdings);
    }

    public int CalculateDdf(int age, int m, int adf, int expected, Boolean first = false)
    {
      if (first) //personen er netop blevet invalid
        return Convert.ToInt32(_theta[age * 12 + m] * _zeta * expected);
      else
        return Convert.ToInt32(1 / _p[age * 12 + m] * adf);
    }

    public int CalculateDx(int age, int m, int ax, bool pensionist)
    {
      double tmp = 1/_p[age * 12 + m];
      return pensionist ? Convert.ToInt32(1 / _p[age * 12 + m] * ax) : ax;
    }

    public int CalculateDdi(int age, int m, int latestContribution, int adi, Boolean first = false)
    {
      if (first) //personen er netop blevet invalid
        return Convert.ToInt32(_theta[age * 12 + m] * latestContribution);
      else
        return Convert.ToInt32(1 / _p[age * 12 + m] * adi);
    }

    public int CalculateBdi(int m, int ddi)
    {
      return m == 0 ? ddi : Convert.ToInt32(_bonus * ddi);
    }

    public int CalculateBdf(int m, int ddf)
    {
      return m == 0 ? ddf : Convert.ToInt32(_bonus * ddf);
    }

    public int CalculateAdf(int bdf, int expected)
    {
      return Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * bdf - expected);
    }

    public int CalculateAdi(int bdi, int expected)
    {
      return Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * bdi - expected);
    }

    public int CalculateFd(int age, int m, int bdf)
    {
      return Convert.ToInt32(_eta[age * 12 + m] * bdf);
    }

    public int CalculateFdi(int age, int m, int bdi)
    {
      return Convert.ToInt32(_eta[age * 12 + m] * bdi);
    }

    public int CalculateInstallment(int age, int m, int personalHoldings)
    {
      int installment = Convert.ToInt32(_gammaB[(age - PensionSystem._minPensionAge) * 12 + m] * personalHoldings); //bestem udbetalingens størrelse
      return installment;
    }

    public void Installment(int installment)
    {
      _holdingsW -= Convert.ToUInt32(installment); //pengene tages us af pensionskassensbeholdning
    }

    public double CalculatePidf(int age, int m, int bx, int contribution)
    {
      return _beta[age * 12 + m] * _zeta * InstallmentExpected(age, m, bx, contribution);
    }

    public double CalculatePidi(int age, int m, int contribution)
    {
      return _beta[age * 12 + m] * contribution;
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
      _holdingsW = Convert.ToUInt64(_holdingsW + bx * PensionSystem.InterestRate(12)); //forøg pensionsdebot med rente for persons personlige beholdning
    }

    public void RegisterDx(int ax)
    {
      _sumDx += ax;
    }

    public void YearStart()
    {
      ReadMortalityRates();
      CalculateBonus();
      _sumDx = 0;
    }

    public void YearEnd()
    {
      Console.WriteLine("Samlet beholdning invalidepension: " + _holdingsW + " Kr.");
    }

    public void PersonExit(int age, int m, int holdings)
    {
      if (age * 12 < PensionSystem._minPensionAge * 12)
        _holdingsW -= Convert.ToUInt32((1 + PensionSystem.InterestRate(12)) * holdings); //Pensionen udbetales til familie
      else if (m < 11) //ingen yderligere rente, hvis vi allerede er i december
        _holdingsW += Convert.ToUInt32(holdings * Math.Pow((1 + PensionSystem.InterestRate(12)), 11 - m) - holdings); //en person dør og pensionsdepotet overgår til pensionskassen (med renter for resten af året)

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

//            if (Convert.ToInt32(cols[2]) == Program.year && age < MAXAGE) //hent kun dødsrater i det givne år
            if (Convert.ToInt32(cols[2]) == 2010 && age < MAXAGE) //hent kun dødsrater i det givne år for personer over 60
              mortalityrates[age] += Convert.ToDouble(cols[3]) / 2d; //tag simpelt gennemsnit af raten for mænd og kvinder
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
          l[a] = Convert.ToDecimal((1 - mortalityrates[y - 1])) * l[a - 12];
        else
          l[a] = Convert.ToDecimal((1 - m / 12d * mortalityrates[y])) * l[y * 12];
      }

      for (int a = 0; a < MAXAGE * 12 - 1; a++)
        _p[a] = Convert.ToDouble(l[a + 1] / l[a]);

      double v = 1 / (1 + PensionSystem.InterestRateForecasted(12));

      double[] L = new double[MAXAGE * 12];
      for (int a = 0; a < MAXAGE * 12; a++)
        L[a] = Convert.ToDouble(l[a] * Convert.ToDecimal(Math.Pow(v, a)));


      for (int x = PensionSystem._minPensionAge * 12; x < MAXAGE * 12; x++)
      {
        double sum = 0;
        for (int y = x; y < MAXAGE * 12; y++)
          sum += L[y] / L[x];

        _gammaB[x - PensionSystem._minPensionAge * 12] = Math.Pow(v * sum, -1);
      }

      #region beregn _beta
      for (int x = 0; x < PensionSystem._minPensionAge * 12 - 1; x++)
      {
        double sum = 0;
        for (int y = x + 1; y <= PensionSystem._minPensionAge * 12 - 1; y++)
          sum += L[y] / L[x];

        _beta[x] = _qd[x] * sum;
      }
      for (int x = PensionSystem._minPensionAge * 12 - 1; x < MAXAGE * 12; x++)
        _beta[x] = 0; //sættes til nul i perioden umiddelbart før pensionsalder og fremefter
      #endregion beregn _beta

      #region beregn _alphaB
      double zeta = 0.0;
      for (int x = 0; x <= PensionSystem._pensionAge * 12 - 2; x++)
      {
        double sum1 = 0;
        for (int y = x; y <= PensionSystem._pensionAge * 12 - 2; y++)
          sum1 += Math.Pow(v, y - x + 1) * _beta[y];

        double sum2 = 0;
        for (int y = PensionSystem._pensionAge * 12; y < MAXAGE * 12; y++)
          sum2 += L[y] / L[PensionSystem._pensionAge * 12];

        sum2 *= Math.Pow(v, PensionSystem._minPensionAge * 12 - x + 1);

        _alphaB[x] = Math.Pow(zeta * sum1 + sum2, -1);
      }

      {
        double sum = 0;
        for (int y = PensionSystem._minPensionAge * 12; y < MAXAGE * 12; y++)
          sum += L[y] / L[PensionSystem._minPensionAge * 12];

        _alphaB[PensionSystem._minPensionAge * 12 - 1] = Math.Pow(v * v * sum, -1);
      }

      for (int x = PensionSystem._minPensionAge * 12; x < MAXAGE * 12; x++)
        _alphaB[x] = 1;
      #endregion beregn _alphaB

      #region beregn _alphaI
      for (int x = 0; x <= PensionSystem._pensionAge * 12 - 2; x++)
      {
        double sum1 = 0;
        for (int y = x; y <= PensionSystem._pensionAge * 12 - 1; y++)
          sum1 += Math.Pow(v, y - x + 1);

        double sum2 = 0;
        for (int y = x; y <= PensionSystem._pensionAge * 12 - 2; y++)
          sum2 += Math.Pow(v, y - x + 1) * _beta[y];

        _alphaI[x] = _alphaB[x] * (sum1 - sum2);
      }

      _alphaI[PensionSystem._minPensionAge * 12 - 1] = _alphaB[PensionSystem._minPensionAge * 12 - 1] * v;

      for (int x = PensionSystem._minPensionAge * 12; x < MAXAGE * 12; x++)
        _alphaI[x] = 1;
      #endregion beregn _alphaI

      #region beregn _theta og _eta
      for (int x = 0; x < PensionSystem._minPensionAge * 12; x++)
      {
        double sum = 0;
        for (int y = x; y < PensionSystem._minPensionAge * 12; y++)
          sum += L[y] / L[x];
        _theta[x] = v * sum;

        _eta[x] = Math.Pow(_theta[x], -1);
      }
      #endregion beregn _theta og _eta

      #region beregn _gamma
      for (int x = PensionSystem._minPensionAge * 12; x < MAXAGE * 12; x++)
      {
        double sum = 0;
        for (int y = x; y < MAXAGE * 12; y++)
          sum += L[y] / L[x];

        _gamma[x - PensionSystem._minPensionAge * 12] = Math.Pow(v * sum, -1);
      }
      #endregion beregn _gamma
    }

    private void CalculateBonus()
    {
      _bonus = _sumDx == 0 ? 1 : _holdingsW / _sumDx;
      Console.WriteLine("Invalidepension, bonus: " + _bonus);
    }

    public int UpdateHoldings(int dx, int m)
    {
      return m == 0 ? Convert.ToInt32(_bonus * dx) : dx;
    }

  }
}