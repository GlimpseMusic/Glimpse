using System;
using Glimpse.Forms;

namespace Glimpse;

public static class Program
{
    public static void Main(string[] args)
    {
        //if (args.Length > 0)
            Glimpse.Run(new GlimpsePlayer(), "");
        //else
        //    throw new NotImplementedException();
    }
}