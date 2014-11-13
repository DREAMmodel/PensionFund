using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PensionFund
{
  class AuditorSimple
  {
    /// <summary>
    /// Personens samlede livrate-pensionsopsparing/formue, primo (Bx)
    /// </summary>
    int _livrenteDepotPrimoBx = 0;
    Boolean _activeLivrente = false;
    /// <summary>
    /// Personens samlede rate-pensionsopsparing/formue
    /// </summary>
    int _holdingsRate = 0;
    /// <summary>
    /// Livrentedepot ultimo måned
    /// </summary>
    int _livrenteDepotUltimoAx;
    /// <summary>
    /// Resterende månedlige rater ved ratepension
    /// </summary>
    private int _ratesLeft = -1;
    /// <summary>
    /// Rate-pensionsbeholdning stammende fra afdød ægtefælle
    /// </summary>
    private int _holdingsDeadSpouse = 0;
    /// <summary>
    /// Resterende rater af afdød ægtefælles ratepension
    /// </summary>
    private int _ratesLeftDeadSpouse = 0;
    private int w;
    private int _dx;

    private bool udvalgt = false;
    static int tc = 0;
    
    public AuditorSimple(int holdingsLivsrente = 0, int holdingsRate = 0)
    {
      _livrenteDepotPrimoBx = holdingsLivsrente;
      _holdingsRate = holdingsRate;

      if (tc == 0)
        udvalgt = true;
      tc++;
    }

    public int Update(int age, int[] contributionRate, int[] contributionLivsrente, int startRate = -1, int startLivrente = -1, int dead = -1)
    {
      if (udvalgt && age == 65)
        Console.WriteLine("65 år");

      int installment = 0; //årets samlede pensionsudbetaling
      int installmentLivrente = 0;
      for (int m = 0; m < 12; m++)
      {
        _livrenteDepotPrimoBx = PensionSystem.PensionfundLivrente.UpdateHoldings(age, _livrenteDepotUltimoAx, _dx, m); //beregn ny beholdning (Bx)
        if (m == 0)
          w = _livrenteDepotPrimoBx;

        if (udvalgt && m == 0 && age < 65)
        {
          int expected = PensionSystem.PensionfundLivrente.InstallmentExpected(age, m, _livrenteDepotPrimoBx, contributionLivsrente[m]);
//          Console.WriteLine("Forventet pension: " + expected);
        }

        #region ratepension
        if (startRate == m && _ratesLeft < 1)
          _ratesLeft = 12 * 10; //aktiver ratepensionsudbetaling sæt udbetaling til at vare 10 år

        ContributionRate(contributionRate[m]); //indbetal til ratepension
        installment += InstallmentRate(); //udbetal ratepension
        #endregion ratepension

        _activeLivrente |= startLivrente == m; //aktiver livrentepensionsudbetaling

        PensionSystem.PensionfundLivrente.Growth(w); //faktisk rente tilskrivning
        PensionSystem.PensionfundLivrente.Contribution(contributionLivsrente[m]); //orienter pensionsfund om indbetaling

        if (_activeLivrente /*&& installmentLivrente == 0*/ && _livrenteDepotPrimoBx > 0)
          installmentLivrente = PensionSystem.PensionfundLivrente.CalculateInstallment(age, m, _livrenteDepotPrimoBx); //beregn livsrentepension, hvis aktiv
        PensionSystem.PensionfundLivrente.Installment(installmentLivrente); //orienter pensionsfund om udbetaling

        w = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * w) + contributionLivsrente[m] - installmentLivrente;
        _livrenteDepotUltimoAx = Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * _livrenteDepotPrimoBx) + contributionLivsrente[m] - installmentLivrente;

        installment += installmentLivrente;
        if (udvalgt && m == 0 && installmentLivrente > 0)
          Console.WriteLine("Udbetaling, m=" + m + ", " + installmentLivrente + " Kr.");


        if (dead == m) //personen dør denne måned
        {
          PensionSystem.PensionfundLivrente.PersonExit(w, m);
          PensionSystem.PensionfundRate.PersonExit(_holdingsRate, m);
          return installment;
        }

        if (m == 11)
          _dx = PensionSystem.PensionfundLivrente.CalculateDx(age, m, _livrenteDepotUltimoAx); //beregnes efter Ax
      }

      return installment;
    }

    /*
    public void SpouseDies(int holdings, int dead, int ratesLeft = 12 * 10, int ownDead = 999)
    {
      _holdingsDeadSpouse += holdings; //beholdningen opdateres i tilfælde af at personen allerede har modtaget en pension fra afdød ægtefælle (dette må være sjældne tilfælde).
      _ratesLeftDeadSpouse = ratesLeft; //udbetales over 10 år med mindre udbetaling er påbegyndt, en unøjagtighed i tilfælde af pensioner fra flere afdøde ægtefæller

      int installment = 0; //årets samlede pensionsudbetaling
      for (int m = Math.Min(dead, 0); m < Math.Min(ownDead, 12); m++)
      {
      }

    }
    */

    private void ContributionRate(int contribution)
    {
      int growth = Convert.ToInt32(_holdingsRate * PensionSystem.InterestRateForecasted(12));
      _holdingsRate += growth + contribution; //opdater personlig formue med rente og indbetaling
      PensionSystem.PensionfundRate.Contribution(growth + contribution); //orienter pensionsfund om vækst i beholdning
    }

    private int InstallmentRate()
    {
      int installment = 0;
      if (_ratesLeft-- >= 0)
      {
        installment = PensionSystem.PensionfundRate.Installment(_ratesLeft, _holdingsRate);
        _holdingsRate -= installment; //nedjuster personlig (rate-)pensionsformue

        return installment; //total udbetaling
      }

      int installmentDeadSpouse = 0;
      if (_ratesLeftDeadSpouse-- >= 0)
      {
        installmentDeadSpouse = PensionSystem.PensionfundRate.Installment(_ratesLeftDeadSpouse, _holdingsDeadSpouse);
        _holdingsDeadSpouse -= installmentDeadSpouse; //nedjuster personlig (rate-)pensionsformue
      }

      return installment + installmentDeadSpouse;
    }
    /*
    private int InstallmentRateSpouse()
    {
      if (_ratesLeftDeadSpouse-- >= 0)
      {
        int installmentDeadSpouse = PensionSystem.PensionfundRate.Installment(_ratesLeftDeadSpouse, _holdingsDeadSpouse);
        _holdingsDeadSpouse -= installmentDeadSpouse; //nedjuster personlig (rate-)pensionsformue
        return installmentDeadSpouse;
      }
      else
        return 0;
    }
    */
    public void YearStart()
    {
    }

  }
}