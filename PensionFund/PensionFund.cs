using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PensionFund
{
  class PensionFund
  {
    const int MAXAGE = 130;
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
    private int _minPensionAge = 65;

    private double[] _lifeSpan;

    public PensionFund(int initialHoldings = 0)
    {

      _holdings = initialHoldings;
    }

    public int Installment(int age, int personalHoldings)
    {
      int installment = Convert.ToInt32(personalHoldings / _lifeSpan[age - _minPensionAge]); //bestem udbetalingens størrelse

      _holdings -= installment; //pengene tages us af pensionskassensbeholdning
      return installment;
    }

    public void Contribution(int contribution)
    {
      _holdings += contribution;
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
      _adjustmentFactor *= (1 + Program.r); //opjuster også pensionsformue med rente

      _holdings = Convert.ToInt32(_holdings * _adjustmentFactor); //ikke-personrelaterbare pensionsbeholdninger overgår til samlet beholdning, på person-niveau sker det ved at overlevende personer får deres beholdning justeret med en faktor ved årsstart
      _nonPersonalHoldings = 0;
    }

    public void PersonExit(int holdings)
    {
      _nonPersonalHoldings += holdings; //en person dør og pensionsdepotet overgår til pensionskassen

      //Implementer: Overførsel af opsparing til evt. ægtefælle
    }


    private void ReadMortalityRates()
    {
      double[] mortalityrates;
      mortalityrates = new double[MAXAGE - _minPensionAge];

      try
      {
        using (StreamReader sr = new StreamReader(_mortalityRatesFile))
        {
          string line;
          while ((line = sr.ReadLine()) != null)
          {
            string[] cols = line.Split('\t');
            int age = Convert.ToInt32(cols[1]);

            if (Convert.ToInt32(cols[2]) == Program.year && age >= _minPensionAge) //hent kun dødsrater i det givne år for personer over 60
              mortalityrates[age-_minPensionAge] += Convert.ToDouble(cols[3]) / 2; //tag simpelt gennemsnit af raten for mænd og kvinder
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("The file could not be read:");
        Console.WriteLine(e.Message);
        throw new Exception();
      }

      _lifeSpan = new double[MAXAGE - _minPensionAge]; //forventet gennemsnitlig leveår tilbage, givet alder (beregnes for bestemt år)

      #region omregn fra dødsrater til restlevetid
      for (int age = _minPensionAge; age < MAXAGE; age++) //for hver mulig alderstrin som pensionist
      {       
        double sumProbAlive = 0; //akkumuleret ssh for at være levende ved given alder
        double probAlive = 1; //ssh for at være i live ved alder = a
        for (int a = age; a < MAXAGE; a++) //fra start alder til maxalder
        {
          double sshDyingAtAge = mortalityrates[a-_minPensionAge];
          probAlive *= (1 - sshDyingAtAge); //sandsynligheden for at personen lever på det pågældende alderstrin
          sumProbAlive += probAlive;
        }

        _lifeSpan[age-_minPensionAge] = sumProbAlive;
      }
      #endregion omregn fra dødsrater til restlevetid
    }

    public double AdjustmentFactor
    {
      get { return _adjustmentFactor; }
    }

  }
}
