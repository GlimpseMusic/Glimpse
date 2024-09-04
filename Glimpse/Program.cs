using System;
using Glimpse.Forms;

namespace Glimpse;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args.Length > 0)
            Glimpse.Run(new MiniPlayer(), args[0]);
        else
            throw new NotImplementedException();
    }
}