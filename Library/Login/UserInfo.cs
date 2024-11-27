﻿using System.Buffers.Text;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Login
{
    public class UserInfo
    {
        /*public Dictionary<string, byte[]> CreateID()
        {
            string ID = null;
            string Password = null;

            do
            {
                Console.Write("아이디를 입력하세요: ");
                ID = Console.ReadLine();
            }
            while (ID == null || ID.Length < 3);

            do
            {
                Console.Write("비밀번호를 입력하세요: ");
                Password = Console.ReadLine();
            }
            while (Password == null || Password.Length < 3);

            Dictionary<string, byte[]> info = new();
            info.Add("ID", Encoding.UTF8.GetBytes(ID));
            info.Add("Password", Convert.FromBase64String(Password));

            return info;
        }*/

        public static string CreateID()
        {
            string ID = null;
            string Password = null;

            do
            {
                Console.Write("아이디를 입력하세요: ");
                ID = Console.ReadLine();
            }
            while (ID == null || ID.Length < 3);

            do
            {
                Console.Write("비밀번호를 입력하세요: ");
                Password = Console.ReadLine();
            }
            while (Password == null || Password.Length < 3);

            JObject info = new();
            info.Add(ID, Password);
            return info.ToString();
        }
    }
}
