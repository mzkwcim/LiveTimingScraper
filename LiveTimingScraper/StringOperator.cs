using System.Text;

namespace LiveTimingScraper
{
    internal class StringOperator
    {
        public static string IsPersonalBest(string text) => (text != "-") ? ((Convert.ToInt32(text.Contains("%") ? text.Replace("%", "") : text) > 100) ? "r.ż." : "") : "r.ż.";
        public static string ToTitleString(string fullname)
        {
            string[] words = fullname.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i][1..].ToLower();
            }
            return string.Join(" ", words).Replace(",", "");
        }
        public static string ArabicToRomanianNumbers(int arabicNumber)
        {
            StringBuilder romanianNumber = new StringBuilder();
            int[] arabicValues = [1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1];
            string[] romanianSymbols = ["M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I"];
            for (int i = 0; i < arabicValues.Length; i++)
            {
                while (arabicNumber >= arabicValues[i])
                {
                    romanianNumber.Append(romanianSymbols[i]);
                    arabicNumber -= arabicValues[i];
                }
            }
            return romanianNumber.ToString();
        }
    }
}