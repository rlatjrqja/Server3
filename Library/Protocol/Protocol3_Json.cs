using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Protocols
{
    public class Protocol3_Json
    {
        public static JsonDocument StringToJson(byte[] data)
        {
            // byte[]를 문자열로 변환
            string jsonString = Encoding.UTF8.GetString(data);

            // JSON 문자열을 객체로 역직렬화
            var jsonObject = JsonSerializer.Deserialize<dynamic>(jsonString);


            return jsonObject;
        }

        public static byte[] JsonToByte(JsonDocument jsonObject)
        {
            // JSON 객체를 문자열로 변환
            string jsonString = JsonSerializer.Serialize(jsonObject);

            // 문자열을 byte[]로 변환
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonString);


            return byteArray;
        }

        public class DataDictionary<TKey, TValue>
        {
            public TKey Key;
            public TValue Value;
        }
        public class JsonDataArray<TKey, TValue>
        {
            public List<DataDictionary<TKey, TValue>> data;
        }
        public static string ToJson<TKey, TValue>(Dictionary<TKey, TValue> jsonDicData, bool pretty = false)
        {
            List<DataDictionary<TKey, TValue>> dataList = new List<DataDictionary<TKey, TValue>>();
            DataDictionary<TKey, TValue> dictionaryData;
            foreach (TKey key in jsonDicData.Keys)
            {
                dictionaryData = new DataDictionary<TKey, TValue>();
                dictionaryData.Key = key;
                dictionaryData.Value = jsonDicData[key];
                dataList.Add(dictionaryData);
            }
            JsonDataArray<TKey, TValue> arrayJson = new JsonDataArray<TKey, TValue>();
            arrayJson.data = dataList;

            return JsonUtility.ToJson(arrayJson, pretty);
        }
    }
}
