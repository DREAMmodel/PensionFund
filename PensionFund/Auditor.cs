using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PensionFund
{
  class Auditor
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


    public Auditor(int holdingsLivsrente = 0, int holdingsRate = 0)
    {
      _livrenteDepotPrimoBx = holdingsLivsrente;
      _holdingsRate = holdingsRate;
    }

    public int Update(int age, int[] contributionRate, int[] contributionLivsrente, int startRate = -1, int startLivrente = -1, int dead = -1)
    {
      int installment = 0; //årets samlede pensionsudbetaling
      for (int m = 0; m < 12; m++)
      {
        _livrenteDepotPrimoBx = PensionSystem.PensionfundLivrente.UpdateHoldings(age, _livrenteDepotUltimoAx, m); //beregn ny beholdning (Bx)
        if (m == 0)
          w = _livrenteDepotPrimoBx;
        
        if (dead == m) //personen dør denne måned
        {
          PensionSystem.PensionfundLivrente.PersonExit(_livrenteDepotPrimoBx, m);
          PensionSystem.PensionfundRate.PersonExit(_holdingsRate, m);
          return installment;
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
        int installmentLivrente = _activeLivrente && _livrenteDepotPrimoBx > 0 ? PensionSystem.PensionfundLivrente.Installment(age, _livrenteDepotPrimoBx) : 0; //udbetal livsrentepension, hvis aktiv

        _livrenteDepotUltimoAx = Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * _livrenteDepotPrimoBx + contributionLivsrente[m] - installmentLivrente);

        w = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * w + contributionLivsrente[m] - installmentLivrente);

        installment += installmentLivrente;

        if (m == 12)
        {
          1 / _p[x] * _livrenteDepotUltimoAx; //beregn Dx
        }
      }

      return installment;
    }

    public void SpouseDies(int holdings, int dead, int ratesLeft = 12 * 10, int ownDead = 999)
    {
      _holdingsDeadSpouse += holdings; //beholdningen opdateres i tilfælde af at personen allerede har modtaget en pension fra afdød ægtefælle (dette må være sjældne tilfælde).
      _ratesLeftDeadSpouse = ratesLeft; //udbetales over 10 år med mindre udbetaling er påbegyndt, en unøjagtighed i tilfælde af pensioner fra flere afdøde ægtefæller

      int installment = 0; //årets samlede pensionsudbetaling
      for (int m = Math.Min(dead, 0); m < Math.Min(ownDead, 12); m++)
      {
      }

    }

    private void ContributionRate(int contribution)
    {
      double tmp = PensionSystem.InterestRateForecasted(12);
      int growth = Convert.ToInt32(_holdingsRate * PensionSystem.InterestRateForecasted(12));
      _holdingsRate += growth + contribution; //opdater personlig formue med rente og indbetaling
      PensionSystem.PensionfundRate.Contribution(growth + contribution); //orienter pensionsfund om vækst i beholdning
    }

    private void ContributionLivrente(int contribution)
    {
//      int growth = Convert.ToInt32(_livrenteDepotPrimoBx * PensionSystem.InterestRateForecasted(12));
      //_livrenteDepotPrimoBx += contribution; //opdater personlig formue med forventet rentetilskrivning og indbetaling
      PensionSystem.PensionfundLivrente.Contribution(contribution); //orienter pensionsfund om indbetaling      
    }

    private int InstallmentLivsrente(int age)
    {
      if (_livrenteDepotPrimoBx > 0)
      {
        int installment = PensionSystem.PensionfundLivrente.Installment(age, _livrenteDepotPrimoBx); //beregn udbetaling v. givet alderstrin
//        _livrenteDepotPrimoBx -= installment; //fratræk udbetaling fra beholdning
        return installment;
      }
      else
        return 0;
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

    public void YearStart()
    {
      if (_livrenteDepotPrimoBx > 0)
        _livrenteDepotPrimoBx = Convert.ToInt32(_livrenteDepotPrimoBx * PensionSystem.PensionfundLivrente.AdjustmentFactor); //opdater personlig pensionsbeholdning med faktor givet af pensionskassen (tager højde for uvikling i pensionskassens samlede beholdning)

      Console.WriteLine("Beholdning (rate): " + _holdingsRate + " kr.");
      Console.WriteLine("Beholdning (livrente): " + _livrenteDepotPrimoBx + " kr.");
    }

  }
}