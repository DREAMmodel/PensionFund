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
    Auditor _auditor = new Auditor(0, 0, 0, 999, false, false); //ingen aktive pensioner, ingen initialopsparing
    //Auditor _auditor = new Auditor(0, 0, 10000000, 999, true, false); //har aktiv invalidepension
    //Auditor _auditor = new Auditor(10000000, 0, 10000000, 999, true, true); //har aktiv invalide- og livrentepension
    //Auditor _auditor = new Auditor(10000000, 0, 0, 999, false, true); //har aktiv livrentepension
    //Auditor _auditor = new Auditor(0, 100000, 0, 999, false, false, 12*10); //har aktiv ratepension, med 10 år tilbage
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
        if (NextEvent(1 - PensionSystem.PensionfundLivrente._p[_age * 12 + m], _luck)) //i "produktion" skal dette gøres pga "rigtige" ssh
        {
          dead = m; //dør denne måned
          _alive = false;

          break;
        }

      int invalideres = -1;
      for (int m = 0; m < 12; m++)
        if (NextEvent(PensionSystem.PensionfundInvalide._qd[_age * 12 + m], _luck)) //i "produktion" skal dette gøres pga "rigtige" ssh
        {
          invalideres = m; //invalideres
          break;
        }

      if (_age < 65)
      {
        int[] månedligIndbetalingR = new int[12]; //ratepension
        int[] månedligIndbetalingLr = new int[12]; //livrentepension
        int[] månedligIndbetalingIp = new int[12]; //invalidepension
        for (int m = 0; m < 12; m++)
          månedligIndbetalingIp[m] = 10000;

        _auditor.Update(_age, månedligIndbetalingR, månedligIndbetalingLr, månedligIndbetalingIp, -1, -1, -1, invalideres, dead); //kør pensions-år
      }
      else if (_age == 65)
      {
        //ingen indbetalinger
        int[] månedligIndbetalingR = new int[12];
        int[] månedligIndbetalingLr = new int[12];
        int[] månedligIndbetalingIp = new int[12]; //invalidepension
        _auditor.Update(_age, månedligIndbetalingR, månedligIndbetalingLr, månedligIndbetalingIp, -1, -1, 0, invalideres, dead); //start udbetaling af livrente pension i første måned (=0)
      }
      else
      {
        //ingen indbetalinger
        int[] månedligIndbetalingR = new int[12];
        int[] månedligIndbetalingLr = new int[12];
        int[] månedligIndbetalingIp = new int[12]; //invalidepension
        _auditor.Update(_age, månedligIndbetalingR, månedligIndbetalingLr, månedligIndbetalingIp, -1, -1, -1, invalideres, dead); //start udbetaling af livrente pension i første måned (=0)
      }

    }

    private static bool NextEvent(double p, Random luck)
    {
      return luck.NextDouble() < p;
    }


  }
}
