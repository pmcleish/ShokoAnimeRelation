using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using Shoko.Server.Tasks;
using static System.Console;

namespace ShokoAnimeRelation
{
   internal class Program
   {
      private const int MaxTitleLength = 80;
      private const int WordWrapLength = 35;

      public static void Main(string[] args)
      {
         using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["Shoko"].ConnectionString))
         {
            con.Open();

            var relations = GetRelations(con).ToList();
            var knownAnime = GetAnime(con).ToDictionary(a => a.AnimeId);
            var relationsPerAnime = relations.Select(r => new { From = r.AnimeId, To = r.RelatedAnimeId })
               .Union(relations.Select(r => new { From = r.RelatedAnimeId, To = r.AnimeId }))
               .ToLookup(r => r.From, r => r.To);
            var animeGroupCalculator = AutoAnimeGroupCalculator.CreateFromServerSettings(con);

            List<HashSet<int>> subGraphs = BuildSubGraphs(relationsPerAnime);

            WriteLine("digraph Shoko_Anime_Relations {");
            WriteLine("\tnode [fontsize=11, fillcolor=powderblue, style=filled];");
            WriteLine("\tgraph [nodesep=1.5, ranksep=6, overlap=false, splines=spline, style=dashed, smoothing=power_dist];");

            int sgCount = 0;

            // Render each subgraph
            foreach (var subgraph in subGraphs)
            {
               Write("\tsubgraph cluster_");
               Write(sgCount);
               WriteLine(" {");

               foreach (int animeId in subgraph)
               {
                  Write("\t\t\"");
                  Write(animeId);
                  Write("\" [fillcolor=");

                  if (knownAnime.TryGetValue(animeId, out var anime))
                  {
                     switch (anime.AnimeType)
                     {
                        case AnimeType.Movie:
                           Write("palegreen");
                           break;
                        case AnimeType.OVA:
                           Write("tan");
                           break;
                        case AnimeType.TVSeries:
                           Write("lightblue");
                           break;
                        case AnimeType.TVSpecial:
                           Write("cornsilk");
                           break;
                        case AnimeType.Other:
                           Write("lightpink");
                           break;
                        case AnimeType.Web:
                           Write("pink");
                           break;
                     }

                     if (animeGroupCalculator.GetGroupAnimeId(animeId) == animeId)
                     {
                        Write(", penwidth=2, shape=doubleoctagon");
                     }

                     string title =anime.MainTitle;

                     if (title.Length > MaxTitleLength)
                     {
                        title = title.Substring(0, MaxTitleLength - 3) + "...";
                     }

                     title = WordWrap(title, WordWrapLength);

                     Write(", label=<");
                     Write(anime.AnimeId);

                     if (anime.AirDate != null)
                     {
                        Write("  <i>(");
                        Write(anime.AirDate.Value.ToString("MMM yyyy"));
                        Write(")</i>");
                     }

                     Write("<br/><b>");
                     Write(HttpUtility.HtmlEncode(title).Replace("\n", "<br/>"));
                     Write("</b>>");
                  }
                  else // Write colour for missing series
                  {
                     Write("tomato, shape=box");
                  }

                  Write(", target=_blank, href=\"https://anidb.net/a");
                  Write(animeId);
                  WriteLine("\"];");
               }

               WriteLine("\t}");
               sgCount++;
            }

            // Render all edges
            WriteLine();
            WriteLine("\t#Edges");

            foreach (var edge in relations)
            {
               Write("\t\"");
               Write(edge.AnimeId);
               Write("\" -> \"");
               Write(edge.RelatedAnimeId);
               Write("\" [label=\"");
               Write(edge.RelationType);
               WriteLine("\"];");
            }

            WriteLine("}");
         }
      }

      private static List<HashSet<int>> BuildSubGraphs(ILookup<int, int> relationsPerAnime)
      {
         var subGraphs = new List<HashSet<int>>();
         var visitedAnimeIds = new HashSet<int>();

         foreach (var animeRelations in relationsPerAnime)
         {
            if (!visitedAnimeIds.Contains(animeRelations.Key))
            {
               HashSet<int> subGraphAnimeIds = BuildSubGraphSet(animeRelations.Key, relationsPerAnime);

               visitedAnimeIds.UnionWith(subGraphAnimeIds);
               subGraphs.Add(subGraphAnimeIds);
            }
         }

         return subGraphs;
      }

      private static HashSet<int> BuildSubGraphSet(int rootAnimeId, ILookup<int, int> relationsPerAnime)
      {
         var animeIdSet = new HashSet<int>();
         var visitQueue = new Queue<int>();

         visitQueue.Enqueue(rootAnimeId);

         while (visitQueue.Count > 0)
         {
            int animeId = visitQueue.Dequeue();

            foreach (int relatedAnimeId in relationsPerAnime[animeId])
            {
               // When the related anime ID hasn't been encountered before, queue it up to be visited
               if (animeIdSet.Add(relatedAnimeId))
               {
                  visitQueue.Enqueue(relatedAnimeId);
               }
            }
         }

         return animeIdSet;
      }

      private static IEnumerable<(int AnimeId, int RelatedAnimeId, string RelationType)> GetRelations(SqlConnection con)
      {
         using (SqlCommand cmd = con.CreateCommand())
         {
            cmd.CommandText = "SELECT AnimeID, RelatedAnimeID, RelationType FROM AniDB_Anime_Relation";

            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
            {
               while (reader.Read())
               {
                  yield return (reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2));
               }
            }
         }
      }

      private static IEnumerable<(int AnimeId, string MainTitle, AnimeType AnimeType, DateTime? AirDate)> GetAnime(SqlConnection con)
      {
         using (SqlCommand cmd = con.CreateCommand())
         {
            cmd.CommandText = "SELECT AnimeId, MainTitle, AnimeType, AirDate FROM AniDB_Anime";

            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
            {
               while (reader.Read())
               {
                  yield return (reader.GetInt32(0), reader.GetString(1), (AnimeType)reader.GetInt32(2), reader.IsDBNull(3) ? null : (DateTime?)reader.GetDateTime(3));
               }
            }
         }
      }

      public static string WordWrap(string text, int width)
      {
         int pos;
         int next;
         StringBuilder sb = new StringBuilder();
         const string newLine = "\n";

         // Lucidity check
         if (width < 1)
         {
            return text;
         }

         // Parse each line of text
         for (pos = 0; pos < text.Length; pos = next)
         {
            // Find end of line
            int eol = text.IndexOf(newLine, pos);

            if (eol == -1)
            {
               next = eol = text.Length;
            }
            else
            {
               next = eol + newLine.Length;
            }

            // Copy this line of text, breaking into smaller lines as needed
            if (eol > pos)
            {
               do
               {
                  int len = eol - pos;

                  if (len > width)
                  {
                     len = BreakLine(text, pos, width);
                  }

                  sb.Append(text, pos, len);
                  sb.Append(newLine);

                  // Trim whitespace following break
                  pos += len;

                  while (pos < eol && Char.IsWhiteSpace(text[pos]))
                  {
                     pos++;
                  }
               } while (eol > pos);
            }
            else
            {
               sb.Append(newLine); // Empty line
            }
         }

         return sb.ToString();
      }

      /// <summary>
      /// Locates position to break the given line so as to avoid
      /// breaking words.
      /// </summary>
      /// <param name="text">String that contains line of text</param>
      /// <param name="pos">Index where line of text starts</param>
      /// <param name="max">Maximum line length</param>
      /// <returns>The modified line length</returns>
      public static int BreakLine(string text, int pos, int max)
      {
         // find last whitespace in line
         int i = max - 1;

         while (i >= 0 && !Char.IsWhiteSpace(text[pos + i]))
         {
            i--;
         }

         if (i < 0)
         {
            return max; // no whitespace found; break at maximum length
         }

         // find start of whitespace
         while (i >= 0 && Char.IsWhiteSpace(text[pos + i]))
         {
            i--;
         }

         // return length of text before whitespace
         return i + 1;
      }
   }

   public enum AnimeType
   {
      Movie = 0,
      OVA = 1,
      TVSeries = 2,
      TVSpecial = 3,
      Web = 4,
      Other = 5
   }
}