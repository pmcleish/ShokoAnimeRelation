using System;
using System.Configuration;

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

      public static bool AutoGroupSeriesUseScoreAlgorithm => Boolean.TryParse(ConfigurationManager.AppSettings["AutoGroupSeriesUseScoreAlgorithm"], out bool val) && val;
   }
}