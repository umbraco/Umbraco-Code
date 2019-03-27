using System.Collections.Generic;

namespace Umbraco.Code.MapAll
{
    public static class CommentLineParser
    {
        public static bool ParseCommentLine(string comment, ref List<string> excludes)
        {
            const string tag = "Umbraco.Code.MapAll";

            // single line comment is "//..."
            if (comment.Length < tag.Length + 3)
                return false;
            var i = 0;
            if (comment[i++] != '/')
                return false;
            if (comment[i++] != '/')
                return false;
            while (comment[i] == ' ')
            {
                i++;
                if (i == comment.Length)
                    return false;
            }
            var j = 0;
            while (j < tag.Length && comment[i] == tag[j])
            {
                i++;
                j++;
                if (i == comment.Length)
                    return j == tag.Length;
            }
            if (j != tag.Length)
                return false;

            while (true)
            {
                if (i == comment.Length)
                    return true;

                while (comment[i++] != '-')
                    if (i == comment.Length)
                        return true;

                var p = i;
                while (i < comment.Length && comment[i++] != ' ') { }

                if (excludes == null)
                    excludes = new List<string>();
                excludes.Add(comment.Substring(p, i == comment.Length ? i - p : i - p - 1));
            }
        }
    }
}