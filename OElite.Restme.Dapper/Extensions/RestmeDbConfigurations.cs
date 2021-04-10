
using Microsoft.Extensions.Logging;

namespace OElite.Restme.Dapper
{
    public partial class RestmeDb
    {
        public string DefaultConnectionString { get; set; }

        public static ILogger Logger { get; set; }

    }
}