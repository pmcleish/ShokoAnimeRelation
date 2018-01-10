using System;
using System.Configuration;
using System.Linq;
using Shoko.Server.Tasks;

namespace ShokoAnimeRelation
{
   public class ServerSettings
   {
      public static string AutoGroupSeriesRelationExclusions
      {
         get
         {
            string val = null;
            try
            {
               val = ConfigurationManager.AppSettings["AutoGroupSeriesRelationExclusions"];
            }
            catch
            {
               // ignored
            }
            return val ?? "same setting|character";
         }
      }

      public static AutoGroupExclude GetAutoGroupSeriesRelationExclusions()
      {
         var exclusionTokens = AutoGroupSeriesRelationExclusions
            .Split(new[] {'|'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

         return exclusionTokens
            .Select(s =>
            {

               s = s.Replace(" ", string.Empty);
               Enum.TryParse(s, true, out AutoGroupExclude exclude);

               return exclude;
            })
            .Aggregate(AutoGroupExclude.None, (exclude, allExcludes) => allExcludes | exclude);
      }

      public static bool AutoGroupSeriesUseScoreAlgorithm => Boolean.TryParse(ConfigurationManager.AppSettings["AutoGroupSeriesUseScoreAlgorithm"], out bool val) && val;
   }
}