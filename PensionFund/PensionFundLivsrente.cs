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
    private int _holdings;
    /// <summary>
    /// Samlet opsparing fra personer, der er døde. Til fordeling blandt øvrige pensionskassemedlemmer
    /// </summary>
    private int _nonPersonalHoldings = 0;
    private double _adjustmentFactor;
    private static int _minPensionAge = 65;
    /// <summary>
    /// Teknisk pensionsalder....
    /// </summary>
    private static int _pensionAge = 65;
    private double[] _lifeSpan;
    private double[] _gammaB = new double[(MAXAGE - _minPensionAge) * 12];
    private double[] _alphaB = new double[_pensionAge * 12]; //defineret for alle mulige aldre frem til pension
    private double[] _alphaI = new double[_pensionAge * 12]; //defineret for alle mulige aldre frem til pension
    private double[] _p = new double[MAXAGE * 12];

    public PensionFundLivsrente(int initialHoldings = 0)
    {
      _holdings = initialHoldings;
    }

    public int Installment(int age, int personalHoldings)
    {
//      int installment = Convert.ToInt32(personalHoldings / _lifeSpan[age - _minPensionAge] / 12d); //bestem udbetalingens størrelse
      int installment = Convert.ToInt32(_gammaB[(age - _minPensionAge) * 12] * personalHoldings);
      _holdings -= installment; //pengene tages us af pensionskassensbeholdning
      return installment;
    }

    public int InstallmentExpected(int age, int personalHoldings, int contribution)
    {
      int installment = Convert.ToInt32(_alphaB[age * 12] * personalHoldings + _alphaI[age * 12] * contribution);
      return installment;
    }

    public void Contribution(int contribution)
    {
      _holdings += contribution; //opdater samlet pensionsbeholdning
    }

    public void YearStart()
    {
      ReadMortalityRates();
    }

    public void YearEnd()
    {
      if (_holdings + _nonPersonalHoldings == 0)
      {
        _adjustmentFactor = 0;
        return;
      }

      _adjustmentFactor = _holdings / (_holdings + _nonPersonalHoldings); //den faktor hvorved den samlede pensionsbeholdning skal justeres med

      _holdings = Convert.ToInt32(_holdings * _adjustmentFactor); //ikke-personrelaterbare pensionsbeholdninger overgår til samlet beholdning, på person-niveau sker det ved at overlevende personer får deres beholdning justeret med en faktor ved årsstart
      _nonPersonalHoldings = 0;
    }

    public void PersonExit(int holdings, int m)
    {
      _nonPersonalHoldings = _nonPersonalHoldings + Convert.ToInt32(holdings * Math.Pow(1 + PensionSystem.InterestRate(12), 12 - m)); //en person dør og pensionsdepotet overgår til pensionskassen

      //Implementer: Overførsel af opsparing til evt. ægtefælle
    }

    private void ReadMortalityRates()
    {
      double[] mortalityrates = new double[MAXAGE - _minPensionAge];

      try
      {
        using (StreamReader sr = new StreamReader(_mortalityRatesFile))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            string[] cols = line.Split('\t');
            int age = Convert.ToInt32(cols[1]);

            if (Convert.ToInt32(cols[2]) == Program.year && age >= _minPensionAge && age < MAXAGE) //hent kun dødsrater i det givne år for personer over 60
              mortalityrates[age - _minPensionAge] += Convert.ToDouble(cols[3]) / 2 / 12; //tag simpelt gennemsnit af raten for mænd og kvinder
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
          l[a] = Convert.ToDecimal((1 - m / 12 * mortalityrates[y])) * l[y*12];
      }

      for (int a = 0; a < MAXAGE * 12 - 1; a++)
        _p[a] = Convert.ToDouble(l[a+1] / l[a]);

      double v = 1 / (1 + PensionSystem.InterestRateForecasted(12));

      double[] L = new double[MAXAGE * 12];
      for (int a = 0; a < MAXAGE * 12; a++)
        L[a] = Convert.ToDouble(l[a]) * Math.Pow(v, a);


      for (int x = 0; x < _pensionAge * 12; x++)
      {
        double sum = 0;
        for (int y = _pensionAge * 12; y < ???; y++)
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


        _lifeSpan = new double[MAXAGE - _minPensionAge]; //forventet gennemsnitlig leveår tilbage, givet alder (beregnes for bestemt år)
      /*
      #region omregn fra dødsrater til restlevetid
      for (int age = _minPensionAge; age < MAXAGE; age++) //for hver mulig alderstrin som pensionist
      {
        double sumProbAlive = 0; //akkumuleret ssh for at være levende ved given alder
        double probAlive = 1; //ssh for at være i live ved alder = a
        for (int a = age; a < MAXAGE; a++) //fra start alder til maxalder
        {
          double sshDyingAtAge = mortalityrates[a - _minPensionAge];
          //renten skal ind i beregningen her....?
          probAlive *= (1 - sshDyingAtAge); //sandsynligheden for at personen lever på det pågældende alderstrin
          sumProbAlive += (probAlive * Math.Pow(1 + PensionSystem.InterestRateForecasted(), a));
        }

        _lifeSpan[age-_minPensionAge] = sumProbAlive;
      }
      #endregion omregn fra dødsrater til restlevetid
      */
    }

    public int UpdateHoldings(int holdings, int age, int ax, int m = 0)
    {
      if (m == 0)
        return (1 + bonus) * Dx;
      else
        return Convert.ToInt32(1 / _p[age * 12 + m] * ax);
    }

    public double AdjustmentFactor
    {
      get { return _adjustmentFactor; }
    }

  }
}