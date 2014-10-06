using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PensionFund
{
  class Person
  {
    static Random _luck = new Random();
    Auditor _auditor = new Auditor();
    int _age = 0;
    Boolean _alive = true;

    public Person(int age) 
    {
      _age = age;
    }

    public void YearStart()
    {
      _auditor.YearStart();
    }

    public void YearEnd()
    {
      _age++;
    }

    public void Update()
    {
      if (!_alive)
        return;

      int dead = -1;
      for (int m = 0; m < 12; m++)
        if (NextEvent(1 - PensionSystem.PensionfundLivrente._p[_age * 12 + m], _luck))
        {
          dead = m; //dør denne måned
          _alive = false;

          break;
        }

      if (_age < 65)
      {
        int[] månedligIndbetalingR = new int[12];
        int[] månedligIndbetalingLr = new int[12];
        for (int m = 0; m < 12; m++)
          månedligIndbetalingLr[m] = 10000;

        _auditor.Update(_age, månedligIndbetalingR, månedligIndbetalingLr, -1, -1, dead); //kør pensions-år
      }
      else if (_age == 65)
      {
        //ingen indbetalinger
        int[] månedligIndbetalingR = new int[12];
        int[] månedligIndbetalingLr = new int[12];
        _auditor.Update(_age, månedligIndbetalingLr, månedligIndbetalingR, -1, 0, dead); //start udbetaling af livrente pension i første måned (=0)
      }
      else
      {
        //ingen indbetalinger
        int[] månedligIndbetalingR = new int[12];
        int[] månedligIndbetalingLr = new int[12];
        _auditor.Update(_age, månedligIndbetalingLr, månedligIndbetalingR, -1, -1, dead); //start udbetaling af livrente pension i første måned (=0)
      }

    }

    private static bool NextEvent(double p, Random luck)
    {
      return luck.NextDouble() < p;
    }


  }
}
