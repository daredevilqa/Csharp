using System;
using System.Windows.Forms;

namespace Digger.Architecture
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Game.CreateMap();
            Application.Run(new DiggerWindow());
        }
    }
}