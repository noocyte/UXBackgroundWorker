using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UXBackgroundWorker;

namespace ExampleWorker
{
    public class SimpleWorker:BaseWorker
    {
        protected override void Process()
        {
            var client = new HttpClient();
            client.GetAsync("http://blog.noocyte.net");
        }
    }
}
