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
    int _invalideDepotPrimoBx = 0;
    Boolean _activeLivrente = false;
    Boolean _activeInvalideAlders = false;
    Boolean _invalid = false;
    /// <summary>
    /// Personens samlede rate-pensionsopsparing/formue
    /// </summary>
    int _holdingsRate = 0;
    int _holdingsInvalide = 0;
    /// <summary>
    /// Livrentedepot ultimo måned
    /// </summary>
    int _livrenteDepotUltimoAx;
    int _invalideDepotUltimoAx;
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
    private int wi;
    private int _dx;
    private int _dxInvalide;

    private int _ddf = 0;
    private int _wdf = 0;
    private int _bdf = 0;
    private int _adf = 0;
    private int _expectedPension;

    private int _latestContribution = 0;
    private int _ddi = 0;
    private int _wdi = 0;
    private int _bdi = 0;
    private int _adi = 0;

    private int _timeSinceLastInvalideContribution;

    private bool udvalgt = false;
    static int tc = 0;

    public Auditor(int holdingsLivrente = 0, int holdingsRate = 0, int holdingsInvalide = 0, int lastInvalideContribution = 999, bool activeInvalideAlders = false, bool activeLivrente = false, int ratesLeft = 0)
    {
      _livrenteDepotPrimoBx = holdingsLivrente;
      _dx = _livrenteDepotPrimoBx;
      PensionSystem.PensionfundLivrente.InitialAccount(_livrenteDepotPrimoBx); //orienter pensionskassen om pensionsbeholdning
      _holdingsRate = holdingsRate;
      PensionSystem.PensionfundRate.InitialAccount(_holdingsRate); //orienter pensionskassen om pensionsbeholdning
      _invalideDepotPrimoBx = holdingsInvalide;
      _dxInvalide = holdingsInvalide;
      PensionSystem.PensionfundInvalide.InitialAccount(_invalideDepotPrimoBx); //orienter pensionskassen om pensionsbeholdning
      _timeSinceLastInvalideContribution = lastInvalideContribution;
      _activeInvalideAlders = activeInvalideAlders;
      _activeLivrente = activeLivrente;
      _ratesLeft = ratesLeft;

      if (tc == 0)
        udvalgt = true;
      tc++;
    }

    public int Update(int age, int[] contributionRate, int[] contributionLivsrente, int[] contributionInvalide, int startRate = -1, int startLivrente = -1, int startInvalideAldersPension = -1, int invalid = -1, int dead = -1)
    {
      if (udvalgt && age == 65)
        Console.WriteLine("65 år");

      int installment = 0; //årets samlede pensionsudbetaling

      #region ratepension
      for (int m = 0; m < 12; m++)
      {
        if (startRate == m && _ratesLeft < 1)
          _ratesLeft = 12 * 10; //aktiver ratepensionsudbetaling sæt udbetaling til at vare 10 år

        ContributionRate(contributionRate[m]); //indbetal til ratepension
        installment += InstallmentRate(); //udbetal ratepension

        if (udvalgt && m == 0 && installment > 0)
          Console.WriteLine("Ratepension, udbetalt (m=" + m + "): " + installment);

        if (dead == m) //personen dør denne måned
        {
          PensionSystem.PensionfundRate.PersonExit(_holdingsRate, m);
          break;
        }

//        _dx = _livrenteDepotUltimoAx;
      }
      #endregion ratepension

      #region simpel livrente
      int installmentLivrente = 0;
      for (int m = 0; m < 12; m++)
      {
        _livrenteDepotPrimoBx = PensionSystem.PensionfundLivrente.UpdateHoldings(age, _livrenteDepotUltimoAx, _dx, m); //beregn ny beholdning (Bx)      
        if (m == 0)
          w = _livrenteDepotPrimoBx;

        if (udvalgt && m == 0 && age < 65)
        {
          int expected = PensionSystem.PensionfundLivrente.InstallmentExpected(age, m, _livrenteDepotPrimoBx, contributionLivsrente[m]);
          Console.WriteLine("Forventet pension: " + expected);
        }

        _activeLivrente |= startLivrente == m; //aktiver livrentepensionsudbetaling

        PensionSystem.PensionfundLivrente.Growth(w); //faktisk rente tilskrivning
        PensionSystem.PensionfundLivrente.Contribution(contributionLivsrente[m]); //orienter pensionsfund om indbetaling

        if (_activeLivrente /*&& installmentLivrente == 0*/ && _livrenteDepotPrimoBx > 0)
          installmentLivrente = PensionSystem.PensionfundLivrente.CalculateInstallment(age, m, _livrenteDepotPrimoBx); //beregn livsrentepension, hvis aktiv
        PensionSystem.PensionfundLivrente.Installment(installmentLivrente); //orienter pensionsfund om udbetaling

        w = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * w) + contributionLivsrente[m] - installmentLivrente;
        _livrenteDepotUltimoAx = Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * _livrenteDepotPrimoBx) + contributionLivsrente[m] - installmentLivrente;

        installment += installmentLivrente;
        if (udvalgt && installmentLivrente > 0)
          Console.WriteLine("Udbetaling livrente, m=" + m + ", " + installmentLivrente + " Kr.");

        if (dead == m) //personen dør denne måned
        {
          PensionSystem.PensionfundLivrente.PersonExit(w, m);
          break;
        }

        if (m == 11)
          _dx = PensionSystem.PensionfundLivrente.CalculateDx(age, m, _livrenteDepotUltimoAx); //beregnes efter Ax
      }
      #endregion simpel livrente
      
      #region simpel livrente med invalidepension
      int installmentInvalide = 0;
      for (int m = 0; m < 12; m++)
      {
        
        #region selve invalidepensionen
        _invalid |= invalid == m; //aktiver alderspension

        if (_invalid)
        {
          _ddf = PensionSystem.PensionfundInvalide.CalculateDdf(age, m, _adf, _expectedPension, invalid == m); //indvalidebeholdning primo før bonus
          _bdf = PensionSystem.PensionfundInvalide.CalculateBdf(m, _ddf); //indvalidebeholdning, justeret m. bonus
          int fd = PensionSystem.PensionfundInvalide.CalculateFd(age, m, _bdf); //udbetaling
          _adf = PensionSystem.PensionfundInvalide.CalculateAdf(_bdf, fd); //indvalidebeholdning ultimo

          if (m == 0)
            _wdf = _bdf;
          PensionSystem.PensionfundInvalide.Growth(_wdf); //rentetilskrivning
          PensionSystem.PensionfundInvalide.Installment(fd); //orienter pensionsfund om udbetaling
          _wdf = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * _wdf - fd);
        }
        #endregion selve invalidepensionen

        #region opsparingssikring
        int fdi = 0;
        if (_invalid)
        {
          _ddi = PensionSystem.PensionfundInvalide.CalculateDdi(age, m, _latestContribution, _adi, invalid == m); //indvalidebeholdning primo før bonus       
          _bdi = PensionSystem.PensionfundInvalide.CalculateBdi(m, _ddi); //indvalidebeholdning, justeret m. bonus
          fdi = PensionSystem.PensionfundInvalide.CalculateFdi(age, m, _bdi); //udbetaling
          _adi = PensionSystem.PensionfundInvalide.CalculateAdi(_bdi, fdi); //indvalidebeholdning ultimo

          if (m == 0)
            _wdi = _bdi;
          PensionSystem.PensionfundInvalide.Growth(_wdi); //rentetilskrivning
          PensionSystem.PensionfundInvalide.Installment(fdi); //orienter pensionsfund om "udbetaling"
          _wdi = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * _wdi - fdi);
        }
        #endregion opsparingssikring

        #region alderspension og pensionsindbetaling
        _activeInvalideAlders |= startInvalideAldersPension == m; //aktiver alderspension
        _invalideDepotPrimoBx = PensionSystem.PensionfundInvalide.UpdateHoldings(_dxInvalide, m); //beregn ny beholdning (Bx)
        
        int expected = 0;
        if (age < 65)
         expected = PensionSystem.PensionfundInvalide.InstallmentExpected(age, m, _invalideDepotPrimoBx, contributionInvalide[m]);
        if (udvalgt && m == 0 && age < 65)
          Console.WriteLine("Forventet invalide pension: " + expected);

        if (m == 0)
          wi = _invalideDepotPrimoBx;
        PensionSystem.PensionfundInvalide.Growth(wi); //faktisk rente tilskrivning

        if (_invalid)
          PensionSystem.PensionfundInvalide.Contribution(fdi); //orienter pensionsfund om indbetaling
        else
          PensionSystem.PensionfundInvalide.Contribution(contributionInvalide[m]); //orienter pensionsfund om indbetaling

        if (contributionInvalide[m] > 0)
          _latestContribution = contributionInvalide[m]; //anvendes til bestemmelse af opsparingssikring ved invaliditet

        if (_activeInvalideAlders && _invalideDepotPrimoBx > 0)
          installmentInvalide = PensionSystem.PensionfundInvalide.CalculateInstallment(age, m, _invalideDepotPrimoBx); //beregn livsrentepension, hvis aktiv
        PensionSystem.PensionfundInvalide.Installment(installmentInvalide); //orienter pensionsfund om udbetaling

        if (_invalid)
          wi = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * wi) + fdi;
        else
          wi = Convert.ToInt32((1 + PensionSystem.InterestRate(12)) * wi) + contributionInvalide[m] - installmentInvalide;

        double pidf, pidi;
        if ((contributionInvalide[m] <= 0 && _timeSinceLastInvalideContribution++ > 3) || _activeInvalideAlders) //hvis der ikke er indbetalt til invalidepension i 3 måneder, stoppes dækning og præmier
        {
          pidf = 0;
          pidi = 0; 
        }
        else
        {
          _timeSinceLastInvalideContribution = 0;
          pidf = PensionSystem.PensionfundInvalide.CalculatePidf(age, m, _invalideDepotPrimoBx, contributionInvalide[m]); //pidf skal dække pensionsudbetaling for invalide
          pidi = PensionSystem.PensionfundInvalide.CalculatePidi(age, m, contributionInvalide[m]); //pidi skal dække pensionsindbetaling for invalide
        }

        if (_invalid)
          _invalideDepotUltimoAx = Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * _invalideDepotPrimoBx + fdi);
        else
          _invalideDepotUltimoAx = Convert.ToInt32((1 + PensionSystem.InterestRateForecasted(12)) * _invalideDepotPrimoBx + contributionInvalide[m] - installmentInvalide - pidf - pidi);

        installment += installmentInvalide;
        #endregion alderspension og pensionsindbetaling

        if (udvalgt /*&& m == 0*/ && installmentInvalide > 0)
          Console.WriteLine("Udbetaling invalide, m=" + m + ", " + installmentInvalide + " Kr.");

        _expectedPension = expected; //anvendes ved invalidering

        if (dead == m) //personen dør denne måned
        {
          PensionSystem.PensionfundInvalide.PersonExit(age, m, wi); //pensionsbeholdning slettes fra pensionskasse.
          break;
        }

        _dxInvalide = PensionSystem.PensionfundInvalide.CalculateDx(age, m, _invalideDepotUltimoAx, _activeInvalideAlders);

        if (m == 11)
          PensionSystem.PensionfundInvalide.RegisterDx(_dxInvalide);
      }
      #endregion simpel livrente med invalidepension
      
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