using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    /// JSON 파일을 byte[]로 변환
    /// </summary>
    public static byte[] JsonFileToBytes(string filePath)
    {
        // JSON 파일의 내용을 읽어서 문자열로 변환
        string jsonString = File.ReadAllText(filePath, Encoding.UTF8);

        // 문자열을 byte[]로 변환
        return Encoding.UTF8.GetBytes(jsonString);
    }

    /// <summary>
    /// 문자열을 byte[]로 변환
    /// </summary>
    public static byte[] JsonStringToBytes(string jsonString)
    {
        // 문자열을 byte[]로 변환
        return Encoding.UTF8.GetBytes(jsonString);
    }

    /// <summary>
    /// Json 데이터를 받아 지정된 경로에 Json 파일로 저장
    /// </summary>
    public static void SaveJsonToFile(JObject jsonObject, string filePath, bool pretty = true)
    {
        // Formatting.Indented 옵션으로 pretty-print 설정
        Formatting formatting = pretty ? Formatting.Indented : Formatting.None;

        // JSON 객체를 문자열로 변환
        string jsonString = jsonObject.ToString(formatting);

        // 파일에 저장
        File.WriteAllText(filePath, jsonString, Encoding.UTF8);
    }

    /// <summary>
    /// JSON 파일을 읽어 JObject로 변환
    /// </summary>
    public static JObject JsonFileToJson(string filePath)
    {
        // JSON 파일의 내용을 읽어서 문자열로 변환
        string jsonString = File.ReadAllText(filePath, Encoding.UTF8);

        // 문자열을 JObject로 변환
        return JObject.Parse(jsonString);
    }
}
