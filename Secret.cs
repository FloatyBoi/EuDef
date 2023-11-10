using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EuDef
{
    public class Secret
    {
        static public string GetToken(bool debug)
        {
            string fileName = "Token.txt";
            if (debug)
                fileName = "DebugToken.txt";
            var path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            string token = File.ReadAllText(path);
            return token;
        }
    }
}
