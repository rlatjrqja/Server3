namespace Login
{
    public class UserInfo
    {
        public static Dictionary<string, string> CreateID()
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

            Dictionary<string, string> info = new();
            info.Add("ID", ID);
            info.Add("Password", Password);

            return info;
        }
    }
}
