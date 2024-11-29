using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;

public class Protocol3_Json
{
    /// <summary>
    /// Dictionary<string, string> 데이터를 받아 Json으로 변환 후, byte[] 타입으로 반환
    /// </summary>
    public static byte[] DictionaryToJson(Dictionary<string, string> jsonDicData, bool pretty = false)
    {
        // Formatting.Indented 옵션으로 pretty-print 설정
        Formatting formatting = pretty ? Formatting.Indented : Formatting.None;

        // Dictionary를 JSON 문자열로 변환
        string jsonString = JsonConvert.SerializeObject(jsonDicData, formatting);

        // 문자열을 byte[]로 변환
        return Encoding.UTF8.GetBytes(jsonString);
    }

    /// <summary>
    /// byte[] 타입 데이터를 받아 Json으로 변환하고, 그 Json 객체를 반환
    /// </summary>
    public static JObject BytesToJson(byte[] jsonData)
    {
        // byte[]를 문자열로 변환
        string jsonString = Encoding.UTF8.GetString(jsonData);

        // JSON 문자열을 JObject로 변환
        return JObject.Parse(jsonString);
    }

    /// <summary>
    /// byte[] 타입 데이터를 받아 Json으로 변환하고, 그 Json 객체를 반환
    /// </summary>
    public static string BytesToJsonString(byte[] jsonData)
    {
        // byte[]를 문자열로 변환
        string jsonString = Encoding.UTF8.GetString(jsonData);

        // JSON 문자열을 JObject로 변환
        return jsonString;
    }

    /// <summary>
    /// JSON 파일을 읽어 String로 변환.
    /// 서버측 사용중
    /// </summary>
    public static string JsonFileToString(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "{}"); // 빈 JSON 객체 생성
        }

        // JSON 파일 생성 및 쓰기
        string json = File.ReadAllText(path);
        return json;
    }

    /// <summary>
    /// JSON 파일에 요소 추가.
    /// 서버측 사용중
    /// </summary>
    public static string AddDataIntoJson(string Json, string data)
    {
        // JSON 데이터 디시리얼라이즈 (Dictionary로 변환)
        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(Json) ?? new Dictionary<string, object>();
        var newData = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
        if (newData == null || newData.Count != 1)
        {
            Console.WriteLine("Invalid data format. Ensure data is a single key-value pair.");
            return null;
        }

        // 새 데이터의 키와 값 추출
        var newKey = newData.Keys.First();
        var newValue = newData[newKey];

        // 중복 검사 및 추가
        if (dictionary.ContainsKey(newKey))
        {
            Console.WriteLine($"Key '{newKey}' already exists in the JSON. No changes made.");
            return null;
        }
        else
        {
            dictionary[newKey] = newValue;
            Console.WriteLine($"New User '{newKey}' added.");
        }

        // 병합된 데이터를 JSON 파일에 저장
        return JsonConvert.SerializeObject(dictionary, Formatting.Indented);
    }

    /// <summary>
    /// Json 데이터를 받아 지정된 경로에 Json 파일로 저장
    /// </summary>
    public static bool StringToJsonFile(string filePath, string json)
    {
        // 파일에 저장
        File.WriteAllText(filePath, json, Encoding.UTF8);
        return true;
    }

    public static bool IsJsonIncludeData(string json, string data)
    {
        try
        {
            // JSON 문자열을 딕셔너리로 디시리얼라이즈
            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            
            // data 문자열을 딕셔너리로 디시리얼라이즈
            var dataDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);
            
            // data에서 첫 번째 키 추출
            var dataKey = dataDictionary.Keys.First();
            var dataValue = dataDictionary[dataKey];

            // json에서 키 검색
            if (jsonDictionary.TryGetValue(dataKey, out var value) && value.Equals(dataDictionary[dataKey]))
            {
                // 키와 값이 모두 일치
                Console.WriteLine($"User '{dataKey}' is login.");
                return true;
            }
            else
            {
                Console.WriteLine($"Can't find user '{dataKey}' in the JSON.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return false;
        }
    }
}
