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
    }
}
