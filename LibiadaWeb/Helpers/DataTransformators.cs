﻿using System.Text;
using System.Web.Mvc;

namespace LibiadaWeb.Helpers
{
    public static class DataTransformators
    {
        public static MvcHtmlString LineBreaker(this HtmlHelper htmlHelper, string line, int breakingLength)
        {
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < line.Length; i += breakingLength)
            {
                int blockLength = (i + breakingLength >= line.Length) ? (line.Length - i) : breakingLength;
                result.Append(line.Substring(i, blockLength) + " ");
            }

            return MvcHtmlString.Create(result.ToString());
        }

        public static string CleanFastaFile(string file)
        {
            string[] splittedFile = file.Split(new [] { '\0', '\t'});
            
            StringBuilder result = new StringBuilder();

            for (int k = 0; k < splittedFile.Length; k++)
            {
                result.Append(splittedFile[k]);
            }

            return result.ToString();
        }
    }
}