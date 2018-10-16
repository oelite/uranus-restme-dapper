using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OElite.Restme.Dapper
{
    public class OEliteDbQueryString
    {
        public OEliteDbQueryString(string query, dynamic paramValues = null, RestmeDb dbCentre = null,
            string[] selectColumnNames = null)
        {
            Query = query;
            ParamValues = (paramValues is ExpandoObject)
                ? paramValues
                : JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(paramValues),
                    new ExpandoObjectConverter());
            DbCentre = dbCentre ?? new RestmeDb();

            SelectColumnNames = selectColumnNames;
        }

        public string Query { get; internal set; }
        public string InitQuery { get; internal set; }
        public string[] SelectColumnNames { get; set; }
        public ExpandoObject ParamValues { get; internal set; }
        public bool IsPaginated = false;
        public bool IsExpectingIdentityScope = false;


        public RestmeDb DbCentre { get; internal set; }

        public OEliteDbQueryString Params(dynamic paramValues)
        {
            ParamValues = (paramValues is ExpandoObject)
                ? paramValues
                : JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(paramValues),
                    new ExpandoObjectConverter());
            return this;
        }

        public OEliteDbQueryString AddParams(dynamic paramValues)
        {
            if (ParamValues != null)
            {
                var merger = (IDictionary<string, object>) ParamValues;

                if (paramValues != null)
                {
                    var obj = (paramValues is ExpandoObject)
                        ? paramValues
                        : JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(paramValues),
                            new ExpandoObjectConverter());
                    ((IDictionary<string, object>) obj).ToList().ForEach(item => { merger[item.Key] = item.Value; });
                }

                ParamValues = (ExpandoObject) merger;
            }
            else
                ParamValues = paramValues;

            return this;
        }
    }
}