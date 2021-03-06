﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagnetX.Searcher.WebSearcher
{
    class ZhongzisoWebSearcher : WebSearcher
    {
        public override string Name
        {
            get
            {
                return "zhongziso.com";
            }
        }

        protected override async Task<string> GetURL(string word, int page)
        {
            string name = Uri.EscapeUriString(word);

            return "http://m.zhongziso.com/list_ctime/" + name + "/" + page;
        }

        protected override IEnumerable<string> PrepareParts(string content)
        {
            string[] parts = content.Split(new string[] { "<li class=\"list-group-item title" }, StringSplitOptions.None);
            for (int i = 1; i < parts.Length; i++)
            {
                yield return parts[i];
            }
        }

        protected Regex regName = new Regex("class=\"text-success\">(.+?)<\\/a>", RegexOptions.Compiled);
        protected Regex regMagnet = new Regex("\"(magnet.+?)[\"&]", RegexOptions.Compiled);
        protected Regex regSize = new Regex("class=\"text-size\">(.+?)<\\/dd>", RegexOptions.Compiled);

        protected override async Task<Result> ReadPart(string part)
        {
            Result r = new Result() { From = this.Name };
            try
            {
                var matchName = regName.Match(part);
                var matchMagnet = regMagnet.Match(part);
                var matchSize = regSize.Match(part);
                if (!matchName.Success || !matchMagnet.Success || !matchSize.Success) return null;

                r.Name = regName.Match(part).Groups[1].Value.Replace("<span class=\"highlight\">", "").Replace("</span>", "");
                r.Magnet = regMagnet.Match(part).Groups[1].Value;
                r.Size = regSize.Match(part).Groups[1].Value;
                return r;
            }
            catch
            {
                return null;
            }
        }
    }
}
