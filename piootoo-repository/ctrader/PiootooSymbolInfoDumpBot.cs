using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using cAlgo.API;
using cAlgo.API.Internals;
using File = System.IO.File;

namespace cAlgo.Robots
{
    // ------------------------------------------------------------------------------------------
    // PiootooSymbolInfoDumpBot
    //
    // cBot di utilita', non un bot di trading: all'avvio (OnStart) scorre tutti i symbol
    // disponibili sull'account cTrader collegato e scrive su file, in JSON, tutte le proprieta'
    // esposte da SymbolInfo per ciascuno (tick size, pip size, volumi min/max/step, commissioni,
    // swap, leva, ecc.). Le proprieta' vengono lette per reflection cosi' il dump resta completo
    // anche se l'API di cAlgo ne aggiunge di nuove, senza dover mantenere un elenco a mano.
    //
    // Non piazza ordini, non apre sessioni Piootoo: si ferma da solo (Stop()) appena finito di
    // scrivere il file. Serve per ispezionare rapidamente cosa il broker espone per uno strumento
    // (es. per popolare i metadata degli strumenti di un piano, vedi docs/domini/trading-plans.md),
    // non fa parte del flusso di esecuzione live.
    // ------------------------------------------------------------------------------------------

    [Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
    public class PiootooSymbolInfoDumpBot : Robot
    {
        [Parameter("Cartella di output", DefaultValue = "", Group = "Output")]
        public string OutputFolder { get; set; }

        [Parameter("Nome file", DefaultValue = "symbols-info.json", Group = "Output")]
        public string FileName { get; set; }

        protected override void OnStart()
        {
            try
            {
                var path = ResolveOutputPath();
                var symbolNames = Symbols
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                Print("PiootooSymbolInfoDumpBot: trovati {0} symbol, scrittura in corso su {1}", symbolNames.Count, path);

                var dump = new List<Dictionary<string, object>>(symbolNames.Count);
                foreach (var name in symbolNames)
                {
                    try
                    {
                        var info = Symbols.GetSymbolInfo(name);
                        dump.Add(DumpSymbolInfo(info));
                    }
                    catch (Exception ex)
                    {
                        Print("PiootooSymbolInfoDumpBot: impossibile leggere '{0}': {1}", name, ex.Message);
                    }
                }

                WriteFile(path, dump);

                Print("PiootooSymbolInfoDumpBot: completato, {0} symbol scritti su {1}", dump.Count, path);
            }
            catch (Exception ex)
            {
                Print("PiootooSymbolInfoDumpBot: errore fatale: {0}", ex);
            }
            finally
            {
                Stop();
            }
        }

        protected override void OnBar()
        {
            // Il bot lavora solo in OnStart: nessuna logica per barra.
        }

        private string ResolveOutputPath()
        {
            var folder = OutputFolder;
            if (string.IsNullOrWhiteSpace(folder))
                folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiootooSymbolInfoDumpBot");

            Directory.CreateDirectory(folder);

            var fileName = string.IsNullOrWhiteSpace(FileName) ? "symbols-info.json" : FileName;
            return Path.Combine(folder, fileName);
        }

        // Legge tutte le proprieta' pubbliche leggibili di SymbolInfo per reflection, cosi' il
        // dump resta "tutte le informazioni" anche se la superficie dell'API cambia tra versioni
        // di cAlgo, senza dover elencare a mano ogni campo.
        private static Dictionary<string, object> DumpSymbolInfo(SymbolInfo info)
        {
            var result = new Dictionary<string, object>();
            var properties = info.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name, StringComparer.Ordinal);

            foreach (var property in properties)
            {
                object value;
                try
                {
                    value = property.GetValue(info);
                }
                catch (Exception ex)
                {
                    value = $"<errore lettura: {ex.Message}>";
                }

                result[property.Name] = NormalizeForJson(value);
            }

            return result;
        }

        // Riduce i tipi non direttamente serializzabili (enum, struct sconosciute) a stringa,
        // lasciando numeri/bool/stringhe cosi' come sono.
        private static object NormalizeForJson(object value)
        {
            if (value == null)
                return null;

            var type = value.GetType();
            if (type.IsPrimitive || value is string || value is decimal || value is DateTime || value is DateTimeOffset || value is TimeSpan)
                return value;

            if (type.IsEnum)
                return value.ToString();

            return value.ToString();
        }

        private void WriteFile(string path, List<Dictionary<string, object>> dump)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
            };
            var json = JsonSerializer.Serialize(dump, options);

            var temporary = path + ".tmp";
            File.WriteAllText(temporary, json);
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }
    }
}
