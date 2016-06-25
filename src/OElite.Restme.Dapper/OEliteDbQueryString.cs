using System.Runtime.InteropServices.ComTypes;

namespace OElite.Restme.Dapper
{
    public class OEliteDbQueryString
    {
        public OEliteDbQueryString(string query, object paramValues = null, RestmeDb dbCentre = null)
        {
            Query = query;
            ParamValues = paramValues;
            DbCentre = dbCentre ?? new RestmeDb();
        }

        public string Query { get; internal set; }
        public object ParamValues { get; internal set; }
        public RestmeDb DbCentre { get; internal set; }

        public OEliteDbQueryString Params(object paramValues)
        {
            this.ParamValues = paramValues;
            return this;
        }
    }
}