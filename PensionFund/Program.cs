﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PensionFund
{
  class Program
  {
    public static int year = 2012;
    /// <summary>
    /// Eksogent givet rente
    /// </summary>
    public static double r = 0.00; //hvordan skal denne sættes?

    static void Main(string[] args)
    {
      Stopwatch sw = new Stopwatch(); //measure performance
      sw.Start();
      
      PensionSystem pensionSystem = new PensionSystem(); //opret pensionssystem

      Person[] _persons = new Person[1000000];
      for (int n = 0; n < _persons.Length; n++)
        _persons[n] = new Person(50); //Opret 30-årig person

      int indby = 25; //år der simuleres
      Console.WriteLine("Indbetal til livrentepension i "+indby+" fulde år");
      
      for (int y = 0; y < indby; y++)
      {
        year++; //nyt år
        PensionSystem.PensionfundLivrente.YearStart();
        PensionSystem.PensionfundRate.YearStart();

        for (int n = 0; n < _persons.Length; n++)
          _persons[n].YearStart();

        for (int n = 0; n < _persons.Length; n++)
          _persons[n].Update(); //ind- og udbetal

        for (int n = 0; n < _persons.Length; n++)
          _persons[n].YearEnd(); //et år ældre

        PensionSystem.PensionfundLivrente.YearEnd(); //afslut år, opgør samlet pensionsformue
        PensionSystem.PensionfundRate.YearEnd(); //afslut år, opgør samlet pensionsformue
      }

      Console.WriteLine("\rElapsed= {0:hh\\:mm\\:ss}.", sw.Elapsed);
      sw.Restart();
      

      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }


    private static void YearEnd()
    {

    }

  }
}