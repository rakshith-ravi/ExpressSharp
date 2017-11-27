using System;
using System.Threading;
using System.Threading.Tasks;

namespace ExpressSharp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Express express = new Express();
            express.Listen(8080);
            express.Use((req, res, next) =>
            {
                next();
            });
            express.Use(async (req, res, next) =>
            {
                await res.Send("test");
            });
            express.Wait();
        }
    }
}
