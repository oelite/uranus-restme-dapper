using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using System.Data;

namespace OElite.Restme.Dapper
{
    public class OEliteDbQueryString
    {
        public OEliteDbQueryString(string query, dynamic paramValues = null, RestmeDb dbCentre = null)
        {
            Query = query;
            ParamValues = paramValues is ExpandoObject ? paramValues : StringUtils.JsonDeserialize<ExpandoObject>(StringUtils.JsonSerialize(paramValues));
            DbCentre = dbCentre ?? new RestmeDb();
        }

        public string Query { get; internal set; }
        public ExpandoObject ParamValues { get; internal set; }
        public bool Paginated = false;


        public RestmeDb DbCentre { get; internal set; }

        public OEliteDbQueryString Params(dynamic paramValues)
        {
            ParamValues = StringUtils.JsonDeserialize<ExpandoObject>(StringUtils.JsonSerialize(paramValues));
            return this;
        }
        public OEliteDbQueryString AddParams(dynamic paramValues)
        {
            if (ParamValues != null)
            {
                var merger = (IDictionary<string, object>)ParamValues;

                if (paramValues != null)
                {
                    var obj = StringUtils.JsonDeserialize<ExpandoObject>(StringUtils.JsonSerialize(paramValues));
                    ((IDictionary<string, object>)obj).ToList().ForEach(item =>
                     {
                         merger[item.Key] = item.Value;
                     });
                }

                ParamValues = (ExpandoObject)merger;
            }
            else
                ParamValues = paramValues;
            return this;
        }
    }
}