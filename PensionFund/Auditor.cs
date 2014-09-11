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
    /// Personens samlede pensionsopsparing/formue
    /// </summary>
    int _holdings;
    /// <summary>
    /// Reference til pensionskasse
    /// </summary>
    PensionFund _pensionfund;

    public Auditor(PensionFund pensionfund, int holdings = 0)
    {
      _holdings = holdings;
      _pensionfund = pensionfund;
    }

    public void Contribution(int contribution)
    {
      _holdings += contribution; //opdater personlig formue
      _pensionfund.Contribution(contribution); //orienter pensionsfund om indbetaling      
    }

    public int Installment(int age)
    {
      int installment = _pensionfund.Installment(age, _holdings); //beregn udbetaling v. givet alderstrin
      _holdings -= installment; //fratræk udbetaling fra beholdning
      return installment;
    }

    public void YearStart()
    {
      _holdings = Convert.ToInt32(_holdings * _pensionfund.AdjustmentFactor); //opdater personlig pensionsbeholdning med faktor givet af pensionskassen (tager højde for rente og uvikling i pensionskassens samlede beholdning)
    }


  }
}
