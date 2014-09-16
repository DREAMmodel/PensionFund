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
    /// Personens samlede livsrate-pensionsopsparing/formue
    /// </summary>
    int _holdingsLivsrente;
    Boolean _activeLivsrente = false;
    /// <summary>
    /// Personens samlede rate-pensionsopsparing/formue
    /// </summary>
    int _holdingsRate;
    /// <summary>
    /// Reference til pensionskasse
    /// </summary>
    static PensionFundLivsrente _pensionfundLivsrente;
    /// <summary>
    /// Reference til pensionskasse
    /// </summary>
    static PensionFundRate _pensionfundRate;
    /// <summary>
    /// Resterende månedlige rater ved ratepension
    /// </summary>
    private int _ratesLeft = 0;
    /// <summary>
    /// Beregnet størrelsen af rate
    /// </summary>
    private int _rateSize;
    /// <summary>
    /// Rate-pensionsbeholdning stammende fra afdød ægtefælle
    /// </summary>
    private int _holdingsDeadSpouse = 0;
    /// <summary>
    /// Resterende rater af afdød ægtefælles ratepension
    /// </summary>
    private int _ratesLeftDeadSpouse = 0;

    public Auditor(PensionFundLivsrente p1, PensionFundRate p2, int holdingsLivsrente = 0, int holdingsRate = 0)
    {
      _pensionfundLivsrente = p1;
      _pensionfundRate = p2;
      _holdingsLivsrente = holdingsLivsrente;
      _holdingsRate = holdingsRate;
    }

    public int Update(int age, int[] contributionRate, int[] contributionLivsrente, int startRate = -1, int startLivrente = -1)
    {
      int installment = 0; //årets samlede pensionsudbetaling
      for (int m = 0; m < 12; m++)
      {
        if (startRate == m)
          _ratesLeft = 12 * 10; //aktiver ratepensionsudbetaling sæt udbetaling til at vare 10 år
        if (startLivrente == m)
          _activeLivsrente = true; //aktiver livrentepensionsudbetaling

        ContributionRate(contributionRate[m]); //indbetal til ratepension
        installment += InstallmentRate(); //udbetal ratepension

        ContributionLivrente(contributionLivsrente[m]); //indbetal til livsrentepension            

        if (_activeLivsrente)
          installment += InstallmentLivsrente(age); //udbetal livsrentepension
      }

      return installment;
    }

    public void SpouseDies(int holdings, int ratesLeft = 12 * 10)
    {
      _holdingsDeadSpouse += holdings; //beholdningen opdateres i tilfælde af at personen allerede har modtaget en pension fra afdød ægtefælle (dette må være sjældne tilfælde).
      _ratesLeftDeadSpouse = ratesLeft; //udbetales over 10 år med mindre udbetaling er påbegyndt, en unøjagtighed i tilfælde af pensioner fra flere afdøde ægtefæller
    }

    private void ContributionRate(int contribution)
    {
      double tmp = PensionSystem.InterestRateForecasted(12);
      int growth = Convert.ToInt32(_holdingsRate * PensionSystem.InterestRateForecasted(12));
      _holdingsRate += growth + contribution; //opdater personlig formue med rente og indbetaling
      _pensionfundRate.Contribution(growth + contribution); //orienter pensionsfund om vækst i beholdning
    }

    private void ContributionLivrente(int contribution)
    {
      int growth = Convert.ToInt32(_holdingsLivsrente * PensionSystem.InterestRateForecasted(12));
      _holdingsLivsrente += growth + contribution; //opdater personlig formue med rente og indbetaling
      _pensionfundLivsrente.Contribution(growth + contribution); //orienter pensionsfund om indbetaling      
    }

    private int InstallmentLivsrente(int age)
    {
      if (_holdingsLivsrente > 0)
      {
        int installment = _pensionfundLivsrente.Installment(age, _holdingsLivsrente); //beregn udbetaling v. givet alderstrin
        _holdingsLivsrente -= installment; //fratræk udbetaling fra beholdning
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
        installment = _pensionfundRate.Installment(_ratesLeft, _holdingsRate);
        _holdingsRate -= installment; //nedjuster personlig (rate-)pensionsformue

        return installment; //total udbetaling
      }

      int installmentDeadSpouse = 0;
      if (_ratesLeftDeadSpouse-- >= 0)
      {
        installmentDeadSpouse = _pensionfundRate.Installment(_ratesLeftDeadSpouse, _holdingsDeadSpouse);
        _holdingsDeadSpouse -= installmentDeadSpouse; //nedjuster personlig (rate-)pensionsformue
      }

      return installment + installmentDeadSpouse;
    }

    public void YearStart()
    {
      if (_holdingsLivsrente > 0)
        _holdingsLivsrente = Convert.ToInt32(_holdingsLivsrente * _pensionfundLivsrente.AdjustmentFactor); //opdater personlig pensionsbeholdning med faktor givet af pensionskassen (tager højde for uvikling i pensionskassens samlede beholdning)

      Console.WriteLine("Beholdning (rate): " + _holdingsRate + " kr.");
      Console.WriteLine("Beholdning (livrente): " + _holdingsLivsrente + " kr.");
    }

  }
}